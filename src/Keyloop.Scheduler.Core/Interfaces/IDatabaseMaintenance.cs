namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Interface dedicated to database administrative and maintenance operations.
/// </summary>
public interface IDatabaseMaintenance
{
    Task ResetDemoDatabaseAsync(CancellationToken ct = default);
}
