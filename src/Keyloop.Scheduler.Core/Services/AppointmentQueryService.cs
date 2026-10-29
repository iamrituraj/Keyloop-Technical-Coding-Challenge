using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Keyloop.Scheduler.Core.Services;

public class AppointmentQueryService : IAppointmentQueryService
{
    private readonly IAppointmentRepository _repository;
    private readonly ILogger<AppointmentQueryService> _logger;

    public AppointmentQueryService(IAppointmentRepository repository, ILogger<AppointmentQueryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AppointmentSummaryDto?> GetAppointmentDetailsAsync(string appointmentId, CancellationToken ct = default)
    {
        var apt = await _repository.GetAppointmentByIdAsync(appointmentId, ct);
        return apt?.ToSummaryDto();
    }

    public async Task<List<AppointmentSummaryDto>> ListAppointmentsAsync(string? dealershipId = null, CancellationToken ct = default)
    {
        var list = await _repository.GetAllAppointmentsAsync(dealershipId, ct);
        return list.Select(apt => apt.ToSummaryDto()).ToList();
    }
}
