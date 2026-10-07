using System.Security.Claims;
using RoommateFinder.Application.Common;

namespace RoommateFinder.Api.Infrastructure;

public static class ClaimsExtensions
{
    public static long GetUserId(this ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new BusinessException(ErrorCode.Unauthorized, "Phiên đăng nhập không hợp lệ.");

    public static string GetRole(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) ?? "";

    public static CurrentUser ToCurrentUser(this ClaimsPrincipal user) => new(user.GetUserId(), user.GetRole());

    /// <summary>Endpoint công khai nhưng có thể kèm token (ví dụ chi tiết tin).</summary>
    public static CurrentUser? ToCurrentUserOrNull(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true ? user.ToCurrentUser() : null;
}
