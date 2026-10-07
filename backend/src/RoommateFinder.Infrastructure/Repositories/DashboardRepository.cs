using Microsoft.EntityFrameworkCore;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Repositories;

/// <summary>Thống kê gom nhóm ngay trong SQL — không tải toàn bộ bảng về bộ nhớ.</summary>
public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _db;
    public DashboardRepository(AppDbContext db) => _db = db;

    public async Task<DashboardDto> GetAsync(DateOnly from, DateOnly to, DateTime fromUtc, DateTime toUtc, DateTime now, CancellationToken ct = default)
    {
        var postsInRange = _db.Posts.AsNoTracking().Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toUtc && !p.IsDeleted);
        var moderated = _db.Posts.AsNoTracking().Where(p => p.ModeratedAt >= fromUtc && p.ModeratedAt < toUtc);

        var byType = await postsInRange.GroupBy(p => p.PostType).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var rejected = await moderated.CountAsync(p => p.Status == PostStatus.Rejected, ct);
        var approved = await moderated.CountAsync(p => p.Status != PostStatus.Rejected && p.Status != PostStatus.Pending, ct);
        // Thời gian duyệt = từ lúc gửi duyệt (tạo/sửa) đến lúc duyệt; UpdatedAt không dùng được vì bị ghi sau khi duyệt.
        var avgMinutes = await moderated.Select(p => (double?)EF.Functions.DateDiffMinute(p.SubmittedAt ?? p.CreatedAt, p.ModeratedAt!.Value))
            .AverageAsync(ct);

        var topAreas = await postsInRange.GroupBy(p => p.Area.Name)
            .Select(g => new AreaCountDto { AreaName = g.Key, PostCount = g.Count() })
            .OrderByDescending(a => a.PostCount).Take(5).ToListAsync(ct);

        // Nhóm theo ngày giờ Việt Nam (GMT+7) — quyết định f.
        var dailyPosts = await postsInRange.GroupBy(p => p.CreatedAt.AddHours(7).Date)
            .Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(ct);
        var dailyUsers = await _db.Users.AsNoTracking().Where(u => u.CreatedAt >= fromUtc && u.CreatedAt < toUtc)
            .GroupBy(u => u.CreatedAt.AddHours(7).Date)
            .Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(ct);

        return new DashboardDto
        {
            From = from,
            To = to,
            NewUsers = dailyUsers.Sum(d => d.Count),
            NewPostsHasRoom = byType.FirstOrDefault(t => t.Key == PostType.HasRoom)?.Count ?? 0,
            NewPostsSeeking = byType.FirstOrDefault(t => t.Key == PostType.Seeking)?.Count ?? 0,
            ApprovedCount = approved,
            RejectedCount = rejected,
            ApprovalRate = approved + rejected == 0 ? null : Math.Round(approved * 100.0 / (approved + rejected), 1),
            AvgModerationHours = avgMinutes == null ? null : Math.Round(avgMinutes.Value / 60.0, 1),
            AcceptedConnections = await _db.ConnectionRequests.CountAsync(r => r.Status == RequestStatus.Accepted
                && r.RespondedAt >= fromUtc && r.RespondedAt < toUtc, ct),
            PendingReports = await _db.Reports.CountAsync(r => r.Status == ReportStatus.Pending, ct),
            PendingPosts = await _db.Posts.CountAsync(p => p.Status == PostStatus.Pending && !p.IsDeleted, ct),
            ActivePosts = await _db.Posts.CountAsync(p => p.Status == PostStatus.Approved && !p.IsDeleted
                && (p.ExpiredAt == null || p.ExpiredAt > now), ct),
            TotalUsers = await _db.Users.CountAsync(ct),
            TopAreas = topAreas,
            DailyPosts = FillDays(from, to, dailyPosts.ToDictionary(d => DateOnly.FromDateTime(d.Day), d => d.Count)),
            DailyUsers = FillDays(from, to, dailyUsers.ToDictionary(d => DateOnly.FromDateTime(d.Day), d => d.Count)),
        };
    }

    private static List<DailyCountDto> FillDays(DateOnly from, DateOnly to, Dictionary<DateOnly, int> counts)
    {
        var result = new List<DailyCountDto>();
        for (var d = from; d <= to; d = d.AddDays(1))
            result.Add(new DailyCountDto { Date = d, Count = counts.GetValueOrDefault(d) });
        return result;
    }
}
