using Keyloop.Scheduler.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Scheduler.Infrastructure.Data.Configurations;

public class DealershipConfiguration : IEntityTypeConfiguration<Dealership>
{
    public void Configure(EntityTypeBuilder<Dealership> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasMaxLength(50);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Address)
            .HasMaxLength(500);

        builder.Property(d => d.OpeningTime)
            .HasMaxLength(10);

        builder.Property(d => d.ClosingTime)
            .HasMaxLength(10);

        builder.Property(d => d.Timezone)
            .HasMaxLength(100);
    }
}
