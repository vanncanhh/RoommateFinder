using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

/// <summary>
/// Gom thông báo trong một request: <see cref="Queue"/> thêm bản ghi vào CSDL cùng giao dịch của nghiệp vụ,
/// <see cref="FlushAsync"/> đẩy realtime sau khi đã lưu/commit — không đẩy thông báo cho giao dịch chưa chắc thành công.
/// </summary>
public class NotificationPublisher
{
    private readonly INotificationRepository _notifications;
    private readonly IRealtimeNotifier _realtime;
    private readonly IDateTimeProvider _clock;
    private readonly List<Notification> _pending = new();

    public NotificationPublisher(INotificationRepository notifications, IRealtimeNotifier realtime, IDateTimeProvider clock)
    {
        _notifications = notifications;
        _realtime = realtime;
        _clock = clock;
    }

    public void Queue(long userId, string type, string content, long? relatedId = null, string? link = null)
    {
        var n = new Notification
        {
            UserId = userId,
            Type = type,
            Content = content.Length > 500 ? content[..497] + "..." : content,
            RelatedId = relatedId,
            Link = link,
            CreatedAt = _clock.UtcNow,
        };
        _notifications.Add(n);
        _pending.Add(n);
    }

    public void QueueMany(IEnumerable<long> userIds, string type, string content, long? relatedId = null, string? link = null)
    {
        foreach (var id in userIds.Distinct()) Queue(id, type, content, relatedId, link);
    }

    /// <summary>Gọi sau SaveChanges/Commit. Lỗi đẩy realtime không làm hỏng nghiệp vụ đã lưu.</summary>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        if (_pending.Count == 0) return;
        var dtos = _pending.Select(ToDto).ToList();
        _pending.Clear();
        try
        {
            await _realtime.NotificationsCreatedAsync(dtos, ct);
        }
        catch
        {
            // Người dùng vẫn thấy thông báo khi tải lại danh sách.
        }
    }

    public static NotificationDto ToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        UserId = n.UserId,
        Type = n.Type,
        Content = n.Content,
        RelatedId = n.RelatedId,
        Link = n.Link,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
    };
}
