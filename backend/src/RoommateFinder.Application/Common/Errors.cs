namespace RoommateFinder.Application.Common;

public enum ErrorCode
{
    Validation,      // 400
    Unauthorized,    // 401
    Forbidden,       // 403
    NotFound,        // 404
    Conflict,        // 409
    TooManyRequests, // 429
    Locked,          // 423 — khóa tạm do đăng nhập sai
}

/// <summary>Lỗi nghiệp vụ có chủ đích; middleware đổi thành mã HTTP tương ứng với thông điệp tiếng Việt.</summary>
public class BusinessException : Exception
{
    public ErrorCode Code { get; }
    /// <summary>Mã lỗi ngắn cho frontend phân nhánh (ví dụ ACCOUNT_LOCKED), có thể null.</summary>
    public string? Reason { get; }

    public BusinessException(ErrorCode code, string message, string? reason = null) : base(message)
    {
        Code = code;
        Reason = reason;
    }

    public static BusinessException NotFound(string message = "Không tìm thấy dữ liệu.") => new(ErrorCode.NotFound, message);
    public static BusinessException Forbidden(string message = "Bạn không có quyền thực hiện thao tác này.") => new(ErrorCode.Forbidden, message);
    public static BusinessException Conflict(string message) => new(ErrorCode.Conflict, message);
    public static BusinessException Validation(string message) => new(ErrorCode.Validation, message);
}

/// <summary>
/// Vi phạm UNIQUE constraint (SQL 2627) hoặc unique index / filtered unique index (SQL 2601).
/// Tầng Infrastructure ném lỗi này; Service bắt để trả thông điệp thân thiện, nếu không bắt thì middleware trả 409.
/// </summary>
public class DuplicateKeyException : Exception
{
    public string? IndexName { get; }

    public DuplicateKeyException(string? indexName, Exception inner)
        : base("Dữ liệu bị trùng.", inner)
    {
        IndexName = indexName;
    }

    public bool Is(string name) => IndexName != null && IndexName.Contains(name, StringComparison.OrdinalIgnoreCase);
}
