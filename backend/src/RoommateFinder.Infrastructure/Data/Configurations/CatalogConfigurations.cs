using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> e)
    {
        e.ToTable("Areas");
        e.HasKey(x => x.AreaId);
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.Property(x => x.District).HasMaxLength(100);
        e.Property(x => x.City).HasMaxLength(100).IsRequired();
        e.Property(x => x.Latitude).HasPrecision(9, 6);
        e.Property(x => x.Longitude).HasPrecision(9, 6);
    }
}

public class LandmarkConfiguration : IEntityTypeConfiguration<Landmark>
{
    public void Configure(EntityTypeBuilder<Landmark> e)
    {
        e.ToTable("Landmarks", t => t.HasCheckConstraint("CK_Landmark_Type", "[Type] IN (N'school', N'company', N'other')"));
        e.HasKey(x => x.LandmarkId);
        e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        e.Property(x => x.Type).HasMaxLength(15).IsRequired();
        e.Property(x => x.Address).HasMaxLength(255);
        e.Property(x => x.Latitude).HasPrecision(9, 6);
        e.Property(x => x.Longitude).HasPrecision(9, 6);
        e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId);
    }
}

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> e)
    {
        e.ToTable("Amenities");
        e.HasKey(x => x.AmenityId);
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_Amenities_Name");
        e.HasData(SeedData.Amenities);
    }
}

public class ReportReasonConfiguration : IEntityTypeConfiguration<ReportReason>
{
    public void Configure(EntityTypeBuilder<ReportReason> e)
    {
        e.ToTable("ReportReasons", t => t.HasCheckConstraint("CK_ReportReason_AppliesTo", "[AppliesTo] IN (N'post', N'user', N'both')"));
        e.HasKey(x => x.ReasonId);
        e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        e.Property(x => x.AppliesTo).HasMaxLength(10).IsRequired().HasDefaultValue("both");
        e.HasData(SeedData.ReportReasons);
    }
}

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> e)
    {
        e.ToTable("SystemConfigs");
        e.HasKey(x => x.ConfigKey);
        e.Property(x => x.ConfigKey).HasMaxLength(50);
        e.Property(x => x.ConfigValue).HasMaxLength(255).IsRequired();
        e.Property(x => x.Description).HasMaxLength(255);
        e.HasData(SeedData.SystemConfigs);
    }
}
