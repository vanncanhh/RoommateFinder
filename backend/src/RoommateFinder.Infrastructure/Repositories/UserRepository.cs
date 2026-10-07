using Microsoft.EntityFrameworkCore;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetAsync(long userId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);

    public void Add(User user) => _db.Users.Add(user);

    public Task<UserStatusInfo?> GetStatusAsync(long userId, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().Where(u => u.UserId == userId)
            .Select(u => new UserStatusInfo(u.Status, u.SuspendedUntil, u.Role, u.SecurityStamp))
            .FirstOrDefaultAsync(ct);

    public Task<ProfileDto?> GetProfileAsync(long userId, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().Where(u => u.UserId == userId).Select(u => new ProfileDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            Gender = u.Gender,
            DateOfBirth = u.DateOfBirth,
            Role = u.Role,
            Occupation = u.Occupation,
            SchoolOrCompany = u.SchoolOrCompany,
            LandmarkId = u.LandmarkId,
            LandmarkName = u.Landmark != null ? u.Landmark.Name : null,
            AvatarUrl = u.AvatarUrl,
            Bio = u.Bio,
            SleepSchedule = u.SleepSchedule,
            IsSmoker = u.IsSmoker,
            HasPet = u.HasPet,
            CleanlinessLevel = u.CleanlinessLevel,
            AvgRating = u.AvgRating,
            ReviewCount = u.ReviewCount,
            CreatedAt = u.CreatedAt,
        }).FirstOrDefaultAsync(ct);

    public Task<PublicProfileDto?> GetPublicProfileAsync(long userId, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().Where(u => u.UserId == userId && u.Status != UserStatus.Banned).Select(u => new PublicProfileDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,
            Gender = u.Gender,
            Occupation = u.Occupation,
            SchoolOrCompany = u.SchoolOrCompany,
            Bio = u.Bio,
            SleepSchedule = u.SleepSchedule,
            IsSmoker = u.IsSmoker,
            HasPet = u.HasPet,
            CleanlinessLevel = u.CleanlinessLevel,
            AvgRating = u.AvgRating,
            ReviewCount = u.ReviewCount,
            CreatedAt = u.CreatedAt,
        }).FirstOrDefaultAsync(ct);

    public Task<CurrentUserDto?> GetCurrentUserAsync(long userId, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().Where(u => u.UserId == userId).Select(u => new CurrentUserDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            AvatarUrl = u.AvatarUrl,
        }).FirstOrDefaultAsync(ct);

    public Task<List<long>> GetActiveIdsByRoleAsync(string role, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().Where(u => u.Role == role && u.Status == UserStatus.Active).Select(u => u.UserId).ToListAsync(ct);

    public Task<int> CountByRoleAsync(string role, CancellationToken ct = default) =>
        _db.Users.CountAsync(u => u.Role == role, ct);

    public async Task<PagedResult<AdminUserDto>> SearchForAdminAsync(AdminUserQuery q, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (q.Keyword is { } k)
            query = query.Where(u => u.FullName.Contains(k) || u.Email.Contains(k) || (u.Phone != null && u.Phone.Contains(k)));
        if (q.Role != null) query = query.Where(u => u.Role == q.Role);
        if (q.Status != null) query = query.Where(u => u.Status == q.Status);

        var total = await query.CountAsync(ct);
        var items = await ProjectAdmin(query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return PagedResult<AdminUserDto>.Create(items, page, pageSize, total);
    }

    public Task<AdminUserDto?> GetForAdminAsync(long userId, CancellationToken ct = default) =>
        ProjectAdmin(_db.Users.AsNoTracking().Where(u => u.UserId == userId)).FirstOrDefaultAsync(ct);

    private IQueryable<AdminUserDto> ProjectAdmin(IQueryable<User> query) => query.Select(u => new AdminUserDto
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role,
        Status = u.Status,
        SuspendedUntil = u.SuspendedUntil,
        AvgRating = u.AvgRating,
        ReviewCount = u.ReviewCount,
        PostCount = _db.Posts.Count(p => p.UserId == u.UserId && !p.IsDeleted),
        ViolationCount = _db.ViolationHistory.Count(v => v.UserId == u.UserId),
        CreatedAt = u.CreatedAt,
    });

    public async Task<List<long>> ReactivateExpiredSuspensionsAsync(DateTime now, CancellationToken ct = default)
    {
        var ids = await _db.Users.Where(u => u.Status == UserStatus.Suspended && u.SuspendedUntil <= now)
            .Select(u => u.UserId).ToListAsync(ct);
        if (ids.Count > 0)
        {
            await _db.Users.Where(u => ids.Contains(u.UserId) && u.Status == UserStatus.Suspended)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.Status, UserStatus.Active)
                    .SetProperty(u => u.SuspendedUntil, (DateTime?)null)
                    .SetProperty(u => u.UpdatedAt, now), ct);
        }
        return ids;
    }

    public Task LockRowAsync(long userId, CancellationToken ct = default) =>
        _db.Users.Where(u => u.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.ReviewCount, u => u.ReviewCount), ct);
}

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;
    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public void Add(PasswordResetToken token) => _db.PasswordResetTokens.Add(token);

    public Task<PasswordResetToken?> GetValidAsync(string tokenHash, DateTime now, CancellationToken ct = default) =>
        _db.PasswordResetTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > now, ct);

    public Task<int> CountCreatedSinceAsync(long userId, DateTime since, CancellationToken ct = default) =>
        _db.PasswordResetTokens.CountAsync(t => t.UserId == userId && t.CreatedAt >= since, ct);

    public async Task<bool> TryConsumeAsync(long tokenId, DateTime now, CancellationToken ct = default) =>
        await _db.PasswordResetTokens.Where(t => t.TokenId == tokenId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct) == 1;

    public Task InvalidateAllAsync(long userId, DateTime now, CancellationToken ct = default) =>
        _db.PasswordResetTokens.Where(t => t.UserId == userId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);
}
