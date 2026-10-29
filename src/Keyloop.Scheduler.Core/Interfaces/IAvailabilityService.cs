using Keyloop.Scheduler.Core.DTOs;

namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Service dedicated to evaluating workshop availability and generating valid time slots.
/// </summary>
public interface IAvailabilityService
{
    Task<AvailabilityResultDto> GetAvailabilityAsync(AvailabilityQueryDto query, CancellationToken ct = default);
}
