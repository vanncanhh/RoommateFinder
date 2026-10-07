namespace RoommateFinder.Application.Dtos;

public class RejectPostRequest
{
    public string Reason { get; set; } = "";
}

public class CreateReportRequest
{
    public long? PostId { get; set; }
    public long? ReportedUserId { get; set; }
    public int ReasonId { get; set; }
    public string? Description { get; set; }
}

public class ReportDto
{
    public long ReportId { get; init; }
    public long ReporterId { get; init; }
    public string ReporterName { get; init; } = "";
    public long? PostId { get; init; }
    public string? PostTitle { get; init; }
    public string? PostStatus { get; init; }
    public long? ReportedUserId { get; init; }
    public string? ReportedUserName { get; init; }
    /// <summary>Người chịu trách nhiệm: người bị báo cáo, hoặc chủ tin bị báo cáo.</summary>
    public long TargetUserId { get; init; }
    public string TargetUserName { get; init; } = "";
    public int ReasonId { get; init; }
    public string ReasonName { get; init; } = "";
    public string? Description { get; init; }
    public string Status { get; init; } = "";
    public long? HandledBy { get; init; }
    public string? HandlerName { get; init; }
    public DateTime? HandledAt { get; init; }
    public string? HandledNote { get; init; }
    public DateTime CreatedAt { get; init; }
    public int PendingReportsOnTarget { get; init; }
}

public static class ReportDecision
{
    public const string Dismiss = "dismiss";
    public const string Warning = "warning";
    public const string HidePost = "hide_post";
    public const string Suspend = "suspend";
    public const string Ban = "ban";

    public static readonly string[] All = { Dismiss, Warning, HidePost, Suspend, Ban };
}

public class HandleReportRequest
{
    /// <summary>dismiss | warning | hide_post | suspend | ban</summary>
    public string Decision { get; set; } = "";
    public int? SuspendDays { get; set; }
    public string? Note { get; set; }
}

public class ViolationDto
{
    public long ViolationId { get; init; }
    public long UserId { get; init; }
    public string UserName { get; init; } = "";
    public long? ReportId { get; init; }
    public long? PostId { get; init; }
    public string Level { get; init; } = "";
    public string Action { get; init; } = "";
    public int? SuspendDays { get; init; }
    public string? Note { get; init; }
    public long HandledBy { get; init; }
    public string HandlerName { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}

public class NotificationDto
{
    public long NotificationId { get; init; }
    public long UserId { get; init; }
    public string Type { get; init; } = "";
    public string Content { get; init; } = "";
    public long? RelatedId { get; init; }
    public string? Link { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
