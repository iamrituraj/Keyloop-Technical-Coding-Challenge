using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Core.DTOs;

public record AvailableSlotDto(
    DateTime StartTime,
    DateTime EndTime,
    string SuggestedBayId,
    string SuggestedBayName,
    BayType BayType,
    string SuggestedTechnicianId,
    string SuggestedTechnicianName,
    List<SkillCode> TechnicianSkills
);
