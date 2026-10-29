using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Keyloop.Scheduler.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulerCore(this IServiceCollection services)
    {
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<ISlotRecommender, SlotRecommender>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IAppointmentQueryService, AppointmentQueryService>();
        services.AddScoped<ISchedulerEngine>(sp => new SchedulerEngine(
            sp.GetRequiredService<IAvailabilityService>(),
            sp.GetRequiredService<IBookingService>(),
            sp.GetRequiredService<IAppointmentQueryService>()));

        return services;
    }
}
