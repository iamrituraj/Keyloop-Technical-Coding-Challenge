using Keyloop.Scheduler.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Scheduler.Infrastructure.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Vin);

        builder.Property(v => v.Vin)
            .HasMaxLength(50);

        builder.Property(v => v.Make)
            .HasMaxLength(100);

        builder.Property(v => v.Model)
            .HasMaxLength(100);
    }
}
