using RoommateFinder.Application.Common;

namespace RoommateFinder.Api.Infrastructure;

/// <summary>Lỗi trả về dạng { code, message } tiếng Việt; lỗi 500 không lộ stack trace ra client.</summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            var (status, code) = Map(ex);
            await WriteAsync(context, status, code, ex.Message);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Trùng khóa chưa được Service xử lý: {Index}", ex.IndexName);
            await WriteAsync(context, StatusCodes.Status409Conflict, "CONFLICT", "Dữ liệu đã tồn tại.");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client đã hủy request — không ghi log lỗi.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xử lý: {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "SERVER_ERROR", "Hệ thống đang gặp sự cố. Vui lòng thử lại sau.");
        }
    }

    public static (int Status, string Code) Map(BusinessException ex) => ex.Code switch
    {
        ErrorCode.Validation => (StatusCodes.Status400BadRequest, ex.Reason ?? "VALIDATION"),
        ErrorCode.Unauthorized => (StatusCodes.Status401Unauthorized, ex.Reason ?? "UNAUTHORIZED"),
        ErrorCode.Forbidden => (StatusCodes.Status403Forbidden, ex.Reason ?? "FORBIDDEN"),
        ErrorCode.NotFound => (StatusCodes.Status404NotFound, ex.Reason ?? "NOT_FOUND"),
        ErrorCode.Conflict => (StatusCodes.Status409Conflict, ex.Reason ?? "CONFLICT"),
        ErrorCode.TooManyRequests => (StatusCodes.Status429TooManyRequests, ex.Reason ?? "TOO_MANY_REQUESTS"),
        ErrorCode.Locked => (StatusCodes.Status423Locked, ex.Reason ?? "LOGIN_LOCKED"),
        _ => (StatusCodes.Status400BadRequest, "VALIDATION"),
    };

    public static async Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ApiError(code, message));
    }
}

public record ApiError(string Code, string Message);
