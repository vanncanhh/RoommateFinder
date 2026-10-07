using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;

namespace RoommateFinder.Application.Services;

/// <summary>Người dùng chỉ đọc và đánh dấu thông báo của chính mình (truy vấn luôn lọc theo UserId).</summary>
public class NotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications) => _notifications = notifications;

    public Task<PagedResult<NotificationDto>> GetAsync(long userId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize, 20);
        return _notifications.GetPagedAsync(userId, page, pageSize, ct);
    }

    public Task<int> CountUnreadAsync(long userId, CancellationToken ct = default) => _notifications.CountUnreadAsync(userId, ct);

    public async Task MarkReadAsync(long userId, long notificationId, CancellationToken ct = default)
    {
        if (await _notifications.MarkReadAsync(userId, notificationId, ct) == 0)
            throw BusinessException.NotFound("Không tìm thấy thông báo.");
    }

    public Task MarkAllReadAsync(long userId, CancellationToken ct = default) => _notifications.MarkAllReadAsync(userId, ct);
}
