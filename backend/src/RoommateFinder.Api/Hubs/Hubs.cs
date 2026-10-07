using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;

namespace RoommateFinder.Api.Hubs;

/// <summary>Chỉ đẩy từ server: sự kiện "notification" gửi tới đúng người nhận (Clients.User), không broadcast.</summary>
[Authorize]
public class NotificationHub : Hub
{
}

/// <summary>Chat realtime. Mọi kiểm tra quyền nằm trong ChatService (dùng chung với REST).</summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chat;

    public ChatHub(ChatService chat) => _chat = chat;

    public async Task<MessageDto> SendMessage(long conversationId, string content)
    {
        try
        {
            return await _chat.SendAsync(Context.User!.GetUserId(), conversationId, content, Context.ConnectionAborted);
        }
        catch (BusinessException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public async Task MarkRead(long conversationId)
    {
        try
        {
            await _chat.MarkReadAsync(Context.User!.GetUserId(), conversationId, Context.ConnectionAborted);
        }
        catch (BusinessException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _notifications;
    private readonly IHubContext<ChatHub> _chat;

    public SignalRRealtimeNotifier(IHubContext<NotificationHub> notifications, IHubContext<ChatHub> chat)
    {
        _notifications = notifications;
        _chat = chat;
    }

    public Task NotificationsCreatedAsync(IReadOnlyList<NotificationDto> notifications, CancellationToken ct = default) =>
        Task.WhenAll(notifications.Select(n => _notifications.Clients.User(n.UserId.ToString()).SendAsync("notification", n, ct)));

    public Task MessageSentAsync(MessageDto message, IReadOnlyList<long> recipientIds, CancellationToken ct = default) =>
        _chat.Clients.Users(recipientIds.Select(id => id.ToString()).ToList()).SendAsync("message", message, ct);

    public Task MessagesReadAsync(long conversationId, long readerId, long otherUserId, CancellationToken ct = default) =>
        _chat.Clients.Users(new[] { readerId.ToString(), otherUserId.ToString() })
            .SendAsync("messagesRead", new { conversationId, readerId }, ct);
}
