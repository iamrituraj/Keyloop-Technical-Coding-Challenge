using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.Scheduler.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(IAvailabilityService availabilityService, ILogger<AvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Check real-time availability for a dealership, service type, and date.
    /// Evaluates concurrent availability of both an appropriate Service Bay and a qualified Technician.
    /// </summary>
    /// <param name="dealershipId">Dealership identifier (e.g. DLR-001)</param>
    /// <param name="serviceTypeId">Service identifier (e.g. SRV-EV-HEALTH)</param>
    /// <param name="date">Target booking date (YYYY-MM-DD)</param>
    [HttpGet]
    [ProducesResponseType(typeof(AvailabilityResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityResultDto>> GetAvailability(
        [FromQuery] string dealershipId,
        [FromQuery] string serviceTypeId,
        [FromQuery] DateTime? date,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dealershipId))
            return BadRequest(new { error = "dealershipId parameter is required." });

        if (string.IsNullOrWhiteSpace(serviceTypeId))
            return BadRequest(new { error = "serviceTypeId parameter is required." });

        var targetDate = date ?? DateTime.Today;

        var query = new AvailabilityQueryDto(dealershipId, serviceTypeId, targetDate);
        var result = await _availabilityService.GetAvailabilityAsync(query, ct);
        return Ok(result);
    }
}
