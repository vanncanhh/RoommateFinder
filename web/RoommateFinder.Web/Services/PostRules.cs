using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Web.Services;

/// <summary>
/// Quy tắc hiển thị nút thao tác trên tin — khớp PostService ở backend (backend vẫn là nơi chặn thật;
/// ở đây chỉ để không hiện nút mà bấm vào chắc chắn bị từ chối).
/// </summary>
public static class PostRules
{
    /// <summary>Khớp POST_MAX_EXTEND mặc định (API công khai không trả cấu hình này).</summary>
    public const int MaxExtend = 3;
    public const int ExtendWindowDays = 3;
    public const int MaxImages = 10;
    public const long MaxImageBytes = 5 * 1024 * 1024;

    public static bool IsPublic(string status, DateTime? expiredAt) =>
        status == PostStatus.Approved && (expiredAt == null || expiredAt > DateTime.UtcNow);

    /// <summary>Sửa được khi chờ duyệt, đang hiển thị hoặc bị từ chối (sửa xong quay về chờ duyệt).</summary>
    public static bool CanEdit(string status) =>
        status is PostStatus.Pending or PostStatus.Approved or PostStatus.Rejected;

    /// <summary>Gia hạn: tin hết hạn, hoặc đang hiển thị và còn dưới 3 ngày; chưa quá số lần cho phép.</summary>
    public static bool CanExtend(string status, DateTime? expiredAt, int extendCount) =>
        extendCount < MaxExtend
        && (status == PostStatus.Expired
            || (status == PostStatus.Approved && expiredAt is { } exp && exp <= DateTime.UtcNow.AddDays(ExtendWindowDays)));

    public static bool CanClose(string status) => status == PostStatus.Approved;

    public static bool CanEdit(PostDetailDto p) => CanEdit(p.Status);
    public static bool CanExtend(PostDetailDto p) => CanExtend(p.Status, p.ExpiredAt, p.ExtendCount);
    public static bool CanExtend(PostSummaryDto p) => CanExtend(p.Status, p.ExpiredAt, p.ExtendCount);

    public static string PriceLabel(string postType) => postType == PostType.HasRoom ? "/người/tháng" : "ngân sách/người/tháng";
}
