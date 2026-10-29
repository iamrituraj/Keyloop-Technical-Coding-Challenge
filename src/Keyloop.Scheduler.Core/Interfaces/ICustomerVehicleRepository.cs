using Keyloop.Scheduler.Domain.Entities;

namespace Keyloop.Scheduler.Core.Interfaces;

/// <summary>
/// Repository dedicated to Customer and Vehicle lifecycle and lookups.
/// </summary>
public interface ICustomerVehicleRepository
{
    Task<Customer> GetOrCreateCustomerAsync(string name, string email, string phone, CancellationToken ct = default);
    Task<Vehicle?> GetVehicleByVinAsync(string vin, CancellationToken ct = default);
    Task<Vehicle> RegisterVehicleIfNotExistsAsync(Vehicle vehicle, CancellationToken ct = default);
}
