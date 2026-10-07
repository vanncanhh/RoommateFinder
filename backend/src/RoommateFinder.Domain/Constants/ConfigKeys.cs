namespace RoommateFinder.Domain.Constants;

/// <summary>Khóa trong bảng SystemConfigs. Giá trị mặc định nằm trong seed của migration.</summary>
public static class ConfigKeys
{
    public const string PostExpireDays = "POST_EXPIRE_DAYS";
    public const string PostMaxExtend = "POST_MAX_EXTEND";
    public const string PostMaxPerDay = "POST_MAX_PER_DAY";
    public const string PostMaxImages = "POST_MAX_IMAGES";
    public const string ImageMaxSizeMb = "IMAGE_MAX_SIZE_MB";
    public const string AutoHideReportCount = "AUTO_HIDE_REPORT_COUNT";
    public const string AutoHideWindowHours = "AUTO_HIDE_WINDOW_HOURS";
    public const string LoginMaxFailed = "LOGIN_MAX_FAILED";
    public const string LoginLockMinutes = "LOGIN_LOCK_MINUTES";
    public const string ResetTokenMinutes = "RESET_TOKEN_MINUTES";
    public const string JwtAccessMinutes = "JWT_ACCESS_MINUTES";
    public const string ReviewMinDays = "REVIEW_MIN_DAYS";
    public const string SearchMaxDistanceKm = "SEARCH_MAX_DISTANCE_KM";

    /// <summary>Miền giá trị hợp lệ (min, max) của từng khóa — dùng khi Admin sửa cấu hình.</summary>
    public static readonly IReadOnlyDictionary<string, (int Min, int Max)> Ranges = new Dictionary<string, (int, int)>
    {
        [PostExpireDays] = (1, 365),
        [PostMaxExtend] = (0, 20),
        [PostMaxPerDay] = (1, 100),
        [PostMaxImages] = (1, 30),
        [ImageMaxSizeMb] = (1, 20),
        [AutoHideReportCount] = (2, 100),
        [AutoHideWindowHours] = (1, 720),
        [LoginMaxFailed] = (3, 20),
        [LoginLockMinutes] = (1, 1440),
        [ResetTokenMinutes] = (5, 1440),
        [JwtAccessMinutes] = (5, 10080),
        [ReviewMinDays] = (0, 90),
        [SearchMaxDistanceKm] = (1, 100),
    };
}
