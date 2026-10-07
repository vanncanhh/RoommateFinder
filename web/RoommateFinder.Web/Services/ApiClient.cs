using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace RoommateFinder.Web.Services;

/// <summary>Lỗi trả về từ API dạng { code, message } — Message là tiếng Việt, hiển thị trực tiếp được.</summary>
public class ApiException : Exception
{
    public HttpStatusCode Status { get; }
    public string Code { get; }

    public ApiException(HttpStatusCode status, string code, string message) : base(message)
    {
        Status = status;
        Code = code;
    }

    public bool IsConflict => Status == HttpStatusCode.Conflict;
    public bool IsNotFound => Status == HttpStatusCode.NotFound;
}

/// <summary>
/// Lớp gọi HTTP dùng chung: gắn JWT, đổi lỗi thành <see cref="ApiException"/>,
/// tự đăng xuất khi phiên hết hạn (401) hoặc tài khoản bị khóa giữa phiên (403 ACCOUNT_LOCKED).
/// Trang/Component không dùng HttpClient trực tiếp mà đi qua các lớp *Api trong Services/Api.cs.
/// </summary>
public class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Endpoint mà 401/423 là lỗi nghiệp vụ hiển thị tại form, không phải hết phiên.</summary>
    private static readonly string[] AuthFormPaths =
        { "api/auth/login", "api/auth/register", "api/auth/forgot-password", "api/auth/reset-password" };

    private readonly HttpClient _http;
    private readonly AuthSession _session;
    private readonly NavigationManager _nav;
    private readonly ToastService _toast;

    public ApiClient(HttpClient http, AuthSession session, NavigationManager nav, ToastService toast)
    {
        _http = http;
        _session = session;
        _nav = nav;
        _toast = toast;
    }

    /// <summary>Đổi URL ảnh tương đối (/uploads/...) thành URL đầy đủ tới backend.</summary>
    public string Url(string? relative) =>
        string.IsNullOrEmpty(relative) ? "" : relative.StartsWith("http") ? relative : new Uri(_http.BaseAddress!, relative).ToString();

    public Uri BaseAddress => _http.BaseAddress!;

    public Task<T> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<T> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, Content(body));
    public Task<T> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, Content(body));
    public Task PostAsync(string url, object? body = null) => SendAsync<object?>(HttpMethod.Post, url, Content(body), expectBody: false);
    public Task PutAsync(string url, object? body = null) => SendAsync<object?>(HttpMethod.Put, url, Content(body), expectBody: false);
    public Task DeleteAsync(string url) => SendAsync<object?>(HttpMethod.Delete, url, null, expectBody: false);
    public Task<T> SendFormAsync<T>(HttpMethod method, string url, MultipartFormDataContent form) => SendAsync<T>(method, url, form);

    private static HttpContent? Content(object? body) => body == null ? null : JsonContent.Create(body, body.GetType(), options: Json);

    private async Task<T> SendAsync<T>(HttpMethod method, string url, HttpContent? content, bool expectBody = true)
    {
        using var req = new HttpRequestMessage(method, url) { Content = content };
        if (_session.Token is { } token) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage res;
        try
        {
            res = await _http.SendAsync(req);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(0, "NETWORK", "Không kết nối được máy chủ. Vui lòng kiểm tra backend đang chạy.");
        }

        using (res)
        {
            if (res.IsSuccessStatusCode)
            {
                if (!expectBody || res.StatusCode == HttpStatusCode.NoContent) return default!;
                return (await res.Content.ReadFromJsonAsync<T>(Json))!;
            }

            var (code, message) = await ReadErrorAsync(res);
            var isAuthForm = AuthFormPaths.Any(p => url.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (!isAuthForm && _session.Token != null)
            {
                if (res.StatusCode == HttpStatusCode.Unauthorized && code == "UNAUTHORIZED_EMPTY")
                    await ExpireSessionAsync("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                else if (res.StatusCode == HttpStatusCode.Forbidden && code == "ACCOUNT_LOCKED")
                    await ExpireSessionAsync(message);
            }
            throw new ApiException(res.StatusCode, code, message);
        }
    }

    private static async Task<(string Code, string Message)> ReadErrorAsync(HttpResponseMessage res)
    {
        var text = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            // 401 không có body = token hết hạn / không hợp lệ (khác 401 có body = sai mật khẩu)
            return res.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ("UNAUTHORIZED_EMPTY", "Phiên đăng nhập đã hết hạn."),
                HttpStatusCode.Forbidden => ("FORBIDDEN", "Bạn không có quyền thực hiện thao tác này."),
                HttpStatusCode.NotFound => ("NOT_FOUND", "Không tìm thấy dữ liệu."),
                _ => ("SERVER_ERROR", "Hệ thống đang gặp sự cố. Vui lòng thử lại sau."),
            };
        }
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var code = root.TryGetProperty("code", out var c) ? c.GetString() ?? "ERROR" : "ERROR";
            var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            return (code, string.IsNullOrEmpty(message) ? "Đã xảy ra lỗi." : message);
        }
        catch (JsonException)
        {
            return ("ERROR", "Đã xảy ra lỗi.");
        }
    }

    private async Task ExpireSessionAsync(string message)
    {
        await _session.LogoutAsync();
        _toast.Warning(message);
        var path = _nav.ToBaseRelativePath(_nav.Uri);
        var returnUrl = string.IsNullOrEmpty(path) || path.StartsWith("login") ? "" : "?returnUrl=" + Uri.EscapeDataString("/" + path);
        _nav.NavigateTo("/login" + returnUrl);
    }
}
