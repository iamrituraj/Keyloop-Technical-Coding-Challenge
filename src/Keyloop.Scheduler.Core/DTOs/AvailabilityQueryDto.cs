using System.ComponentModel.DataAnnotations;

namespace Keyloop.Scheduler.Core.DTOs;

public record AvailabilityQueryDto(
    [Required] string DealershipId,
    [Required] string ServiceTypeId,
    [Required] DateTime Date
);
