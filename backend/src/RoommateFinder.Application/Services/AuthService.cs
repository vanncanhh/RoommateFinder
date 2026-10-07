using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

public class AuthService
{
    public const string ForgotPasswordMessage = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
    private const int ForgotPasswordMaxPerWindow = 3;
    private static readonly TimeSpan ForgotPasswordWindow = TimeSpan.FromMinutes(15);
    private static readonly Regex PhoneRegex = new(@"^0\d{9,10}$", RegexOptions.Compiled);

    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _resetTokens;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IEmailSender _email;
    private readonly IAppLinks _links;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;

    public AuthService(IUserRepository users, IPasswordResetTokenRepository resetTokens, IUnitOfWork uow,
        IPasswordHasher hasher, IJwtTokenGenerator jwt, IEmailSender email, IAppLinks links,
        ISystemSettings settings, IDateTimeProvider clock)
    {
        _users = users;
        _resetTokens = resetTokens;
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _email = email;
        _links = links;
        _settings = settings;
        _clock = clock;
    }

    public static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    public static void ValidatePhone(string? phone)
    {
        if (!string.IsNullOrWhiteSpace(phone) && !PhoneRegex.IsMatch(phone.Trim()))
            throw BusinessException.Validation("Số điện thoại không hợp lệ (10–11 chữ số, bắt đầu bằng 0).");
    }

    public async Task RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var fullName = (req.FullName ?? "").Trim();
        if (fullName.Length is < 2 or > 100) throw BusinessException.Validation("Họ tên phải từ 2 đến 100 ký tự.");
        var email = NormalizeEmail(req.Email);
        if (email.Length > 100 || !MailAddress.TryCreate(email, out _)) throw BusinessException.Validation("Email không hợp lệ.");
        ValidatePhone(req.Phone);
        PasswordPolicy.Ensure(req.Password);

        if (await _users.EmailExistsAsync(email, ct)) throw BusinessException.Conflict("Email đã được sử dụng.");

        _users.Add(new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = _hasher.Hash(req.Password),
            Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
            Role = Roles.User,
            Status = UserStatus.Active,
            CreatedAt = _clock.UtcNow,
        });
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            throw BusinessException.Conflict("Email đã được sử dụng.");
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var user = await _users.GetByEmailAsync(NormalizeEmail(req.Email), ct);
        // Không tiết lộ email có tồn tại hay không.
        const string wrongCredentials = "Email hoặc mật khẩu không đúng.";
        if (user == null) throw new BusinessException(ErrorCode.Unauthorized, wrongCredentials);

        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            var minutes = (int)Math.Ceiling((lockedUntil - now).TotalMinutes);
            throw new BusinessException(ErrorCode.Locked,
                $"Tài khoản đang tạm khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau {minutes} phút.", "LOGIN_LOCKED");
        }

        if (!_hasher.Verify(req.Password ?? "", user.PasswordHash))
        {
            var justLocked = await RegisterFailedAttemptAsync(user, now, ct);
            if (justLocked != null) throw justLocked;
            throw new BusinessException(ErrorCode.Unauthorized, wrongCredentials);
        }

        // Chỉ báo trạng thái khóa vi phạm sau khi đúng mật khẩu — không lộ thông tin tài khoản cho người lạ.
        EnsureNotLocked(user, now);
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await _uow.SaveChangesAsync(ct);

        var lifetime = await _settings.GetIntAsync(ConfigKeys.JwtAccessMinutes, ct);
        var (token, expiresAt) = _jwt.Generate(user.UserId, user.Email, user.Role, user.SecurityStamp, lifetime);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new CurrentUserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl,
            },
        };
    }

    /// <summary>Chặn tài khoản banned/suspended; tự mở khóa nếu hạn suspended đã qua.</summary>
    private static void EnsureNotLocked(User user, DateTime now)
    {
        if (user.Status == UserStatus.Banned)
            throw new BusinessException(ErrorCode.Forbidden, "Tài khoản đã bị khóa vĩnh viễn do vi phạm quy định.", "ACCOUNT_LOCKED");
        if (user.Status == UserStatus.Suspended)
        {
            if (user.SuspendedUntil is { } until && until > now)
                throw new BusinessException(ErrorCode.Forbidden,
                    $"Tài khoản đang bị tạm khóa đến {VnTime.Format(until)} do vi phạm quy định.", "ACCOUNT_LOCKED");
            user.Status = UserStatus.Active;
            user.SuspendedUntil = null;
        }
    }

    /// <summary>Tăng bộ đếm sai mật khẩu; trả về lỗi 423 nếu vừa đủ ngưỡng khóa.</summary>
    private async Task<BusinessException?> RegisterFailedAttemptAsync(User user, DateTime now, CancellationToken ct)
    {
        var maxFailed = await _settings.GetIntAsync(ConfigKeys.LoginMaxFailed, ct);
        var lockMinutes = await _settings.GetIntAsync(ConfigKeys.LoginLockMinutes, ct);
        user.FailedLoginAttempts++;
        BusinessException? result = null;
        if (user.FailedLoginAttempts >= maxFailed)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = now.AddMinutes(lockMinutes);
            result = new BusinessException(ErrorCode.Locked,
                $"Bạn đã nhập sai mật khẩu {maxFailed} lần liên tiếp. Tài khoản bị tạm khóa {lockMinutes} phút.", "LOGIN_LOCKED");
        }
        await _uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(long userId, CancellationToken ct = default) =>
        await _users.GetCurrentUserAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy tài khoản.");

    public async Task ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var user = await _users.GetByEmailAsync(NormalizeEmail(req.Email), ct);
        if (user == null || user.Status == UserStatus.Banned) return; // cùng thông điệp, không gửi email

        var recent = await _resetTokens.CountCreatedSinceAsync(user.UserId, now - ForgotPasswordWindow, ct);
        // Vượt giới hạn thì im lặng bỏ qua: trả 429 chỉ với email có thật sẽ làm lộ email đã đăng ký.
        if (recent >= ForgotPasswordMaxPerWindow) return;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var minutes = await _settings.GetIntAsync(ConfigKeys.ResetTokenMinutes, ct);
        _resetTokens.Add(new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = HashToken(rawToken),
            ExpiresAt = now.AddMinutes(minutes),
            CreatedAt = now,
        });
        await _uow.SaveChangesAsync(ct);

        var link = _links.ResetPasswordUrl(rawToken);
        var body = $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>"
                 + $"<p>Bạn vừa yêu cầu đặt lại mật khẩu RoommateFinder. Đường dẫn có hiệu lực trong {minutes} phút và chỉ dùng được một lần:</p>"
                 + $"<p><a href=\"{link}\">{link}</a></p>"
                 + "<p>Nếu không phải bạn yêu cầu, hãy bỏ qua email này.</p>";
        await _email.SendAsync(user.Email, "Đặt lại mật khẩu RoommateFinder", body, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var token = string.IsNullOrWhiteSpace(req.Token) ? null : await _resetTokens.GetValidAsync(HashToken(req.Token.Trim()), now, ct);
        if (token == null)
            throw BusinessException.Validation("Đường dẫn đặt lại mật khẩu không còn hiệu lực. Vui lòng yêu cầu lại.");
        PasswordPolicy.Ensure(req.NewPassword);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        // Hai request song song cùng một mã: chỉ một bên tiêu thụ được.
        if (!await _resetTokens.TryConsumeAsync(token.TokenId, now, ct))
            throw BusinessException.Validation("Đường dẫn đặt lại mật khẩu không còn hiệu lực. Vui lòng yêu cầu lại.");
        await _resetTokens.InvalidateAllAsync(token.UserId, now, ct);

        var user = token.User;
        user.PasswordHash = _hasher.Hash(req.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.SecurityStamp = NewStamp(); // thu hồi mọi phiên đăng nhập cũ
        user.UpdatedAt = now;
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest req, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var user = await _users.GetAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy tài khoản.");
        if (!_hasher.Verify(req.CurrentPassword ?? "", user.PasswordHash))
        {
            var justLocked = await RegisterFailedAttemptAsync(user, now, ct);
            if (justLocked != null) throw justLocked;
            throw BusinessException.Validation("Mật khẩu hiện tại không đúng.");
        }
        PasswordPolicy.Ensure(req.NewPassword);
        if (_hasher.Verify(req.NewPassword, user.PasswordHash))
            throw BusinessException.Validation("Mật khẩu mới phải khác mật khẩu hiện tại.");

        user.PasswordHash = _hasher.Hash(req.NewPassword);
        user.FailedLoginAttempts = 0;
        user.SecurityStamp = NewStamp(); // các phiên khác phải đăng nhập lại
        user.UpdatedAt = now;
        await _uow.SaveChangesAsync(ct);
    }

    public static string NewStamp() => Guid.NewGuid().ToString("N");

    public static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
