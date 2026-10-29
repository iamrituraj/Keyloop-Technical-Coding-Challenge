using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Domain.Entities;

public class Vehicle
{
    public string Vin { get; set; } = string.Empty; // Primary Key
    public string CustomerId { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public PowertrainType PowertrainType { get; set; } = PowertrainType.InternalCombustion;

    public Customer? Customer { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
