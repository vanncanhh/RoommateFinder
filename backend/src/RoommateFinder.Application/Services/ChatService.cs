using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

public class ChatService
{
    private readonly IConversationRepository _conversations;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;
    private readonly IDateTimeProvider _clock;

    public ChatService(IConversationRepository conversations, IUserRepository users, IUnitOfWork uow,
        IRealtimeNotifier realtime, IDateTimeProvider clock)
    {
        _conversations = conversations;
        _users = users;
        _uow = uow;
        _realtime = realtime;
        _clock = clock;
    }

    public Task<List<ConversationDto>> GetConversationsAsync(long userId, CancellationToken ct = default) =>
        _conversations.GetForUserAsync(userId, ct);

    public async Task<List<MessageDto>> GetMessagesAsync(long userId, long conversationId, long? before, int limit, CancellationToken ct = default)
    {
        await GetMemberConversationAsync(userId, conversationId, ct);
        return await _conversations.GetMessagesAsync(conversationId, before, Math.Clamp(limit, 1, 100), ct);
    }

    /// <summary>Dùng chung cho REST và ChatHub — kiểm tra thành viên ở cả hai đường vào.</summary>
    public async Task<MessageDto> SendAsync(long userId, long conversationId, string? content, CancellationToken ct = default)
    {
        var text = (content ?? "").Trim();
        if (text.Length is < 1 or > 1000) throw BusinessException.Validation("Tin nhắn phải từ 1 đến 1000 ký tự.");
        var conversation = await GetMemberConversationAsync(userId, conversationId, ct);

        // Hub không đi qua middleware kiểm tra trạng thái nên kiểm tra lại cả hai phía ở đây.
        var now = _clock.UtcNow;
        var me = await _users.GetStatusAsync(userId, ct);
        if (me == null || User.IsLockedStatus(me.Status, me.SuspendedUntil, now)) throw new BusinessException(ErrorCode.Forbidden, "Tài khoản của bạn đang bị khóa.", "ACCOUNT_LOCKED");
        var otherId = conversation.OtherMember(userId);
        var other = await _users.GetStatusAsync(otherId, ct);
        if (other == null || User.IsLockedStatus(other.Status, other.SuspendedUntil, now)) throw BusinessException.Conflict("Người dùng này đang bị khóa, không thể nhận tin nhắn.");

        var message = new Message { ConversationId = conversationId, SenderId = userId, Content = text, SentAt = now };
        _conversations.AddMessage(message);
        conversation.LastMessageAt = now;
        await _uow.SaveChangesAsync(ct);

        var dto = new MessageDto
        {
            MessageId = message.MessageId,
            ConversationId = conversationId,
            SenderId = userId,
            Content = text,
            SentAt = now,
            IsRead = false,
        };
        try
        {
            await _realtime.MessageSentAsync(dto, new[] { userId, otherId }, ct);
        }
        catch
        {
            // Phía nhận sẽ thấy khi tải lại (phương án dự phòng tải định kỳ).
        }
        return dto;
    }

    public async Task MarkReadAsync(long userId, long conversationId, CancellationToken ct = default)
    {
        var conversation = await GetMemberConversationAsync(userId, conversationId, ct);
        if (await _conversations.MarkReadAsync(conversationId, userId, ct) > 0)
        {
            try
            {
                await _realtime.MessagesReadAsync(conversationId, userId, conversation.OtherMember(userId), ct);
            }
            catch
            {
                // không ảnh hưởng dữ liệu
            }
        }
    }

    private async Task<Conversation> GetMemberConversationAsync(long userId, long conversationId, CancellationToken ct)
    {
        var conversation = await _conversations.GetAsync(conversationId, ct);
        if (conversation == null || !conversation.HasMember(userId)) throw BusinessException.NotFound("Không tìm thấy cuộc trò chuyện.");
        return conversation;
    }
}
