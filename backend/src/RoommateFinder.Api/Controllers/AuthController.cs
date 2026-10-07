using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;

namespace RoommateFinder.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<MessageResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        await _auth.RegisterAsync(req, ct);
        return StatusCode(StatusCodes.Status201Created, new MessageResponse("Đăng ký thành công. Vui lòng đăng nhập."));
    }

    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest req, CancellationToken ct) => _auth.LoginAsync(req, ct);

    [HttpPost("forgot-password")]
    public async Task<MessageResponse> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(req, ct);
        return new MessageResponse(AuthService.ForgotPasswordMessage);
    }

    [HttpPost("reset-password")]
    public async Task<MessageResponse> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(req, ct);
        return new MessageResponse("Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.");
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<MessageResponse> ChangePassword(ChangePasswordRequest req, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(User.GetUserId(), req, ct);
        return new MessageResponse("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
    }

    [Authorize]
    [HttpGet("me")]
    public Task<CurrentUserDto> Me(CancellationToken ct) => _auth.GetCurrentUserAsync(User.GetUserId(), ct);
}
