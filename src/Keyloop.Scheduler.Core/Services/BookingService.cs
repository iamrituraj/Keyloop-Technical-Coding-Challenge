using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;
using Keyloop.Scheduler.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Keyloop.Scheduler.Core.Services;

public class BookingService : IBookingService
{
    private readonly IWorkshopResourceRepository _resourceRepository;
    private readonly ICustomerVehicleRepository _customerVehicleRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ISlotRecommender _slotRecommender;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IWorkshopResourceRepository resourceRepository,
        ICustomerVehicleRepository customerVehicleRepository,
        IAppointmentRepository appointmentRepository,
        ISlotRecommender slotRecommender,
        ILogger<BookingService> logger)
    {
        _resourceRepository = resourceRepository;
        _customerVehicleRepository = customerVehicleRepository;
        _appointmentRepository = appointmentRepository;
        _slotRecommender = slotRecommender;
        _logger = logger;
    }

    public async Task<AppointmentSummaryDto> BookAppointmentAsync(BookAppointmentCommandDto command, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing appointment booking for VIN {VIN} at Dealership {DealershipId} for {Time:yyyy-MM-dd HH:mm}",
            command.VehicleVin, command.DealershipId, command.DesiredStartTime);

        var dealership = await _resourceRepository.GetDealershipAsync(command.DealershipId, ct)
            ?? throw new ResourceNotFoundException(nameof(Dealership), command.DealershipId);

        var serviceType = await _resourceRepository.GetServiceTypeAsync(command.ServiceTypeId, ct)
            ?? throw new ResourceNotFoundException(nameof(ServiceType), command.ServiceTypeId);

        var slotStart = command.DesiredStartTime;
        var slotEnd = slotStart.AddMinutes(serviceType.DurationMinutes);

        // Validate within operating hours
        if (!dealership.IsWithinOperatingHours(slotStart, slotEnd))
        {
            throw new BusinessRuleValidationException(
                $"Requested time [{slotStart:HH:mm} - {slotEnd:HH:mm}] falls outside dealership operating hours [{dealership.OpeningTime} - {dealership.ClosingTime}].");
        }

        var (dayOpen, dayClose) = dealership.GetOperatingWindow(slotStart);
        var activeAppointments = await _appointmentRepository.GetActiveAppointmentsInRangeAsync(dealership.Id, dayOpen, dayClose, ct) 
            ?? new List<Appointment>();

        // Determine & Validate Service Bay and Qualified Technician
        var selectedBay = await ResolveServiceBayAsync(dealership, serviceType, command.PreferredBayId, slotStart, slotEnd, activeAppointments, ct);
        var selectedTech = await ResolveTechnicianAsync(dealership, serviceType, command.PreferredTechnicianId, slotStart, slotEnd, activeAppointments, ct);

        // Check for double booking race condition
        var hasConflict = await _appointmentRepository.HasConflictAsync(selectedBay.Id, selectedTech.Id, slotStart, slotEnd, null, ct);
        if (hasConflict)
        {
            _logger.LogWarning("Race condition detected: Bay {BayId} or Tech {TechId} is already booked for [{Start} - {End}]",
                selectedBay.Id, selectedTech.Id, slotStart, slotEnd);
            var alternativeSlots = await _slotRecommender.CalculateAlternativeSlotsAsync(dealership, serviceType, slotStart, ct);
            throw new ResourceConflictException(
                "The selected bay or technician has just been booked by another user. Please choose another slot.",
                "ServiceBay/Technician",
                suggestedAlternatives: alternativeSlots);
        }

        // Ensure Customer & Vehicle are persisted
        var customer = await _customerVehicleRepository.GetOrCreateCustomerAsync(
            command.CustomerName, command.CustomerEmail, command.CustomerPhone, ct);

        var vehicle = await EnsureVehicleRegisteredAsync(command.VehicleVin, customer.Id, serviceType.RequiredBayType, ct);

        // Create atomic appointment
        var appointment = new Appointment
        {
            DealershipId = dealership.Id,
            ServiceTypeId = serviceType.Id,
            VehicleVin = vehicle.Vin,
            CustomerId = customer.Id,
            TechnicianId = selectedTech.Id,
            ServiceBayId = selectedBay.Id,
            StartTime = slotStart,
            EndTime = slotEnd,
            Status = AppointmentStatus.Confirmed,
            Notes = command.Notes ?? "Standard booking via Unified Service Scheduler",
            CreatedAt = DateTime.UtcNow
        };

        var savedAppointment = await _appointmentRepository.CreateAppointmentAsync(appointment, ct);
        _logger.LogInformation("Successfully created Appointment {AppointmentId} in Bay {Bay} with Tech {Tech}",
            savedAppointment.Id, selectedBay.Name, selectedTech.Name);

        return savedAppointment.ToSummaryDto(dealership, serviceType, vehicle, customer, selectedTech, selectedBay);
    }

    private async Task<ServiceBay> ResolveServiceBayAsync(
        Dealership dealership,
        ServiceType serviceType,
        string? preferredBayId,
        DateTime slotStart,
        DateTime slotEnd,
        List<Appointment> activeAppointments,
        CancellationToken ct)
    {
        var eligibleBays = await _resourceRepository.GetEligibleBaysAsync(dealership.Id, serviceType.RequiredBayType, ct);
        if (!eligibleBays.Any())
        {
            throw new BusinessRuleValidationException($"Dealership has no active bays matching required type: {serviceType.RequiredBayType}");
        }

        if (!string.IsNullOrWhiteSpace(preferredBayId))
        {
            var preferredBay = eligibleBays.FirstOrDefault(b => b.Id == preferredBayId)
                ?? throw new BusinessRuleValidationException($"Preferred bay '{preferredBayId}' is not eligible or active for this service.");

            if (activeAppointments.Any(a => a.ServiceBayId == preferredBay.Id && a.OverlapsWith(slotStart, slotEnd)))
            {
                var alternativeSlots = await _slotRecommender.CalculateAlternativeSlotsAsync(dealership, serviceType, slotStart, ct);
                throw new ResourceConflictException(
                    $"Preferred service bay '{preferredBay.Name}' is already occupied during the requested time window.",
                    "ServiceBay",
                    preferredBay.Id,
                    alternativeSlots);
            }

            return preferredBay;
        }

        var availableBay = eligibleBays.FirstOrDefault(bay =>
            !activeAppointments.Any(a => a.ServiceBayId == bay.Id && a.OverlapsWith(slotStart, slotEnd))
        );

        if (availableBay == null)
        {
            var alternativeSlots = await _slotRecommender.CalculateAlternativeSlotsAsync(dealership, serviceType, slotStart, ct);
            throw new ResourceConflictException(
                "No eligible service bays are available during the requested time window.",
                "ServiceBay",
                suggestedAlternatives: alternativeSlots);
        }

        return availableBay;
    }

    private async Task<Technician> ResolveTechnicianAsync(
        Dealership dealership,
        ServiceType serviceType,
        string? preferredTechId,
        DateTime slotStart,
        DateTime slotEnd,
        List<Appointment> activeAppointments,
        CancellationToken ct)
    {
        var qualifiedTechs = await _resourceRepository.GetQualifiedTechniciansAsync(dealership.Id, serviceType.RequiredSkills, ct);
        if (!qualifiedTechs.Any())
        {
            throw new BusinessRuleValidationException($"Dealership has no active technicians qualified with skills: {string.Join(", ", serviceType.RequiredSkills)}");
        }

        if (!string.IsNullOrWhiteSpace(preferredTechId))
        {
            var preferredTech = qualifiedTechs.FirstOrDefault(t => t.Id == preferredTechId)
                ?? throw new BusinessRuleValidationException($"Preferred technician '{preferredTechId}' does not possess all required skills for this service.");

            if (activeAppointments.Any(a => a.TechnicianId == preferredTech.Id && a.OverlapsWith(slotStart, slotEnd)))
            {
                var alternativeSlots = await _slotRecommender.CalculateAlternativeSlotsAsync(dealership, serviceType, slotStart, ct);
                throw new ResourceConflictException(
                    $"Preferred technician '{preferredTech.Name}' is already committed to another appointment during the requested time window.",
                    "Technician",
                    preferredTech.Id,
                    alternativeSlots);
            }

            return preferredTech;
        }

        var availableTech = qualifiedTechs.FirstOrDefault(tech =>
            !activeAppointments.Any(a => a.TechnicianId == tech.Id && a.OverlapsWith(slotStart, slotEnd))
        );

        if (availableTech == null)
        {
            var alternativeSlots = await _slotRecommender.CalculateAlternativeSlotsAsync(dealership, serviceType, slotStart, ct);
            throw new ResourceConflictException(
                "No qualified technicians are available during the requested time window.",
                "Technician",
                suggestedAlternatives: alternativeSlots);
        }

        return availableTech;
    }

    private async Task<Vehicle> EnsureVehicleRegisteredAsync(string vin, string customerId, BayType requiredBayType, CancellationToken ct)
    {
        var vehicle = await _customerVehicleRepository.GetVehicleByVinAsync(vin, ct);
        if (vehicle != null) return vehicle;

        return await _customerVehicleRepository.RegisterVehicleIfNotExistsAsync(new Vehicle
        {
            Vin = vin.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            Make = "Generic",
            Model = "Vehicle",
            Year = DateTime.UtcNow.Year,
            PowertrainType = requiredBayType == BayType.EvCertified ? PowertrainType.BatteryElectric : PowertrainType.InternalCombustion
        }, ct);
    }
}
