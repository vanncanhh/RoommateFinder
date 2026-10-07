using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;
using RoommateFinder.Domain.Constants;
using RoommateFinder.UnitTests.Fakes;
using Xunit;

namespace RoommateFinder.UnitTests;

public class AuthServiceTests
{
    private readonly FakeUsers _users = new();
    private readonly FakeResetTokens _tokens = new();
    private readonly FakeEmail _email = new();
    private readonly FakeClock _clock = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        _users.Add(1);
        _auth = new AuthService(_users, _tokens, new FakeUnitOfWork(), new FakeHasher(), new FakeJwt(), _email,
            new FakeLinks(), new FakeSettings(), _clock);
    }

    private Task<AuthResponse> Login(string password, string email = "u1@test.vn") =>
        _auth.LoginAsync(new LoginRequest { Email = email, Password = password });

    [Fact]
    public async Task Login_Correct_ReturnsTokenAndResetsCounter()
    {
        _users.Items[0].FailedLoginAttempts = 3;
        var res = await Login("User@1234");
        Assert.StartsWith("token-1-user", res.AccessToken);
        Assert.Equal(0, _users.Items[0].FailedLoginAttempts);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_SameMessage()
    {
        var unknown = await Assert.ThrowsAsync<BusinessException>(() => Login("User@1234", "nobody@test.vn"));
        var wrong = await Assert.ThrowsAsync<BusinessException>(() => Login("Wrong123"));
        Assert.Equal(ErrorCode.Unauthorized, unknown.Code);
        Assert.Equal(unknown.Message, wrong.Message); // không tiết lộ email có tồn tại
    }

    [Fact]
    public async Task Login_FiveWrongAttempts_Locks15Minutes()
    {
        for (var i = 0; i < 4; i++)
            Assert.Equal(ErrorCode.Unauthorized, (await Assert.ThrowsAsync<BusinessException>(() => Login("Wrong123"))).Code);

        var fifth = await Assert.ThrowsAsync<BusinessException>(() => Login("Wrong123"));
        Assert.Equal(ErrorCode.Locked, fifth.Code);
        Assert.Equal(_clock.UtcNow.AddMinutes(15), _users.Items[0].LockedUntil);

        // Đúng mật khẩu nhưng còn trong thời gian khóa vẫn bị chặn
        Assert.Equal(ErrorCode.Locked, (await Assert.ThrowsAsync<BusinessException>(() => Login("User@1234"))).Code);

        // Hết 15 phút thì đăng nhập được
        _clock.UtcNow = _clock.UtcNow.AddMinutes(16);
        Assert.NotEmpty((await Login("User@1234")).AccessToken);
    }

    [Fact]
    public async Task Login_Banned_ReturnsAccountLocked()
    {
        _users.Items[0].Status = UserStatus.Banned;
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Login("User@1234"));
        Assert.Equal(ErrorCode.Forbidden, ex.Code);
        Assert.Equal("ACCOUNT_LOCKED", ex.Reason);
    }

    [Fact]
    public async Task Login_SuspensionExpired_ReactivatesAccount()
    {
        _users.Items[0].Status = UserStatus.Suspended;
        _users.Items[0].SuspendedUntil = _clock.UtcNow.AddMinutes(-1);
        await Login("User@1234");
        Assert.Equal(UserStatus.Active, _users.Items[0].Status);
        Assert.Null(_users.Items[0].SuspendedUntil);
    }

    [Fact]
    public async Task Register_NormalizesEmailAndRejectsDuplicate()
    {
        await _auth.RegisterAsync(new RegisterRequest { FullName = "Nguyễn Văn A", Email = "  New@Test.VN ", Password = "Abcdef12" });
        Assert.Contains(_users.Items, u => u.Email == "new@test.vn" && u.Role == Roles.User);

        var dup = await Assert.ThrowsAsync<BusinessException>(() =>
            _auth.RegisterAsync(new RegisterRequest { FullName = "Trần Văn B", Email = "NEW@test.vn", Password = "Abcdef12" }));
        Assert.Equal(ErrorCode.Conflict, dup.Code);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_SendsNothing()
    {
        await _auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@test.vn" });
        Assert.Empty(_email.Sent);
        Assert.Empty(_tokens.Items);
    }

    [Fact]
    public async Task ResetPassword_TokenWorksOnceAndStoresHashOnly()
    {
        await _auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "u1@test.vn" });
        Assert.Single(_email.Sent);
        var stored = _tokens.Items.Single();

        // Không biết token gốc từ CSDL (chỉ lưu SHA-256) → dựng lại token gốc để thử
        const string raw = "known-raw-token";
        stored.TokenHash = AuthService.HashToken(raw);
        stored.User = _users.Items[0]; // repository thật Include(t => t.User)

        await _auth.ResetPasswordAsync(new ResetPasswordRequest { Token = raw, NewPassword = "NewPass99" });
        Assert.Equal("hash:NewPass99", _users.Items[0].PasswordHash);
        Assert.NotNull(stored.UsedAt);

        var reuse = await Assert.ThrowsAsync<BusinessException>(() =>
            _auth.ResetPasswordAsync(new ResetPasswordRequest { Token = raw, NewPassword = "Another99" }));
        Assert.Equal(ErrorCode.Validation, reuse.Code);
    }

    [Fact]
    public async Task ForgotPassword_MoreThan3In15Minutes_SilentlySkipped_NoEnumeration()
    {
        for (var i = 0; i < 4; i++) await _auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "u1@test.vn" });
        // Không ném lỗi (giống hệt email không tồn tại), nhưng lần thứ 4 không gửi email.
        Assert.Equal(3, _email.Sent.Count);
    }

    [Fact]
    public async Task Login_LockedAccount_WrongPassword_DoesNotRevealStatus()
    {
        _users.Items[0].Status = UserStatus.Banned;
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Login("Wrong123"));
        Assert.Equal(ErrorCode.Unauthorized, ex.Code); // chỉ người biết mật khẩu mới thấy "tài khoản bị khóa"
    }

    [Fact]
    public async Task ChangePassword_RotatesSecurityStamp()
    {
        var before = _users.Items[0].SecurityStamp;
        await _auth.ChangePasswordAsync(1, new ChangePasswordRequest { CurrentPassword = "User@1234", NewPassword = "NewPass99" });
        Assert.NotEqual(before, _users.Items[0].SecurityStamp);
    }
}
