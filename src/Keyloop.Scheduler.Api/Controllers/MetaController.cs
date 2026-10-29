using Keyloop.Scheduler.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.Scheduler.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class MetaController : ControllerBase
{
    private readonly IWorkshopResourceRepository _workshopResourceRepository;

    public MetaController(IWorkshopResourceRepository workshopResourceRepository)
    {
        _workshopResourceRepository = workshopResourceRepository;
    }

    [HttpGet("dealerships")]
    public async Task<IActionResult> GetDealerships(CancellationToken ct)
    {
        var dealerships = await _workshopResourceRepository.GetAllDealershipsAsync(ct);
        return Ok(dealerships);
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServices(CancellationToken ct)
    {
        var services = await _workshopResourceRepository.GetAllServiceTypesAsync(ct);
        return Ok(services);
    }

    [HttpGet("resources/{dealershipId}")]
    public async Task<IActionResult> GetDealershipResources(string dealershipId, CancellationToken ct)
    {
        var (bays, technicians) = await _workshopResourceRepository.GetDealershipResourcesAsync(dealershipId, ct);
        return Ok(new { bays, technicians });
    }
}
