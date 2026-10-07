using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> e)
    {
        e.ToTable("Reports", t =>
        {
            t.HasCheckConstraint("CK_Report_Status", "[Status] IN (N'pending', N'resolved', N'dismissed')");
            t.HasCheckConstraint("CK_Report_Target",
                "([PostId] IS NOT NULL AND [ReportedUserId] IS NULL) OR ([PostId] IS NULL AND [ReportedUserId] IS NOT NULL)");
            t.HasCheckConstraint("CK_Report_NotSelf", "[ReportedUserId] IS NULL OR [ReportedUserId] <> [ReporterId]");
        });
        e.HasKey(x => x.ReportId);
        e.Property(x => x.Description).HasMaxLength(500);
        e.Property(x => x.Status).HasMaxLength(10).IsRequired().HasDefaultValue("pending");
        e.Property(x => x.HandledNote).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId);
        e.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId);
        e.HasOne(x => x.ReportedUser).WithMany().HasForeignKey(x => x.ReportedUserId);
        e.HasOne(x => x.Reason).WithMany().HasForeignKey(x => x.ReasonId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.HandledBy);

        // Một người chỉ có một báo cáo đang chờ cho cùng tin — chống tự gửi đủ ngưỡng để ép ẩn tin.
        e.HasIndex(x => new { x.ReporterId, x.PostId })
            .IsUnique()
            .HasFilter("[Status] = N'pending' AND [PostId] IS NOT NULL")
            .HasDatabaseName("UQ_Report_ReporterPost_Pending");
        e.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("IX_Reports_Status");
        e.HasIndex(x => new { x.PostId, x.CreatedAt }).HasDatabaseName("IX_Reports_Post").HasFilter("[PostId] IS NOT NULL");
    }
}

public class ViolationHistoryConfiguration : IEntityTypeConfiguration<ViolationHistory>
{
    public void Configure(EntityTypeBuilder<ViolationHistory> e)
    {
        e.ToTable("ViolationHistory", t =>
        {
            t.HasCheckConstraint("CK_Violation_Level", "[Level] IN (N'light', N'medium', N'severe')");
            t.HasCheckConstraint("CK_Violation_Action", "[Action] IN (N'warning', N'hide_post', N'suspend_account', N'ban_account')");
            t.HasCheckConstraint("CK_Violation_SuspendDays", "[SuspendDays] > 0");
            t.HasCheckConstraint("CK_Violation_Suspend", "[Action] <> N'suspend_account' OR [SuspendDays] IS NOT NULL");
            t.HasCheckConstraint("CK_Violation_HidePost", "[Action] <> N'hide_post' OR [PostId] IS NOT NULL");
        });
        e.HasKey(x => x.ViolationId);
        e.Property(x => x.Level).HasMaxLength(15).IsRequired();
        e.Property(x => x.Action).HasMaxLength(20).IsRequired();
        e.Property(x => x.Note).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        e.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId);
        e.HasOne<Post>().WithMany().HasForeignKey(x => x.PostId);
        e.HasOne(x => x.Handler).WithMany().HasForeignKey(x => x.HandledBy);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> e)
    {
        e.ToTable("Notifications");
        e.HasKey(x => x.NotificationId);
        e.Property(x => x.Type).HasMaxLength(30).IsRequired();
        e.Property(x => x.Content).HasMaxLength(500).IsRequired();
        e.Property(x => x.Link).HasMaxLength(255);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_Notif_User");
    }
}
