using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Abstractions;

/// <summary>Giao dịch CSDL. Dispose mà chưa Commit thì tự rollback.</summary>
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}

public interface IUnitOfWork
{
    /// <summary>Lưu thay đổi; vi phạm unique (SQL 2601/2627) được đổi thành <see cref="DuplicateKeyException"/>.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

public record PostRef(long PostId, long UserId, string Title);
public record UserStatusInfo(string Status, DateTime? SuspendedUntil, string Role, string SecurityStamp);

public interface IUserRepository
{
    Task<User?> GetAsync(long userId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);
    void Add(User user);
    Task<UserStatusInfo?> GetStatusAsync(long userId, CancellationToken ct = default);
    Task<ProfileDto?> GetProfileAsync(long userId, CancellationToken ct = default);
    Task<PublicProfileDto?> GetPublicProfileAsync(long userId, CancellationToken ct = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(long userId, CancellationToken ct = default);
    /// <summary>Id các tài khoản đang hoạt động có vai trò cho trước (nhận thông báo kiểm duyệt).</summary>
    Task<List<long>> GetActiveIdsByRoleAsync(string role, CancellationToken ct = default);
    Task<int> CountByRoleAsync(string role, CancellationToken ct = default);
    Task<PagedResult<AdminUserDto>> SearchForAdminAsync(AdminUserQuery query, int page, int pageSize, CancellationToken ct = default);
    Task<AdminUserDto?> GetForAdminAsync(long userId, CancellationToken ct = default);
    /// <summary>Tài khoản suspended đã hết hạn khóa → active (BR-03). Trả về Id đã mở khóa.</summary>
    Task<List<long>> ReactivateExpiredSuspensionsAsync(DateTime now, CancellationToken ct = default);
    /// <summary>Khóa dòng người dùng (UPDATE không đổi dữ liệu nghiệp vụ) để tuần tự hóa các giao dịch tính lại điểm đánh giá.</summary>
    Task LockRowAsync(long userId, CancellationToken ct = default);
}

public interface IPasswordResetTokenRepository
{
    void Add(PasswordResetToken token);
    Task<PasswordResetToken?> GetValidAsync(string tokenHash, DateTime now, CancellationToken ct = default);
    Task<int> CountCreatedSinceAsync(long userId, DateTime since, CancellationToken ct = default);
    /// <summary>Đánh dấu đã dùng bằng UPDATE có điều kiện (UsedAt IS NULL) — hai request song song chỉ một bên thành công.</summary>
    Task<bool> TryConsumeAsync(long tokenId, DateTime now, CancellationToken ct = default);
    /// <summary>Vô hiệu mọi mã đặt lại còn hiệu lực của người dùng.</summary>
    Task InvalidateAllAsync(long userId, DateTime now, CancellationToken ct = default);
}

public interface ICatalogRepository
{
    Task<List<AreaDto>> GetAreasAsync(bool includeInactive, CancellationToken ct = default);
    Task<Area?> GetAreaAsync(int areaId, CancellationToken ct = default);
    Task<bool> AreaNameExistsAsync(string name, string city, int? excludeId, CancellationToken ct = default);
    void AddArea(Area area);

    Task<List<LandmarkDto>> GetLandmarksAsync(int? areaId, bool includeInactive, CancellationToken ct = default);
    Task<Landmark?> GetLandmarkAsync(int landmarkId, CancellationToken ct = default);
    Task<bool> LandmarkNameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    void AddLandmark(Landmark landmark);

    Task<List<AmenityDto>> GetAmenitiesAsync(bool includeInactive, CancellationToken ct = default);
    Task<Amenity?> GetAmenityAsync(int amenityId, CancellationToken ct = default);
    Task<bool> AmenityNameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    Task<List<int>> GetActiveAmenityIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    void AddAmenity(Amenity amenity);

    Task<List<ReportReasonDto>> GetReportReasonsAsync(string? appliesTo, bool includeInactive, CancellationToken ct = default);
    Task<ReportReason?> GetReportReasonAsync(int reasonId, CancellationToken ct = default);
    Task<bool> ReportReasonNameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    void AddReportReason(ReportReason reason);
}

public interface IPostRepository
{
    /// <summary>Tin (tracked) không kèm ảnh/tiện ích.</summary>
    Task<Post?> GetAsync(long postId, CancellationToken ct = default);
    /// <summary>Tin (tracked) kèm ảnh và tiện ích, dùng khi sửa.</summary>
    Task<Post?> GetWithDetailsAsync(long postId, CancellationToken ct = default);
    void Add(Post post);
    Task<int> CountCreatedSinceAsync(long userId, DateTime since, CancellationToken ct = default);

    /// <summary>Chi tiết tin chưa xóa mềm (không lọc theo trạng thái — Service quyết định ai được xem).</summary>
    Task<PostDetailDto?> GetDetailAsync(long postId, CancellationToken ct = default);
    Task<PagedResult<PostSummaryDto>> SearchAsync(PostSearchQuery query, int page, int pageSize, DateTime now, CancellationToken ct = default);
    /// <summary>Ứng viên cho tìm theo khoảng cách: lọc như Search + khung tọa độ, chưa phân trang.</summary>
    Task<List<PostSummaryDto>> SearchCandidatesAsync(PostSearchQuery query, GeoBox box, DateTime now, int max, CancellationToken ct = default);
    Task<List<PostSummaryDto>> GetLatestAsync(int count, DateTime now, CancellationToken ct = default);
    Task<List<PostSummaryDto>> GetByOwnerAsync(long userId, string? status, CancellationToken ct = default);
    Task<List<PostSummaryDto>> GetPublicByOwnerAsync(long userId, DateTime now, CancellationToken ct = default);
    Task<PagedResult<PostSummaryDto>> GetForModerationAsync(string status, int page, int pageSize, CancellationToken ct = default);
    Task IncrementViewCountAsync(long postId, CancellationToken ct = default);

    /// <summary>
    /// Trừ 1 chỗ bằng UPDATE có điều kiện (NeededOccupants &gt; 0, approved, chưa xóa).
    /// Trả về false nếu không còn chỗ — chống tranh chấp khi chấp nhận đồng thời.
    /// </summary>
    Task<bool> TryTakeSlotAsync(long postId, DateTime now, CancellationToken ct = default);
    /// <summary>Chuyển tin approved đã hết chỗ sang closed. Trả về true nếu vừa đóng.</summary>
    Task<bool> CloseIfFullAsync(long postId, DateTime now, CancellationToken ct = default);
    /// <summary>approved → closed bằng UPDATE có điều kiện (khóa dòng tin trước, cùng thứ tự với chấp nhận kết nối).</summary>
    Task<bool> TryCloseAsync(long postId, DateTime now, CancellationToken ct = default);
    /// <summary>Xóa mềm bằng UPDATE có điều kiện.</summary>
    Task<bool> TrySoftDeleteAsync(long postId, DateTime now, CancellationToken ct = default);
    /// <summary>Ẩn mọi tin approved của một người (khi bị khóa tài khoản — BR-08).</summary>
    Task<int> HideApprovedOfUserAsync(long userId, string reason, DateTime now, CancellationToken ct = default);
    Task<List<PostRef>> GetDueForExpiryAsync(DateTime now, CancellationToken ct = default);
    Task<int> MarkExpiredAsync(IReadOnlyCollection<long> postIds, DateTime now, CancellationToken ct = default);
}

public interface ISavedPostRepository
{
    Task<bool> ExistsAsync(long userId, long postId, CancellationToken ct = default);
    Task<HashSet<long>> GetSavedPostIdsAsync(long userId, IEnumerable<long> postIds, CancellationToken ct = default);
    void Add(SavedPost saved);
    Task RemoveAsync(long userId, long postId, CancellationToken ct = default);
    Task<List<PostSummaryDto>> GetSavedAsync(long userId, CancellationToken ct = default);
}

public interface IConnectionRequestRepository
{
    void Add(ConnectionRequest request);
    Task<ConnectionRequest?> GetAsync(long requestId, CancellationToken ct = default);
    Task<ConnectionRequestDto?> GetDtoAsync(long requestId, CancellationToken ct = default);
    Task<List<ConnectionRequestDto>> GetIncomingAsync(long userId, string? status, CancellationToken ct = default);
    Task<List<ConnectionRequestDto>> GetOutgoingAsync(long userId, string? status, CancellationToken ct = default);
    /// <summary>Đã có yêu cầu pending/accepted của người gửi cho tin này chưa.</summary>
    Task<ConnectionRequest?> GetActiveAsync(long postId, long senderId, CancellationToken ct = default);
    /// <summary>Yêu cầu gần nhất của người gửi cho tin (để hiển thị trạng thái ở trang chi tiết).</summary>
    Task<ConnectionRequest?> GetLatestAsync(long postId, long senderId, CancellationToken ct = default);
    Task<bool> HasAcceptedAsync(long postId, long senderId, CancellationToken ct = default);
    /// <summary>UPDATE ... WHERE Status = from. Trả về false nếu trạng thái đã đổi trước đó.</summary>
    Task<bool> TryTransitionAsync(long requestId, string from, string to, DateTime now, CancellationToken ct = default);
    Task<List<long>> GetPendingSenderIdsAsync(long postId, CancellationToken ct = default);
    Task<int> ExpirePendingForPostAsync(long postId, DateTime now, CancellationToken ct = default);
    /// <summary>Hết hạn mọi yêu cầu pending gửi tới các tin của một người (khi bị khóa). Trả về Id người gửi bị ảnh hưởng.</summary>
    Task<List<long>> ExpirePendingForOwnerAsync(long ownerId, DateTime now, CancellationToken ct = default);
}

public interface IConversationRepository
{
    void Add(Conversation conversation);
    Task<Conversation?> GetAsync(long conversationId, CancellationToken ct = default);
    Task<List<ConversationDto>> GetForUserAsync(long userId, CancellationToken ct = default);
    void AddMessage(Message message);
    Task<List<MessageDto>> GetMessagesAsync(long conversationId, long? beforeMessageId, int limit, CancellationToken ct = default);
    /// <summary>Đánh dấu đã đọc các tin do người kia gửi. Trả về số tin cập nhật.</summary>
    Task<int> MarkReadAsync(long conversationId, long readerId, CancellationToken ct = default);
}

public interface IReviewRepository
{
    void Add(Review review);
    Task<Review?> GetAsync(long reviewId, CancellationToken ct = default);
    Task<ReviewDto?> GetDtoAsync(long reviewId, CancellationToken ct = default);
    Task<bool> ExistsAsync(long reviewerId, long requestId, CancellationToken ct = default);
    Task<HashSet<long>> GetReviewedRequestIdsAsync(long reviewerId, IEnumerable<long> requestIds, CancellationToken ct = default);
    /// <summary>Điểm trung bình và số đánh giá đang hiển thị của một người (BR-06).</summary>
    Task<(decimal Average, int Count)> GetStatsAsync(long revieweeId, CancellationToken ct = default);
    Task<PagedResult<ReviewDto>> GetForUserAsync(long revieweeId, bool includeHidden, int page, int pageSize, CancellationToken ct = default);
}

public interface IReportRepository
{
    void Add(Report report);
    Task<Report?> GetAsync(long reportId, CancellationToken ct = default);
    Task<ReportDto?> GetDtoAsync(long reportId, CancellationToken ct = default);
    /// <summary><paramref name="excludeTargetUserId"/>: ẩn các báo cáo nhắm vào chính người xem (BR-09).</summary>
    Task<PagedResult<ReportDto>> GetPagedAsync(string status, long? excludeTargetUserId, int page, int pageSize, CancellationToken ct = default);
    /// <summary>pending → status cuối bằng UPDATE có điều kiện; false nếu đã có người xử lý trước.</summary>
    Task<bool> TryClaimAsync(long reportId, string status, long handlerId, string? note, DateTime now, CancellationToken ct = default);
    Task<int> ResolvePendingForUserAsync(long reportedUserId, long excludeReportId, long handlerId, string note, DateTime now, CancellationToken ct = default);
    Task<bool> HasPendingUserReportAsync(long reporterId, long reportedUserId, CancellationToken ct = default);
    /// <summary>Số người báo cáo khác nhau (báo cáo còn chờ xử lý) cho một tin kể từ thời điểm cho trước (BR-07).</summary>
    Task<int> CountDistinctReportersAsync(long postId, DateTime since, CancellationToken ct = default);
    Task<int> CountPendingForPostAsync(long postId, long excludeReportId, CancellationToken ct = default);
    Task<int> ResolvePendingForPostAsync(long postId, long excludeReportId, long handlerId, string note, DateTime now, CancellationToken ct = default);
}

public interface IViolationRepository
{
    void Add(ViolationHistory violation);
    Task<bool> HasActionAsync(long userId, string action, CancellationToken ct = default);
    /// <summary>Đã từng bị khóa (tạm hoặc vĩnh viễn) — dùng xác định tái phạm nặng (BR-08).</summary>
    Task<bool> HasSevereAsync(long userId, CancellationToken ct = default);
    Task<bool> PostHiddenByViolationAsync(long postId, CancellationToken ct = default);
    Task<PagedResult<ViolationDto>> GetPagedAsync(long? userId, int page, int pageSize, CancellationToken ct = default);
}

public interface INotificationRepository
{
    void Add(Notification notification);
    Task<PagedResult<NotificationDto>> GetPagedAsync(long userId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountUnreadAsync(long userId, CancellationToken ct = default);
    Task<int> MarkReadAsync(long userId, long notificationId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(long userId, CancellationToken ct = default);
}

public interface ISystemConfigRepository
{
    Task<List<SystemConfig>> GetAllAsync(CancellationToken ct = default);
    Task<SystemConfig?> GetAsync(string key, CancellationToken ct = default);
}

public interface IDashboardRepository
{
    Task<DashboardDto> GetAsync(DateOnly from, DateOnly to, DateTime fromUtc, DateTime toUtcExclusive, DateTime now, CancellationToken ct = default);
}
