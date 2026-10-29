using Keyloop.Scheduler.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Scheduler.Infrastructure.Data.Configurations;

public class ServiceBayConfiguration : IEntityTypeConfiguration<ServiceBay>
{
    public void Configure(EntityTypeBuilder<ServiceBay> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasMaxLength(50);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(b => b.DealershipId);
    }
}
