using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data;

/// <summary>Dữ liệu khởi tạo đưa vào migration (HasData) — khớp mục 9 của script v2. Giá trị là đề xuất, chờ chốt.</summary>
public static class SeedData
{
    public static readonly SystemConfig[] SystemConfigs =
    {
        new() { ConfigKey = ConfigKeys.PostExpireDays, ConfigValue = "30", Description = "Số ngày hiển thị của một tin sau khi duyệt" },
        new() { ConfigKey = ConfigKeys.PostMaxExtend, ConfigValue = "3", Description = "Số lần gia hạn tối đa của một tin" },
        new() { ConfigKey = ConfigKeys.PostMaxPerDay, ConfigValue = "3", Description = "Số tin tối đa một người được đăng mỗi ngày (chống spam)" },
        new() { ConfigKey = ConfigKeys.PostMaxImages, ConfigValue = "10", Description = "Số ảnh tối đa mỗi tin" },
        new() { ConfigKey = ConfigKeys.ImageMaxSizeMb, ConfigValue = "5", Description = "Dung lượng tối đa mỗi ảnh (MB)" },
        new() { ConfigKey = ConfigKeys.AutoHideReportCount, ConfigValue = "5", Description = "Số báo cáo để tự ẩn tin" },
        new() { ConfigKey = ConfigKeys.AutoHideWindowHours, ConfigValue = "24", Description = "Khoảng thời gian tính ngưỡng báo cáo (giờ)" },
        new() { ConfigKey = ConfigKeys.LoginMaxFailed, ConfigValue = "5", Description = "Số lần đăng nhập sai liên tiếp trước khi khóa tạm" },
        new() { ConfigKey = ConfigKeys.LoginLockMinutes, ConfigValue = "15", Description = "Thời gian khóa tạm sau khi đăng nhập sai (phút)" },
        new() { ConfigKey = ConfigKeys.ResetTokenMinutes, ConfigValue = "30", Description = "Thời hạn mã đặt lại mật khẩu (phút)" },
        new() { ConfigKey = ConfigKeys.JwtAccessMinutes, ConfigValue = "120", Description = "Thời hạn JWT (phút)" },
        new() { ConfigKey = ConfigKeys.ReviewMinDays, ConfigValue = "7", Description = "Số ngày tối thiểu sau khi chấp nhận kết nối mới được đánh giá" },
        new() { ConfigKey = ConfigKeys.SearchMaxDistanceKm, ConfigValue = "20", Description = "Bán kính tối đa của bộ lọc khoảng cách (km)" },
    };

    public static readonly ReportReason[] ReportReasons =
    {
        new() { ReasonId = 1, Name = "Tin đăng sai sự thật / tin ảo", AppliesTo = ReasonAppliesTo.Post, IsActive = true },
        new() { ReasonId = 2, Name = "Lừa đảo, yêu cầu chuyển tiền trước", AppliesTo = ReasonAppliesTo.Both, IsActive = true },
        new() { ReasonId = 3, Name = "Nội dung phản cảm, không phù hợp", AppliesTo = ReasonAppliesTo.Both, IsActive = true },
        new() { ReasonId = 4, Name = "Tin đăng trùng lặp", AppliesTo = ReasonAppliesTo.Post, IsActive = true },
        new() { ReasonId = 5, Name = "Quấy rối, ngôn từ xúc phạm", AppliesTo = ReasonAppliesTo.User, IsActive = true },
        new() { ReasonId = 6, Name = "Giả mạo danh tính", AppliesTo = ReasonAppliesTo.User, IsActive = true },
        new() { ReasonId = 7, Name = "Lý do khác", AppliesTo = ReasonAppliesTo.Both, IsActive = true },
    };

    public static readonly Amenity[] Amenities =
    {
        new() { AmenityId = 1, Name = "Điều hòa", IsActive = true },
        new() { AmenityId = 2, Name = "Wifi", IsActive = true },
        new() { AmenityId = 3, Name = "Máy giặt", IsActive = true },
        new() { AmenityId = 4, Name = "Tủ lạnh", IsActive = true },
        new() { AmenityId = 5, Name = "Chỗ để xe", IsActive = true },
        new() { AmenityId = 6, Name = "Nhà vệ sinh riêng", IsActive = true },
        new() { AmenityId = 7, Name = "Bếp nấu ăn", IsActive = true },
        new() { AmenityId = 8, Name = "Giờ giấc tự do", IsActive = true },
        new() { AmenityId = 9, Name = "Không chung chủ", IsActive = true },
        new() { AmenityId = 10, Name = "Ban công", IsActive = true },
    };
}
