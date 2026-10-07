namespace RoommateFinder.Domain.Constants;

/// <summary>Giá trị cột Posts.Status — xem sơ đồ trạng thái tin đăng.</summary>
public static class PostStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Hidden = "hidden";
    public const string Expired = "expired";
    public const string Closed = "closed";

    public static readonly string[] All = { Pending, Approved, Rejected, Hidden, Expired, Closed };
}

public static class PostType
{
    /// <summary>Có phòng, cần tìm thêm người.</summary>
    public const string HasRoom = "has_room";
    /// <summary>Đang tìm phòng để ở ghép.</summary>
    public const string Seeking = "seeking";

    public static readonly string[] All = { HasRoom, Seeking };
}

/// <summary>Giá trị cột ConnectionRequests.Status — xem sơ đồ trạng thái yêu cầu kết nối.</summary>
public static class RequestStatus
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";

    public static readonly string[] All = { Pending, Accepted, Rejected, Cancelled, Expired };
}

public static class ReportStatus
{
    public const string Pending = "pending";
    public const string Resolved = "resolved";
    public const string Dismissed = "dismissed";

    public static readonly string[] All = { Pending, Resolved, Dismissed };
}

public static class UserStatus
{
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Banned = "banned";

    public static readonly string[] All = { Active, Suspended, Banned };
}

public static class Roles
{
    public const string User = "user";
    public const string Moderator = "moderator";
    public const string Admin = "admin";

    public const string ModeratorOrAdmin = Moderator + "," + Admin;
    public static readonly string[] All = { User, Moderator, Admin };
}

public static class ViolationLevel
{
    public const string Light = "light";
    public const string Medium = "medium";
    public const string Severe = "severe";
}

public static class ViolationAction
{
    public const string Warning = "warning";
    public const string HidePost = "hide_post";
    public const string SuspendAccount = "suspend_account";
    public const string BanAccount = "ban_account";
}

public static class Gender
{
    public const string Male = "male";
    public const string Female = "female";
    public const string Other = "other";
    /// <summary>Chỉ dùng cho Posts.PreferredGender.</summary>
    public const string Any = "any";

    public static readonly string[] UserValues = { Male, Female, Other };
    public static readonly string[] PreferredValues = { Male, Female, Any };
}

public static class Occupation
{
    public const string Student = "student";
    public const string Worker = "worker";

    public static readonly string[] All = { Student, Worker };
}

public static class SleepSchedule
{
    public const string Early = "early";
    public const string Late = "late";
    public const string Flexible = "flexible";

    public static readonly string[] All = { Early, Late, Flexible };
}

public static class LandmarkType
{
    public const string School = "school";
    public const string Company = "company";
    public const string Other = "other";

    public static readonly string[] All = { School, Company, Other };
}

public static class ReasonAppliesTo
{
    public const string Post = "post";
    public const string User = "user";
    public const string Both = "both";

    public static readonly string[] All = { Post, User, Both };
}

public static class NotificationType
{
    public const string PostSubmitted = "post_submitted";
    public const string PostApproved = "post_approved";
    public const string PostRejected = "post_rejected";
    public const string PostHidden = "post_hidden";
    public const string PostRestored = "post_restored";
    public const string PostExpired = "post_expired";
    public const string RequestNew = "request_new";
    public const string RequestAccepted = "request_accepted";
    public const string RequestRejected = "request_rejected";
    public const string RequestExpired = "request_expired";
    public const string RequestCancelled = "request_cancelled";
    public const string ReviewNew = "review_new";
    public const string ReportNew = "report_new";
    public const string Violation = "violation";
    public const string AccountStatus = "account_status";
    public const string RoleChanged = "role_changed";
}
