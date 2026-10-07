using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Application.Services;

public class AdminService
{
    private static readonly Dictionary<string, string> RoleNames = new()
    {
        [Roles.User] = "Người dùng",
        [Roles.Moderator] = "Người kiểm duyệt",
        [Roles.Admin] = "Quản trị viên",
    };

    private readonly IUserRepository _users;
    private readonly ISystemConfigRepository _configs;
    private readonly IDashboardRepository _dashboard;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationPublisher _notifier;
    private readonly AccountSanctions _sanctions;

    public AdminService(IUserRepository users, ISystemConfigRepository configs, IDashboardRepository dashboard, IUnitOfWork uow,
        ISystemSettings settings, IDateTimeProvider clock, NotificationPublisher notifier, AccountSanctions sanctions)
    {
        _users = users;
        _configs = configs;
        _dashboard = dashboard;
        _uow = uow;
        _settings = settings;
        _clock = clock;
        _notifier = notifier;
        _sanctions = sanctions;
    }

    // ------------------------------------------------------------------ Người dùng & phân quyền

    public Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserQuery q, CancellationToken ct = default)
    {
        if (q.Role != null && !Roles.All.Contains(q.Role)) throw BusinessException.Validation("Vai trò không hợp lệ.");
        if (q.Status != null && !UserStatus.All.Contains(q.Status)) throw BusinessException.Validation("Trạng thái không hợp lệ.");
        q.Keyword = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim();
        var (page, pageSize) = Paging.Normalize(q.Page, q.PageSize, 20);
        return _users.SearchForAdminAsync(q, page, pageSize, ct);
    }

    public async Task<AdminUserDto> ChangeRoleAsync(CurrentUser admin, long userId, UpdateRoleRequest req, CancellationToken ct = default)
    {
        if (!Roles.All.Contains(req.Role)) throw BusinessException.Validation("Vai trò không hợp lệ.");
        if (userId == admin.UserId) throw BusinessException.Forbidden("Bạn không thể tự đổi vai trò của mình.");
        var user = await _users.GetAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy người dùng.");
        if (user.Role == req.Role) return (await _users.GetForAdminAsync(userId, ct))!;
        if (user.Role == Roles.Admin && await _users.CountByRoleAsync(Roles.Admin, ct) <= 1)
            throw BusinessException.Conflict("Không thể hạ quyền Admin cuối cùng của hệ thống.");

        user.Role = req.Role;
        user.UpdatedAt = _clock.UtcNow;
        _notifier.Queue(userId, NotificationType.RoleChanged,
            $"Vai trò của bạn đã được đổi thành {RoleNames[req.Role]}. Vui lòng đăng nhập lại để áp dụng.");
        await _uow.SaveChangesAsync(ct);
        await _notifier.FlushAsync(ct);
        return (await _users.GetForAdminAsync(userId, ct))!;
    }

    public async Task<AdminUserDto> ChangeStatusAsync(CurrentUser admin, long userId, UpdateUserStatusRequest req, CancellationToken ct = default)
    {
        if (!UserStatus.All.Contains(req.Status)) throw BusinessException.Validation("Trạng thái không hợp lệ.");
        if (userId == admin.UserId) throw BusinessException.Forbidden("Bạn không thể tự khóa tài khoản của mình.");
        var reason = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason.Trim();
        if (reason?.Length > 500) throw BusinessException.Validation("Lý do tối đa 500 ký tự.");
        var user = await _users.GetAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy người dùng.");
        if (user.Role == Roles.Admin && req.Status != UserStatus.Active)
            throw BusinessException.Forbidden("Không thể khóa tài khoản Admin. Hãy hạ vai trò trước.");

        var now = _clock.UtcNow;
        await using var tx = await _uow.BeginTransactionAsync(ct);
        switch (req.Status)
        {
            case UserStatus.Active:
                if (user.Status == UserStatus.Active) break;
                user.Status = UserStatus.Active;
                user.SuspendedUntil = null;
                user.UpdatedAt = now;
                _notifier.Queue(userId, NotificationType.AccountStatus,
                    "Tài khoản của bạn đã được mở khóa. Các tin bị ẩn trước đó cần được đăng lại hoặc liên hệ kiểm duyệt viên.");
                break;
            case UserStatus.Suspended:
                if (req.SuspendDays is not (>= 1 and <= 365)) throw BusinessException.Validation("Số ngày khóa phải từ 1 đến 365.");
                if (reason == null) throw BusinessException.Validation("Vui lòng nhập lý do khóa.");
                await _sanctions.SuspendAsync(user, req.SuspendDays.Value, admin.UserId, null, reason, now, ct);
                break;
            case UserStatus.Banned:
                if (reason == null) throw BusinessException.Validation("Vui lòng nhập lý do khóa.");
                await _sanctions.BanAsync(user, admin.UserId, null, reason, now, ct);
                break;
        }
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _notifier.FlushAsync(ct);
        return (await _users.GetForAdminAsync(userId, ct))!;
    }

    // ------------------------------------------------------------------ Cấu hình hệ thống

    public async Task<List<SystemConfigDto>> GetConfigsAsync(CancellationToken ct = default) =>
        (await _configs.GetAllAsync(ct)).Select(ToDto).OrderBy(c => c.ConfigKey).ToList();

    public async Task<SystemConfigDto> UpdateConfigAsync(string key, UpdateConfigRequest req, CancellationToken ct = default)
    {
        var config = await _configs.GetAsync(key, ct) ?? throw BusinessException.NotFound("Không tìm thấy tham số cấu hình.");
        if (!int.TryParse(req.Value?.Trim(), out var value)) throw BusinessException.Validation("Giá trị phải là số nguyên.");
        // Dùng khóa chuẩn lấy từ CSDL: so sánh SQL không phân biệt hoa/thường nên khóa trên URL có thể khác chữ hoa.
        if (!ConfigKeys.Ranges.TryGetValue(config.ConfigKey, out var range))
            throw BusinessException.Validation("Tham số này không được phép sửa.");
        if (value < range.Min || value > range.Max)
            throw BusinessException.Validation($"Giá trị của {config.ConfigKey} phải từ {range.Min} đến {range.Max}.");
        config.ConfigValue = value.ToString();
        await _uow.SaveChangesAsync(ct);
        _settings.Invalidate(); // áp dụng ngay cho các thao tác sau, không tính lại hạn tin đã duyệt
        return ToDto(config);
    }

    private static SystemConfigDto ToDto(Domain.Entities.SystemConfig c)
    {
        var hasRange = ConfigKeys.Ranges.TryGetValue(c.ConfigKey, out var r);
        return new SystemConfigDto
        {
            ConfigKey = c.ConfigKey,
            ConfigValue = c.ConfigValue,
            Description = c.Description,
            Min = hasRange ? r.Min : null,
            Max = hasRange ? r.Max : null,
        };
    }

    // ------------------------------------------------------------------ Dashboard

    public Task<DashboardDto> GetDashboardAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var end = to ?? VnTime.Today(now);
        var start = from ?? end.AddDays(-29);
        if (start > end) throw BusinessException.Validation("Ngày bắt đầu phải trước ngày kết thúc.");
        if (end.DayNumber - start.DayNumber > 366) throw BusinessException.Validation("Khoảng thời gian tối đa 1 năm.");
        return _dashboard.GetAsync(start, end, VnTime.StartOfDayUtc(start), VnTime.StartOfDayUtc(end.AddDays(1)), now, ct);
    }
}
