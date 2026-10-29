using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Domain.Entities;

public class Appointment
{
    public string Id { get; set; } = "APT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public string DealershipId { get; set; } = string.Empty;
    public string ServiceTypeId { get; set; } = string.Empty;
    public string VehicleVin { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string TechnicianId { get; set; } = string.Empty;
    public string ServiceBayId { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Dealership? Dealership { get; set; }
    public ServiceType? ServiceType { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Customer? Customer { get; set; }
    public Technician? Technician { get; set; }
    public ServiceBay? ServiceBay { get; set; }

    public bool OverlapsWith(DateTime start, DateTime end) => StartTime < end && EndTime > start;
}
