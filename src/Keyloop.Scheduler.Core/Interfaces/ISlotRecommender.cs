using Keyloop.Scheduler.Domain.Entities;

namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Service dedicated to calculating fallback recommendations when a scheduling conflict occurs.
/// </summary>
public interface ISlotRecommender
{
    Task<List<string>> CalculateAlternativeSlotsAsync(
        Dealership dealership,
        ServiceType serviceType,
        DateTime desiredTime,
        CancellationToken ct = default);
}
