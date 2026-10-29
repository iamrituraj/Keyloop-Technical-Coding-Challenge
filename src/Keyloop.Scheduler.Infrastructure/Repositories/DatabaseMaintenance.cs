using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Scheduler.Infrastructure.Repositories;

public class DatabaseMaintenance : IDatabaseMaintenance
{
    private readonly SchedulerDbContext _context;

    public DatabaseMaintenance(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task ResetDemoDatabaseAsync(CancellationToken ct = default)
    {
        await _context.Database.EnsureDeletedAsync(ct);
        await DatabaseSeeder.InitializeAndSeedAsync(_context, ct);
    }
}
