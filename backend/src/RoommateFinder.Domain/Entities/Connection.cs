namespace RoommateFinder.Domain.Entities;

public class ConnectionRequest
{
    public long RequestId { get; set; }
    public long PostId { get; set; }
    public Post Post { get; set; } = null!;
    public long SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public long ReceiverId { get; set; }
    public User Receiver { get; set; } = null!;
    public string? Message { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class Conversation
{
    public long ConversationId { get; set; }
    public long RequestId { get; set; }
    public ConnectionRequest Request { get; set; } = null!;
    public long User1Id { get; set; }
    public User User1 { get; set; } = null!;
    public long User2Id { get; set; }
    public User User2 { get; set; } = null!;
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool HasMember(long userId) => User1Id == userId || User2Id == userId;
    public long OtherMember(long userId) => User1Id == userId ? User2Id : User1Id;
}

public class Message
{
    public long MessageId { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

public class Review
{
    public long ReviewId { get; set; }
    public long RequestId { get; set; }
    public ConnectionRequest Request { get; set; } = null!;
    public long ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;
    public long RevieweeId { get; set; }
    public User Reviewee { get; set; } = null!;
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
}
