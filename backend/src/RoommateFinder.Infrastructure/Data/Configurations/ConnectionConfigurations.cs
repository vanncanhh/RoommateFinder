using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data.Configurations;

public class ConnectionRequestConfiguration : IEntityTypeConfiguration<ConnectionRequest>
{
    public void Configure(EntityTypeBuilder<ConnectionRequest> e)
    {
        e.ToTable("ConnectionRequests", t =>
        {
            t.HasCheckConstraint("CK_Request_Status", "[Status] IN (N'pending', N'accepted', N'rejected', N'cancelled', N'expired')");
            t.HasCheckConstraint("CK_Request_NotSelf", "[SenderId] <> [ReceiverId]");
        });
        e.HasKey(x => x.RequestId);
        e.Property(x => x.Message).HasMaxLength(500);
        e.Property(x => x.Status).HasMaxLength(10).IsRequired().HasDefaultValue("pending");
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId);
        e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
        e.HasOne(x => x.Receiver).WithMany().HasForeignKey(x => x.ReceiverId);

        // Chỉ chặn khi đã có yêu cầu đang chờ / đã chấp nhận — bị từ chối vẫn gửi lại được.
        // Vi phạm filtered unique index → SQL error 2601 (quyết định d).
        e.HasIndex(x => new { x.PostId, x.SenderId })
            .IsUnique()
            .HasFilter("[Status] IN (N'pending', N'accepted')")
            .HasDatabaseName("UQ_Request_SenderPost_Active");
        e.HasIndex(x => new { x.ReceiverId, x.Status }).HasDatabaseName("IX_Requests_Receiver");
        e.HasIndex(x => new { x.SenderId, x.Status }).HasDatabaseName("IX_Requests_Sender");
    }
}

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> e)
    {
        e.ToTable("Conversations", t => t.HasCheckConstraint("CK_Conversation_NotSelf", "[User1Id] <> [User2Id]"));
        e.HasKey(x => x.ConversationId);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
        e.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("UQ_Conversations_Request");
        e.HasOne(x => x.User1).WithMany().HasForeignKey(x => x.User1Id);
        e.HasOne(x => x.User2).WithMany().HasForeignKey(x => x.User2Id);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> e)
    {
        e.ToTable("Messages");
        e.HasKey(x => x.MessageId);
        e.Property(x => x.Content).HasMaxLength(1000).IsRequired();
        e.Property(x => x.SentAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderId);
        e.HasIndex(x => new { x.ConversationId, x.SentAt }).HasDatabaseName("IX_Messages_Conv");
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> e)
    {
        e.ToTable("Reviews", t =>
        {
            t.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_Review_NotSelf", "[ReviewerId] <> [RevieweeId]");
        });
        e.HasKey(x => x.ReviewId);
        e.Property(x => x.Comment).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
        e.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId);
        e.HasOne(x => x.Reviewee).WithMany().HasForeignKey(x => x.RevieweeId);
        e.HasIndex(x => new { x.ReviewerId, x.RequestId }).IsUnique().HasDatabaseName("UQ_Review_ReviewerRequest");
        e.HasIndex(x => x.RevieweeId).HasDatabaseName("IX_Reviews_Reviewee").HasFilter("[IsHidden] = 0");
    }
}
