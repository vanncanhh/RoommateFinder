namespace RoommateFinder.Application.Dtos;

public class CreateConnectionRequest
{
    public long PostId { get; set; }
    public string? Message { get; set; }
}

public class ConnectionRequestDto
{
    public long RequestId { get; init; }
    public long PostId { get; init; }
    public string PostTitle { get; init; } = "";
    public string PostType { get; init; } = "";
    public string PostStatus { get; init; } = "";
    public string? PostThumbnailUrl { get; init; }
    public long SenderId { get; init; }
    public string SenderName { get; init; } = "";
    public string? SenderAvatarUrl { get; init; }
    public decimal SenderRating { get; init; }
    public int SenderReviewCount { get; init; }
    public long ReceiverId { get; init; }
    public string ReceiverName { get; init; } = "";
    public string? ReceiverAvatarUrl { get; init; }
    public string? Message { get; init; }
    public string Status { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public long? ConversationId { get; init; }

    // Đánh giá (BR-05) — tính theo người đang xem
    public bool HasReviewed { get; set; }
    public bool CanReview { get; set; }
    public DateTime? ReviewAvailableAt { get; set; }
}

public class ConversationDto
{
    public long ConversationId { get; init; }
    public long RequestId { get; init; }
    public long PostId { get; init; }
    public string PostTitle { get; init; } = "";
    public long OtherUserId { get; init; }
    public string OtherUserName { get; init; } = "";
    public string? OtherUserAvatarUrl { get; init; }
    public string? LastMessage { get; init; }
    public long? LastMessageSenderId { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public int UnreadCount { get; init; }
}

public class MessageDto
{
    public long MessageId { get; init; }
    public long ConversationId { get; init; }
    public long SenderId { get; init; }
    public string Content { get; init; } = "";
    public DateTime SentAt { get; init; }
    public bool IsRead { get; init; }
}

public class SendMessageRequest
{
    public string Content { get; set; } = "";
}
