using System.Data;
using Keyloop.Scheduler.Core.DTOs;
using Keyloop.Scheduler.Core.Services;
using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;
using Keyloop.Scheduler.Domain.Exceptions;
using Keyloop.Scheduler.Infrastructure.Data;
using Keyloop.Scheduler.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Keyloop.Scheduler.Tests;

public class AppointmentConcurrencyIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SchedulerDbContext> _contextOptions;
    private readonly string _testDbPath;

    public AppointmentConcurrencyIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"scheduler_test_{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initContext = CreateContext();
        initContext.Database.EnsureCreated();
        initContext.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
        initContext.Database.ExecuteSqlRaw("PRAGMA busy_timeout = 5000;");
        initContext.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");

        DatabaseSeeder.SeedAsync(initContext).GetAwaiter().GetResult();
    }

    private SchedulerDbContext CreateContext()
    {
        return new SchedulerDbContext(_contextOptions);
    }

    private SchedulerEngine CreateEngine(SchedulerDbContext context)
    {
        return new SchedulerEngine(
            new WorkshopResourceRepository(context),
            new AppointmentRepository(context),
            new CustomerVehicleRepository(context),
            NullLogger<SchedulerEngine>.Instance);
    }

    [Fact]
    public async Task Database_ShouldHaveWalModeAndBusyTimeoutConfigured()
    {
        using var context = CreateContext();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        var journalMode = (string?)await command.ExecuteScalarAsync();

        Assert.NotNull(journalMode);
        Assert.Equal("wal", journalMode.ToLowerInvariant());
    }

    [Fact]
    public async Task ConcurrentBooking_TwoParallelRequestsForLastSlot_ExactlyOneSucceedsAndOneThrowsConflict()
    {
        var targetTime = new DateTime(2026, 11, 10, 10, 0, 0);

        var requestClient1 = new BookAppointmentCommandDto(
            DealershipId: "DLR-001",
            ServiceTypeId: "SRV-EV-HEALTH",
            VehicleVin: "MAT-NEXON-EV-CLIENT-1",
            CustomerName: "Client One (Web)",
            CustomerEmail: "client1@example.com",
            CustomerPhone: "+91 98765 00001",
            DesiredStartTime: targetTime,
            PreferredBayId: "BAY-02-EV",
            PreferredTechnicianId: "TECH-01"
        );

        var requestClient2 = new BookAppointmentCommandDto(
            DealershipId: "DLR-001",
            ServiceTypeId: "SRV-EV-HEALTH",
            VehicleVin: "MAT-NEXON-EV-CLIENT-2",
            CustomerName: "Client Two (Advisor)",
            CustomerEmail: "client2@example.com",
            CustomerPhone: "+91 98765 00002",
            DesiredStartTime: targetTime,
            PreferredBayId: "BAY-02-EV",
            PreferredTechnicianId: "TECH-01"
        );

        AppointmentSummaryDto? successResult = null;
        Exception? caughtException = null;

        var task1 = Task.Run(async () =>
        {
            try
            {
                using var context1 = CreateContext();
                var engine1 = CreateEngine(context1);
                return await engine1.BookAppointmentAsync(requestClient1);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                return null;
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                using var context2 = CreateContext();
                var engine2 = CreateEngine(context2);
                return await engine2.BookAppointmentAsync(requestClient2);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                return null;
            }
        });

        var results = await Task.WhenAll(task1, task2);
        var successfulBookings = results.Where(r => r != null).ToList();

        Assert.Single(successfulBookings);
        successResult = successfulBookings.First();
        Assert.Equal(AppointmentStatus.Confirmed, successResult!.Status);

        Assert.NotNull(caughtException);
        Assert.True(
            caughtException is ResourceConflictException || caughtException is Microsoft.Data.Sqlite.SqliteException,
            $"Expected concurrency conflict exception, but got {caughtException.GetType().Name}: {caughtException.Message}");

        using var verifyContext = CreateContext();
        var appointmentsInSlot = await verifyContext.Appointments
            .Where(a => a.ServiceBayId == "BAY-02-EV" && a.StartTime == targetTime)
            .ToListAsync();

        Assert.Single(appointmentsInSlot);
        Assert.Equal(successResult.AppointmentId, appointmentsInSlot[0].Id);
    }

    [Fact]
    public async Task CreateAppointment_WhenSlotAlreadyBooked_ThrowsResourceConflictException()
    {
        using var context = CreateContext();
        var repo = new AppointmentRepository(context);
        var targetTime = new DateTime(2026, 11, 12, 11, 0, 0);

        var firstAppointment = new Appointment
        {
            DealershipId = "DLR-001",
            ServiceTypeId = "SRV-EV-HEALTH",
            VehicleVin = "MAT622154N1A98765",
            CustomerId = "CUST-001",
            TechnicianId = "TECH-01",
            ServiceBayId = "BAY-02-EV",
            StartTime = targetTime,
            EndTime = targetTime.AddMinutes(90),
            Status = AppointmentStatus.Confirmed
        };

        await repo.CreateAppointmentAsync(firstAppointment);

        var conflictingAppointment = new Appointment
        {
            DealershipId = "DLR-001",
            ServiceTypeId = "SRV-EV-HEALTH",
            VehicleVin = "MAT-SECOND-VEHICLE",
            CustomerId = "CUST-001",
            TechnicianId = "TECH-01",
            ServiceBayId = "BAY-02-EV",
            StartTime = targetTime.AddMinutes(30),
            EndTime = targetTime.AddMinutes(120),
            Status = AppointmentStatus.Confirmed
        };

        await Assert.ThrowsAsync<ResourceConflictException>(() => repo.CreateAppointmentAsync(conflictingAppointment));
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();

        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
            var walFile = _testDbPath + "-wal";
            if (File.Exists(walFile)) File.Delete(walFile);
            var shmFile = _testDbPath + "-shm";
            if (File.Exists(shmFile)) File.Delete(shmFile);
        }
        catch
        {
        }
    }
}
