namespace Keyloop.Scheduler.Domain.Entities;

public class Technician
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DealershipId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Dealership? Dealership { get; set; }
    public ICollection<TechnicianSkill> Skills { get; set; } = new List<TechnicianSkill>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
