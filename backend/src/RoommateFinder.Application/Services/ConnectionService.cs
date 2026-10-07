using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

public class ConnectionService
{
    private readonly IConnectionRequestRepository _requests;
    private readonly IPostRepository _posts;
    private readonly IConversationRepository _conversations;
    private readonly IReviewRepository _reviews;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationPublisher _notifier;

    public ConnectionService(IConnectionRequestRepository requests, IPostRepository posts, IConversationRepository conversations,
        IReviewRepository reviews, IUserRepository users, IUnitOfWork uow, ISystemSettings settings,
        IDateTimeProvider clock, NotificationPublisher notifier)
    {
        _requests = requests;
        _posts = posts;
        _conversations = conversations;
        _reviews = reviews;
        _users = users;
        _uow = uow;
        _settings = settings;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<ConnectionRequestDto> SendAsync(CurrentUser user, CreateConnectionRequest req, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var message = string.IsNullOrWhiteSpace(req.Message) ? null : req.Message.Trim();
        if (message?.Length > 500) throw BusinessException.Validation("Lời nhắn tối đa 500 ký tự.");

        var post = await _posts.GetAsync(req.PostId, ct);
        if (post == null || post.IsDeleted || !PostService.IsPublic(post.Status, post.ExpiredAt, now))
            throw BusinessException.NotFound("Tin đăng không tồn tại hoặc không còn hiển thị.");
        if (post.UserId == user.UserId) throw BusinessException.Validation("Bạn không thể gửi yêu cầu tới tin của chính mình."); // BR-04
        if (post.NeededOccupants <= 0) throw BusinessException.Conflict("Tin đã đủ người.");

        var active = await _requests.GetActiveAsync(post.PostId, user.UserId, ct);
        if (active != null) throw BusinessException.Conflict(ActiveMessage(active.Status));

        var request = new ConnectionRequest
        {
            PostId = post.PostId,
            SenderId = user.UserId,
            ReceiverId = post.UserId,
            Message = message,
            Status = RequestStatus.Pending,
            CreatedAt = now,
        };
        _requests.Add(request);
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException) // UQ_Request_SenderPost_Active — gửi trùng song song (quyết định d)
        {
            throw BusinessException.Conflict(ActiveMessage(RequestStatus.Pending));
        }

        var sender = await _users.GetCurrentUserAsync(user.UserId, ct);
        _notifier.Queue(post.UserId, NotificationType.RequestNew,
            $"{sender?.FullName ?? "Một người dùng"} muốn kết nối về tin \"{post.Title}\".", request.RequestId, "/connections?tab=incoming");
        await _uow.SaveChangesAsync(ct);
        await _notifier.FlushAsync(ct);
        return (await _requests.GetDtoAsync(request.RequestId, ct))!;
    }

    private static string ActiveMessage(string status) => status == RequestStatus.Accepted
        ? "Bạn đã được chấp nhận kết nối với tin này."
        : "Bạn đã gửi yêu cầu cho tin này và yêu cầu đang chờ xử lý.";

    public async Task<List<ConnectionRequestDto>> GetIncomingAsync(CurrentUser user, string? status, CancellationToken ct = default)
    {
        EnsureStatus(status);
        return await EnrichReviewInfoAsync(user, await _requests.GetIncomingAsync(user.UserId, status, ct), ct);
    }

    public async Task<List<ConnectionRequestDto>> GetOutgoingAsync(CurrentUser user, string? status, CancellationToken ct = default)
    {
        EnsureStatus(status);
        return await EnrichReviewInfoAsync(user, await _requests.GetOutgoingAsync(user.UserId, status, ct), ct);
    }

    private static void EnsureStatus(string? status)
    {
        if (status != null && !RequestStatus.All.Contains(status)) throw BusinessException.Validation("Trạng thái không hợp lệ.");
    }

    /// <summary>BR-05: chỉ đánh giá khi đã accepted đủ REVIEW_MIN_DAYS ngày, mỗi bên một lần mỗi lượt kết nối.</summary>
    private async Task<List<ConnectionRequestDto>> EnrichReviewInfoAsync(CurrentUser user, List<ConnectionRequestDto> list, CancellationToken ct)
    {
        var accepted = list.Where(r => r.Status == RequestStatus.Accepted).ToList();
        if (accepted.Count == 0) return list;
        var minDays = await _settings.GetIntAsync(ConfigKeys.ReviewMinDays, ct);
        var reviewed = await _reviews.GetReviewedRequestIdsAsync(user.UserId, accepted.Select(r => r.RequestId), ct);
        var now = _clock.UtcNow;
        foreach (var r in accepted)
        {
            r.HasReviewed = reviewed.Contains(r.RequestId);
            r.ReviewAvailableAt = (r.RespondedAt ?? r.CreatedAt).AddDays(minDays);
            r.CanReview = !r.HasReviewed && now >= r.ReviewAvailableAt;
        }
        return list;
    }

    /// <summary>
    /// Quyết định (c): mọi bước trong một giao dịch; trừ chỗ bằng UPDATE có điều kiện và kiểm tra số dòng ảnh hưởng,
    /// không đọc-rồi-ghi. Hai chủ tin bấm đồng thời thì chỉ một bên lấy được chỗ cuối cùng, bên kia nhận 409.
    /// </summary>
    public async Task<ConnectionRequestDto> AcceptAsync(CurrentUser user, long requestId, CancellationToken ct = default)
    {
        var request = await GetForReceiverAsync(user, requestId, ct);
        var now = _clock.UtcNow;
        var post = request.Post;
        var sender = await _users.GetStatusAsync(request.SenderId, ct);
        if (sender == null || User.IsLockedStatus(sender.Status, sender.SuspendedUntil, now))
            throw BusinessException.Conflict("Người gửi đang bị khóa tài khoản, không thể chấp nhận yêu cầu này.");

        await using (var tx = await _uow.BeginTransactionAsync(ct))
        {
            // Khóa dòng tin đăng TRƯỚC: mọi giao dịch chấp nhận cùng tin xếp hàng tại đây, nên không có giao dịch nào
            // vừa giữ khóa một yêu cầu vừa chờ khóa tin (tránh deadlock với bước hết hạn các yêu cầu pending bên dưới).
            if (!await _posts.TryTakeSlotAsync(post.PostId, now, ct))
                throw BusinessException.Conflict("Tin đã đủ người hoặc không còn hiển thị, không thể chấp nhận thêm.");

            if (!await _requests.TryTransitionAsync(requestId, RequestStatus.Pending, RequestStatus.Accepted, now, ct))
                throw BusinessException.Conflict("Yêu cầu không còn ở trạng thái chờ (người gửi có thể đã rút lại hoặc tin vừa đủ người).");
            // Thoát bằng exception ở trên → giao dịch dispose chưa commit → rollback, trả lại chỗ vừa trừ.

            var conversation = new Conversation
            {
                RequestId = requestId,
                User1Id = request.SenderId,
                User2Id = request.ReceiverId,
                CreatedAt = now,
            };
            _conversations.Add(conversation);
            await _uow.SaveChangesAsync(ct);

            _notifier.Queue(request.SenderId, NotificationType.RequestAccepted,
                $"Yêu cầu kết nối về tin \"{post.Title}\" đã được chấp nhận. Bạn có thể trò chuyện ngay.",
                requestId, $"/chat/{conversation.ConversationId}");

            if (await _posts.CloseIfFullAsync(post.PostId, now, ct))
            {
                var others = await _requests.GetPendingSenderIdsAsync(post.PostId, ct);
                await _requests.ExpirePendingForPostAsync(post.PostId, now, ct);
                _notifier.QueueMany(others, NotificationType.RequestExpired,
                    $"Tin \"{post.Title}\" đã đủ người, yêu cầu kết nối của bạn hết hiệu lực.", post.PostId, "/connections?tab=outgoing");
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        await _notifier.FlushAsync(ct); // chỉ đẩy realtime sau khi commit
        return (await _requests.GetDtoAsync(requestId, ct))!;
    }

    public async Task<ConnectionRequestDto> RejectAsync(CurrentUser user, long requestId, CancellationToken ct = default)
    {
        var request = await GetForReceiverAsync(user, requestId, ct);
        if (!await _requests.TryTransitionAsync(requestId, RequestStatus.Pending, RequestStatus.Rejected, _clock.UtcNow, ct))
            throw BusinessException.Conflict("Yêu cầu không còn ở trạng thái chờ.");
        _notifier.Queue(request.SenderId, NotificationType.RequestRejected,
            $"Yêu cầu kết nối về tin \"{request.Post.Title}\" đã bị từ chối.", requestId, "/connections?tab=outgoing");
        await _uow.SaveChangesAsync(ct);
        await _notifier.FlushAsync(ct);
        return (await _requests.GetDtoAsync(requestId, ct))!;
    }

    public async Task<ConnectionRequestDto> CancelAsync(CurrentUser user, long requestId, CancellationToken ct = default)
    {
        var request = await _requests.GetAsync(requestId, ct) ?? throw BusinessException.NotFound("Không tìm thấy yêu cầu.");
        if (request.SenderId != user.UserId) throw BusinessException.Forbidden("Bạn chỉ rút lại được yêu cầu của mình.");
        if (!await _requests.TryTransitionAsync(requestId, RequestStatus.Pending, RequestStatus.Cancelled, _clock.UtcNow, ct))
            throw BusinessException.Conflict("Chỉ rút lại được yêu cầu đang chờ.");
        _notifier.Queue(request.ReceiverId, NotificationType.RequestCancelled,
            $"Một yêu cầu kết nối về tin \"{request.Post.Title}\" đã được người gửi rút lại.", requestId, "/connections?tab=incoming");
        await _uow.SaveChangesAsync(ct);
        await _notifier.FlushAsync(ct);
        return (await _requests.GetDtoAsync(requestId, ct))!;
    }

    private async Task<ConnectionRequest> GetForReceiverAsync(CurrentUser user, long requestId, CancellationToken ct)
    {
        var request = await _requests.GetAsync(requestId, ct) ?? throw BusinessException.NotFound("Không tìm thấy yêu cầu.");
        if (request.ReceiverId != user.UserId) throw BusinessException.Forbidden("Chỉ chủ tin được xử lý yêu cầu này.");
        if (request.Status != RequestStatus.Pending) throw BusinessException.Conflict("Yêu cầu không còn ở trạng thái chờ.");
        return request;
    }
}
