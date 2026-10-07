using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> e)
    {
        e.ToTable("Posts", t =>
        {
            t.HasCheckConstraint("CK_Post_Type", "[PostType] IN (N'has_room', N'seeking')");
            t.HasCheckConstraint("CK_Post_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Post_CurrentOccupants", "[CurrentOccupants] >= 0");
            t.HasCheckConstraint("CK_Post_NeededOccupants", "[NeededOccupants] >= 0");
            t.HasCheckConstraint("CK_Post_PreferredGender", "[PreferredGender] IN (N'male', N'female', N'any')");
            t.HasCheckConstraint("CK_Post_Status", "[Status] IN (N'pending', N'approved', N'rejected', N'hidden', N'expired', N'closed')");
            t.HasCheckConstraint("CK_Post_Reject", "[Status] <> N'rejected' OR [RejectReason] IS NOT NULL");
        });
        e.HasKey(x => x.PostId);
        e.Property(x => x.PostType).HasMaxLength(10).IsRequired();
        e.Property(x => x.Title).HasMaxLength(150).IsRequired();
        e.Property(x => x.Description).HasMaxLength(2000);
        e.Property(x => x.Price).HasPrecision(12, 0);
        e.Property(x => x.Address).HasMaxLength(255);
        e.Property(x => x.Latitude).HasPrecision(9, 6);
        e.Property(x => x.Longitude).HasPrecision(9, 6);
        e.Property(x => x.PreferredGender).HasMaxLength(10);
        e.Property(x => x.Status).HasMaxLength(12).IsRequired().HasDefaultValue("pending");
        e.Property(x => x.RejectReason).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.ModeratedBy);

        // Chỉ mục tìm kiếm — khớp IX_Posts_Search trong script v2
        e.HasIndex(x => new { x.Status, x.AreaId, x.PostType, x.Price })
            .HasDatabaseName("IX_Posts_Search")
            .HasFilter("[IsDeleted] = 0")
            .IncludeProperties(x => new { x.PreferredGender, x.NeededOccupants, x.ExpiredAt, x.Latitude, x.Longitude });
        e.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("IX_Posts_User");
        e.HasIndex(x => x.ExpiredAt).HasDatabaseName("IX_Posts_ExpiredAt").HasFilter("[Status] = N'approved'");
    }
}

public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> e)
    {
        e.ToTable("PostImages");
        e.HasKey(x => x.ImageId);
        e.Property(x => x.ImageUrl).HasMaxLength(255).IsRequired();
        e.HasOne<Post>().WithMany(p => p.Images).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostAmenityConfiguration : IEntityTypeConfiguration<PostAmenity>
{
    public void Configure(EntityTypeBuilder<PostAmenity> e)
    {
        e.ToTable("PostAmenities");
        e.HasKey(x => new { x.PostId, x.AmenityId });
        e.HasOne<Post>().WithMany(p => p.PostAmenities).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Amenity).WithMany().HasForeignKey(x => x.AmenityId);
    }
}

public class SavedPostConfiguration : IEntityTypeConfiguration<SavedPost>
{
    public void Configure(EntityTypeBuilder<SavedPost> e)
    {
        e.ToTable("SavedPosts");
        e.HasKey(x => x.SavedId);
        e.Property(x => x.SavedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        e.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId);
        e.HasIndex(x => new { x.UserId, x.PostId }).IsUnique().HasDatabaseName("UQ_Saved_UserPost");
    }
}
