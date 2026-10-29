using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;
using Keyloop.Scheduler.Domain.Exceptions;
using Keyloop.Scheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Scheduler.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly SchedulerDbContext _context;

    public AppointmentRepository(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<List<Appointment>> GetActiveAppointmentsInRangeAsync(string dealershipId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DealershipId == dealershipId 
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartTime < end 
                     && a.EndTime > start)
            .ToListAsync(ct);
    }

    public async Task<bool> HasConflictAsync(string? bayId, string? technicianId, DateTime start, DateTime end, string? excludeAppointmentId = null, CancellationToken ct = default)
    {
        var baseQuery = _context.Appointments
            .AsNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled
                     && a.StartTime < end 
                     && a.EndTime > start);

        if (!string.IsNullOrEmpty(excludeAppointmentId))
        {
            baseQuery = baseQuery.Where(a => a.Id != excludeAppointmentId);
        }

        // Check bay conflict via composite index (ServiceBayId, StartTime, EndTime)
        if (bayId != null && await baseQuery.AnyAsync(a => a.ServiceBayId == bayId, ct))
        {
            return true;
        }

        // Check technician conflict via composite index (TechnicianId, StartTime, EndTime)
        if (technicianId != null && await baseQuery.AnyAsync(a => a.TechnicianId == technicianId, ct))
        {
            return true;
        }

        return false;
    }

    public async Task<Appointment> CreateAppointmentAsync(Appointment appointment, CancellationToken ct = default)
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var conflict = await HasConflictAsync(
                appointment.ServiceBayId, appointment.TechnicianId, 
                appointment.StartTime, appointment.EndTime, null, ct);

            if (conflict)
            {
                await transaction.RollbackAsync(ct);
                throw new ResourceConflictException("The selected service bay or qualified technician is already booked for this interval.", "ServiceBay/Technician");
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return appointment;
        }
        catch (ResourceConflictException)
        {
            throw;
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5)
        {
            // SQLite Error 5: SQLITE_BUSY (Database locked by concurrent writer)
            if (transaction != null) { try { await transaction.RollbackAsync(ct); } catch { } }
            throw new ResourceConflictException("The selected service bay or qualified technician is undergoing concurrent reservation.", "ServiceBay/Technician");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("nested transactions") || ex.Message.Contains("already started"))
        {
            throw new ResourceConflictException("The selected service bay or qualified technician is undergoing concurrent reservation.", "ServiceBay/Technician");
        }
        catch
        {
            if (transaction != null) { try { await transaction.RollbackAsync(ct); } catch { } }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(string appointmentId, CancellationToken ct = default)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Dealership)
            .Include(a => a.ServiceType)
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .Include(a => a.Technician)
            .Include(a => a.ServiceBay)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync(string? dealershipId = null, CancellationToken ct = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Dealership)
            .Include(a => a.ServiceType)
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .Include(a => a.Technician)
            .Include(a => a.ServiceBay)
            .OrderByDescending(a => a.StartTime)
            .AsQueryable();

        if (!string.IsNullOrEmpty(dealershipId))
        {
            query = query.Where(a => a.DealershipId == dealershipId);
        }

        return await query.ToListAsync(ct);
    }
}
