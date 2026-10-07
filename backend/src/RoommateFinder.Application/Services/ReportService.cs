using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

public class ReportService
{
    public const string AutoHideReason = "Tạm ẩn do nhận nhiều báo cáo, đang chờ kiểm duyệt viên xác minh.";

    private readonly IReportRepository _reports;
    private readonly ICatalogRepository _catalog;
    private readonly IPostRepository _posts;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationPublisher _notifier;
    private readonly IConnectionRequestRepository _requests;

    public ReportService(IReportRepository reports, ICatalogRepository catalog, IPostRepository posts, IUserRepository users,
        IUnitOfWork uow, ISystemSettings settings, IDateTimeProvider clock, NotificationPublisher notifier,
        IConnectionRequestRepository requests)
    {
        _requests = requests;
        _reports = reports;
        _catalog = catalog;
        _posts = posts;
        _users = users;
        _uow = uow;
        _settings = settings;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task CreateAsync(CurrentUser user, CreateReportRequest req, CancellationToken ct = default)
    {
        if (req.PostId.HasValue == req.ReportedUserId.HasValue)
            throw BusinessException.Validation("Báo cáo phải nhắm đúng một đối tượng: tin đăng hoặc người dùng.");
        var description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        if (description?.Length > 500) throw BusinessException.Validation("Mô tả tối đa 500 ký tự.");

        var reason = await _catalog.GetReportReasonAsync(req.ReasonId, ct);
        var targetKind = req.PostId.HasValue ? ReasonAppliesTo.Post : ReasonAppliesTo.User;
        if (reason is not { IsActive: true } || (reason.AppliesTo != ReasonAppliesTo.Both && reason.AppliesTo != targetKind))
            throw BusinessException.Validation("Lý do báo cáo không hợp lệ.");

        var now = _clock.UtcNow;
        Post? post = null;
        if (req.PostId is { } postId)
        {
            post = await _posts.GetAsync(postId, ct);
            if (post == null || post.IsDeleted) throw BusinessException.NotFound("Không tìm thấy tin đăng.");
            if (post.UserId == user.UserId) throw BusinessException.Validation("Bạn không thể báo cáo tin của chính mình.");
            // Chỉ báo cáo tin mình xem được: tin công khai, hoặc tin mình đã được chấp nhận kết nối.
            if (!PostService.IsPublic(post.Status, post.ExpiredAt, now) && !await _requests.HasAcceptedAsync(post.PostId, user.UserId, ct))
                throw BusinessException.NotFound("Không tìm thấy tin đăng.");
        }
        else
        {
            var reportedId = req.ReportedUserId!.Value;
            if (reportedId == user.UserId) throw BusinessException.Validation("Bạn không thể báo cáo chính mình.");
            if (await _users.GetStatusAsync(reportedId, ct) == null) throw BusinessException.NotFound("Không tìm thấy người dùng.");
            if (await _reports.HasPendingUserReportAsync(user.UserId, reportedId, ct))
                throw BusinessException.Conflict("Bạn đã báo cáo người dùng này, báo cáo đang chờ xử lý.");
        }

        var report = new Report
        {
            ReporterId = user.UserId,
            PostId = req.PostId,
            ReportedUserId = req.ReportedUserId,
            ReasonId = reason.ReasonId,
            Description = description,
            Status = ReportStatus.Pending,
            CreatedAt = now,
        };

        await using (var tx = await _uow.BeginTransactionAsync(ct))
        {
            _reports.Add(report);
            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch (DuplicateKeyException) // UQ_Report_ReporterPost_Pending
            {
                throw BusinessException.Conflict("Bạn đã báo cáo tin này, báo cáo đang chờ xử lý.");
            }

            // Không báo cho chính người bị báo cáo nếu họ là kiểm duyệt viên (BR-09).
            var targetId = post?.UserId ?? req.ReportedUserId!.Value;
            var mods = (await _users.GetActiveIdsByRoleAsync(Roles.Moderator, ct)).Where(id => id != targetId);
            _notifier.QueueMany(mods, NotificationType.ReportNew, $"Có báo cáo vi phạm mới: {reason.Name}.", report.ReportId, "/moderator/reports");

            // BR-07: đủ số người báo cáo khác nhau trong cửa sổ thời gian thì tự ẩn tin chờ xác minh.
            if (post is { Status: PostStatus.Approved })
            {
                var threshold = await _settings.GetIntAsync(ConfigKeys.AutoHideReportCount, ct);
                var windowHours = await _settings.GetIntAsync(ConfigKeys.AutoHideWindowHours, ct);
                var reporters = await _reports.CountDistinctReportersAsync(post.PostId, now.AddHours(-windowHours), ct);
                if (reporters >= threshold)
                {
                    post.Status = PostStatus.Hidden;
                    post.RejectReason = AutoHideReason;
                    post.UpdatedAt = now;
                    _notifier.Queue(post.UserId, NotificationType.PostHidden,
                        $"Tin \"{post.Title}\" tạm bị ẩn do nhận nhiều báo cáo, đang chờ xác minh.", post.PostId, "/my-posts?status=hidden");
                }
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        await _notifier.FlushAsync(ct);
    }
}
