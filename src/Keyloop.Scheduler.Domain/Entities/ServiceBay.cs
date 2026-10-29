using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Domain.Entities;

public class ServiceBay
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DealershipId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BayType BayType { get; set; } = BayType.StandardLift;
    public bool IsActive { get; set; } = true;

    public Dealership? Dealership { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
