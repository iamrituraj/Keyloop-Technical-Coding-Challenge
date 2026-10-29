using Keyloop.Scheduler.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Scheduler.Infrastructure.Data.Configurations;

public class TechnicianSkillConfiguration : IEntityTypeConfiguration<TechnicianSkill>
{
    public void Configure(EntityTypeBuilder<TechnicianSkill> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasMaxLength(50);

        builder.Property(s => s.CertificationLevel)
            .HasMaxLength(150);

        builder.HasIndex(s => s.TechnicianId);
    }
}
