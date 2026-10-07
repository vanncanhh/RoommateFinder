using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>Quyết định (g): đọc SystemConfigs có cache 5 phút, xóa cache khi Admin sửa.</summary>
public class SystemSettings : ISystemSettings
{
    private const string CacheKey = "system-configs";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public SystemSettings(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<int> GetIntAsync(string key, CancellationToken ct = default)
    {
        var all = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _db.SystemConfigs.AsNoTracking().ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue, ct);
        });
        if (all != null && all.TryGetValue(key, out var raw) && int.TryParse(raw, out var value)) return value;

        // Thiếu trong CSDL thì dùng giá trị seed mặc định.
        var fallback = SeedData.SystemConfigs.FirstOrDefault(c => c.ConfigKey == key)
                       ?? throw new InvalidOperationException($"Thiếu cấu hình {key}.");
        return int.Parse(fallback.ConfigValue);
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}

public class SmtpOptions
{
    public const string Section = "Smtp";
    /// <summary>Để trống thì chỉ ghi email ra log (môi trường dev).</summary>
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@roommatefinder.local";
    public string FromName { get; set; } = "RoommateFinder";
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogInformation("[Email chưa cấu hình SMTP — chỉ ghi log] To: {To} | {Subject}\n{Body}", to, subject, htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTlsWhenAvailable, ct);
        if (!string.IsNullOrEmpty(_options.User)) await client.AuthenticateAsync(_options.User, _options.Password ?? "", ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}

/// <summary>Lưu ảnh vào wwwroot/uploads với tên GUID do server sinh (NFR-06).</summary>
public class LocalFileStorage : IFileStorage
{
    private const string UrlPrefix = "/uploads/";
    private readonly string _root;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _root = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default)
    {
        if (extension is not (".jpg" or ".png" or ".webp")) throw new ArgumentException("Đuôi file không hợp lệ.", nameof(extension));
        if (folder.Any(c => !char.IsLetterOrDigit(c))) throw new ArgumentException("Thư mục không hợp lệ.", nameof(folder));

        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine(folder, now.ToString("yyyy"), now.ToString("MM"));
        var dir = Path.Combine(_root, relativeDir);
        Directory.CreateDirectory(dir);
        var fileName = Guid.NewGuid().ToString("N") + extension;

        if (content.CanSeek) content.Seek(0, SeekOrigin.Begin);
        await using (var file = new FileStream(Path.Combine(dir, fileName), FileMode.CreateNew, FileAccess.Write))
            await content.CopyToAsync(file, ct);

        return UrlPrefix + relativeDir.Replace('\\', '/') + "/" + fileName;
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        if (!url.StartsWith(UrlPrefix, StringComparison.Ordinal)) return Task.CompletedTask;
        var full = Path.GetFullPath(Path.Combine(_root, url[UrlPrefix.Length..].Replace('/', Path.DirectorySeparatorChar)));
        // Chặn path traversal: chỉ xóa file nằm trong thư mục uploads.
        if (full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
            File.Delete(full);
        return Task.CompletedTask;
    }
}

public class FrontendOptions
{
    public const string Section = "Frontend";
    public string BaseUrl { get; set; } = "http://localhost:5173";
}

public class AppLinks : IAppLinks
{
    private readonly FrontendOptions _options;
    public AppLinks(IOptions<FrontendOptions> options) => _options = options.Value;

    public string ResetPasswordUrl(string token) =>
        $"{_options.BaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
}
