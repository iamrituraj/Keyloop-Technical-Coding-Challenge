using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Domain.Entities;

public class ServiceType
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
    public BayType RequiredBayType { get; set; } = BayType.StandardLift;

    public ICollection<SkillCode> RequiredSkills { get; set; } = new List<SkillCode>();
}
