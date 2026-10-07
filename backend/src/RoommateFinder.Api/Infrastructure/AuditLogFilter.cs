using Microsoft.AspNetCore.Mvc.Filters;

namespace RoommateFinder.Api.Infrastructure;

/// <summary>NFR-12: ghi nhật ký mọi thao tác thay đổi dữ liệu của moderator/admin (duyệt, khóa, gán quyền...).</summary>
public class AuditLogFilter : IAsyncActionFilter
{
    private readonly ILogger<AuditLogFilter> _logger;

    public AuditLogFilter(ILogger<AuditLogFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method))
        {
            await next();
            return;
        }

        var executed = await next();
        var user = context.HttpContext.User;
        var status = executed.Exception != null && !executed.ExceptionHandled ? "lỗi" : context.HttpContext.Response.StatusCode.ToString();
        _logger.LogInformation("AUDIT {Role} #{UserId} {Method} {Path} → {Status}",
            user.GetRole(), user.GetUserId(), request.Method, request.Path.Value, status);
    }
}
