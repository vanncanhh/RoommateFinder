namespace RoommateFinder.Application.Dtos;

public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Phone { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class CurrentUserDto
{
    public long UserId { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public string? AvatarUrl { get; init; }
}

public class AuthResponse
{
    public string AccessToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public CurrentUserDto User { get; init; } = null!;
}

public class MessageResponse
{
    public string Message { get; init; } = "";
    public MessageResponse() { }
    public MessageResponse(string message) => Message = message;
}
