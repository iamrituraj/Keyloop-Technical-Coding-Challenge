using Keyloop.Scheduler.Domain.Entities;

namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Repository dedicated purely to Appointment persistence and temporal conflict detection.
/// </summary>
public interface IAppointmentRepository
{
    Task<List<Appointment>> GetActiveAppointmentsInRangeAsync(string dealershipId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<bool> HasConflictAsync(string? bayId, string? technicianId, DateTime start, DateTime end, string? excludeAppointmentId = null, CancellationToken ct = default);
    Task<Appointment> CreateAppointmentAsync(Appointment appointment, CancellationToken ct = default);
    Task<Appointment?> GetAppointmentByIdAsync(string appointmentId, CancellationToken ct = default);
    Task<List<Appointment>> GetAllAppointmentsAsync(string? dealershipId = null, CancellationToken ct = default);
}
