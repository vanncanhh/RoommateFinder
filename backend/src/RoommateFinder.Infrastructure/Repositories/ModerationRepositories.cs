using Microsoft.EntityFrameworkCore;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;
    public ReportRepository(AppDbContext db) => _db = db;

    public void Add(Report report) => _db.Reports.Add(report);

    public Task<Report?> GetAsync(long reportId, CancellationToken ct = default) =>
        _db.Reports.Include(r => r.Post).Include(r => r.Reason).FirstOrDefaultAsync(r => r.ReportId == reportId, ct);

    private IQueryable<ReportDto> Project(IQueryable<Report> query) => query.Select(r => new ReportDto
    {
        ReportId = r.ReportId,
        ReporterId = r.ReporterId,
        ReporterName = r.Reporter.FullName,
        PostId = r.PostId,
        PostTitle = r.Post != null ? r.Post.Title : null,
        PostStatus = r.Post != null ? r.Post.Status : null,
        ReportedUserId = r.ReportedUserId,
        ReportedUserName = r.ReportedUser != null ? r.ReportedUser.FullName : null,
        TargetUserId = r.ReportedUserId ?? r.Post!.UserId,
        TargetUserName = r.ReportedUser != null ? r.ReportedUser.FullName : r.Post!.User.FullName,
        ReasonId = r.ReasonId,
        ReasonName = r.Reason.Name,
        Description = r.Description,
        Status = r.Status,
        HandledBy = r.HandledBy,
        HandlerName = _db.Users.Where(u => u.UserId == r.HandledBy).Select(u => u.FullName).FirstOrDefault(),
        HandledAt = r.HandledAt,
        HandledNote = r.HandledNote,
        CreatedAt = r.CreatedAt,
        PendingReportsOnTarget = r.PostId != null
            ? _db.Reports.Count(x => x.PostId == r.PostId && x.Status == ReportStatus.Pending)
            : _db.Reports.Count(x => x.ReportedUserId == r.ReportedUserId && x.Status == ReportStatus.Pending),
    });

    public Task<ReportDto?> GetDtoAsync(long reportId, CancellationToken ct = default) =>
        Project(_db.Reports.AsNoTracking().Where(r => r.ReportId == reportId)).FirstOrDefaultAsync(ct);

    public async Task<PagedResult<ReportDto>> GetPagedAsync(string status, long? excludeTargetUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Reports.AsNoTracking().Where(r => r.Status == status);
        if (excludeTargetUserId is { } me)
            query = query.Where(r => r.ReportedUserId != me && (r.PostId == null || r.Post!.UserId != me));
        var total = await query.CountAsync(ct);
        query = status == ReportStatus.Pending
            ? query.OrderBy(r => r.CreatedAt)
            : query.OrderByDescending(r => r.HandledAt ?? r.CreatedAt);
        var items = await Project(query.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return PagedResult<ReportDto>.Create(items, page, pageSize, total);
    }

    public Task<bool> HasPendingUserReportAsync(long reporterId, long reportedUserId, CancellationToken ct = default) =>
        _db.Reports.AnyAsync(r => r.ReporterId == reporterId && r.ReportedUserId == reportedUserId && r.Status == ReportStatus.Pending, ct);

    public Task<int> CountDistinctReportersAsync(long postId, DateTime since, CancellationToken ct = default) =>
        _db.Reports.Where(r => r.PostId == postId && r.CreatedAt >= since && r.Status == ReportStatus.Pending)
            .Select(r => r.ReporterId).Distinct().CountAsync(ct);

    public async Task<bool> TryClaimAsync(long reportId, string status, long handlerId, string? note, DateTime now, CancellationToken ct = default) =>
        await _db.Reports.Where(r => r.ReportId == reportId && r.Status == ReportStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.HandledBy, handlerId)
                .SetProperty(r => r.HandledAt, now)
                .SetProperty(r => r.HandledNote, note), ct) == 1;

    public Task<int> ResolvePendingForUserAsync(long reportedUserId, long excludeReportId, long handlerId, string note, DateTime now, CancellationToken ct = default) =>
        _db.Reports.Where(r => r.ReportedUserId == reportedUserId && r.ReportId != excludeReportId && r.Status == ReportStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ReportStatus.Resolved)
                .SetProperty(r => r.HandledBy, handlerId)
                .SetProperty(r => r.HandledAt, now)
                .SetProperty(r => r.HandledNote, note), ct);

    public Task<int> CountPendingForPostAsync(long postId, long excludeReportId, CancellationToken ct = default) =>
        _db.Reports.CountAsync(r => r.PostId == postId && r.ReportId != excludeReportId && r.Status == ReportStatus.Pending, ct);

    public Task<int> ResolvePendingForPostAsync(long postId, long excludeReportId, long handlerId, string note, DateTime now, CancellationToken ct = default) =>
        _db.Reports.Where(r => r.PostId == postId && r.ReportId != excludeReportId && r.Status == ReportStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ReportStatus.Resolved)
                .SetProperty(r => r.HandledBy, handlerId)
                .SetProperty(r => r.HandledAt, now)
                .SetProperty(r => r.HandledNote, note), ct);
}

public class ViolationRepository : IViolationRepository
{
    private readonly AppDbContext _db;
    public ViolationRepository(AppDbContext db) => _db = db;

    public void Add(ViolationHistory violation) => _db.ViolationHistory.Add(violation);

    public Task<bool> HasActionAsync(long userId, string action, CancellationToken ct = default) =>
        _db.ViolationHistory.AnyAsync(v => v.UserId == userId && v.Action == action, ct);

    public Task<bool> HasSevereAsync(long userId, CancellationToken ct = default) =>
        _db.ViolationHistory.AnyAsync(v => v.UserId == userId
            && (v.Action == ViolationAction.SuspendAccount || v.Action == ViolationAction.BanAccount), ct);

    public Task<bool> PostHiddenByViolationAsync(long postId, CancellationToken ct = default) =>
        _db.ViolationHistory.AnyAsync(v => v.PostId == postId && v.Action == ViolationAction.HidePost, ct);

    public async Task<PagedResult<ViolationDto>> GetPagedAsync(long? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ViolationHistory.AsNoTracking().Where(v => userId == null || v.UserId == userId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(v => v.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new ViolationDto
            {
                ViolationId = v.ViolationId,
                UserId = v.UserId,
                UserName = v.User.FullName,
                ReportId = v.ReportId,
                PostId = v.PostId,
                Level = v.Level,
                Action = v.Action,
                SuspendDays = v.SuspendDays,
                Note = v.Note,
                HandledBy = v.HandledBy,
                HandlerName = v.Handler.FullName,
                CreatedAt = v.CreatedAt,
            }).ToListAsync(ct);
        return PagedResult<ViolationDto>.Create(items, page, pageSize, total);
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;
    public NotificationRepository(AppDbContext db) => _db = db;

    public void Add(Notification notification) => _db.Notifications.Add(notification);

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(long userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.NotificationId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId, UserId = n.UserId, Type = n.Type, Content = n.Content,
                RelatedId = n.RelatedId, Link = n.Link, IsRead = n.IsRead, CreatedAt = n.CreatedAt,
            }).ToListAsync(ct);
        return PagedResult<NotificationDto>.Create(items, page, pageSize, total);
    }

    public Task<int> CountUnreadAsync(long userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public Task<int> MarkReadAsync(long userId, long notificationId, CancellationToken ct = default) =>
        _db.Notifications.Where(n => n.NotificationId == notificationId && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

    public Task<int> MarkAllReadAsync(long userId, CancellationToken ct = default) =>
        _db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
}

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly AppDbContext _db;
    public SystemConfigRepository(AppDbContext db) => _db = db;

    public Task<List<SystemConfig>> GetAllAsync(CancellationToken ct = default) => _db.SystemConfigs.AsNoTracking().ToListAsync(ct);

    public Task<SystemConfig?> GetAsync(string key, CancellationToken ct = default) =>
        _db.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == key, ct);
}
