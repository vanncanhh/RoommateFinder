using System.Globalization;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Web.Services;

/// <summary>Định dạng hiển thị: tiền "3.500.000 đ", giờ Việt Nam (API trả UTC), nhãn tiếng Việt cho giá trị liệt kê.</summary>
public static class Fmt
{
    // Không dùng CultureInfo("vi-VN"): Blazor WASM mặc định chỉ tải một phần dữ liệu ICU theo ngôn ngữ trình duyệt.
    private static readonly NumberFormatInfo Vi = new() { NumberGroupSeparator = ".", NumberDecimalSeparator = "," };
    private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

    public static string Money(decimal value) => value.ToString("#,##0", Vi) + " đ";

    /// <summary>Giá rút gọn cho thẻ tin: 1,8 triệu.</summary>
    public static string MoneyShort(decimal value) =>
        value >= 1_000_000 ? (value / 1_000_000m).ToString("0.#", Vi) + " triệu" : Money(value);

    public static DateTime ToVn(DateTime utc) =>
        (utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : System.DateTime.SpecifyKind(utc, DateTimeKind.Utc)) + VnOffset;

    public static DateTime VnNow => System.DateTime.UtcNow + VnOffset;

    public static string DateAndTime(DateTime? utc) => utc is { } v ? ToVn(v).ToString("HH:mm dd/MM/yyyy") : "";
    public static string Date(DateTime? utc) => utc is { } v ? ToVn(v).ToString("dd/MM/yyyy") : "";
    public static string Date(DateOnly? d) => d?.ToString("dd/MM/yyyy") ?? "";

    /// <summary>"vừa xong", "5 phút trước", "3 giờ trước", "2 ngày trước", hoặc ngày.</summary>
    public static string Relative(DateTime utc)
    {
        var diff = System.DateTime.UtcNow - DateTime_Utc(utc);
        if (diff.TotalMinutes < 1) return "vừa xong";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
        return Date(utc);
    }

    /// <summary>Giờ ngắn cho chat: hôm nay "HH:mm", ngày khác "HH:mm dd/MM".</summary>
    public static string ChatTime(DateTime utc)
    {
        var vn = ToVn(utc);
        return vn.Date == VnNow.Date ? vn.ToString("HH:mm") : vn.ToString("HH:mm dd/MM");
    }

    private static DateTime DateTime_Utc(DateTime v) => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : System.DateTime.SpecifyKind(v, DateTimeKind.Utc);

    public static string Initial(string? name) => string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[0].ToString().ToUpperInvariant();
}

/// <summary>Nhãn tiếng Việt + màu badge Bootstrap cho các giá trị liệt kê (khớp Domain/Constants/Statuses.cs).</summary>
public static class Labels
{
    public static string PostType(string? v) => v switch
    {
        Domain.Constants.PostType.HasRoom => "Có phòng, cần người ở ghép",
        Domain.Constants.PostType.Seeking => "Đang tìm phòng ở ghép",
        _ => v ?? "",
    };

    public static string PostTypeShort(string? v) => v == Domain.Constants.PostType.HasRoom ? "Có phòng" : "Tìm phòng";

    public static string PostStatus(string? v) => v switch
    {
        Domain.Constants.PostStatus.Pending => "Chờ duyệt",
        Domain.Constants.PostStatus.Approved => "Đang hiển thị",
        Domain.Constants.PostStatus.Rejected => "Bị từ chối",
        Domain.Constants.PostStatus.Hidden => "Bị ẩn",
        Domain.Constants.PostStatus.Expired => "Hết hạn",
        Domain.Constants.PostStatus.Closed => "Đã đóng",
        _ => v ?? "",
    };

    public static string PostStatusColor(string? v) => v switch
    {
        Domain.Constants.PostStatus.Pending => "warning",
        Domain.Constants.PostStatus.Approved => "success",
        Domain.Constants.PostStatus.Rejected => "danger",
        Domain.Constants.PostStatus.Hidden => "dark",
        _ => "secondary",
    };

    public static string RequestStatus(string? v) => v switch
    {
        Domain.Constants.RequestStatus.Pending => "Đang chờ",
        Domain.Constants.RequestStatus.Accepted => "Đã chấp nhận",
        Domain.Constants.RequestStatus.Rejected => "Bị từ chối",
        Domain.Constants.RequestStatus.Cancelled => "Đã rút lại",
        Domain.Constants.RequestStatus.Expired => "Hết hiệu lực",
        _ => v ?? "",
    };

    public static string RequestStatusColor(string? v) => v switch
    {
        Domain.Constants.RequestStatus.Pending => "warning",
        Domain.Constants.RequestStatus.Accepted => "success",
        Domain.Constants.RequestStatus.Rejected => "danger",
        _ => "secondary",
    };

    public static string ReportStatus(string? v) => v switch
    {
        Domain.Constants.ReportStatus.Pending => "Chờ xử lý",
        Domain.Constants.ReportStatus.Resolved => "Đã xử lý",
        Domain.Constants.ReportStatus.Dismissed => "Đã bỏ qua",
        _ => v ?? "",
    };

    public static string UserStatus(string? v) => v switch
    {
        Domain.Constants.UserStatus.Active => "Hoạt động",
        Domain.Constants.UserStatus.Suspended => "Tạm khóa",
        Domain.Constants.UserStatus.Banned => "Khóa vĩnh viễn",
        _ => v ?? "",
    };

    public static string UserStatusColor(string? v) => v switch
    {
        Domain.Constants.UserStatus.Active => "success",
        Domain.Constants.UserStatus.Suspended => "warning",
        _ => "danger",
    };

    public static string Role(string? v) => v switch
    {
        Roles.User => "Người dùng",
        Roles.Moderator => "Kiểm duyệt viên",
        Roles.Admin => "Quản trị viên",
        _ => v ?? "",
    };

    public static string Gender(string? v) => v switch
    {
        Domain.Constants.Gender.Male => "Nam",
        Domain.Constants.Gender.Female => "Nữ",
        Domain.Constants.Gender.Other => "Khác",
        Domain.Constants.Gender.Any => "Không yêu cầu",
        _ => "Không yêu cầu",
    };

    public static string Occupation(string? v) => v switch
    {
        Domain.Constants.Occupation.Student => "Sinh viên",
        Domain.Constants.Occupation.Worker => "Người đi làm",
        _ => "",
    };

    public static string SleepSchedule(string? v) => v switch
    {
        Domain.Constants.SleepSchedule.Early => "Ngủ sớm",
        Domain.Constants.SleepSchedule.Late => "Ngủ muộn",
        Domain.Constants.SleepSchedule.Flexible => "Linh hoạt",
        _ => "",
    };

    public static string LandmarkType(string? v) => v switch
    {
        Domain.Constants.LandmarkType.School => "Trường học",
        Domain.Constants.LandmarkType.Company => "Công ty",
        _ => "Khác",
    };

    public static string AppliesTo(string? v) => v switch
    {
        ReasonAppliesTo.Post => "Tin đăng",
        ReasonAppliesTo.User => "Người dùng",
        _ => "Cả hai",
    };

    public static string ViolationAction(string? v) => v switch
    {
        Domain.Constants.ViolationAction.Warning => "Cảnh cáo",
        Domain.Constants.ViolationAction.HidePost => "Ẩn tin",
        Domain.Constants.ViolationAction.SuspendAccount => "Tạm khóa tài khoản",
        Domain.Constants.ViolationAction.BanAccount => "Khóa vĩnh viễn",
        _ => v ?? "",
    };

    public static string ViolationLevel(string? v) => v switch
    {
        Domain.Constants.ViolationLevel.Light => "Nhẹ",
        Domain.Constants.ViolationLevel.Medium => "Trung bình",
        Domain.Constants.ViolationLevel.Severe => "Nặng",
        _ => v ?? "",
    };
}
