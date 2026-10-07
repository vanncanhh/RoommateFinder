using RoommateFinder.Application.Abstractions;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.Application.Common;

namespace RoommateFinder.Application.Services;

/// <summary>
/// Khóa tài khoản dùng chung cho Moderator (xử lý báo cáo) và Admin (quản lý người dùng).
/// Không tự lưu — người gọi SaveChanges/Commit rồi Flush thông báo.
/// </summary>
public class AccountSanctions
{
    private readonly IPostRepository _posts;
    private readonly IViolationRepository _violations;
    private readonly NotificationPublisher _notifier;
    private readonly IConnectionRequestRepository _requests;

    public AccountSanctions(IPostRepository posts, IViolationRepository violations, NotificationPublisher notifier,
        IConnectionRequestRepository requests)
    {
        _requests = requests;
        _posts = posts;
        _violations = violations;
        _notifier = notifier;
    }

    public async Task SuspendAsync(User target, int days, long handlerId, long? reportId, string reason, DateTime now, CancellationToken ct)
    {
        target.Status = UserStatus.Suspended;
        target.SuspendedUntil = now.AddDays(days);
        target.UpdatedAt = now;
        await _posts.HideApprovedOfUserAsync(target.UserId, "Tài khoản đang bị tạm khóa", now, ct); // BR-08
        await ExpireIncomingRequestsAsync(target.UserId, now, ct);
        _violations.Add(new ViolationHistory
        {
            UserId = target.UserId, ReportId = reportId, Level = ViolationLevel.Severe,
            Action = ViolationAction.SuspendAccount, SuspendDays = days, Note = reason, HandledBy = handlerId, CreatedAt = now,
        });
        _notifier.Queue(target.UserId, NotificationType.AccountStatus,
            $"Tài khoản của bạn bị tạm khóa {days} ngày (đến {VnTime.Format(target.SuspendedUntil.Value)}). Lý do: {reason}");
    }

    public async Task BanAsync(User target, long handlerId, long? reportId, string reason, DateTime now, CancellationToken ct)
    {
        target.Status = UserStatus.Banned;
        target.SuspendedUntil = null;
        target.UpdatedAt = now;
        await _posts.HideApprovedOfUserAsync(target.UserId, "Tài khoản đã bị khóa", now, ct);
        await ExpireIncomingRequestsAsync(target.UserId, now, ct);
        _violations.Add(new ViolationHistory
        {
            UserId = target.UserId, ReportId = reportId, Level = ViolationLevel.Severe,
            Action = ViolationAction.BanAccount, Note = reason, HandledBy = handlerId, CreatedAt = now,
        });
        _notifier.Queue(target.UserId, NotificationType.AccountStatus, $"Tài khoản của bạn đã bị khóa vĩnh viễn. Lý do: {reason}");
    }

    /// <summary>Tin của người bị khóa đã ẩn → các yêu cầu đang chờ gửi tới họ hết hiệu lực.</summary>
    private async Task ExpireIncomingRequestsAsync(long ownerId, DateTime now, CancellationToken ct)
    {
        var senders = await _requests.ExpirePendingForOwnerAsync(ownerId, now, ct);
        _notifier.QueueMany(senders, NotificationType.RequestExpired,
            "Một yêu cầu kết nối của bạn hết hiệu lực vì chủ tin tạm thời không hoạt động.", null, "/connections?tab=outgoing");
    }
}
