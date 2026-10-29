using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Infrastructure.Data;
using Keyloop.Scheduler.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Keyloop.Scheduler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulerInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SchedulerDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IWorkshopResourceRepository, WorkshopResourceRepository>();
        services.AddScoped<ICustomerVehicleRepository, CustomerVehicleRepository>();
        services.AddScoped<IDatabaseMaintenance, DatabaseMaintenance>();

        return services;
    }
}
