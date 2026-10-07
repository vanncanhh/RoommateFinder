namespace RoommateFinder.Application.Dtos;

public class AdminUserQuery
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminUserDto
{
    public long UserId { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public string Role { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime? SuspendedUntil { get; init; }
    public decimal AvgRating { get; init; }
    public int ReviewCount { get; init; }
    public int PostCount { get; init; }
    public int ViolationCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = "";
}

public class UpdateUserStatusRequest
{
    /// <summary>active | suspended | banned</summary>
    public string Status { get; set; } = "";
    public int? SuspendDays { get; set; }
    public string? Reason { get; set; }
}

public class SystemConfigDto
{
    public string ConfigKey { get; init; } = "";
    public string ConfigValue { get; init; } = "";
    public string? Description { get; init; }
    public int? Min { get; init; }
    public int? Max { get; init; }
}

public class UpdateConfigRequest
{
    public string Value { get; set; } = "";
}

public class DashboardDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public int NewUsers { get; init; }
    public int NewPostsHasRoom { get; init; }
    public int NewPostsSeeking { get; init; }
    public int ApprovedCount { get; init; }
    public int RejectedCount { get; init; }
    public double? ApprovalRate { get; init; }
    public double? AvgModerationHours { get; init; }
    public int AcceptedConnections { get; init; }
    public int PendingReports { get; init; }
    public int PendingPosts { get; init; }
    public int ActivePosts { get; init; }
    public int TotalUsers { get; init; }
    public List<AreaCountDto> TopAreas { get; init; } = new();
    public List<DailyCountDto> DailyPosts { get; init; } = new();
    public List<DailyCountDto> DailyUsers { get; init; } = new();
}

public class AreaCountDto
{
    public string AreaName { get; init; } = "";
    public int PostCount { get; init; }
}

public class DailyCountDto
{
    public DateOnly Date { get; init; }
    public int Count { get; init; }
}
