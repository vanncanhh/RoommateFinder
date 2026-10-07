using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

public class PostService
{
    public const decimal MaxPrice = 1_000_000_000m;
    private const int MaxDistanceCandidates = 2000;
    private const int ExtendWindowDays = 3;

    private readonly IPostRepository _posts;
    private readonly ICatalogRepository _catalog;
    private readonly ISavedPostRepository _saved;
    private readonly IConnectionRequestRepository _requests;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _files;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationPublisher _notifier;

    public PostService(IPostRepository posts, ICatalogRepository catalog, ISavedPostRepository saved,
        IConnectionRequestRepository requests, IUserRepository users, IUnitOfWork uow, IFileStorage files,
        ISystemSettings settings, IDateTimeProvider clock, NotificationPublisher notifier)
    {
        _posts = posts;
        _catalog = catalog;
        _saved = saved;
        _requests = requests;
        _users = users;
        _uow = uow;
        _files = files;
        _settings = settings;
        _clock = clock;
        _notifier = notifier;
    }

    // ------------------------------------------------------------------ Đăng tin / sửa tin

    public async Task<PostDetailDto> CreateAsync(CurrentUser user, PostFormData form, IReadOnlyList<FileUpload> images, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var amenityIds = await ValidateFormAsync(form, ct);

        var maxPerDay = await _settings.GetIntAsync(ConfigKeys.PostMaxPerDay, ct);
        if (await _posts.CountCreatedSinceAsync(user.UserId, now.AddHours(-24), ct) >= maxPerDay)
            throw new BusinessException(ErrorCode.TooManyRequests, $"Bạn chỉ được đăng tối đa {maxPerDay} tin trong 24 giờ.");

        var extensions = await ValidateImagesAsync(form.PostType, keptCount: 0, images, ct);

        var post = new Post { UserId = user.UserId, Status = PostStatus.Pending, CreatedAt = now, SubmittedAt = now };
        ApplyForm(post, form, amenityIds);

        var savedUrls = new List<string>();
        try
        {
            for (var i = 0; i < images.Count; i++)
            {
                var url = await _files.SaveAsync(images[i].Content, extensions[i], "posts", ct);
                savedUrls.Add(url);
                post.Images.Add(new PostImage { ImageUrl = url, SortOrder = i });
            }
            // Tin và thông báo cho kiểm duyệt viên lưu trong một giao dịch: lỗi giữa chừng không để lại tin "mồ côi".
            await using var tx = await _uow.BeginTransactionAsync(ct);
            _posts.Add(post);
            await _uow.SaveChangesAsync(ct);
            await NotifyModeratorsAsync(post, ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            foreach (var url in savedUrls) await _files.DeleteAsync(url, ct);
            throw;
        }
        await _notifier.FlushAsync(ct);
        return await GetDetailForAsync(user, post.PostId, countView: false, ct);
    }

    public async Task<PostDetailDto> UpdateAsync(CurrentUser user, long postId, PostFormData form, IReadOnlyList<FileUpload> newImages, CancellationToken ct = default)
    {
        var post = await GetOwnedAsync(user, postId, withDetails: true, ct);
        if (post.Status is not (PostStatus.Pending or PostStatus.Approved or PostStatus.Rejected))
            throw BusinessException.Conflict(post.Status == PostStatus.Hidden
                ? "Tin đang bị ẩn do vi phạm, không thể sửa."
                : "Chỉ sửa được tin đang chờ duyệt, đang hiển thị hoặc bị từ chối.");

        if (post.PostType != form.PostType)
            throw BusinessException.Validation("Không thể đổi loại tin sau khi đăng. Hãy đăng tin mới.");

        var amenityIds = await ValidateFormAsync(form, ct);
        var keep = post.Images.Where(i => form.KeepImageIds.Contains(i.ImageId)).ToList();
        var removed = post.Images.Except(keep).ToList();
        var extensions = await ValidateImagesAsync(form.PostType, keep.Count, newImages, ct);

        var wasPending = post.Status == PostStatus.Pending;
        ApplyForm(post, form, amenityIds);
        post.Status = PostStatus.Pending;
        post.RejectReason = null;
        post.ExpiredAt = null;
        post.UpdatedAt = _clock.UtcNow;
        post.SubmittedAt = post.UpdatedAt;

        foreach (var img in removed) post.Images.Remove(img);
        var order = keep.OrderBy(i => i.SortOrder).ToList();
        for (var i = 0; i < order.Count; i++) order[i].SortOrder = i;

        var savedUrls = new List<string>();
        try
        {
            for (var i = 0; i < newImages.Count; i++)
            {
                var url = await _files.SaveAsync(newImages[i].Content, extensions[i], "posts", ct);
                savedUrls.Add(url);
                post.Images.Add(new PostImage { ImageUrl = url, SortOrder = order.Count + i });
            }
            if (!wasPending) await NotifyModeratorsAsync(post, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch
        {
            foreach (var url in savedUrls) await _files.DeleteAsync(url, ct);
            throw;
        }

        foreach (var img in removed) await _files.DeleteAsync(img.ImageUrl, ct);
        await _notifier.FlushAsync(ct);
        return await GetDetailForAsync(user, postId, countView: false, ct);
    }

    /// <summary>Kiểm tra dữ liệu form; trả về danh sách tiện ích hợp lệ.</summary>
    private async Task<List<int>> ValidateFormAsync(PostFormData form, CancellationToken ct)
    {
        if (!PostType.All.Contains(form.PostType)) throw BusinessException.Validation("Loại tin không hợp lệ.");
        var title = (form.Title ?? "").Trim();
        if (title.Length is < 10 or > 150) throw BusinessException.Validation("Tiêu đề phải từ 10 đến 150 ký tự.");
        if (form.Description?.Length > 2000) throw BusinessException.Validation("Mô tả tối đa 2000 ký tự.");
        if (form.Price <= 0 || form.Price > MaxPrice) throw BusinessException.Validation("Giá phải lớn hơn 0 và không quá 1 tỷ đồng.");
        if (form.Price != Math.Round(form.Price)) throw BusinessException.Validation("Giá phải là số nguyên (đồng).");
        if (form.Address?.Trim().Length > 255) throw BusinessException.Validation("Địa chỉ tối đa 255 ký tự.");
        if (form.Latitude.HasValue != form.Longitude.HasValue) throw BusinessException.Validation("Cần nhập đủ cả vĩ độ và kinh độ.");
        if (form.Latitude is < -90 or > 90 || form.Longitude is < -180 or > 180) throw BusinessException.Validation("Tọa độ không hợp lệ.");
        if (form.CurrentOccupants is < 0 or > 20) throw BusinessException.Validation("Số người hiện tại phải từ 0 đến 20.");
        if (form.NeededOccupants is < 1 or > 20) throw BusinessException.Validation("Số người cần thêm phải từ 1 đến 20.");
        if (form.PreferredGender != null && !Gender.PreferredValues.Contains(form.PreferredGender))
            throw BusinessException.Validation("Giới tính mong muốn không hợp lệ.");

        var area = await _catalog.GetAreaAsync(form.AreaId, ct);
        if (area is not { IsActive: true }) throw BusinessException.Validation("Khu vực không tồn tại.");

        var requested = form.AmenityIds.Distinct().ToList();
        var valid = await _catalog.GetActiveAmenityIdsAsync(requested, ct);
        if (valid.Count != requested.Count) throw BusinessException.Validation("Có tiện ích không hợp lệ.");
        return valid;
    }

    /// <summary>BR-10 + NFR-06: số lượng, dung lượng, định dạng thật của ảnh. Trả về đuôi file theo thứ tự.</summary>
    private async Task<List<string>> ValidateImagesAsync(string postType, int keptCount, IReadOnlyList<FileUpload> images, CancellationToken ct)
    {
        var maxImages = await _settings.GetIntAsync(ConfigKeys.PostMaxImages, ct);
        var maxMb = await _settings.GetIntAsync(ConfigKeys.ImageMaxSizeMb, ct);
        var total = keptCount + images.Count;
        if (total > maxImages) throw BusinessException.Validation($"Mỗi tin tối đa {maxImages} ảnh.");
        if (postType == PostType.HasRoom && total == 0) throw BusinessException.Validation("Tin có phòng cần ít nhất 1 ảnh.");

        var result = new List<string>();
        foreach (var img in images)
        {
            if (img.Length <= 0 || img.Length > maxMb * 1024L * 1024L)
                throw BusinessException.Validation($"Ảnh \"{img.FileName}\" vượt quá {maxMb} MB.");
            var ext = await ImageValidator.DetectExtensionAsync(img.Content, ct)
                      ?? throw BusinessException.Validation($"Tệp \"{img.FileName}\" không phải ảnh JPG, PNG hoặc WEBP.");
            result.Add(ext);
        }
        return result;
    }

    private static void ApplyForm(Post post, PostFormData form, List<int> amenityIds)
    {
        post.PostType = form.PostType;
        post.Title = form.Title.Trim();
        post.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        post.Price = form.Price;
        post.AreaId = form.AreaId;
        post.Address = string.IsNullOrWhiteSpace(form.Address) ? null : form.Address.Trim();
        post.Latitude = form.Latitude;
        post.Longitude = form.Longitude;
        post.CurrentOccupants = form.CurrentOccupants;
        post.NeededOccupants = form.NeededOccupants;
        post.PreferredGender = form.PreferredGender;

        post.PostAmenities.RemoveAll(pa => !amenityIds.Contains(pa.AmenityId));
        foreach (var id in amenityIds.Where(id => post.PostAmenities.All(pa => pa.AmenityId != id)))
            post.PostAmenities.Add(new PostAmenity { AmenityId = id });
    }

    private async Task NotifyModeratorsAsync(Post post, CancellationToken ct)
    {
        var mods = await _users.GetActiveIdsByRoleAsync(Roles.Moderator, ct);
        if (mods.Count == 0) mods = await _users.GetActiveIdsByRoleAsync(Roles.Admin, ct);
        _notifier.QueueMany(mods.Where(id => id != post.UserId), NotificationType.PostSubmitted,
            $"Tin mới chờ duyệt: \"{post.Title}\"", post.PostId, "/moderator");
    }

    // ------------------------------------------------------------------ Quản lý tin của tôi

    public async Task DeleteAsync(CurrentUser user, long postId, CancellationToken ct = default)
    {
        var post = await GetOwnedAsync(user, postId, withDetails: false, ct);
        var now = _clock.UtcNow;
        await using var tx = await _uow.BeginTransactionAsync(ct);
        // Khóa dòng tin trước (cùng thứ tự với chấp nhận kết nối) rồi mới hết hạn các yêu cầu — tránh deadlock.
        if (!await _posts.TrySoftDeleteAsync(postId, now, ct)) throw BusinessException.NotFound("Không tìm thấy tin đăng.");
        await ExpirePendingRequestsAsync(post, "đã bị chủ tin xóa", now, ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _notifier.FlushAsync(ct);
    }

    public async Task<PostDetailDto> ExtendAsync(CurrentUser user, long postId, CancellationToken ct = default)
    {
        var post = await GetOwnedAsync(user, postId, withDetails: false, ct);
        var now = _clock.UtcNow;
        if (post.Status is not (PostStatus.Approved or PostStatus.Expired))
            throw BusinessException.Conflict("Chỉ gia hạn được tin đang hiển thị hoặc đã hết hạn.");
        var maxExtend = await _settings.GetIntAsync(ConfigKeys.PostMaxExtend, ct);
        if (post.ExtendCount >= maxExtend)
            throw BusinessException.Conflict($"Tin đã gia hạn đủ {maxExtend} lần. Vui lòng đăng tin mới.");
        if (post.Status == PostStatus.Approved && post.ExpiredAt is { } exp && exp > now.AddDays(ExtendWindowDays))
            throw BusinessException.Conflict($"Tin còn hạn đến {VnTime.FormatDate(exp)}. Chỉ gia hạn khi còn dưới {ExtendWindowDays} ngày.");

        var days = await _settings.GetIntAsync(ConfigKeys.PostExpireDays, ct);
        post.ExpiredAt = now.AddDays(days);
        post.Status = PostStatus.Approved; // gia hạn không đổi nội dung nên không cần duyệt lại
        post.ExtendCount++;
        post.UpdatedAt = now;
        await _uow.SaveChangesAsync(ct);
        return await GetDetailForAsync(user, postId, countView: false, ct);
    }

    public async Task<PostDetailDto> CloseAsync(CurrentUser user, long postId, CancellationToken ct = default)
    {
        var post = await GetOwnedAsync(user, postId, withDetails: false, ct);
        if (post.Status != PostStatus.Approved) throw BusinessException.Conflict("Chỉ đóng được tin đang hiển thị.");
        var now = _clock.UtcNow;
        await using var tx = await _uow.BeginTransactionAsync(ct);
        // UPDATE có điều kiện: kiểm duyệt viên vừa ẩn tin thì không ghi đè thành closed.
        if (!await _posts.TryCloseAsync(postId, now, ct)) throw BusinessException.Conflict("Tin vừa thay đổi trạng thái, không thể đóng.");
        await ExpirePendingRequestsAsync(post, "đã được đóng (đã tìm được người)", now, ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _notifier.FlushAsync(ct);
        return await GetDetailForAsync(user, postId, countView: false, ct);
    }

    private async Task ExpirePendingRequestsAsync(Post post, string why, DateTime now, CancellationToken ct)
    {
        var senders = await _requests.GetPendingSenderIdsAsync(post.PostId, ct);
        if (senders.Count == 0) return;
        await _requests.ExpirePendingForPostAsync(post.PostId, now, ct);
        _notifier.QueueMany(senders, NotificationType.RequestExpired,
            $"Yêu cầu kết nối của bạn hết hiệu lực vì tin \"{post.Title}\" {why}.", post.PostId, "/connections?tab=outgoing");
    }

    public Task<List<PostSummaryDto>> GetMineAsync(CurrentUser user, string? status, CancellationToken ct = default)
    {
        if (status != null && !PostStatus.All.Contains(status)) throw BusinessException.Validation("Trạng thái không hợp lệ.");
        return _posts.GetByOwnerAsync(user.UserId, status, ct);
    }

    private async Task<Post> GetOwnedAsync(CurrentUser user, long postId, bool withDetails, CancellationToken ct)
    {
        var post = withDetails ? await _posts.GetWithDetailsAsync(postId, ct) : await _posts.GetAsync(postId, ct);
        if (post == null || post.IsDeleted) throw BusinessException.NotFound("Không tìm thấy tin đăng.");
        if (post.UserId != user.UserId) throw BusinessException.Forbidden("Bạn chỉ thao tác được trên tin của mình.");
        return post;
    }

    // ------------------------------------------------------------------ Lưu tin

    public async Task SaveAsync(CurrentUser user, long postId, CancellationToken ct = default)
    {
        var post = await _posts.GetDetailAsync(postId, ct);
        if (post == null || !IsPublic(post.Status, post.ExpiredAt, _clock.UtcNow)) throw BusinessException.NotFound("Không tìm thấy tin đăng.");
        if (await _saved.ExistsAsync(user.UserId, postId, ct)) return;
        _saved.Add(new SavedPost { UserId = user.UserId, PostId = postId, SavedAt = _clock.UtcNow });
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            // Đã lưu ở request song song — coi như thành công.
        }
    }

    public Task UnsaveAsync(CurrentUser user, long postId, CancellationToken ct = default) =>
        _saved.RemoveAsync(user.UserId, postId, ct);

    public Task<List<PostSummaryDto>> GetSavedAsync(CurrentUser user, CancellationToken ct = default) =>
        _saved.GetSavedAsync(user.UserId, ct);

    // ------------------------------------------------------------------ Xem và tìm kiếm

    public static bool IsPublic(string status, DateTime? expiredAt, DateTime now) =>
        status == PostStatus.Approved && (expiredAt == null || expiredAt > now);

    public Task<PostDetailDto> GetDetailAsync(CurrentUser? viewer, long postId, bool countView = true, CancellationToken ct = default) =>
        GetDetailForAsync(viewer, postId, countView, ct);

    private async Task<PostDetailDto> GetDetailForAsync(CurrentUser? viewer, long postId, bool countView, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var dto = await _posts.GetDetailAsync(postId, ct) ?? throw BusinessException.NotFound("Không tìm thấy tin đăng.");
        var isOwner = viewer != null && viewer.UserId == dto.OwnerId;
        var isPublic = IsPublic(dto.Status, dto.ExpiredAt, now);
        var mine = viewer != null && !isOwner ? await _requests.GetLatestAsync(postId, viewer.UserId, ct) : null;
        // Tin không còn công khai (đã đóng, hết hạn...) vẫn xem được bởi chủ tin, kiểm duyệt viên,
        // và người đã được chấp nhận kết nối (để xem lại thông tin liên hệ).
        var canView = isPublic || isOwner || viewer?.IsModerator == true
                      || (mine?.Status == RequestStatus.Accepted && dto.Status != PostStatus.Hidden);
        if (!canView) throw BusinessException.NotFound("Không tìm thấy tin đăng.");

        dto.IsOwner = isOwner;
        // Lý do ẩn/từ chối chỉ dành cho chủ tin và kiểm duyệt viên.
        if (!isOwner && viewer?.IsModerator != true) dto.RejectReason = null;
        var phone = dto.OwnerPhone;
        dto.OwnerPhone = null;
        if (viewer != null)
        {
            dto.IsSaved = await _saved.ExistsAsync(viewer.UserId, postId, ct);
            if (!isOwner)
            {
                dto.MyRequestId = mine?.RequestId;
                dto.MyRequestStatus = mine?.Status;
                dto.CanSendRequest = isPublic && dto.NeededOccupants > 0
                                     && mine?.Status is not (RequestStatus.Pending or RequestStatus.Accepted);
            }
            // NFR-05: số điện thoại chỉ hiện với chủ tin, kiểm duyệt viên, hoặc người đã được chấp nhận kết nối.
            if (isOwner || viewer.IsModerator || dto.MyRequestStatus == RequestStatus.Accepted)
                dto.OwnerPhone = phone;
        }

        if (countView && !isOwner && isPublic)
            await _posts.IncrementViewCountAsync(postId, ct);
        return dto;
    }

    public async Task<PagedResult<PostSummaryDto>> SearchAsync(PostSearchQuery q, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var (page, pageSize) = Paging.Normalize(q.Page, q.PageSize);
        if (q.PostType != null && !PostType.All.Contains(q.PostType)) throw BusinessException.Validation("Loại tin không hợp lệ.");
        if (q.Gender != null && !Gender.UserValues.Contains(q.Gender)) throw BusinessException.Validation("Giới tính không hợp lệ.");
        if (q.MinPrice is < 0 or > MaxPrice || q.MaxPrice is < 0 or > MaxPrice)
            throw BusinessException.Validation("Khoảng giá không hợp lệ (0 – 1 tỷ đồng).");
        if (q.MinPrice > q.MaxPrice) throw BusinessException.Validation("Giá tối thiểu phải nhỏ hơn giá tối đa.");
        if (q.MinNeeded is < 0 or > 20) throw BusinessException.Validation("Số người cần thêm không hợp lệ.");
        q.Keyword = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim();

        if (q.LandmarkId is not { } landmarkId)
            return await _posts.SearchAsync(q, page, pageSize, now, ct);

        // Tìm theo khoảng cách: lọc thô bằng khung tọa độ trong SQL, tính Haversine chính xác ở đây (quyết định e).
        var landmark = await _catalog.GetLandmarkAsync(landmarkId, ct);
        if (landmark is not { IsActive: true }) throw BusinessException.Validation("Địa điểm mốc không tồn tại.");
        var maxKm = await _settings.GetIntAsync(ConfigKeys.SearchMaxDistanceKm, ct);
        var radius = Math.Clamp(q.RadiusKm ?? maxKm, 0.5, maxKm);
        var lat = (double)landmark.Latitude;
        var lon = (double)landmark.Longitude;

        var candidates = await _posts.SearchCandidatesAsync(q, GeoUtils.BoundingBox(lat, lon, radius), now, MaxDistanceCandidates, ct);
        foreach (var c in candidates)
        {
            if (c.Latitude is { } pLat && c.Longitude is { } pLon)
                c.DistanceKm = Math.Round(GeoUtils.HaversineKm(lat, lon, (double)pLat, (double)pLon), 2);
        }
        var filtered = candidates.Where(c => c.DistanceKm <= radius);
        filtered = q.Sort switch
        {
            "price_asc" => filtered.OrderBy(c => c.Price).ThenBy(c => c.DistanceKm),
            "price_desc" => filtered.OrderByDescending(c => c.Price).ThenBy(c => c.DistanceKm),
            "newest" => filtered.OrderByDescending(c => c.CreatedAt),
            _ => filtered.OrderBy(c => c.DistanceKm).ThenByDescending(c => c.CreatedAt),
        };
        var list = filtered.ToList();
        return PagedResult<PostSummaryDto>.Create(list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, list.Count);
    }

    public Task<List<PostSummaryDto>> GetLatestAsync(int count, CancellationToken ct = default) =>
        _posts.GetLatestAsync(Math.Clamp(count, 1, 20), _clock.UtcNow, ct);
}
