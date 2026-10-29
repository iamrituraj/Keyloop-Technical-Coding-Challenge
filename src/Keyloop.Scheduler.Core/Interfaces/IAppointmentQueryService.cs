using Keyloop.Scheduler.Core.DTOs;

namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Service dedicated to read-only queries for appointments.
/// </summary>
public interface IAppointmentQueryService
{
    Task<AppointmentSummaryDto?> GetAppointmentDetailsAsync(string appointmentId, CancellationToken ct = default);
    Task<List<AppointmentSummaryDto>> ListAppointmentsAsync(string? dealershipId = null, CancellationToken ct = default);
}
