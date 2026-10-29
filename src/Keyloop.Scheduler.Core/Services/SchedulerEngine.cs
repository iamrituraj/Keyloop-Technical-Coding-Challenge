using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Keyloop.Scheduler.Core.Services;

/// <summary>
/// Unified Facade implementing ISchedulerEngine by delegating to single-responsibility services.
/// </summary>
public class SchedulerEngine : ISchedulerEngine
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IBookingService _bookingService;
    private readonly IAppointmentQueryService _queryService;

    // Primary DI Constructor
    [ActivatorUtilitiesConstructor]
    public SchedulerEngine(
        IAvailabilityService availabilityService,
        IBookingService bookingService,
        IAppointmentQueryService queryService)
    {
        _availabilityService = availabilityService;
        _bookingService = bookingService;
        _queryService = queryService;
    }

    // Direct Constructor for unit tests and composition
    public SchedulerEngine(
        IWorkshopResourceRepository resourceRepository,
        IAppointmentRepository appointmentRepository,
        ICustomerVehicleRepository customerVehicleRepository,
        ILogger<SchedulerEngine> logger)
    {
        _availabilityService = new AvailabilityService(resourceRepository, appointmentRepository, NullLogger<AvailabilityService>.Instance);
        var recommender = new SlotRecommender(_availabilityService, NullLogger<SlotRecommender>.Instance);
        _bookingService = new BookingService(resourceRepository, customerVehicleRepository, appointmentRepository, recommender, NullLogger<BookingService>.Instance);
        _queryService = new AppointmentQueryService(appointmentRepository, NullLogger<AppointmentQueryService>.Instance);
    }

    public Task<AvailabilityResultDto> GetAvailabilityAsync(AvailabilityQueryDto query, CancellationToken ct = default)
        => _availabilityService.GetAvailabilityAsync(query, ct);

    public Task<AppointmentSummaryDto> BookAppointmentAsync(BookAppointmentCommandDto command, CancellationToken ct = default)
        => _bookingService.BookAppointmentAsync(command, ct);

    public Task<AppointmentSummaryDto?> GetAppointmentDetailsAsync(string appointmentId, CancellationToken ct = default)
        => _queryService.GetAppointmentDetailsAsync(appointmentId, ct);

    public Task<List<AppointmentSummaryDto>> ListAppointmentsAsync(string? dealershipId = null, CancellationToken ct = default)
        => _queryService.ListAppointmentsAsync(dealershipId, ct);
}
