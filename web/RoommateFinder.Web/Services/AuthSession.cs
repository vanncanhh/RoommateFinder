using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Web.Services;

/// <summary>
/// Phiên đăng nhập phía client: giữ JWT (localStorage) và thông tin người dùng hiện tại.
/// Mọi nơi cần biết "ai đang đăng nhập" đều đọc từ đây; thay đổi phát qua sự kiện <see cref="Changed"/>.
/// </summary>
public class AuthSession
{
    private const string TokenKey = "rf.token";
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;

    public AuthSession(IJSRuntime js, HttpClient http)
    {
        _js = js;
        _http = http;
    }

    public string? Token { get; private set; }
    public CurrentUserDto? User { get; private set; }
    public bool IsInitialized { get; private set; }

    public bool IsAuthenticated => User != null && Token != null;
    public long UserId => User?.UserId ?? 0;
    public string? Role => User?.Role;
    public bool IsModerator => Role is Roles.Moderator or Roles.Admin;
    public bool IsAdmin => Role == Roles.Admin;

    /// <summary>Phát khi đăng nhập / đăng xuất / cập nhật thông tin người dùng.</summary>
    public event Action? Changed;

    /// <summary>Gọi một lần khi ứng dụng khởi động: đọc token đã lưu và tải lại thông tin người dùng.</summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        try
        {
            var token = await _js.InvokeAsync<string?>("rf.storageGet", TokenKey);
            if (!string.IsNullOrEmpty(token) && !IsExpired(token))
            {
                Token = token;
                User = await FetchMeAsync(token);
                if (User == null) await ClearAsync();
            }
            else if (token != null)
            {
                await ClearAsync();
            }
        }
        finally
        {
            IsInitialized = true;
            Changed?.Invoke();
        }
    }

    public async Task LoginAsync(AuthResponse response)
    {
        Token = response.AccessToken;
        User = response.User;
        await _js.InvokeVoidAsync("rf.storageSet", TokenKey, Token);
        Changed?.Invoke();
    }

    public async Task LogoutAsync()
    {
        await ClearAsync();
        Changed?.Invoke();
    }

    /// <summary>Tải lại tên/ảnh đại diện sau khi sửa hồ sơ.</summary>
    public async Task RefreshUserAsync()
    {
        if (Token == null) return;
        var me = await FetchMeAsync(Token);
        if (me != null)
        {
            User = me;
            Changed?.Invoke();
        }
    }

    private async Task ClearAsync()
    {
        Token = null;
        User = null;
        await _js.InvokeVoidAsync("rf.storageRemove", TokenKey);
    }

    private async Task<CurrentUserDto?> FetchMeAsync(string token)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<CurrentUserDto>(ApiClient.Json) : null;
        }
        catch (HttpRequestException)
        {
            return null; // backend chưa chạy: coi như chưa đăng nhập
        }
    }

    /// <summary>Đọc claim "exp" trong phần payload của JWT (không cần kiểm chữ ký — server sẽ kiểm).</summary>
    public static bool IsExpired(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return true;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            if (!doc.RootElement.TryGetProperty("exp", out var exp)) return false;
            return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) <= DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
