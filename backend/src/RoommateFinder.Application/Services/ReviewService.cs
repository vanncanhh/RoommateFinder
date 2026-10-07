using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

public class ReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IConnectionRequestRepository _requests;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationPublisher _notifier;

    public ReviewService(IReviewRepository reviews, IConnectionRequestRepository requests, IUserRepository users,
        IUnitOfWork uow, ISystemSettings settings, IDateTimeProvider clock, NotificationPublisher notifier)
    {
        _reviews = reviews;
        _requests = requests;
        _users = users;
        _uow = uow;
        _settings = settings;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<ReviewDto> CreateAsync(CurrentUser user, CreateReviewRequest req, CancellationToken ct = default)
    {
        if (req.Rating is < 1 or > 5) throw BusinessException.Validation("Số sao phải từ 1 đến 5.");
        var comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim();
        if (comment?.Length > 500) throw BusinessException.Validation("Nhận xét tối đa 500 ký tự.");

        var request = await _requests.GetAsync(req.RequestId, ct) ?? throw BusinessException.NotFound("Không tìm thấy lượt kết nối.");
        if (request.SenderId != user.UserId && request.ReceiverId != user.UserId)
            throw BusinessException.Forbidden("Bạn không thuộc lượt kết nối này.");
        if (request.Status != RequestStatus.Accepted)
            throw BusinessException.Conflict("Chỉ đánh giá được khi yêu cầu kết nối đã được chấp nhận.");

        // BR-05
        var now = _clock.UtcNow;
        var minDays = await _settings.GetIntAsync(ConfigKeys.ReviewMinDays, ct);
        var availableAt = (request.RespondedAt ?? request.CreatedAt).AddDays(minDays);
        if (now < availableAt)
            throw BusinessException.Conflict($"Bạn có thể đánh giá từ {VnTime.Format(availableAt)} (sau {minDays} ngày kể từ khi kết nối).");
        if (await _reviews.ExistsAsync(user.UserId, request.RequestId, ct))
            throw BusinessException.Conflict("Bạn đã đánh giá cho lượt kết nối này.");

        var revieweeId = request.SenderId == user.UserId ? request.ReceiverId : request.SenderId;
        var review = new Review
        {
            RequestId = request.RequestId,
            ReviewerId = user.UserId,
            RevieweeId = revieweeId,
            Rating = req.Rating,
            Comment = comment,
            CreatedAt = now,
        };

        await using (var tx = await _uow.BeginTransactionAsync(ct))
        {
            // Khóa dòng người được đánh giá trước: hai đánh giá song song cho cùng người tính điểm tuần tự (BR-06).
            await _users.LockRowAsync(revieweeId, ct);
            _reviews.Add(review);
            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch (DuplicateKeyException) // UQ_Review_ReviewerRequest
            {
                throw BusinessException.Conflict("Bạn đã đánh giá cho lượt kết nối này.");
            }
            await RecalculateRatingAsync(_reviews, _users, revieweeId, ct); // BR-06, cùng giao dịch
            _notifier.Queue(revieweeId, NotificationType.ReviewNew,
                $"Bạn vừa nhận được đánh giá {req.Rating} sao.", review.ReviewId, $"/users/{revieweeId}");
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        await _notifier.FlushAsync(ct);
        return (await _reviews.GetDtoAsync(review.ReviewId, ct))!;
    }

    /// <summary>Tính lại AvgRating, ReviewCount từ các đánh giá đang hiển thị (người gọi SaveChanges).</summary>
    public static async Task RecalculateRatingAsync(IReviewRepository reviews, IUserRepository users, long userId, CancellationToken ct)
    {
        var (average, count) = await reviews.GetStatsAsync(userId, ct);
        var user = await users.GetAsync(userId, ct);
        if (user == null) return;
        user.AvgRating = Math.Round(average, 2);
        user.ReviewCount = count;
    }
}
