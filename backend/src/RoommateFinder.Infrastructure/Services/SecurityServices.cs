using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoommateFinder.Application.Abstractions;

namespace RoommateFinder.Infrastructure.Services;

/// <summary>NFR-03: BCrypt, work factor 11.</summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "RoommateFinder";
    public string Audience { get; set; } = "RoommateFinder.Web";
    /// <summary>Đặt bằng user-secrets / biến môi trường, tối thiểu 32 ký tự. Không commit.</summary>
    public string Key { get; set; } = "";
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public const string SecurityStampClaim = "sst";

    public (string Token, DateTime ExpiresAt) Generate(long userId, string email, string role, string securityStamp, int lifetimeMinutes)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(lifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(SecurityStampClaim, securityStamp),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now, expires,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
