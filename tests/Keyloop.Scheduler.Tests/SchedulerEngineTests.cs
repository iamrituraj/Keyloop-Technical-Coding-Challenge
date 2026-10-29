using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Core.Services;
using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;
using Keyloop.Scheduler.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Keyloop.Scheduler.Tests;

public class SchedulerEngineTests
{
    private readonly Mock<IWorkshopResourceRepository> _mockResourceRepo;
    private readonly Mock<IAppointmentRepository> _mockApptRepo;
    private readonly Mock<ICustomerVehicleRepository> _mockCustRepo;
    private readonly SchedulerEngine _sut; // System Under Test

    private readonly Dealership _sampleDealership;
    private readonly ServiceType _evService;
    private readonly ServiceType _brakeService;
    private readonly ServiceBay _standardBay;
    private readonly ServiceBay _evBay;
    private readonly Technician _evTech;
    private readonly Technician _juniorTech;

    public SchedulerEngineTests()
    {
        _mockResourceRepo = new Mock<IWorkshopResourceRepository>();
        _mockApptRepo = new Mock<IAppointmentRepository>();
        _mockCustRepo = new Mock<ICustomerVehicleRepository>();
        _sut = new SchedulerEngine(
            _mockResourceRepo.Object,
            _mockApptRepo.Object,
            _mockCustRepo.Object,
            NullLogger<SchedulerEngine>.Instance);

        _sampleDealership = new Dealership
        {
            Id = "DLR-001",
            Name = "Test Dealership",
            OpeningTime = "09:00",
            ClosingTime = "12:00",
            SlotIntervalMinutes = 60
        };

        _standardBay = new ServiceBay { Id = "BAY-STD", DealershipId = "DLR-001", Name = "Standard Bay", BayType = BayType.StandardLift };
        _evBay = new ServiceBay { Id = "BAY-EV", DealershipId = "DLR-001", Name = "EV Bay", BayType = BayType.EvCertified };

        _evTech = new Technician
        {
            Id = "TECH-EV",
            DealershipId = "DLR-001",
            Name = "EV Master",
            Skills = new List<TechnicianSkill>
            {
                new() { SkillCode = SkillCode.EvPowertrain }
            }
        };

        _juniorTech = new Technician
        {
            Id = "TECH-JR",
            DealershipId = "DLR-001",
            Name = "Junior Tech",
            Skills = new List<TechnicianSkill>
            {
                new() { SkillCode = SkillCode.GeneralMaintenance }
            }
        };

        _evService = new ServiceType
        {
            Id = "SRV-EV",
            Name = "EV Battery Diagnostic",
            DurationMinutes = 60,
            RequiredBayType = BayType.EvCertified,
            RequiredSkills = new List<SkillCode> { SkillCode.EvPowertrain }
        };

        _brakeService = new ServiceType
        {
            Id = "SRV-BRAKE",
            Name = "Brake Service",
            DurationMinutes = 60,
            RequiredBayType = BayType.StandardLift,
            RequiredSkills = new List<SkillCode> { SkillCode.BrakeAndChassis }
        };
    }

    [Fact]
    public async Task GetAvailability_ShouldReturnAvailableSlots_WhenBayAndQualifiedTechAreFree()
    {
        // Arrange
        var targetDate = new DateTime(2026, 11, 1);
        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-EV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evService);
        _mockResourceRepo.Setup(r => r.GetEligibleBaysAsync("DLR-001", BayType.EvCertified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBay> { _evBay });
        _mockResourceRepo.Setup(r => r.GetQualifiedTechniciansAsync("DLR-001", It.IsAny<IEnumerable<SkillCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Technician> { _evTech });
        _mockApptRepo.Setup(r => r.GetActiveAppointmentsInRangeAsync("DLR-001", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>()); // No existing appointments

        var query = new AvailabilityQueryDto("DLR-001", "SRV-EV", targetDate);

        // Act
        var result = await _sut.GetAvailabilityAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.AvailableSlots.Count); // 09:00, 10:00, 11:00
        Assert.All(result.AvailableSlots, slot =>
        {
            Assert.Equal("BAY-EV", slot.SuggestedBayId);
            Assert.Equal("TECH-EV", slot.SuggestedTechnicianId);
        });
    }

    [Fact]
    public async Task GetAvailability_ShouldReturnEmpty_WhenNoTechnicianHoldsRequiredCertification()
    {
        // Arrange
        var targetDate = new DateTime(2026, 11, 1);
        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-BRAKE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_brakeService);
        _mockResourceRepo.Setup(r => r.GetEligibleBaysAsync("DLR-001", BayType.StandardLift, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBay> { _standardBay });
        // No technicians hold BrakeAndChassis skill
        _mockResourceRepo.Setup(r => r.GetQualifiedTechniciansAsync("DLR-001", It.IsAny<IEnumerable<SkillCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Technician>());

        var query = new AvailabilityQueryDto("DLR-001", "SRV-BRAKE", targetDate);

        // Act
        var result = await _sut.GetAvailabilityAsync(query);

        // Assert
        Assert.Empty(result.AvailableSlots);
    }

    [Fact]
    public async Task GetAvailability_BayEquipmentMatching_ShouldOnlyMatchRequiredBayType()
    {
        // Arrange
        var targetDate = new DateTime(2026, 11, 1);
        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-EV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evService);
        // Only return EV bays for EV service (repository filters by required bay type)
        _mockResourceRepo.Setup(r => r.GetEligibleBaysAsync("DLR-001", BayType.EvCertified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBay> { _evBay });
        _mockResourceRepo.Setup(r => r.GetQualifiedTechniciansAsync("DLR-001", It.IsAny<IEnumerable<SkillCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Technician> { _evTech });
        _mockApptRepo.Setup(r => r.GetActiveAppointmentsInRangeAsync("DLR-001", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());

        var query = new AvailabilityQueryDto("DLR-001", "SRV-EV", targetDate);

        // Act
        var result = await _sut.GetAvailabilityAsync(query);

        // Assert
        Assert.NotEmpty(result.AvailableSlots);
        Assert.All(result.AvailableSlots, slot => Assert.Equal(BayType.EvCertified, slot.BayType));
        _mockResourceRepo.Verify(r => r.GetEligibleBaysAsync("DLR-001", BayType.EvCertified, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailability_TemporalNonOverlap_ShouldExcludePartiallyOverlappingAppointments()
    {
        // Arrange: Operating window 09:00 - 12:00, slot duration 60 mins
        // Existing appointment on BAY-EV from 09:30 to 10:30
        var targetDate = new DateTime(2026, 11, 1);
        var overlappingAppointment = new Appointment
        {
            ServiceBayId = "BAY-EV",
            TechnicianId = "TECH-EV",
            StartTime = new DateTime(2026, 11, 1, 9, 30, 0),
            EndTime = new DateTime(2026, 11, 1, 10, 30, 0),
            Status = AppointmentStatus.Confirmed
        };

        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-EV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evService);
        _mockResourceRepo.Setup(r => r.GetEligibleBaysAsync("DLR-001", BayType.EvCertified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBay> { _evBay });
        _mockResourceRepo.Setup(r => r.GetQualifiedTechniciansAsync("DLR-001", It.IsAny<IEnumerable<SkillCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Technician> { _evTech });
        _mockApptRepo.Setup(r => r.GetActiveAppointmentsInRangeAsync("DLR-001", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { overlappingAppointment });

        var query = new AvailabilityQueryDto("DLR-001", "SRV-EV", targetDate);

        // Act
        var result = await _sut.GetAvailabilityAsync(query);

        // Assert: 09:00 - 10:00 overlaps with [09:30 - 10:30] -> Excluded!
        // 10:00 - 11:00 overlaps with [09:30 - 10:30] -> Excluded!
        // 11:00 - 12:00 does NOT overlap -> Available!
        Assert.Single(result.AvailableSlots);
        Assert.Equal(new DateTime(2026, 11, 1, 11, 0, 0), result.AvailableSlots[0].StartTime);
    }

    [Fact]
    public async Task BookAppointment_ShouldThrowConflictException_WhenBayOrTechIsDoubleBooked()
    {
        // Arrange
        var slotTime = new DateTime(2026, 11, 1, 10, 0, 0);
        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-EV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evService);
        _mockResourceRepo.Setup(r => r.GetEligibleBaysAsync("DLR-001", BayType.EvCertified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBay> { _evBay });
        _mockResourceRepo.Setup(r => r.GetQualifiedTechniciansAsync("DLR-001", It.IsAny<IEnumerable<SkillCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Technician> { _evTech });
        _mockApptRepo.Setup(r => r.GetActiveAppointmentsInRangeAsync("DLR-001", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());
        
        // Simulate race condition conflict detected by repository
        _mockApptRepo.Setup(r => r.HasConflictAsync("BAY-EV", "TECH-EV", slotTime, slotTime.AddMinutes(60), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cmd = new BookAppointmentCommandDto(
            "DLR-001", "SRV-EV", "WAUZZZF28MA019842",
            "Rajesh Malhotra", "rajesh.malhotra@example.com", "+919876543210",
            slotTime, "BAY-EV", "TECH-EV", "Test note"
        );

        // Act & Assert
        await Assert.ThrowsAsync<ResourceConflictException>(() => _sut.BookAppointmentAsync(cmd));
    }

    [Fact]
    public async Task BookAppointment_ShouldThrowConflictException_WhenPreferredBayIsAlreadyOccupied()
    {
        // Arrange
        var slotTime = new DateTime(2026, 11, 1, 10, 0, 0);
        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-EV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evService);
        _mockResourceRepo.Setup(r => r.GetEligibleBaysAsync("DLR-001", BayType.EvCertified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBay> { _evBay });
        _mockResourceRepo.Setup(r => r.GetQualifiedTechniciansAsync("DLR-001", It.IsAny<IEnumerable<SkillCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Technician> { _evTech });

        // Simulate that BAY-EV is occupied by an active appointment
        _mockApptRepo.Setup(r => r.GetActiveAppointmentsInRangeAsync("DLR-001", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>
            {
                new()
                {
                    Id = "APT-EXISTING",
                    DealershipId = "DLR-001",
                    ServiceBayId = "BAY-EV",
                    TechnicianId = "TECH-EV",
                    StartTime = slotTime,
                    EndTime = slotTime.AddMinutes(60)
                }
            });

        var cmd = new BookAppointmentCommandDto(
            "DLR-001", "SRV-EV", "WAUZZZF28MA019842",
            "Ananya Sen", "ananya.sen@example.in", "+919876543210",
            slotTime, "BAY-EV", null, "Preferred bay request"
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ResourceConflictException>(() => _sut.BookAppointmentAsync(cmd));
        Assert.Contains("already occupied", ex.Message);
        Assert.Equal("ServiceBay", ex.ResourceType);
    }

    [Fact]
    public async Task BookAppointment_ShouldThrowValidationException_WhenTimeOutsideOperatingHours()
    {
        // Arrange
        var slotTime = new DateTime(2026, 11, 1, 19, 0, 0); // Dealership closes at 12:00
        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-EV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evService);

        var cmd = new BookAppointmentCommandDto(
            "DLR-001", "SRV-EV", "WAUZZZF28MA019842",
            "Rajesh Malhotra", "rajesh.malhotra@example.com", "+919876543210",
            slotTime
        );

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _sut.BookAppointmentAsync(cmd));
    }

    [Fact]
    public async Task BookAppointment_SuccessfulAllocation_CreatesPersistentRecord()
    {
        // Arrange
        var slotTime = new DateTime(2026, 11, 1, 9, 0, 0);
        _mockResourceRepo.Setup(r => r.GetDealershipAsync("DLR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleDealership);
        _mockResourceRepo.Setup(r => r.GetServiceTypeAsync("SRV-EV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evService);
        _mockResourceRepo.Setup(r => r.GetEligibleBaysAsync("DLR-001", BayType.EvCertified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBay> { _evBay });
        _mockResourceRepo.Setup(r => r.GetQualifiedTechniciansAsync("DLR-001", It.IsAny<IEnumerable<SkillCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Technician> { _evTech });
        _mockApptRepo.Setup(r => r.GetActiveAppointmentsInRangeAsync("DLR-001", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());
        _mockApptRepo.Setup(r => r.HasConflictAsync("BAY-EV", "TECH-EV", slotTime, slotTime.AddMinutes(60), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockCustRepo.Setup(r => r.GetOrCreateCustomerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = "CUST-1", Name = "John Doe" });
        _mockCustRepo.Setup(r => r.GetVehicleByVinAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vehicle { Vin = "VIN-123", Make = "Tesla", Model = "Model 3", Year = 2023 });

        _mockApptRepo.Setup(r => r.CreateAppointmentAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment apt, CancellationToken _) => apt);

        var cmd = new BookAppointmentCommandDto(
            "DLR-001", "SRV-EV", "VIN-123",
            "Rajesh Malhotra", "rajesh.malhotra@example.com", "+919876543210",
            slotTime
        );

        // Act
        var result = await _sut.BookAppointmentAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DLR-001", result.DealershipId);
        Assert.Equal("BAY-EV", result.ServiceBayId);
        Assert.Equal("TECH-EV", result.TechnicianId);
        Assert.Equal(AppointmentStatus.Confirmed, result.Status);
        _mockApptRepo.Verify(r => r.CreateAppointmentAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
