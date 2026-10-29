using Keyloop.Scheduler.Core.Interfaces;
using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Scheduler.Infrastructure.Repositories;

public class CustomerVehicleRepository : ICustomerVehicleRepository
{
    private readonly SchedulerDbContext _context;

    public CustomerVehicleRepository(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<Customer> GetOrCreateCustomerAsync(string name, string email, string phone, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail, ct);

        if (existing != null) return existing;

        var customer = new Customer
        {
            Name = name.Trim(),
            Email = normalizedEmail,
            Phone = phone.Trim()
        };

        try
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(ct);
            return customer;
        }
        catch (DbUpdateException)
        {
            // Another concurrent thread inserted this customer concurrently
            _context.Entry(customer).State = EntityState.Detached;
            var reloaded = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail, ct);
            if (reloaded != null) return reloaded;
            throw;
        }
    }

    public async Task<Vehicle?> GetVehicleByVinAsync(string vin, CancellationToken ct = default)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Vin == vin.ToUpperInvariant(), ct);
    }

    public async Task<Vehicle> RegisterVehicleIfNotExistsAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        var existing = await _context.Vehicles.FirstOrDefaultAsync(v => v.Vin == vehicle.Vin, ct);
        if (existing != null) return existing;

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync(ct);
        return vehicle;
    }
}
