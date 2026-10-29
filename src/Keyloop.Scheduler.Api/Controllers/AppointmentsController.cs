using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.Scheduler.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IAppointmentQueryService _queryService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IBookingService bookingService,
        IAppointmentQueryService queryService,
        ILogger<AppointmentsController> logger)
    {
        _bookingService = bookingService;
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a confirmed appointment record associating customer, vehicle, qualified technician, and service bay.
    /// Prevents double bookings via transactional resource locking.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentSummaryDto>> BookAppointment(
        [FromBody] BookAppointmentCommandDto command,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _bookingService.BookAppointmentAsync(command, ct);
        return CreatedAtAction(nameof(GetAppointment), new { id = result.AppointmentId }, result);
    }

    /// <summary>
    /// Retrieves full details of a confirmed appointment.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AppointmentSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentSummaryDto>> GetAppointment(string id, CancellationToken ct)
    {
        var result = await _queryService.GetAppointmentDetailsAsync(id, ct);
        if (result == null) return NotFound(new { error = $"Appointment '{id}' was not found." });
        return Ok(result);
    }

    /// <summary>
    /// Lists all appointments, optionally filtered by dealership.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AppointmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AppointmentSummaryDto>>> ListAppointments(
        [FromQuery] string? dealershipId,
        CancellationToken ct)
    {
        var result = await _queryService.ListAppointmentsAsync(dealershipId, ct);
        return Ok(result);
    }
}
