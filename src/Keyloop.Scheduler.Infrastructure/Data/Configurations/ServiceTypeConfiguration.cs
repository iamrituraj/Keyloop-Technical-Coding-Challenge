using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Scheduler.Infrastructure.Data.Configurations;

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasMaxLength(50);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        // RequiredSkills collection conversion
        builder.Property(s => s.RequiredSkills)
            .HasConversion(
                v => string.Join(',', v.Select(e => (int)e)),
                v => string.IsNullOrWhiteSpace(v) 
                    ? new List<SkillCode>() 
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => (SkillCode)int.Parse(e)).ToList()
            );
    }
}
