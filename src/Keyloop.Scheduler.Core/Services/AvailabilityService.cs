using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Keyloop.Scheduler.Core.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IWorkshopResourceRepository _resourceRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        IWorkshopResourceRepository resourceRepository,
        IAppointmentRepository appointmentRepository,
        ILogger<AvailabilityService> logger)
    {
        _resourceRepository = resourceRepository;
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    public async Task<AvailabilityResultDto> GetAvailabilityAsync(AvailabilityQueryDto query, CancellationToken ct = default)
    {
        _logger.LogInformation("Evaluating availability for Dealership {DealershipId}, ServiceType {ServiceTypeId} on {Date:yyyy-MM-dd}",
            query.DealershipId, query.ServiceTypeId, query.Date);

        var dealership = await _resourceRepository.GetDealershipAsync(query.DealershipId, ct)
            ?? throw new ResourceNotFoundException(nameof(Dealership), query.DealershipId);

        var serviceType = await _resourceRepository.GetServiceTypeAsync(query.ServiceTypeId, ct)
            ?? throw new ResourceNotFoundException(nameof(ServiceType), query.ServiceTypeId);

        var (dayOpen, dayClose) = dealership.GetOperatingWindow(query.Date);

        // Fetch eligible resources
        var eligibleBays = await _resourceRepository.GetEligibleBaysAsync(dealership.Id, serviceType.RequiredBayType, ct);
        var qualifiedTechs = await _resourceRepository.GetQualifiedTechniciansAsync(dealership.Id, serviceType.RequiredSkills, ct);

        if (!eligibleBays.Any() || !qualifiedTechs.Any())
        {
            _logger.LogWarning("No eligible bays ({BayCount}) or qualified technicians ({TechCount}) for ServiceType {ServiceType}",
                eligibleBays.Count, qualifiedTechs.Count, serviceType.Name);

            return new AvailabilityResultDto(
                dealership.Id,
                serviceType.Id,
                serviceType.Name,
                serviceType.DurationMinutes,
                query.Date.Date,
                new List<AvailableSlotDto>()
            );
        }

        // Fetch existing bookings for the day
        var existingAppointments = await _appointmentRepository.GetActiveAppointmentsInRangeAsync(dealership.Id, dayOpen, dayClose, ct)
            ?? new List<Appointment>();

        var slotDuration = TimeSpan.FromMinutes(serviceType.DurationMinutes);
        var stepInterval = TimeSpan.FromMinutes(dealership.SlotIntervalMinutes > 0 ? dealership.SlotIntervalMinutes : 30);

        var availableSlots = ComputeAvailableSlots(dayOpen, dayClose, slotDuration, stepInterval, eligibleBays, qualifiedTechs, existingAppointments);

        _logger.LogInformation("Found {Count} available slots for Service {ServiceName}", availableSlots.Count, serviceType.Name);

        return new AvailabilityResultDto(
            dealership.Id,
            serviceType.Id,
            serviceType.Name,
            serviceType.DurationMinutes,
            query.Date.Date,
            availableSlots
        );
    }

    private static List<AvailableSlotDto> ComputeAvailableSlots(
        DateTime dayOpen,
        DateTime dayClose,
        TimeSpan slotDuration,
        TimeSpan stepInterval,
        IReadOnlyList<ServiceBay> eligibleBays,
        IReadOnlyList<Technician> qualifiedTechs,
        IReadOnlyList<Appointment> existingAppointments)
    {
        var availableSlots = new List<AvailableSlotDto>();

        for (var slotStart = dayOpen; slotStart + slotDuration <= dayClose; slotStart += stepInterval)
        {
            var slotEnd = slotStart + slotDuration;

            // Find free bays during [slotStart, slotEnd]
            var freeBays = eligibleBays.Where(bay =>
                !existingAppointments.Any(a => a.ServiceBayId == bay.Id && a.OverlapsWith(slotStart, slotEnd))
            ).ToList();

            // Find free qualified technicians during [slotStart, slotEnd]
            var freeTechs = qualifiedTechs.Where(tech =>
                !existingAppointments.Any(a => a.TechnicianId == tech.Id && a.OverlapsWith(slotStart, slotEnd))
            ).ToList();

            if (freeBays.Any() && freeTechs.Any())
            {
                var chosenBay = freeBays.First();
                // Load balancing: pick technician with fewest appointments today
                var chosenTech = freeTechs.OrderBy(t => existingAppointments.Count(a => a.TechnicianId == t.Id)).First();

                availableSlots.Add(new AvailableSlotDto(
                    slotStart,
                    slotEnd,
                    chosenBay.Id,
                    chosenBay.Name,
                    chosenBay.BayType,
                    chosenTech.Id,
                    chosenTech.Name,
                    chosenTech.Skills.Select(s => s.SkillCode).ToList()
                ));
            }
        }

        return availableSlots;
    }
}
