using Keyloop.Scheduler.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.Scheduler.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class DemoController : ControllerBase
{
    private readonly IDatabaseMaintenance _databaseMaintenance;
    private readonly ILogger<DemoController> _logger;

    public DemoController(IDatabaseMaintenance databaseMaintenance, ILogger<DemoController> logger)
    {
        _databaseMaintenance = databaseMaintenance;
        _logger = logger;
    }

    /// <summary>
    /// DEMONSTRATION & TEST HARNESS ONLY: Wipes and reseeds SQLite database to initial clean demo state.
    /// In production environments, this endpoint is omitted or secured with role-based auth ([Authorize(Roles = "KeyloopAdmin")]).
    /// </summary>
    [HttpPost("api/v1/demo/reset")]
    [HttpPost("api/v1/meta/reset-demo")] // Route alias for backward compatibility
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetDemo(CancellationToken ct)
    {
        _logger.LogInformation("Resetting database to initial seed state for demo walkthrough.");
        await _databaseMaintenance.ResetDemoDatabaseAsync(ct);
        return Ok(new { message = "Demo database reset to clean seed state successfully." });
    }
}
