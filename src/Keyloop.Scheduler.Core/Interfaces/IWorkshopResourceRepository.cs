using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Repository dedicated to Dealership, Service Type, Bay, and Technician queries.
/// </summary>
public interface IWorkshopResourceRepository
{
    Task<Dealership?> GetDealershipAsync(string dealershipId, CancellationToken ct = default);
    Task<List<Dealership>> GetAllDealershipsAsync(CancellationToken ct = default);
    Task<(List<ServiceBay> Bays, List<Technician> Technicians)> GetDealershipResourcesAsync(string dealershipId, CancellationToken ct = default);
    Task<ServiceType?> GetServiceTypeAsync(string serviceTypeId, CancellationToken ct = default);
    Task<List<ServiceType>> GetAllServiceTypesAsync(CancellationToken ct = default);
    Task<List<ServiceBay>> GetEligibleBaysAsync(string dealershipId, BayType requiredBayType, CancellationToken ct = default);
    Task<List<Technician>> GetQualifiedTechniciansAsync(string dealershipId, IEnumerable<SkillCode> requiredSkills, CancellationToken ct = default);
}
