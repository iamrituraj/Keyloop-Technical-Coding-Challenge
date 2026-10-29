namespace Keyloop.Scheduler.Core.DTOs;

public record AvailabilityResultDto(
    string DealershipId,
    string ServiceTypeId,
    string ServiceName,
    int DurationMinutes,
    DateTime QueryDate,
    List<AvailableSlotDto> AvailableSlots
);
