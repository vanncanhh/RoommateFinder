using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly AppDbContext _db;
    public PostRepository(AppDbContext db) => _db = db;

    /// <summary>Projection danh sách; tọa độ hiệu lực = của tin, nếu không có thì tâm khu vực (BR-01).</summary>
    internal static readonly Expression<Func<Post, PostSummaryDto>> SummaryProjection = p => new PostSummaryDto
    {
        PostId = p.PostId,
        PostType = p.PostType,
        Title = p.Title,
        Price = p.Price,
        AreaId = p.AreaId,
        AreaName = p.Area.Name,
        District = p.Area.District,
        City = p.Area.City,
        Address = p.Address,
        CurrentOccupants = p.CurrentOccupants,
        NeededOccupants = p.NeededOccupants,
        PreferredGender = p.PreferredGender,
        ThumbnailUrl = p.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault(),
        Status = p.Status,
        CreatedAt = p.CreatedAt,
        ExpiredAt = p.ExpiredAt,
        ViewCount = p.ViewCount,
        OwnerId = p.UserId,
        OwnerName = p.User.FullName,
        OwnerAvatarUrl = p.User.AvatarUrl,
        OwnerRating = p.User.AvgRating,
        Latitude = p.Latitude ?? p.Area.Latitude,
        Longitude = p.Longitude ?? p.Area.Longitude,
        RejectReason = p.RejectReason,
        ExtendCount = p.ExtendCount,
    };

    public Task<Post?> GetAsync(long postId, CancellationToken ct = default) =>
        _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId, ct);

    public Task<Post?> GetWithDetailsAsync(long postId, CancellationToken ct = default) =>
        _db.Posts.Include(p => p.Images).Include(p => p.PostAmenities)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.PostId == postId, ct);

    public void Add(Post post) => _db.Posts.Add(post);

    public Task<int> CountCreatedSinceAsync(long userId, DateTime since, CancellationToken ct = default) =>
        _db.Posts.CountAsync(p => p.UserId == userId && p.CreatedAt >= since, ct);

    public Task<PostDetailDto?> GetDetailAsync(long postId, CancellationToken ct = default) =>
        _db.Posts.AsNoTracking().Where(p => p.PostId == postId && !p.IsDeleted).Select(p => new PostDetailDto
        {
            PostId = p.PostId,
            PostType = p.PostType,
            Title = p.Title,
            Description = p.Description,
            Price = p.Price,
            AreaId = p.AreaId,
            AreaName = p.Area.Name,
            District = p.Area.District,
            City = p.Area.City,
            Address = p.Address,
            Latitude = p.Latitude ?? p.Area.Latitude,
            Longitude = p.Longitude ?? p.Area.Longitude,
            CurrentOccupants = p.CurrentOccupants,
            NeededOccupants = p.NeededOccupants,
            PreferredGender = p.PreferredGender,
            Status = p.Status,
            RejectReason = p.RejectReason,
            ExtendCount = p.ExtendCount,
            ViewCount = p.ViewCount,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            ExpiredAt = p.ExpiredAt,
            Images = p.Images.OrderBy(i => i.SortOrder)
                .Select(i => new PostImageDto { ImageId = i.ImageId, ImageUrl = i.ImageUrl, SortOrder = i.SortOrder }).ToList(),
            Amenities = p.PostAmenities.OrderBy(a => a.AmenityId)
                .Select(a => new AmenityDto { AmenityId = a.AmenityId, Name = a.Amenity.Name, IsActive = a.Amenity.IsActive }).ToList(),
            OwnerId = p.UserId,
            OwnerName = p.User.FullName,
            OwnerAvatarUrl = p.User.AvatarUrl,
            OwnerGender = p.User.Gender,
            OwnerOccupation = p.User.Occupation,
            OwnerRating = p.User.AvgRating,
            OwnerReviewCount = p.User.ReviewCount,
            OwnerPhone = p.User.Phone, // Service quyết định có trả ra hay không (NFR-05)
        }).AsSplitQuery().FirstOrDefaultAsync(ct);

    /// <summary>Điều kiện "đang hiển thị công khai" + bộ lọc tìm kiếm (không gồm khoảng cách).</summary>
    private IQueryable<Post> PublicQuery(PostSearchQuery q, DateTime now)
    {
        var query = _db.Posts.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == PostStatus.Approved && (p.ExpiredAt == null || p.ExpiredAt > now));

        if (q.Keyword is { } k)
            query = query.Where(p => p.Title.Contains(k) || (p.Description != null && p.Description.Contains(k))
                                     || (p.Address != null && p.Address.Contains(k)) || p.Area.Name.Contains(k));
        if (q.PostType != null) query = query.Where(p => p.PostType == q.PostType);
        if (q.AreaId is { } areaId) query = query.Where(p => p.AreaId == areaId);
        if (q.MinPrice is { } min) query = query.Where(p => p.Price >= min);
        if (q.MaxPrice is { } max) query = query.Where(p => p.Price <= max);
        if (q.Gender is { } g) query = query.Where(p => p.PreferredGender == null || p.PreferredGender == Gender.Any || p.PreferredGender == g);
        if (q.MinNeeded is { } needed) query = query.Where(p => p.NeededOccupants >= needed);
        if (q.AmenityIds is { Count: > 0 } amenities)
        {
            foreach (var id in amenities.Distinct())
                query = query.Where(p => p.PostAmenities.Any(a => a.AmenityId == id));
        }
        return query;
    }

    public async Task<PagedResult<PostSummaryDto>> SearchAsync(PostSearchQuery q, int page, int pageSize, DateTime now, CancellationToken ct = default)
    {
        var query = PublicQuery(q, now);
        var total = await query.CountAsync(ct);
        query = q.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price).ThenByDescending(p => p.PostId),
            "price_desc" => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.PostId),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.PostId),
        };
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(SummaryProjection).ToListAsync(ct);
        return PagedResult<PostSummaryDto>.Create(items, page, pageSize, total);
    }

    public Task<List<PostSummaryDto>> SearchCandidatesAsync(PostSearchQuery q, GeoBox box, DateTime now, int max, CancellationToken ct = default) =>
        PublicQuery(q, now)
            .Where(p => (p.Latitude ?? p.Area.Latitude) >= box.MinLat && (p.Latitude ?? p.Area.Latitude) <= box.MaxLat
                     && (p.Longitude ?? p.Area.Longitude) >= box.MinLon && (p.Longitude ?? p.Area.Longitude) <= box.MaxLon)
            .OrderByDescending(p => p.CreatedAt)
            .Take(max)
            .Select(SummaryProjection)
            .ToListAsync(ct);

    public Task<List<PostSummaryDto>> GetLatestAsync(int count, DateTime now, CancellationToken ct = default) =>
        PublicQuery(new PostSearchQuery(), now).OrderByDescending(p => p.ModeratedAt ?? p.CreatedAt)
            .Take(count).Select(SummaryProjection).ToListAsync(ct);

    public Task<List<PostSummaryDto>> GetByOwnerAsync(long userId, string? status, CancellationToken ct = default) =>
        _db.Posts.AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsDeleted && (status == null || p.Status == status))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostSummaryDto
            {
                PostId = p.PostId,
                PostType = p.PostType,
                Title = p.Title,
                Price = p.Price,
                AreaId = p.AreaId,
                AreaName = p.Area.Name,
                District = p.Area.District,
                City = p.Area.City,
                Address = p.Address,
                CurrentOccupants = p.CurrentOccupants,
                NeededOccupants = p.NeededOccupants,
                PreferredGender = p.PreferredGender,
                ThumbnailUrl = p.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                ExpiredAt = p.ExpiredAt,
                ViewCount = p.ViewCount,
                OwnerId = p.UserId,
                OwnerName = p.User.FullName,
                OwnerAvatarUrl = p.User.AvatarUrl,
                OwnerRating = p.User.AvgRating,
                Latitude = p.Latitude ?? p.Area.Latitude,
                Longitude = p.Longitude ?? p.Area.Longitude,
                RejectReason = p.RejectReason,
                ExtendCount = p.ExtendCount,
                PendingRequestCount = _db.ConnectionRequests.Count(r => r.PostId == p.PostId && r.Status == RequestStatus.Pending),
            }).ToListAsync(ct);

    public Task<List<PostSummaryDto>> GetPublicByOwnerAsync(long userId, DateTime now, CancellationToken ct = default) =>
        PublicQuery(new PostSearchQuery(), now).Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt).Take(50).Select(SummaryProjection).ToListAsync(ct);

    public async Task<PagedResult<PostSummaryDto>> GetForModerationAsync(string status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Posts.AsNoTracking().Where(p => !p.IsDeleted && p.Status == status);
        var total = await query.CountAsync(ct);
        // Hàng đợi chờ duyệt: tin cũ nhất trước (FIFO); trạng thái khác: mới nhất trước.
        query = status == PostStatus.Pending
            ? query.OrderBy(p => p.UpdatedAt ?? p.CreatedAt)
            : query.OrderByDescending(p => p.ModeratedAt ?? p.CreatedAt);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(SummaryProjection).ToListAsync(ct);
        return PagedResult<PostSummaryDto>.Create(items, page, pageSize, total);
    }

    public Task IncrementViewCountAsync(long postId, CancellationToken ct = default) =>
        _db.Posts.Where(p => p.PostId == postId).ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

    public async Task<bool> TryTakeSlotAsync(long postId, DateTime now, CancellationToken ct = default)
    {
        // UPDATE Posts SET NeededOccupants = NeededOccupants - 1
        // WHERE PostId = @id AND NeededOccupants > 0 AND Status = 'approved' AND IsDeleted = 0 AND (chưa hết hạn)
        // SQL Server giữ khóa U/X trên dòng nên giao dịch đồng thời phải chờ rồi đánh giá lại điều kiện.
        var rows = await _db.Posts
            .Where(p => p.PostId == postId && p.NeededOccupants > 0 && p.Status == PostStatus.Approved && !p.IsDeleted
                        && (p.ExpiredAt == null || p.ExpiredAt > now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.NeededOccupants, p => p.NeededOccupants - 1)
                .SetProperty(p => p.UpdatedAt, now), ct);
        return rows == 1;
    }

    public async Task<bool> CloseIfFullAsync(long postId, DateTime now, CancellationToken ct = default)
    {
        var rows = await _db.Posts
            .Where(p => p.PostId == postId && p.NeededOccupants == 0 && p.Status == PostStatus.Approved)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PostStatus.Closed).SetProperty(p => p.UpdatedAt, now), ct);
        return rows == 1;
    }

    public async Task<bool> TryCloseAsync(long postId, DateTime now, CancellationToken ct = default) =>
        await _db.Posts.Where(p => p.PostId == postId && p.Status == PostStatus.Approved && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PostStatus.Closed).SetProperty(p => p.UpdatedAt, now), ct) == 1;

    public async Task<bool> TrySoftDeleteAsync(long postId, DateTime now, CancellationToken ct = default) =>
        await _db.Posts.Where(p => p.PostId == postId && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true).SetProperty(p => p.UpdatedAt, now), ct) == 1;

    public Task<int> HideApprovedOfUserAsync(long userId, string reason, DateTime now, CancellationToken ct = default) =>
        _db.Posts.Where(p => p.UserId == userId && p.Status == PostStatus.Approved && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PostStatus.Hidden)
                .SetProperty(p => p.RejectReason, reason)
                .SetProperty(p => p.UpdatedAt, now), ct);

    public Task<List<PostRef>> GetDueForExpiryAsync(DateTime now, CancellationToken ct = default) =>
        _db.Posts.AsNoTracking()
            .Where(p => p.Status == PostStatus.Approved && !p.IsDeleted && p.ExpiredAt != null && p.ExpiredAt <= now)
            .Select(p => new PostRef(p.PostId, p.UserId, p.Title))
            .ToListAsync(ct);

    public Task<int> MarkExpiredAsync(IReadOnlyCollection<long> postIds, DateTime now, CancellationToken ct = default) =>
        _db.Posts.Where(p => postIds.Contains(p.PostId) && p.Status == PostStatus.Approved)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PostStatus.Expired).SetProperty(p => p.UpdatedAt, now), ct);
}

public class SavedPostRepository : ISavedPostRepository
{
    private readonly AppDbContext _db;
    public SavedPostRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(long userId, long postId, CancellationToken ct = default) =>
        _db.SavedPosts.AnyAsync(s => s.UserId == userId && s.PostId == postId, ct);

    public async Task<HashSet<long>> GetSavedPostIdsAsync(long userId, IEnumerable<long> postIds, CancellationToken ct = default)
    {
        var ids = postIds.ToList();
        var saved = await _db.SavedPosts.Where(s => s.UserId == userId && ids.Contains(s.PostId)).Select(s => s.PostId).ToListAsync(ct);
        return saved.ToHashSet();
    }

    public void Add(SavedPost saved) => _db.SavedPosts.Add(saved);

    public Task RemoveAsync(long userId, long postId, CancellationToken ct = default) =>
        _db.SavedPosts.Where(s => s.UserId == userId && s.PostId == postId).ExecuteDeleteAsync(ct);

    public Task<List<PostSummaryDto>> GetSavedAsync(long userId, CancellationToken ct = default) =>
        _db.SavedPosts.AsNoTracking()
            .Where(s => s.UserId == userId && !s.Post.IsDeleted)
            .OrderByDescending(s => s.SavedAt)
            .Select(s => s.Post)
            .Select(PostRepository.SummaryProjection)
            .Select(p => new PostSummaryDto
            {
                // Không lộ lý do ẩn/từ chối của tin người khác cho người đã lưu tin.
                PostId = p.PostId, PostType = p.PostType, Title = p.Title, Price = p.Price, AreaId = p.AreaId, AreaName = p.AreaName,
                District = p.District, City = p.City, Address = p.Address, CurrentOccupants = p.CurrentOccupants,
                NeededOccupants = p.NeededOccupants, PreferredGender = p.PreferredGender, ThumbnailUrl = p.ThumbnailUrl,
                Status = p.Status, CreatedAt = p.CreatedAt, ExpiredAt = p.ExpiredAt, ViewCount = p.ViewCount, OwnerId = p.OwnerId,
                OwnerName = p.OwnerName, OwnerAvatarUrl = p.OwnerAvatarUrl, OwnerRating = p.OwnerRating,
                Latitude = p.Latitude, Longitude = p.Longitude, RejectReason = null, ExtendCount = p.ExtendCount,
            })
            .ToListAsync(ct);
}
