using Microsoft.EntityFrameworkCore;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Repositories;

public class ConnectionRequestRepository : IConnectionRequestRepository
{
    private readonly AppDbContext _db;
    public ConnectionRequestRepository(AppDbContext db) => _db = db;

    public void Add(ConnectionRequest request) => _db.ConnectionRequests.Add(request);

    public Task<ConnectionRequest?> GetAsync(long requestId, CancellationToken ct = default) =>
        _db.ConnectionRequests.Include(r => r.Post).FirstOrDefaultAsync(r => r.RequestId == requestId, ct);

    private IQueryable<ConnectionRequestDto> Project(IQueryable<ConnectionRequest> query) => query.Select(r => new ConnectionRequestDto
    {
        RequestId = r.RequestId,
        PostId = r.PostId,
        PostTitle = r.Post.Title,
        PostType = r.Post.PostType,
        PostStatus = r.Post.Status,
        PostThumbnailUrl = r.Post.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault(),
        SenderId = r.SenderId,
        SenderName = r.Sender.FullName,
        SenderAvatarUrl = r.Sender.AvatarUrl,
        SenderRating = r.Sender.AvgRating,
        SenderReviewCount = r.Sender.ReviewCount,
        ReceiverId = r.ReceiverId,
        ReceiverName = r.Receiver.FullName,
        ReceiverAvatarUrl = r.Receiver.AvatarUrl,
        Message = r.Message,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        RespondedAt = r.RespondedAt,
        ConversationId = _db.Conversations.Where(c => c.RequestId == r.RequestId).Select(c => (long?)c.ConversationId).FirstOrDefault(),
    });

    public Task<ConnectionRequestDto?> GetDtoAsync(long requestId, CancellationToken ct = default) =>
        Project(_db.ConnectionRequests.AsNoTracking().Where(r => r.RequestId == requestId)).FirstOrDefaultAsync(ct);

    public Task<List<ConnectionRequestDto>> GetIncomingAsync(long userId, string? status, CancellationToken ct = default) =>
        Project(_db.ConnectionRequests.AsNoTracking()
                .Where(r => r.ReceiverId == userId && (status == null || r.Status == status))
                .OrderByDescending(r => r.CreatedAt))
            .ToListAsync(ct);

    public Task<List<ConnectionRequestDto>> GetOutgoingAsync(long userId, string? status, CancellationToken ct = default) =>
        Project(_db.ConnectionRequests.AsNoTracking()
                .Where(r => r.SenderId == userId && (status == null || r.Status == status))
                .OrderByDescending(r => r.CreatedAt))
            .ToListAsync(ct);

    public Task<ConnectionRequest?> GetActiveAsync(long postId, long senderId, CancellationToken ct = default) =>
        _db.ConnectionRequests.AsNoTracking().FirstOrDefaultAsync(r => r.PostId == postId && r.SenderId == senderId
            && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Accepted), ct);

    public Task<ConnectionRequest?> GetLatestAsync(long postId, long senderId, CancellationToken ct = default) =>
        _db.ConnectionRequests.AsNoTracking().Where(r => r.PostId == postId && r.SenderId == senderId)
            .OrderByDescending(r => r.RequestId).FirstOrDefaultAsync(ct);

    public Task<bool> HasAcceptedAsync(long postId, long senderId, CancellationToken ct = default) =>
        _db.ConnectionRequests.AnyAsync(r => r.PostId == postId && r.SenderId == senderId && r.Status == RequestStatus.Accepted, ct);

    public async Task<bool> TryTransitionAsync(long requestId, string from, string to, DateTime now, CancellationToken ct = default)
    {
        var rows = await _db.ConnectionRequests.Where(r => r.RequestId == requestId && r.Status == from)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, to).SetProperty(r => r.RespondedAt, now), ct);
        return rows == 1;
    }

    public Task<List<long>> GetPendingSenderIdsAsync(long postId, CancellationToken ct = default) =>
        _db.ConnectionRequests.Where(r => r.PostId == postId && r.Status == RequestStatus.Pending)
            .Select(r => r.SenderId).ToListAsync(ct);

    public Task<int> ExpirePendingForPostAsync(long postId, DateTime now, CancellationToken ct = default) =>
        _db.ConnectionRequests.Where(r => r.PostId == postId && r.Status == RequestStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RequestStatus.Expired).SetProperty(r => r.RespondedAt, now), ct);

    public async Task<List<long>> ExpirePendingForOwnerAsync(long ownerId, DateTime now, CancellationToken ct = default)
    {
        var pending = _db.ConnectionRequests.Where(r => r.ReceiverId == ownerId && r.Status == RequestStatus.Pending);
        var senders = await pending.Select(r => r.SenderId).Distinct().ToListAsync(ct);
        if (senders.Count > 0)
            await pending.ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RequestStatus.Expired).SetProperty(r => r.RespondedAt, now), ct);
        return senders;
    }
}

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _db;
    public ConversationRepository(AppDbContext db) => _db = db;

    public void Add(Conversation conversation) => _db.Conversations.Add(conversation);

    public Task<Conversation?> GetAsync(long conversationId, CancellationToken ct = default) =>
        _db.Conversations.FirstOrDefaultAsync(c => c.ConversationId == conversationId, ct);

    public Task<List<ConversationDto>> GetForUserAsync(long userId, CancellationToken ct = default) =>
        _db.Conversations.AsNoTracking()
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Select(c => new ConversationDto
            {
                ConversationId = c.ConversationId,
                RequestId = c.RequestId,
                PostId = c.Request.PostId,
                PostTitle = c.Request.Post.Title,
                OtherUserId = c.User1Id == userId ? c.User2Id : c.User1Id,
                OtherUserName = c.User1Id == userId ? c.User2.FullName : c.User1.FullName,
                OtherUserAvatarUrl = c.User1Id == userId ? c.User2.AvatarUrl : c.User1.AvatarUrl,
                LastMessage = _db.Messages.Where(m => m.ConversationId == c.ConversationId)
                    .OrderByDescending(m => m.MessageId).Select(m => m.Content).FirstOrDefault(),
                LastMessageSenderId = _db.Messages.Where(m => m.ConversationId == c.ConversationId)
                    .OrderByDescending(m => m.MessageId).Select(m => (long?)m.SenderId).FirstOrDefault(),
                LastMessageAt = c.LastMessageAt,
                CreatedAt = c.CreatedAt,
                UnreadCount = _db.Messages.Count(m => m.ConversationId == c.ConversationId && m.SenderId != userId && !m.IsRead),
            }).ToListAsync(ct);

    public void AddMessage(Message message) => _db.Messages.Add(message);

    public async Task<List<MessageDto>> GetMessagesAsync(long conversationId, long? beforeMessageId, int limit, CancellationToken ct = default)
    {
        var page = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && (beforeMessageId == null || m.MessageId < beforeMessageId))
            .OrderByDescending(m => m.MessageId)
            .Take(limit)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId, ConversationId = m.ConversationId, SenderId = m.SenderId,
                Content = m.Content, SentAt = m.SentAt, IsRead = m.IsRead,
            }).ToListAsync(ct);
        page.Reverse(); // trả theo thứ tự cũ → mới
        return page;
    }

    public Task<int> MarkReadAsync(long conversationId, long readerId, CancellationToken ct = default) =>
        _db.Messages.Where(m => m.ConversationId == conversationId && m.SenderId != readerId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), ct);
}

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;
    public ReviewRepository(AppDbContext db) => _db = db;

    public void Add(Review review) => _db.Reviews.Add(review);

    public Task<Review?> GetAsync(long reviewId, CancellationToken ct = default) =>
        _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId, ct);

    private static IQueryable<ReviewDto> Project(IQueryable<Review> query) => query.Select(r => new ReviewDto
    {
        ReviewId = r.ReviewId,
        RequestId = r.RequestId,
        ReviewerId = r.ReviewerId,
        ReviewerName = r.Reviewer.FullName,
        ReviewerAvatarUrl = r.Reviewer.AvatarUrl,
        RevieweeId = r.RevieweeId,
        RevieweeName = r.Reviewee.FullName,
        Rating = r.Rating,
        Comment = r.Comment,
        PostTitle = r.Request.Post.Title,
        IsHidden = r.IsHidden,
        CreatedAt = r.CreatedAt,
    });

    public Task<ReviewDto?> GetDtoAsync(long reviewId, CancellationToken ct = default) =>
        Project(_db.Reviews.AsNoTracking().Where(r => r.ReviewId == reviewId)).FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(long reviewerId, long requestId, CancellationToken ct = default) =>
        _db.Reviews.AnyAsync(r => r.ReviewerId == reviewerId && r.RequestId == requestId, ct);

    public async Task<HashSet<long>> GetReviewedRequestIdsAsync(long reviewerId, IEnumerable<long> requestIds, CancellationToken ct = default)
    {
        var ids = requestIds.ToList();
        var list = await _db.Reviews.Where(r => r.ReviewerId == reviewerId && ids.Contains(r.RequestId)).Select(r => r.RequestId).ToListAsync(ct);
        return list.ToHashSet();
    }

    public async Task<(decimal Average, int Count)> GetStatsAsync(long revieweeId, CancellationToken ct = default)
    {
        var stats = await _db.Reviews.Where(r => r.RevieweeId == revieweeId && !r.IsHidden)
            .GroupBy(r => r.RevieweeId)
            .Select(g => new { Avg = g.Average(r => (decimal)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(ct);
        return stats == null ? (0m, 0) : (stats.Avg, stats.Count);
    }

    public async Task<PagedResult<ReviewDto>> GetForUserAsync(long revieweeId, bool includeHidden, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Reviews.AsNoTracking().Where(r => r.RevieweeId == revieweeId && (includeHidden || !r.IsHidden));
        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return PagedResult<ReviewDto>.Create(items, page, pageSize, total);
    }
}
