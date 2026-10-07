using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using RoommateFinder.Application.Dtos;

namespace RoommateFinder.Web.Services;

/// <summary>
/// Kết nối SignalR tới /hubs/notifications và /hubs/chat khi đã đăng nhập; tự kết nối lại; ngắt khi đăng xuất.
/// Component đăng ký các sự kiện bên dưới và nhớ hủy đăng ký trong Dispose.
/// </summary>
public class RealtimeService : IAsyncDisposable
{
    private readonly AuthSession _session;
    private readonly Uri _baseAddress;
    private HubConnection? _notifications;
    private HubConnection? _chat;
    private string? _connectedToken;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RealtimeService(AuthSession session, HttpClient http)
    {
        _session = session;
        _baseAddress = http.BaseAddress!;
        _session.Changed += () => _ = SyncAsync();
    }

    public event Action<NotificationDto>? NotificationReceived;
    public event Action<MessageDto>? MessageReceived;
    public event Action<long, long>? MessagesRead; // conversationId, readerId
    /// <summary>Phát khi trạng thái kết nối chat đổi; true = đã (tái) kết nối → nên tải lại dữ liệu có thể bị lỡ.</summary>
    public event Action<bool>? ChatConnectionChanged;
    public event Action<bool>? NotificationConnectionChanged;

    public bool IsChatConnected => _chat?.State == HubConnectionState.Connected;
    public bool IsNotificationConnected => _notifications?.State == HubConnectionState.Connected;

    /// <summary>Gửi tin nhắn qua hub; ném <see cref="ApiException"/> với thông điệp tiếng Việt khi server từ chối.</summary>
    public async Task<MessageDto> SendMessageAsync(long conversationId, string content)
    {
        if (_chat is not { State: HubConnectionState.Connected })
            throw new InvalidOperationException("Chưa kết nối máy chủ chat.");
        try
        {
            return await _chat.InvokeAsync<MessageDto>("SendMessage", conversationId, content);
        }
        catch (HubException ex)
        {
            var msg = ex.Message.Contains("HubException: ") ? ex.Message[(ex.Message.LastIndexOf("HubException: ") + 14)..] : ex.Message;
            throw new ApiException(System.Net.HttpStatusCode.BadRequest, "HUB", msg);
        }
    }

    public async Task MarkReadAsync(long conversationId)
    {
        if (_chat is { State: HubConnectionState.Connected }) await _chat.InvokeAsync("MarkRead", conversationId);
    }

    /// <summary>Gọi khi ứng dụng khởi động và mỗi khi đăng nhập/đăng xuất.</summary>
    public async Task SyncAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var token = _session.IsAuthenticated ? _session.Token : null;
            if (token == _connectedToken) return;
            await StopAsync();
            if (token == null) return;
            _connectedToken = token;
            _notifications = Build("hubs/notifications");
            _notifications.On<NotificationDto>("notification", n => NotificationReceived?.Invoke(n));
            Hook(_notifications, c => NotificationConnectionChanged?.Invoke(c));

            _chat = Build("hubs/chat");
            _chat.On<MessageDto>("message", m => MessageReceived?.Invoke(m));
            _chat.On<ReadEvent>("messagesRead", e => MessagesRead?.Invoke(e.ConversationId, e.ReaderId));
            Hook(_chat, c => ChatConnectionChanged?.Invoke(c));

            _ = StartWithRetryAsync(_notifications, c => NotificationConnectionChanged?.Invoke(c));
            _ = StartWithRetryAsync(_chat, c => ChatConnectionChanged?.Invoke(c));
        }
        finally
        {
            _lock.Release();
        }
    }

    private HubConnection Build(string path) => new HubConnectionBuilder()
        .WithUrl(new Uri(_baseAddress, path), o => o.AccessTokenProvider = () => Task.FromResult(_session.Token))
        .WithAutomaticReconnect(new RetryForever())
        .Build();

    private static void Hook(HubConnection conn, Action<bool> changed)
    {
        conn.Reconnecting += _ => { changed(false); return Task.CompletedTask; };
        conn.Reconnected += _ => { changed(true); return Task.CompletedTask; };
        conn.Closed += _ => { changed(false); return Task.CompletedTask; };
    }

    /// <summary>Lần kết nối đầu thất bại thì WithAutomaticReconnect không áp dụng → tự thử lại.</summary>
    private async Task StartWithRetryAsync(HubConnection conn, Action<bool> changed)
    {
        for (var attempt = 0; ; attempt++)
        {
            if (conn != _chat && conn != _notifications) return; // đã đăng xuất / đổi tài khoản
            try
            {
                await conn.StartAsync();
                changed(true);
                return;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 2 * (attempt + 1))));
            }
        }
    }

    private async Task StopAsync()
    {
        var old = new[] { _notifications, _chat };
        _notifications = null;
        _chat = null;
        _connectedToken = null;
        foreach (var conn in old.Where(c => c != null))
        {
            try { await conn!.DisposeAsync(); } catch { /* bỏ qua */ }
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private record ReadEvent(long ConversationId, long ReaderId);

    private sealed class RetryForever : IRetryPolicy
    {
        private static readonly int[] Delays = { 0, 2, 5, 10 };
        public TimeSpan? NextRetryDelay(RetryContext ctx) =>
            TimeSpan.FromSeconds(ctx.PreviousRetryCount < Delays.Length ? Delays[ctx.PreviousRetryCount] : 15);
    }
}
