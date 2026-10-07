using RoommateFinder.Application.Abstractions;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Application.Services;

/// <summary>
/// BR-03, chạy định kỳ bởi tác vụ nền: tin approved quá hạn → expired (kèm yêu cầu pending),
/// tài khoản suspended hết hạn → active. Chạy lặp lại nhiều lần không làm sai dữ liệu.
/// </summary>
public class MaintenanceService
{
    private readonly IPostRepository _posts;
    private readonly IConnectionRequestRepository _requests;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationPublisher _notifier;

    public MaintenanceService(IPostRepository posts, IConnectionRequestRepository requests, IUserRepository users,
        IUnitOfWork uow, IDateTimeProvider clock, NotificationPublisher notifier)
    {
        _posts = posts;
        _requests = requests;
        _users = users;
        _uow = uow;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<(int ExpiredPosts, int ReactivatedUsers)> RunAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        await using var tx = await _uow.BeginTransactionAsync(ct);

        var due = await _posts.GetDueForExpiryAsync(now, ct);
        var expired = due.Count == 0 ? 0 : await _posts.MarkExpiredAsync(due.Select(p => p.PostId).ToList(), now, ct);
        foreach (var post in due)
        {
            var senders = await _requests.GetPendingSenderIdsAsync(post.PostId, ct);
            if (senders.Count > 0)
            {
                await _requests.ExpirePendingForPostAsync(post.PostId, now, ct);
                _notifier.QueueMany(senders, NotificationType.RequestExpired,
                    $"Tin \"{post.Title}\" đã hết hạn, yêu cầu kết nối của bạn hết hiệu lực.", post.PostId, "/connections?tab=outgoing");
            }
            _notifier.Queue(post.UserId, NotificationType.PostExpired,
                $"Tin \"{post.Title}\" đã hết hạn hiển thị. Bạn có thể gia hạn trong mục Tin của tôi.", post.PostId, "/my-posts?status=expired");
        }

        var reactivated = await _users.ReactivateExpiredSuspensionsAsync(now, ct);
        _notifier.QueueMany(reactivated, NotificationType.AccountStatus, "Thời hạn khóa đã kết thúc, tài khoản của bạn hoạt động trở lại.");

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _notifier.FlushAsync(ct);
        return (expired, reactivated.Count);
    }
}
