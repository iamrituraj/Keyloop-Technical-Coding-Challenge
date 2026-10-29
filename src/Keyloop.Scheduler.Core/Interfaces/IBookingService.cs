using Keyloop.Scheduler.Core.DTOs;

namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Service dedicated to booking commands, resource validation, and appointment creation.
/// </summary>
public interface IBookingService
{
    Task<AppointmentSummaryDto> BookAppointmentAsync(BookAppointmentCommandDto command, CancellationToken ct = default);
}
