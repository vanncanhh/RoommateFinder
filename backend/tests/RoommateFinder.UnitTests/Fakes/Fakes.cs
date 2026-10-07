using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.UnitTests.Fakes;

// Fake viết tay (kế hoạch 5.1): chỉ cài các phương thức mà Service được kiểm thử thực sự gọi;
// phương thức khác ném NotImplementedException để lộ ra ngay nếu Service gọi ngoài dự kiến.

public class FakeClock : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 10, 6, 3, 0, 0, DateTimeKind.Utc);
}

public class FakeSettings : ISystemSettings
{
    public Dictionary<string, int> Values { get; } = new()
    {
        [ConfigKeys.PostExpireDays] = 30, [ConfigKeys.PostMaxExtend] = 3, [ConfigKeys.PostMaxPerDay] = 3,
        [ConfigKeys.PostMaxImages] = 10, [ConfigKeys.ImageMaxSizeMb] = 5, [ConfigKeys.AutoHideReportCount] = 5,
        [ConfigKeys.AutoHideWindowHours] = 24, [ConfigKeys.LoginMaxFailed] = 5, [ConfigKeys.LoginLockMinutes] = 15,
        [ConfigKeys.ResetTokenMinutes] = 30, [ConfigKeys.JwtAccessMinutes] = 120, [ConfigKeys.ReviewMinDays] = 7,
        [ConfigKeys.SearchMaxDistanceKm] = 20,
    };
    public Task<int> GetIntAsync(string key, CancellationToken ct = default) => Task.FromResult(Values[key]);
    public void Invalidate() { }
}

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    /// <summary>Gán Id cho thực thể mới khi "lưu", giống IDENTITY trong CSDL.</summary>
    public List<Action> OnSave { get; } = new();
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        foreach (var a in OnSave) a();
        return Task.CompletedTask;
    }
    public Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct = default) => Task.FromResult<IAppTransaction>(new Tx());
    private sealed class Tx : IAppTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public class FakeHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string password, string hash) => hash == "hash:" + password;
}

public class FakeJwt : IJwtTokenGenerator
{
    public (string Token, DateTime ExpiresAt) Generate(long userId, string email, string role, string securityStamp, int lifetimeMinutes) =>
        ($"token-{userId}-{role}", DateTime.UtcNow.AddMinutes(lifetimeMinutes));
}

public class FakeEmail : IEmailSender
{
    public List<(string To, string Subject)> Sent { get; } = new();
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        Sent.Add((to, subject));
        return Task.CompletedTask;
    }
}

public class FakeLinks : IAppLinks
{
    public string ResetPasswordUrl(string token) => "http://test/reset?token=" + token;
}

public class FakeRealtime : IRealtimeNotifier
{
    public List<NotificationDto> Pushed { get; } = new();
    public Task NotificationsCreatedAsync(IReadOnlyList<NotificationDto> notifications, CancellationToken ct = default)
    {
        Pushed.AddRange(notifications);
        return Task.CompletedTask;
    }
    public Task MessageSentAsync(MessageDto message, IReadOnlyList<long> recipientIds, CancellationToken ct = default) => Task.CompletedTask;
    public Task MessagesReadAsync(long conversationId, long readerId, long otherUserId, CancellationToken ct = default) => Task.CompletedTask;
}

public class FakeNotifications : INotificationRepository
{
    public List<Notification> Items { get; } = new();
    public void Add(Notification notification) => Items.Add(notification);
    public Task<PagedResult<NotificationDto>> GetPagedAsync(long userId, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<int> CountUnreadAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<int> MarkReadAsync(long userId, long notificationId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<int> MarkAllReadAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
}

public class FakeUsers : IUserRepository
{
    public List<User> Items { get; } = new();
    public User Add(long id, string role = Roles.User, string status = UserStatus.Active, string? password = "User@1234")
    {
        var u = new User { UserId = id, FullName = $"User {id}", Email = $"u{id}@test.vn", PasswordHash = "hash:" + password, Role = role, Status = status };
        Items.Add(u);
        return u;
    }
    public Task<User?> GetAsync(long userId, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(u => u.UserId == userId));
    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(u => u.Email == normalizedEmail));
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default) => Task.FromResult(Items.Any(u => u.Email == normalizedEmail));
    public void Add(User user) => Items.Add(user);
    public Task<UserStatusInfo?> GetStatusAsync(long userId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(u => u.UserId == userId).Select(u => new UserStatusInfo(u.Status, u.SuspendedUntil, u.Role, u.SecurityStamp)).FirstOrDefault());
    public Task<List<long>> GetActiveIdsByRoleAsync(string role, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(u => u.Role == role && u.Status == UserStatus.Active).Select(u => u.UserId).ToList());
    public Task<CurrentUserDto?> GetCurrentUserAsync(long userId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(u => u.UserId == userId).Select(u => new CurrentUserDto { UserId = u.UserId, FullName = u.FullName, Email = u.Email, Role = u.Role }).FirstOrDefault());
    public Task<int> CountByRoleAsync(string role, CancellationToken ct = default) => Task.FromResult(Items.Count(u => u.Role == role));
    public Task<ProfileDto?> GetProfileAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PublicProfileDto?> GetPublicProfileAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PagedResult<AdminUserDto>> SearchForAdminAsync(AdminUserQuery query, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<AdminUserDto?> GetForAdminAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<long>> ReactivateExpiredSuspensionsAsync(DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task LockRowAsync(long userId, CancellationToken ct = default) => Task.CompletedTask;
}

public class FakeResetTokens : IPasswordResetTokenRepository
{
    public List<PasswordResetToken> Items { get; } = new();
    public void Add(PasswordResetToken token) => Items.Add(token);
    public Task<PasswordResetToken?> GetValidAsync(string tokenHash, DateTime now, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > now));
    public Task<int> CountCreatedSinceAsync(long userId, DateTime since, CancellationToken ct = default) =>
        Task.FromResult(Items.Count(t => t.UserId == userId && t.CreatedAt >= since));
    public Task<bool> TryConsumeAsync(long tokenId, DateTime now, CancellationToken ct = default)
    {
        var t = Items.FirstOrDefault(x => x.TokenId == tokenId && x.UsedAt == null);
        if (t == null) return Task.FromResult(false);
        t.UsedAt = now;
        return Task.FromResult(true);
    }
    public Task InvalidateAllAsync(long userId, DateTime now, CancellationToken ct = default)
    {
        foreach (var t in Items.Where(x => x.UserId == userId && x.UsedAt == null)) t.UsedAt = now;
        return Task.CompletedTask;
    }
}

public class FakePosts : IPostRepository
{
    public List<Post> Items { get; } = new();
    public List<long> HiddenForUsers { get; } = new();
    public Task<Post?> GetAsync(long postId, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(p => p.PostId == postId));
    public Task<int> HideApprovedOfUserAsync(long userId, string reason, DateTime now, CancellationToken ct = default)
    {
        HiddenForUsers.Add(userId);
        var list = Items.Where(p => p.UserId == userId && p.Status == PostStatus.Approved).ToList();
        foreach (var p in list) { p.Status = PostStatus.Hidden; p.RejectReason = reason; }
        return Task.FromResult(list.Count);
    }
    public Task<Post?> GetWithDetailsAsync(long postId, CancellationToken ct = default) => throw new NotImplementedException();
    public void Add(Post post) => Items.Add(post);
    public Task<int> CountCreatedSinceAsync(long userId, DateTime since, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PostDetailDto?> GetDetailAsync(long postId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PagedResult<PostSummaryDto>> SearchAsync(PostSearchQuery query, int page, int pageSize, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<PostSummaryDto>> SearchCandidatesAsync(PostSearchQuery query, GeoBox box, DateTime now, int max, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<PostSummaryDto>> GetLatestAsync(int count, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<PostSummaryDto>> GetByOwnerAsync(long userId, string? status, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<PostSummaryDto>> GetPublicByOwnerAsync(long userId, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PagedResult<PostSummaryDto>> GetForModerationAsync(string status, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
    public Task IncrementViewCountAsync(long postId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> TryTakeSlotAsync(long postId, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> CloseIfFullAsync(long postId, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<PostRef>> GetDueForExpiryAsync(DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<int> MarkExpiredAsync(IReadOnlyCollection<long> postIds, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> TryCloseAsync(long postId, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> TrySoftDeleteAsync(long postId, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
}

public class FakeRequests : IConnectionRequestRepository
{
    public List<ConnectionRequest> Items { get; } = new();
    public Task<ConnectionRequest?> GetAsync(long requestId, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(r => r.RequestId == requestId));
    public void Add(ConnectionRequest request) => Items.Add(request);
    public Task<ConnectionRequestDto?> GetDtoAsync(long requestId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<ConnectionRequestDto>> GetIncomingAsync(long userId, string? status, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<ConnectionRequestDto>> GetOutgoingAsync(long userId, string? status, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<ConnectionRequest?> GetActiveAsync(long postId, long senderId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<ConnectionRequest?> GetLatestAsync(long postId, long senderId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> HasAcceptedAsync(long postId, long senderId, CancellationToken ct = default) => Task.FromResult(AcceptedExists);
    public Task<bool> TryTransitionAsync(long requestId, string from, string to, DateTime now, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<long>> GetPendingSenderIdsAsync(long postId, CancellationToken ct = default) => Task.FromResult(new List<long>());
    public Task<int> ExpirePendingForPostAsync(long postId, DateTime now, CancellationToken ct = default) => Task.FromResult(0);
    public Task<List<long>> ExpirePendingForOwnerAsync(long ownerId, DateTime now, CancellationToken ct = default) => Task.FromResult(new List<long>());
    public bool AcceptedExists { get; set; }
}

public class FakeReviews : IReviewRepository
{
    public List<Review> Items { get; } = new();
    public void Add(Review review) => Items.Add(review);
    public Task<Review?> GetAsync(long reviewId, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(r => r.ReviewId == reviewId));
    public Task<ReviewDto?> GetDtoAsync(long reviewId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(r => r.ReviewId == reviewId).Select(r => new ReviewDto { ReviewId = r.ReviewId, Rating = r.Rating, RevieweeId = r.RevieweeId }).FirstOrDefault());
    public Task<bool> ExistsAsync(long reviewerId, long requestId, CancellationToken ct = default) =>
        Task.FromResult(Items.Any(r => r.ReviewerId == reviewerId && r.RequestId == requestId));
    public Task<(decimal Average, int Count)> GetStatsAsync(long revieweeId, CancellationToken ct = default)
    {
        var list = Items.Where(r => r.RevieweeId == revieweeId && !r.IsHidden).ToList();
        return Task.FromResult(list.Count == 0 ? (0m, 0) : ((decimal)list.Average(r => (double)r.Rating), list.Count));
    }
    public Task<HashSet<long>> GetReviewedRequestIdsAsync(long reviewerId, IEnumerable<long> requestIds, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<PagedResult<ReviewDto>> GetForUserAsync(long revieweeId, bool includeHidden, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
}

public class FakeReports : IReportRepository
{
    public List<Report> Items { get; } = new();
    public void Add(Report report) => Items.Add(report);
    public Task<Report?> GetAsync(long reportId, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(r => r.ReportId == reportId));
    public Task<ReportDto?> GetDtoAsync(long reportId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(r => r.ReportId == reportId).Select(r => new ReportDto { ReportId = r.ReportId, Status = r.Status }).FirstOrDefault());
    public Task<bool> HasPendingUserReportAsync(long reporterId, long reportedUserId, CancellationToken ct = default) =>
        Task.FromResult(Items.Any(r => r.ReporterId == reporterId && r.ReportedUserId == reportedUserId && r.Status == ReportStatus.Pending));
    public Task<int> CountDistinctReportersAsync(long postId, DateTime since, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(r => r.PostId == postId && r.CreatedAt >= since && r.Status == ReportStatus.Pending).Select(r => r.ReporterId).Distinct().Count());
    public Task<int> CountPendingForPostAsync(long postId, long excludeReportId, CancellationToken ct = default) =>
        Task.FromResult(Items.Count(r => r.PostId == postId && r.ReportId != excludeReportId && r.Status == ReportStatus.Pending));
    public Task<int> ResolvePendingForPostAsync(long postId, long excludeReportId, long handlerId, string note, DateTime now, CancellationToken ct = default)
    {
        var list = Items.Where(r => r.PostId == postId && r.ReportId != excludeReportId && r.Status == ReportStatus.Pending).ToList();
        foreach (var r in list) r.Status = ReportStatus.Resolved;
        return Task.FromResult(list.Count);
    }
    public Task<PagedResult<ReportDto>> GetPagedAsync(string status, long? excludeTargetUserId, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> TryClaimAsync(long reportId, string status, long handlerId, string? note, DateTime now, CancellationToken ct = default)
    {
        var r = Items.FirstOrDefault(x => x.ReportId == reportId && x.Status == ReportStatus.Pending);
        if (r == null) return Task.FromResult(false);
        r.Status = status; r.HandledBy = handlerId; r.HandledAt = now; r.HandledNote = note;
        return Task.FromResult(true);
    }
    public Task<int> ResolvePendingForUserAsync(long reportedUserId, long excludeReportId, long handlerId, string note, DateTime now, CancellationToken ct = default)
    {
        var list = Items.Where(r => r.ReportedUserId == reportedUserId && r.ReportId != excludeReportId && r.Status == ReportStatus.Pending).ToList();
        foreach (var r in list) r.Status = ReportStatus.Resolved;
        return Task.FromResult(list.Count);
    }
}

public class FakeViolations : IViolationRepository
{
    public List<ViolationHistory> Items { get; } = new();
    public void Add(ViolationHistory violation) => Items.Add(violation);
    public Task<bool> HasActionAsync(long userId, string action, CancellationToken ct = default) => Task.FromResult(Items.Any(v => v.UserId == userId && v.Action == action));
    public Task<bool> HasSevereAsync(long userId, CancellationToken ct = default) =>
        Task.FromResult(Items.Any(v => v.UserId == userId && (v.Action == ViolationAction.SuspendAccount || v.Action == ViolationAction.BanAccount)));
    public Task<bool> PostHiddenByViolationAsync(long postId, CancellationToken ct = default) => Task.FromResult(Items.Any(v => v.PostId == postId && v.Action == ViolationAction.HidePost));
    public Task<PagedResult<ViolationDto>> GetPagedAsync(long? userId, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
}

public class FakeCatalog : ICatalogRepository
{
    public List<ReportReason> Reasons { get; } = new()
    {
        new() { ReasonId = 1, Name = "Tin ảo", AppliesTo = ReasonAppliesTo.Post, IsActive = true },
        new() { ReasonId = 2, Name = "Lừa đảo", AppliesTo = ReasonAppliesTo.Both, IsActive = true },
        new() { ReasonId = 5, Name = "Quấy rối", AppliesTo = ReasonAppliesTo.User, IsActive = true },
    };
    public Task<ReportReason?> GetReportReasonAsync(int reasonId, CancellationToken ct = default) => Task.FromResult(Reasons.FirstOrDefault(r => r.ReasonId == reasonId));
    public Task<List<AreaDto>> GetAreasAsync(bool includeInactive, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Area?> GetAreaAsync(int areaId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> AreaNameExistsAsync(string name, string city, int? excludeId, CancellationToken ct = default) => throw new NotImplementedException();
    public void AddArea(Area area) => throw new NotImplementedException();
    public Task<List<LandmarkDto>> GetLandmarksAsync(int? areaId, bool includeInactive, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Landmark?> GetLandmarkAsync(int landmarkId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> LandmarkNameExistsAsync(string name, int? excludeId, CancellationToken ct = default) => throw new NotImplementedException();
    public void AddLandmark(Landmark landmark) => throw new NotImplementedException();
    public Task<List<AmenityDto>> GetAmenitiesAsync(bool includeInactive, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Amenity?> GetAmenityAsync(int amenityId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> AmenityNameExistsAsync(string name, int? excludeId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<int>> GetActiveAmenityIdsAsync(IEnumerable<int> ids, CancellationToken ct = default) => throw new NotImplementedException();
    public void AddAmenity(Amenity amenity) => throw new NotImplementedException();
    public Task<List<ReportReasonDto>> GetReportReasonsAsync(string? appliesTo, bool includeInactive, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> ReportReasonNameExistsAsync(string name, int? excludeId, CancellationToken ct = default) => throw new NotImplementedException();
    public void AddReportReason(ReportReason reason) => throw new NotImplementedException();
}
