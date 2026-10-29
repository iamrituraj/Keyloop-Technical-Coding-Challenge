using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Domain.Entities;

namespace Keyloop.Scheduler.Core.Interfaces;

public interface ISchedulerEngine
{
    Task<AvailabilityResultDto> GetAvailabilityAsync(AvailabilityQueryDto query, CancellationToken ct = default);
    Task<AppointmentSummaryDto> BookAppointmentAsync(BookAppointmentCommandDto command, CancellationToken ct = default);
    Task<AppointmentSummaryDto?> GetAppointmentDetailsAsync(string appointmentId, CancellationToken ct = default);
    Task<List<AppointmentSummaryDto>> ListAppointmentsAsync(string? dealershipId = null, CancellationToken ct = default);
}
