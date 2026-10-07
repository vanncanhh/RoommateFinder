using Microsoft.Extensions.Caching.Memory;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Api.Infrastructure;

/// <summary>
/// Kiểm tra Users.Status ở mỗi request đã xác thực: tài khoản bị khóa giữa phiên bị chặn ngay (403 ACCOUNT_LOCKED);
/// vai trò trong token khác vai trò hiện tại (Admin vừa đổi quyền) thì buộc đăng nhập lại (401).
/// Cache 15 giây để không truy vấn CSDL ở mọi request.
/// </summary>
public class AccountStatusMiddleware
{
    private static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(15);
    private readonly RequestDelegate _next;

    public AccountStatusMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IUserRepository users, IMemoryCache cache, IDateTimeProvider clock)
    {
        if (context.User.Identity?.IsAuthenticated == true && !context.Request.Path.StartsWithSegments("/hubs"))
        {
            var userId = context.User.GetUserId();
            var info = await cache.GetOrCreateAsync($"user-status:{userId}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTime;
                return users.GetStatusAsync(userId, context.RequestAborted);
            });

            if (info == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            var locked = RoommateFinder.Domain.Entities.User.IsLockedStatus(info.Status, info.SuspendedUntil, clock.UtcNow);
            if (locked)
            {
                await ErrorHandlingMiddleware.WriteAsync(context, StatusCodes.Status403Forbidden, "ACCOUNT_LOCKED",
                    "Tài khoản của bạn đang bị khóa. Vui lòng liên hệ quản trị viên.");
                return;
            }
            // Vai trò đổi (Admin đổi quyền) hoặc mật khẩu đổi (security stamp khác) → token cũ hết hiệu lực.
            if (info.Role != context.User.GetRole()
                || info.SecurityStamp != context.User.FindFirst(RoommateFinder.Infrastructure.Services.JwtTokenGenerator.SecurityStampClaim)?.Value)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }
        await _next(context);
    }
}
