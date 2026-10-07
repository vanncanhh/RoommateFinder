using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("Users", t =>
        {
            t.HasCheckConstraint("CK_User_Gender", "[Gender] IN (N'male', N'female', N'other')");
            t.HasCheckConstraint("CK_User_Role", "[Role] IN (N'user', N'moderator', N'admin')");
            t.HasCheckConstraint("CK_User_Occupation", "[Occupation] IN (N'student', N'worker')");
            t.HasCheckConstraint("CK_User_SleepSchedule", "[SleepSchedule] IN (N'early', N'late', N'flexible')");
            t.HasCheckConstraint("CK_User_Cleanliness", "[CleanlinessLevel] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_User_Status", "[Status] IN (N'active', N'suspended', N'banned')");
            t.HasCheckConstraint("CK_User_Suspend", "[Status] <> N'suspended' OR [SuspendedUntil] IS NOT NULL");
        });
        e.HasKey(x => x.UserId);
        e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        e.Property(x => x.Email).HasMaxLength(100).IsRequired();
        e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        e.Property(x => x.Phone).HasMaxLength(15);
        e.Property(x => x.Gender).HasMaxLength(10);
        e.Property(x => x.Role).HasMaxLength(20).IsRequired().HasDefaultValue("user");
        e.Property(x => x.Occupation).HasMaxLength(20);
        e.Property(x => x.SchoolOrCompany).HasMaxLength(150);
        e.Property(x => x.AvatarUrl).HasMaxLength(255);
        e.Property(x => x.Bio).HasMaxLength(500);
        e.Property(x => x.SleepSchedule).HasMaxLength(10);
        e.Property(x => x.AvgRating).HasPrecision(3, 2).HasDefaultValue(0m);
        e.Property(x => x.Status).HasMaxLength(15).IsRequired().HasDefaultValue("active");
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.SecurityStamp).HasMaxLength(32).IsRequired().HasDefaultValueSql("REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')");
        e.HasOne(x => x.Landmark).WithMany().HasForeignKey(x => x.LandmarkId);
        e.HasIndex(x => new { x.Role, x.Status }).HasDatabaseName("IX_Users_Role_Status");
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> e)
    {
        e.ToTable("PasswordResetTokens");
        e.HasKey(x => x.TokenId);
        e.Property(x => x.TokenHash).HasMaxLength(255).IsRequired();
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        e.HasIndex(x => x.TokenHash).HasDatabaseName("IX_PasswordResetTokens_TokenHash");
    }
}
