namespace RoommateFinder.Domain.Entities;

public class Report
{
    public long ReportId { get; set; }
    public long ReporterId { get; set; }
    public User Reporter { get; set; } = null!;
    /// <summary>Đúng một trong hai: PostId hoặc ReportedUserId (CK_Report_Target).</summary>
    public long? PostId { get; set; }
    public Post? Post { get; set; }
    public long? ReportedUserId { get; set; }
    public User? ReportedUser { get; set; }
    public int ReasonId { get; set; }
    public ReportReason Reason { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "pending";
    public long? HandledBy { get; set; }
    public DateTime? HandledAt { get; set; }
    public string? HandledNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ViolationHistory
{
    public long ViolationId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long? ReportId { get; set; }
    public long? PostId { get; set; }
    public string Level { get; set; } = null!;
    public string Action { get; set; } = null!;
    public int? SuspendDays { get; set; }
    public string? Note { get; set; }
    public long HandledBy { get; set; }
    public User Handler { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class Notification
{
    public long NotificationId { get; set; }
    public long UserId { get; set; }
    public string Type { get; set; } = null!;
    public string Content { get; set; } = null!;
    public long? RelatedId { get; set; }
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
