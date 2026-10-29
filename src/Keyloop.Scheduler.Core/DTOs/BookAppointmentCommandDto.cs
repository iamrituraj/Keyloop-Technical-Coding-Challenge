using System.ComponentModel.DataAnnotations;

namespace Keyloop.Scheduler.Core.DTOs;

public record BookAppointmentCommandDto(
    [Required(ErrorMessage = "DealershipId is required.")] 
    string DealershipId,

    [Required(ErrorMessage = "ServiceTypeId is required.")] 
    string ServiceTypeId,

    [Required(ErrorMessage = "Vehicle registration number or VIN is required.")]
    [RegularExpression(@"^[A-Za-z0-9\s\-]{6,20}$", ErrorMessage = "Vehicle identifier must be a valid Indian registration number (e.g. TS 09 EA 4521, DL 01 AB 1234, 22 BH 1234 AA) or 17-character VIN.")]
    string VehicleVin,

    [Required(ErrorMessage = "CustomerName is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Customer name must be between 2 and 100 characters.")]
    string CustomerName,

    [Required(ErrorMessage = "CustomerEmail is required.")]
    [EmailAddress(ErrorMessage = "Customer email address must be a valid email format.")]
    string CustomerEmail,

    [Required(ErrorMessage = "CustomerPhone is required.")]
    [RegularExpression(@"^\+?[0-9\s\-()]{7,20}$", ErrorMessage = "Customer phone number must be a valid telephone format.")]
    string CustomerPhone,

    [Required(ErrorMessage = "DesiredStartTime is required.")]
    DateTime DesiredStartTime,

    string? PreferredBayId = null,
    string? PreferredTechnicianId = null,
    string? Notes = null
);
