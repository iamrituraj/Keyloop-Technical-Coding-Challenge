using Keyloop.Scheduler.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Scheduler.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasMaxLength(50);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        // Indices for concurrency & fast temporal range querying
        builder.HasIndex(a => new { a.ServiceBayId, a.StartTime, a.EndTime });
        builder.HasIndex(a => new { a.TechnicianId, a.StartTime, a.EndTime });
        builder.HasIndex(a => new { a.DealershipId, a.Status, a.StartTime, a.EndTime });
    }
}
