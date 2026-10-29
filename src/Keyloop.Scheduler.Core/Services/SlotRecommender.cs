using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Keyloop.Scheduler.Core.Services;

public class SlotRecommender : ISlotRecommender
{
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<SlotRecommender> _logger;

    public SlotRecommender(IAvailabilityService availabilityService, ILogger<SlotRecommender> logger)
    {
        _availabilityService = availabilityService;
        _logger = logger;
    }

    public async Task<List<string>> CalculateAlternativeSlotsAsync(
        Dealership dealership,
        ServiceType serviceType,
        DateTime desiredTime,
        CancellationToken ct = default)
    {
        try
        {
            var availability = await _availabilityService.GetAvailabilityAsync(
                new AvailabilityQueryDto(dealership.Id, serviceType.Id, desiredTime.Date),
                ct);

            var nextSlots = availability.AvailableSlots
                .Where(s => s.StartTime > desiredTime)
                .Take(3)
                .Select(s => s.StartTime.ToString("HH:mm"))
                .ToList();

            if (!nextSlots.Any())
            {
                var tomorrowAvailability = await _availabilityService.GetAvailabilityAsync(
                    new AvailabilityQueryDto(dealership.Id, serviceType.Id, desiredTime.Date.AddDays(1)),
                    ct);

                nextSlots = tomorrowAvailability.AvailableSlots
                    .Take(2)
                    .Select(s => $"{s.StartTime:yyyy-MM-dd} {s.StartTime:HH:mm}")
                    .ToList();
            }

            return nextSlots;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not compute alternative suggestions for conflict at Dealership {DealershipId}", dealership.Id);
            return new List<string>();
        }
    }
}
