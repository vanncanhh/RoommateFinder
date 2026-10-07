using RoommateFinder.Application.Dtos;

namespace RoommateFinder.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(long userId, string email, string role, string securityStamp, int lifetimeMinutes);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public interface IFileStorage
{
    /// <summary>Lưu file với tên do server sinh, trả về URL tương đối (ví dụ /uploads/2026/10/abc.jpg).</summary>
    Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default);
    Task DeleteAsync(string url, CancellationToken ct = default);
}

/// <summary>Đẩy dữ liệu thời gian thực (SignalR) — chỉ gọi sau khi giao dịch đã commit.</summary>
public interface IRealtimeNotifier
{
    Task NotificationsCreatedAsync(IReadOnlyList<NotificationDto> notifications, CancellationToken ct = default);
    Task MessageSentAsync(MessageDto message, IReadOnlyList<long> recipientIds, CancellationToken ct = default);
    Task MessagesReadAsync(long conversationId, long readerId, long otherUserId, CancellationToken ct = default);
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

/// <summary>Đọc tham số nghiệp vụ trong SystemConfigs (có cache).</summary>
public interface ISystemSettings
{
    Task<int> GetIntAsync(string key, CancellationToken ct = default);
    void Invalidate();
}

/// <summary>Đường dẫn frontend để ghép link trong email (đặt lại mật khẩu).</summary>
public interface IAppLinks
{
    string ResetPasswordUrl(string token);
}
