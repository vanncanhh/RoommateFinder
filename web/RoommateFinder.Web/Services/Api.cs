using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;

namespace RoommateFinder.Web.Services;

// Các lớp gọi API theo nhóm — khớp docs/API.md. DTO dùng chung với backend (project Application),
// nên tên trường/kiểu dữ liệu không thể lệch hợp đồng.

/// <summary>Dựng query string: bỏ giá trị null/rỗng, danh sách thì lặp khóa (amenityIds=1&amenityIds=3).</summary>
public class Query
{
    private readonly List<string> _parts = new();

    public Query Add(string key, object? value)
    {
        switch (value)
        {
            case null:
            case string s when string.IsNullOrWhiteSpace(s):
                break;
            case System.Collections.IEnumerable list and not string:
                foreach (var v in list) Add(key, v);
                break;
            case IFormattable f:
                _parts.Add($"{key}={Uri.EscapeDataString(f.ToString(null, CultureInfo.InvariantCulture))}");
                break;
            default:
                _parts.Add($"{key}={Uri.EscapeDataString(value.ToString()!)}");
                break;
        }
        return this;
    }

    public string For(string path) => _parts.Count == 0 ? path : $"{path}?{string.Join('&', _parts)}";
}

public class AuthApi(ApiClient api)
{
    public Task RegisterAsync(RegisterRequest req) => api.PostAsync("api/auth/register", req);
    public Task<AuthResponse> LoginAsync(LoginRequest req) => api.PostAsync<AuthResponse>("api/auth/login", req);
    public Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest req) => api.PostAsync<MessageResponse>("api/auth/forgot-password", req);
    public Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest req) => api.PostAsync<MessageResponse>("api/auth/reset-password", req);
    public Task<MessageResponse> ChangePasswordAsync(ChangePasswordRequest req) => api.PutAsync<MessageResponse>("api/auth/change-password", req);
}

public class ProfileApi(ApiClient api)
{
    public Task<ProfileDto> GetAsync() => api.GetAsync<ProfileDto>("api/profile");
    public Task<ProfileDto> UpdateAsync(UpdateProfileRequest req) => api.PutAsync<ProfileDto>("api/profile", req);

    public async Task<ProfileDto> UploadAvatarAsync(IBrowserFile file, long maxBytes)
    {
        using var form = new MultipartFormDataContent();
        form.Add(FileContent.From(file, maxBytes), "file", file.Name);
        return await api.SendFormAsync<ProfileDto>(HttpMethod.Post, "api/profile/avatar", form);
    }

    public Task<PublicProfileDto> GetPublicAsync(long userId) => api.GetAsync<PublicProfileDto>($"api/users/{userId}");
    public Task<PagedResult<ReviewDto>> GetReviewsAsync(long userId, int page, int pageSize = 10) =>
        api.GetAsync<PagedResult<ReviewDto>>(new Query().Add("page", page).Add("pageSize", pageSize).For($"api/users/{userId}/reviews"));
    public Task<List<PostSummaryDto>> GetPostsAsync(long userId) => api.GetAsync<List<PostSummaryDto>>($"api/users/{userId}/posts");
}

public class CatalogApi(ApiClient api)
{
    public Task<List<AreaDto>> AreasAsync() => api.GetAsync<List<AreaDto>>("api/catalog/areas");
    public Task<List<LandmarkDto>> LandmarksAsync(int? areaId = null) => api.GetAsync<List<LandmarkDto>>(new Query().Add("areaId", areaId).For("api/catalog/landmarks"));
    public Task<List<AmenityDto>> AmenitiesAsync() => api.GetAsync<List<AmenityDto>>("api/catalog/amenities");
    /// <param name="appliesTo">"post" hoặc "user"</param>
    public Task<List<ReportReasonDto>> ReportReasonsAsync(string appliesTo) =>
        api.GetAsync<List<ReportReasonDto>>(new Query().Add("appliesTo", appliesTo).For("api/catalog/report-reasons"));
}

public class PostsApi(ApiClient api)
{
    public Task<PagedResult<PostSummaryDto>> SearchAsync(PostSearchQuery q) => api.GetAsync<PagedResult<PostSummaryDto>>(new Query()
        .Add("keyword", q.Keyword).Add("postType", q.PostType).Add("areaId", q.AreaId)
        .Add("minPrice", q.MinPrice).Add("maxPrice", q.MaxPrice).Add("gender", q.Gender).Add("minNeeded", q.MinNeeded)
        .Add("landmarkId", q.LandmarkId).Add("radiusKm", q.RadiusKm).Add("amenityIds", q.AmenityIds)
        .Add("sort", q.Sort).Add("page", q.Page).Add("pageSize", q.PageSize)
        .For("api/posts/search"));

    public Task<List<PostSummaryDto>> LatestAsync(int count = 8) => api.GetAsync<List<PostSummaryDto>>($"api/posts/latest?count={count}");
    public Task<PostDetailDto> GetAsync(long id) => api.GetAsync<PostDetailDto>($"api/posts/{id}");

    public Task<PostDetailDto> CreateAsync(PostFormData data, IReadOnlyList<IBrowserFile> images, long maxBytes) =>
        api.SendFormAsync<PostDetailDto>(HttpMethod.Post, "api/posts", BuildForm(data, images, maxBytes, includeKeep: false));

    public Task<PostDetailDto> UpdateAsync(long id, PostFormData data, IReadOnlyList<IBrowserFile> images, long maxBytes) =>
        api.SendFormAsync<PostDetailDto>(HttpMethod.Put, $"api/posts/{id}", BuildForm(data, images, maxBytes, includeKeep: true));

    public Task DeleteAsync(long id) => api.DeleteAsync($"api/posts/{id}");
    public Task<PostDetailDto> ExtendAsync(long id) => api.PutAsync<PostDetailDto>($"api/posts/{id}/extend");
    public Task<PostDetailDto> CloseAsync(long id) => api.PutAsync<PostDetailDto>($"api/posts/{id}/close");
    public Task<List<PostSummaryDto>> MineAsync(string? status = null) => api.GetAsync<List<PostSummaryDto>>(new Query().Add("status", status).For("api/posts/mine"));
    public Task<List<PostSummaryDto>> SavedAsync() => api.GetAsync<List<PostSummaryDto>>("api/posts/saved");
    public Task SaveAsync(long id) => api.PostAsync($"api/posts/{id}/save");
    public Task UnsaveAsync(long id) => api.DeleteAsync($"api/posts/{id}/save");

    /// <summary>multipart/form-data: các trường PostFormData + amenityIds/keepImageIds lặp lại + file "images".</summary>
    private static MultipartFormDataContent BuildForm(PostFormData d, IReadOnlyList<IBrowserFile> images, long maxBytes, bool includeKeep)
    {
        var form = new MultipartFormDataContent();
        void Field(string name, object? value)
        {
            if (value == null) return;
            var text = value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
            if (!string.IsNullOrEmpty(text)) form.Add(new StringContent(text), name);
        }
        Field("postType", d.PostType);
        Field("title", d.Title);
        Field("description", d.Description);
        Field("price", d.Price);
        Field("areaId", d.AreaId);
        Field("address", d.Address);
        Field("latitude", d.Latitude);
        Field("longitude", d.Longitude);
        Field("currentOccupants", d.CurrentOccupants);
        Field("neededOccupants", d.NeededOccupants);
        Field("preferredGender", d.PreferredGender);
        foreach (var id in d.AmenityIds) Field("amenityIds", id);
        if (includeKeep) foreach (var id in d.KeepImageIds) Field("keepImageIds", id);
        foreach (var file in images) form.Add(FileContent.From(file, maxBytes), "images", file.Name);
        return form;
    }
}

public class ModerationApi(ApiClient api)
{
    public Task<PagedResult<PostSummaryDto>> PostsAsync(string status, int page, int pageSize = 20) =>
        api.GetAsync<PagedResult<PostSummaryDto>>(new Query().Add("status", status).Add("page", page).Add("pageSize", pageSize).For("api/moderation/posts"));
    public Task<PostDetailDto> ApproveAsync(long postId) => api.PostAsync<PostDetailDto>($"api/moderation/posts/{postId}/approve");
    public Task<PostDetailDto> RejectAsync(long postId, string reason) => api.PostAsync<PostDetailDto>($"api/moderation/posts/{postId}/reject", new RejectPostRequest { Reason = reason });
    public Task<PagedResult<ReportDto>> ReportsAsync(string status, int page, int pageSize = 20) =>
        api.GetAsync<PagedResult<ReportDto>>(new Query().Add("status", status).Add("page", page).Add("pageSize", pageSize).For("api/moderation/reports"));
    public Task<ReportDto> HandleReportAsync(long reportId, HandleReportRequest req) => api.PostAsync<ReportDto>($"api/moderation/reports/{reportId}/handle", req);
    public Task<PagedResult<ViolationDto>> ViolationsAsync(long? userId, int page, int pageSize = 20) =>
        api.GetAsync<PagedResult<ViolationDto>>(new Query().Add("userId", userId).Add("page", page).Add("pageSize", pageSize).For("api/moderation/violations"));
    public Task HideReviewAsync(long reviewId) => api.PutAsync($"api/moderation/reviews/{reviewId}/hide");
    public Task UnhideReviewAsync(long reviewId) => api.PutAsync($"api/moderation/reviews/{reviewId}/unhide");
}

public class ConnectionsApi(ApiClient api)
{
    public Task<ConnectionRequestDto> SendAsync(long postId, string? message) =>
        api.PostAsync<ConnectionRequestDto>("api/connections", new CreateConnectionRequest { PostId = postId, Message = message });
    public Task<List<ConnectionRequestDto>> IncomingAsync(string? status = null) => api.GetAsync<List<ConnectionRequestDto>>(new Query().Add("status", status).For("api/connections/incoming"));
    public Task<List<ConnectionRequestDto>> OutgoingAsync(string? status = null) => api.GetAsync<List<ConnectionRequestDto>>(new Query().Add("status", status).For("api/connections/outgoing"));
    public Task<ConnectionRequestDto> AcceptAsync(long id) => api.PutAsync<ConnectionRequestDto>($"api/connections/{id}/accept");
    public Task<ConnectionRequestDto> RejectAsync(long id) => api.PutAsync<ConnectionRequestDto>($"api/connections/{id}/reject");
    public Task<ConnectionRequestDto> CancelAsync(long id) => api.PutAsync<ConnectionRequestDto>($"api/connections/{id}/cancel");
}

public class ChatApi(ApiClient api)
{
    public Task<List<ConversationDto>> ConversationsAsync() => api.GetAsync<List<ConversationDto>>("api/conversations");
    /// <summary>Trả theo thứ tự cũ → mới; <paramref name="before"/> = messageId để tải tin cũ hơn.</summary>
    public Task<List<MessageDto>> MessagesAsync(long conversationId, long? before = null, int limit = 30) =>
        api.GetAsync<List<MessageDto>>(new Query().Add("before", before).Add("limit", limit).For($"api/conversations/{conversationId}/messages"));
    public Task<MessageDto> SendAsync(long conversationId, string content) =>
        api.PostAsync<MessageDto>($"api/conversations/{conversationId}/messages", new SendMessageRequest { Content = content });
    public Task MarkReadAsync(long conversationId) => api.PutAsync($"api/conversations/{conversationId}/read");
}

public class FeedbackApi(ApiClient api)
{
    public Task<ReviewDto> ReviewAsync(CreateReviewRequest req) => api.PostAsync<ReviewDto>("api/reviews", req);
    public Task<MessageResponse> ReportAsync(CreateReportRequest req) => api.PostAsync<MessageResponse>("api/reports", req);
}

public class NotificationsApi(ApiClient api)
{
    public Task<PagedResult<NotificationDto>> ListAsync(int page = 1, int pageSize = 20) =>
        api.GetAsync<PagedResult<NotificationDto>>($"api/notifications?page={page}&pageSize={pageSize}");
    public async Task<int> UnreadCountAsync() => (await api.GetAsync<UnreadCount>("api/notifications/unread-count")).Count;
    public Task MarkReadAsync(long id) => api.PutAsync($"api/notifications/{id}/read");
    public Task MarkAllReadAsync() => api.PutAsync("api/notifications/read-all");

    private record UnreadCount(int Count);
}

public class AdminApi(ApiClient api)
{
    public Task<PagedResult<AdminUserDto>> UsersAsync(AdminUserQuery q) => api.GetAsync<PagedResult<AdminUserDto>>(new Query()
        .Add("keyword", q.Keyword).Add("role", q.Role).Add("status", q.Status).Add("page", q.Page).Add("pageSize", q.PageSize)
        .For("api/admin/users"));
    public Task<AdminUserDto> ChangeRoleAsync(long userId, string role) => api.PutAsync<AdminUserDto>($"api/admin/users/{userId}/role", new UpdateRoleRequest { Role = role });
    public Task<AdminUserDto> ChangeStatusAsync(long userId, UpdateUserStatusRequest req) => api.PutAsync<AdminUserDto>($"api/admin/users/{userId}/status", req);

    public Task<List<AreaDto>> AreasAsync() => api.GetAsync<List<AreaDto>>("api/admin/areas");
    public Task<AreaDto> SaveAreaAsync(int? id, AreaUpsertRequest req) =>
        id is { } v ? api.PutAsync<AreaDto>($"api/admin/areas/{v}", req) : api.PostAsync<AreaDto>("api/admin/areas", req);
    public Task<List<LandmarkDto>> LandmarksAsync() => api.GetAsync<List<LandmarkDto>>("api/admin/landmarks");
    public Task<LandmarkDto> SaveLandmarkAsync(int? id, LandmarkUpsertRequest req) =>
        id is { } v ? api.PutAsync<LandmarkDto>($"api/admin/landmarks/{v}", req) : api.PostAsync<LandmarkDto>("api/admin/landmarks", req);
    public Task<List<AmenityDto>> AmenitiesAsync() => api.GetAsync<List<AmenityDto>>("api/admin/amenities");
    public Task<AmenityDto> SaveAmenityAsync(int? id, AmenityUpsertRequest req) =>
        id is { } v ? api.PutAsync<AmenityDto>($"api/admin/amenities/{v}", req) : api.PostAsync<AmenityDto>("api/admin/amenities", req);
    public Task<List<ReportReasonDto>> ReportReasonsAsync() => api.GetAsync<List<ReportReasonDto>>("api/admin/report-reasons");
    public Task<ReportReasonDto> SaveReportReasonAsync(int? id, ReportReasonUpsertRequest req) =>
        id is { } v ? api.PutAsync<ReportReasonDto>($"api/admin/report-reasons/{v}", req) : api.PostAsync<ReportReasonDto>("api/admin/report-reasons", req);

    public Task<List<SystemConfigDto>> ConfigsAsync() => api.GetAsync<List<SystemConfigDto>>("api/admin/configs");
    public Task<SystemConfigDto> UpdateConfigAsync(string key, string value) =>
        api.PutAsync<SystemConfigDto>($"api/admin/configs/{Uri.EscapeDataString(key)}", new UpdateConfigRequest { Value = value });
    public Task<DashboardDto> DashboardAsync(DateOnly? from, DateOnly? to) => api.GetAsync<DashboardDto>(new Query()
        .Add("from", from?.ToString("yyyy-MM-dd")).Add("to", to?.ToString("yyyy-MM-dd")).For("api/admin/dashboard"));
}

internal static class FileContent
{
    /// <summary>Đọc file trình duyệt vào multipart (giới hạn dung lượng để không treo trình duyệt với file lớn).</summary>
    public static StreamContent From(IBrowserFile file, long maxBytes)
    {
        var content = new StreamContent(file.OpenReadStream(maxBytes));
        content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
        return content;
    }
}
