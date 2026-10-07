using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

public class ModerationService
{
    private readonly IPostRepository _posts;
    private readonly IReportRepository _reports;
    private readonly IViolationRepository _violations;
    private readonly IUserRepository _users;
    private readonly IReviewRepository _reviews;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationPublisher _notifier;
    private readonly AccountSanctions _sanctions;
    private readonly IConnectionRequestRepository _requests;

    public ModerationService(IPostRepository posts, IReportRepository reports, IViolationRepository violations,
        IUserRepository users, IReviewRepository reviews, IUnitOfWork uow, ISystemSettings settings,
        IDateTimeProvider clock, NotificationPublisher notifier, AccountSanctions sanctions, IConnectionRequestRepository requests)
    {
        _requests = requests;
        _posts = posts;
        _reports = reports;
        _violations = violations;
        _users = users;
        _reviews = reviews;
        _uow = uow;
        _settings = settings;
        _clock = clock;
        _notifier = notifier;
        _sanctions = sanctions;
    }

    // ------------------------------------------------------------------ Duyệt tin

    public Task<PagedResult<PostSummaryDto>> GetPostsAsync(string? status, int page, int pageSize, CancellationToken ct = default)
    {
        status ??= PostStatus.Pending;
        if (!PostStatus.All.Contains(status)) throw BusinessException.Validation("Trạng thái không hợp lệ.");
        (page, pageSize) = Paging.Normalize(page, pageSize, 20);
        return _posts.GetForModerationAsync(status, page, pageSize, ct);
    }

    public async Task ApproveAsync(CurrentUser mod, long postId, CancellationToken ct = default)
    {
        var post = await GetPendingPostAsync(mod, postId, ct);
        // BR-08: tin chờ duyệt của tài khoản đang bị khóa không được lên công khai.
        var owner = await _users.GetStatusAsync(post.UserId, ct);
        if (owner == null || User.IsLockedStatus(owner.Status, owner.SuspendedUntil, _clock.UtcNow))
            throw BusinessException.Conflict("Chủ tin đang bị khóa tài khoản, không thể duyệt tin này.");

        var now = _clock.UtcNow;
        var days = await _settings.GetIntAsync(ConfigKeys.PostExpireDays, ct);
        post.Status = PostStatus.Approved;
        post.ExpiredAt = now.AddDays(days); // BR-02: hạn tính từ lúc duyệt
        post.RejectReason = null;
        post.ModeratedBy = mod.UserId;
        post.ModeratedAt = now;
        _notifier.Queue(post.UserId, NotificationType.PostApproved,
            $"Tin \"{post.Title}\" đã được duyệt và hiển thị đến {VnTime.FormatDate(post.ExpiredAt.Value)}.", post.PostId, $"/posts/{post.PostId}");
        await _uow.SaveChangesAsync(ct); // rowversion: chủ tin vừa sửa nội dung thì trả 409, không duyệt bản chưa xem
        await _notifier.FlushAsync(ct);
    }

    public async Task RejectAsync(CurrentUser mod, long postId, RejectPostRequest req, CancellationToken ct = default)
    {
        var reason = (req.Reason ?? "").Trim();
        if (reason.Length is < 5 or > 500) throw BusinessException.Validation("Lý do từ chối phải từ 5 đến 500 ký tự.");
        var post = await GetPendingPostAsync(mod, postId, ct);
        post.Status = PostStatus.Rejected;
        post.RejectReason = reason;
        post.ModeratedBy = mod.UserId;
        post.ModeratedAt = _clock.UtcNow;
        _notifier.Queue(post.UserId, NotificationType.PostRejected,
            $"Tin \"{post.Title}\" bị từ chối: {reason}", post.PostId, "/my-posts?status=rejected");
        await _uow.SaveChangesAsync(ct);
        await _notifier.FlushAsync(ct);
    }

    private async Task<Post> GetPendingPostAsync(CurrentUser mod, long postId, CancellationToken ct)
    {
        var post = await _posts.GetAsync(postId, ct);
        if (post == null || post.IsDeleted) throw BusinessException.NotFound("Không tìm thấy tin đăng.");
        if (post.Status != PostStatus.Pending) throw BusinessException.Conflict("Tin không còn ở trạng thái chờ duyệt.");
        if (post.UserId == mod.UserId) throw BusinessException.Forbidden("Bạn không được duyệt tin của chính mình."); // BR-09
        return post;
    }

    // ------------------------------------------------------------------ Xử lý báo cáo

    /// <summary>Không hiển thị báo cáo nhắm vào chính người xem (BR-09).</summary>
    public Task<PagedResult<ReportDto>> GetReportsAsync(CurrentUser mod, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        status ??= ReportStatus.Pending;
        if (!ReportStatus.All.Contains(status)) throw BusinessException.Validation("Trạng thái không hợp lệ.");
        (page, pageSize) = Paging.Normalize(page, pageSize, 20);
        return _reports.GetPagedAsync(status, mod.UserId, page, pageSize, ct);
    }

    public async Task<ReportDto> HandleReportAsync(CurrentUser mod, long reportId, HandleReportRequest req, CancellationToken ct = default)
    {
        if (!ReportDecision.All.Contains(req.Decision)) throw BusinessException.Validation("Quyết định xử lý không hợp lệ.");
        var note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
        if (note?.Length > 450) throw BusinessException.Validation("Ghi chú tối đa 450 ký tự.");

        var report = await _reports.GetAsync(reportId, ct) ?? throw BusinessException.NotFound("Không tìm thấy báo cáo.");
        if (report.Status != ReportStatus.Pending) throw BusinessException.Conflict("Báo cáo đã được xử lý.");

        var targetUserId = report.ReportedUserId ?? report.Post!.UserId;
        if (targetUserId == mod.UserId) throw BusinessException.Forbidden("Bạn không được xử lý báo cáo nhắm vào chính mình."); // BR-09
        var target = await _users.GetAsync(targetUserId, ct) ?? throw BusinessException.NotFound("Không tìm thấy người bị báo cáo.");
        if (target.Role != Roles.User && !mod.IsAdmin)
            throw BusinessException.Forbidden("Chỉ Admin được xử lý vi phạm của moderator/admin.");
        if (req.Decision == ReportDecision.HidePost && report.PostId == null)
            throw BusinessException.Validation("Chỉ ẩn tin được với báo cáo nhắm vào tin đăng.");
        if (req.Decision == ReportDecision.Suspend && req.SuspendDays is not (>= 1 and <= 365))
            throw BusinessException.Validation("Số ngày khóa phải từ 1 đến 365.");
        if (req.Decision is ReportDecision.Suspend or ReportDecision.Ban && target.Status == UserStatus.Banned)
            throw BusinessException.Conflict("Tài khoản này đã bị khóa vĩnh viễn."); // không hạ cấp ban thành suspend

        var now = _clock.UtcNow;
        var finalStatus = req.Decision == ReportDecision.Dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;
        await using var tx = await _uow.BeginTransactionAsync(ct);
        // Giành quyền xử lý bằng UPDATE có điều kiện — hai kiểm duyệt viên bấm cùng lúc thì chỉ một người xử lý.
        if (!await _reports.TryClaimAsync(reportId, finalStatus, mod.UserId, note, now, ct))
            throw BusinessException.Conflict("Báo cáo vừa được người khác xử lý.");

        if (req.Decision == ReportDecision.Dismiss)
        {
            await RestorePostIfCleanAsync(report, now, ct);
        }
        else
        {
            var reasonText = note ?? report.Reason.Name;
            switch (req.Decision)
            {
                case ReportDecision.Warning:
                    AddViolation(report, targetUserId, mod.UserId, ViolationLevel.Light, ViolationAction.Warning, null, note, now);
                    _notifier.Queue(targetUserId, NotificationType.Violation,
                        $"Bạn nhận cảnh cáo do vi phạm quy định: {reasonText}. Tái phạm có thể bị khóa tài khoản.", report.ReportId, "/profile");
                    break;

                case ReportDecision.HidePost:
                    var post = report.Post!;
                    post.Status = PostStatus.Hidden;
                    post.RejectReason = reasonText;
                    post.UpdatedAt = now; // giữ ModeratedAt/By của lần duyệt để thống kê thời gian duyệt
                    AddViolation(report, targetUserId, mod.UserId, ViolationLevel.Medium, ViolationAction.HidePost, null, note, now, post.PostId);
                    _notifier.Queue(targetUserId, NotificationType.PostHidden,
                        $"Tin \"{post.Title}\" đã bị ẩn do vi phạm: {reasonText}", post.PostId, "/my-posts?status=hidden");
                    var senders = await _requests.GetPendingSenderIdsAsync(post.PostId, ct);
                    await _requests.ExpirePendingForPostAsync(post.PostId, now, ct);
                    _notifier.QueueMany(senders, NotificationType.RequestExpired,
                        $"Tin \"{post.Title}\" đã bị ẩn, yêu cầu kết nối của bạn hết hiệu lực.", post.PostId, "/connections?tab=outgoing");
                    break;

                case ReportDecision.Suspend:
                    // BR-08: đã từng bị khóa (tạm hoặc vĩnh viễn) thì lần nặng tiếp theo khóa vĩnh viễn.
                    if (await _violations.HasSevereAsync(targetUserId, ct))
                        await _sanctions.BanAsync(target, mod.UserId, report.ReportId, Truncate($"Tái phạm nặng. {reasonText}", 500), now, ct);
                    else
                        await _sanctions.SuspendAsync(target, req.SuspendDays!.Value, mod.UserId, report.ReportId, reasonText, now, ct);
                    break;

                case ReportDecision.Ban:
                    await _sanctions.BanAsync(target, mod.UserId, report.ReportId, reasonText, now, ct);
                    break;
            }

            // Các báo cáo khác đang chờ về cùng đối tượng coi như đã xử lý chung (tránh xử phạt nhiều lần cho một sự việc).
            var groupNote = $"Xử lý chung với báo cáo #{report.ReportId}";
            if (report.PostId is { } pid)
                await _reports.ResolvePendingForPostAsync(pid, report.ReportId, mod.UserId, groupNote, now, ct);
            else
                await _reports.ResolvePendingForUserAsync(targetUserId, report.ReportId, mod.UserId, groupNote, now, ct);

            // Chỉ cảnh cáo: tin bị tự ẩn do báo cáo (không phải do vi phạm đã xác nhận) được hiển thị lại.
            if (req.Decision == ReportDecision.Warning)
                await RestorePostIfCleanAsync(report, now, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _notifier.FlushAsync(ct);
        return (await _reports.GetDtoAsync(reportId, ct))!;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    /// <summary>Bỏ qua báo cáo: tin đang bị tự ẩn (không phải do vi phạm đã xác nhận) và hết báo cáo chờ thì hiện lại.</summary>
    private async Task RestorePostIfCleanAsync(Report report, DateTime now, CancellationToken ct)
    {
        if (report.Post is not { Status: PostStatus.Hidden, IsDeleted: false } post) return;
        if (await _reports.CountPendingForPostAsync(post.PostId, report.ReportId, ct) > 0) return;
        if (await _violations.PostHiddenByViolationAsync(post.PostId, ct)) return;
        var owner = await _users.GetAsync(post.UserId, ct);
        if (owner?.Status != UserStatus.Active) return;

        post.Status = post.ExpiredAt is { } exp && exp <= now ? PostStatus.Expired : PostStatus.Approved;
        post.RejectReason = null;
        _notifier.Queue(post.UserId, NotificationType.PostRestored,
            $"Tin \"{post.Title}\" đã được hiển thị lại sau khi xác minh không vi phạm.", post.PostId, $"/posts/{post.PostId}");
    }

    private void AddViolation(Report report, long userId, long handlerId, string level, string action, int? days, string? note, DateTime now, long? postId = null) =>
        _violations.Add(new ViolationHistory
        {
            UserId = userId,
            ReportId = report.ReportId,
            PostId = postId,
            Level = level,
            Action = action,
            SuspendDays = days,
            Note = note,
            HandledBy = handlerId,
            CreatedAt = now,
        });

    public Task<PagedResult<ViolationDto>> GetViolationsAsync(long? userId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize, 20);
        return _violations.GetPagedAsync(userId, page, pageSize, ct);
    }

    // ------------------------------------------------------------------ Ẩn đánh giá

    public async Task SetReviewHiddenAsync(CurrentUser mod, long reviewId, bool hidden, CancellationToken ct = default)
    {
        var review = await _reviews.GetAsync(reviewId, ct) ?? throw BusinessException.NotFound("Không tìm thấy đánh giá.");
        if (review.RevieweeId == mod.UserId || review.ReviewerId == mod.UserId)
            throw BusinessException.Forbidden("Bạn không được ẩn/hiện đánh giá liên quan đến chính mình."); // BR-09
        if (review.IsHidden == hidden) return;
        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _users.LockRowAsync(review.RevieweeId, ct); // tuần tự hóa với đánh giá mới cho cùng người (BR-06)
        review.IsHidden = hidden;
        await _uow.SaveChangesAsync(ct);
        await ReviewService.RecalculateRatingAsync(_reviews, _users, review.RevieweeId, ct); // BR-06
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
