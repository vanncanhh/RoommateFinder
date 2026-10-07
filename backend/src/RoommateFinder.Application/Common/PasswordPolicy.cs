namespace RoommateFinder.Application.Common;

/// <summary>Quy tắc mật khẩu mạnh dùng chung cho Đăng ký, Quên và Đổi mật khẩu.</summary>
public static class PasswordPolicy
{
    public const string Message = "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường và chữ số.";

    public static bool IsStrong(string? password) =>
        password is { Length: >= 8 and <= 100 }
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit);

    public static void Ensure(string? password)
    {
        if (!IsStrong(password)) throw BusinessException.Validation(Message);
    }
}
