namespace RoommateFinder.Application.Common;

/// <summary>Giờ Việt Nam (GMT+7, không có giờ mùa hè) — chỉ dùng khi ghép thông điệp hiển thị.</summary>
public static class VnTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateTime FromUtc(DateTime utc) => utc + Offset;
    public static string Format(DateTime utc) => FromUtc(utc).ToString("HH:mm dd/MM/yyyy");
    public static string FormatDate(DateTime utc) => FromUtc(utc).ToString("dd/MM/yyyy");

    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(FromUtc(utcNow));
    /// <summary>00:00 giờ Việt Nam của ngày cho trước, quy về UTC.</summary>
    public static DateTime StartOfDayUtc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue) - Offset;
}
