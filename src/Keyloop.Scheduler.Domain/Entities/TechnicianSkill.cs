using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Domain.Entities;

public class TechnicianSkill
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TechnicianId { get; set; } = string.Empty;
    public SkillCode SkillCode { get; set; }
    public string CertificationLevel { get; set; } = "Master";

    public Technician? Technician { get; set; }
}
