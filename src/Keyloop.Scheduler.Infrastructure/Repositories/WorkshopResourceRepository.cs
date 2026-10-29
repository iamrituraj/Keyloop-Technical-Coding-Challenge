using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;
using Keyloop.Scheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Scheduler.Infrastructure.Repositories;

public class WorkshopResourceRepository : IWorkshopResourceRepository
{
    private readonly SchedulerDbContext _context;

    public WorkshopResourceRepository(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<Dealership?> GetDealershipAsync(string dealershipId, CancellationToken ct = default)
    {
        return await _context.Dealerships
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealershipId, ct);
    }

    public async Task<List<Dealership>> GetAllDealershipsAsync(CancellationToken ct = default)
    {
        return await _context.Dealerships
            .AsNoTracking()
            .Include(d => d.ServiceBays)
            .Include(d => d.Technicians)
            .ToListAsync(ct);
    }

    public async Task<(List<ServiceBay> Bays, List<Technician> Technicians)> GetDealershipResourcesAsync(string dealershipId, CancellationToken ct = default)
    {
        var bays = await _context.ServiceBays
            .AsNoTracking()
            .Where(b => b.DealershipId == dealershipId)
            .ToListAsync(ct);

        var techs = await _context.Technicians
            .AsNoTracking()
            .Include(t => t.Skills)
            .Where(t => t.DealershipId == dealershipId)
            .ToListAsync(ct);

        return (bays, techs);
    }

    public async Task<ServiceType?> GetServiceTypeAsync(string serviceTypeId, CancellationToken ct = default)
    {
        return await _context.ServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceTypeId, ct);
    }

    public async Task<List<ServiceType>> GetAllServiceTypesAsync(CancellationToken ct = default)
    {
        return await _context.ServiceTypes
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<ServiceBay>> GetEligibleBaysAsync(string dealershipId, BayType requiredBayType, CancellationToken ct = default)
    {
        return await _context.ServiceBays
            .AsNoTracking()
            .Where(b => b.DealershipId == dealershipId && b.IsActive && b.BayType == requiredBayType)
            .ToListAsync(ct);
    }

    public async Task<List<Technician>> GetQualifiedTechniciansAsync(string dealershipId, IEnumerable<SkillCode> requiredSkills, CancellationToken ct = default)
    {
        var query = _context.Technicians
            .AsNoTracking()
            .Include(t => t.Skills)
            .Where(t => t.DealershipId == dealershipId && t.IsActive);

        foreach (var skill in requiredSkills)
        {
            query = query.Where(t => t.Skills.Any(s => s.SkillCode == skill));
        }

        return await query.ToListAsync(ct);
    }
}
