using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Application.Services;

public class ProfileService
{
    private readonly IUserRepository _users;
    private readonly ICatalogRepository _catalog;
    private readonly IReviewRepository _reviews;
    private readonly IPostRepository _posts;
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _files;
    private readonly ISystemSettings _settings;
    private readonly IDateTimeProvider _clock;

    public ProfileService(IUserRepository users, ICatalogRepository catalog, IReviewRepository reviews, IPostRepository posts,
        IUnitOfWork uow, IFileStorage files, ISystemSettings settings, IDateTimeProvider clock)
    {
        _users = users;
        _catalog = catalog;
        _reviews = reviews;
        _posts = posts;
        _uow = uow;
        _files = files;
        _settings = settings;
        _clock = clock;
    }

    public async Task<ProfileDto> GetAsync(long userId, CancellationToken ct = default) =>
        await _users.GetProfileAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy tài khoản.");

    public async Task<ProfileDto> UpdateAsync(long userId, UpdateProfileRequest req, CancellationToken ct = default)
    {
        var user = await _users.GetAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy tài khoản.");

        var fullName = (req.FullName ?? "").Trim();
        if (fullName.Length is < 2 or > 100) throw BusinessException.Validation("Họ tên phải từ 2 đến 100 ký tự.");
        AuthService.ValidatePhone(req.Phone);
        EnsureIn(req.Gender, Gender.UserValues, "Giới tính");
        EnsureIn(req.Occupation, Occupation.All, "Nghề nghiệp");
        EnsureIn(req.SleepSchedule, SleepSchedule.All, "Giờ giấc sinh hoạt");
        if (req.CleanlinessLevel is < 1 or > 5) throw BusinessException.Validation("Mức độ gọn gàng phải từ 1 đến 5.");
        if (req.SchoolOrCompany?.Trim().Length > 150) throw BusinessException.Validation("Trường/công ty tối đa 150 ký tự.");
        if (req.Bio?.Trim().Length > 500) throw BusinessException.Validation("Giới thiệu tối đa 500 ký tự.");
        if (req.DateOfBirth is { } dob)
        {
            var today = VnTime.Today(_clock.UtcNow);
            if (dob > today.AddYears(-16) || dob < today.AddYears(-100))
                throw BusinessException.Validation("Ngày sinh không hợp lệ (người dùng phải từ 16 tuổi).");
        }
        if (req.LandmarkId is { } landmarkId)
        {
            var landmark = await _catalog.GetLandmarkAsync(landmarkId, ct);
            if (landmark is not { IsActive: true }) throw BusinessException.Validation("Địa điểm mốc không tồn tại.");
        }

        user.FullName = fullName;
        user.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
        user.Gender = req.Gender;
        user.DateOfBirth = req.DateOfBirth;
        user.Occupation = req.Occupation;
        user.SchoolOrCompany = string.IsNullOrWhiteSpace(req.SchoolOrCompany) ? null : req.SchoolOrCompany.Trim();
        user.LandmarkId = req.LandmarkId;
        user.Bio = string.IsNullOrWhiteSpace(req.Bio) ? null : req.Bio.Trim();
        user.SleepSchedule = req.SleepSchedule;
        user.IsSmoker = req.IsSmoker;
        user.HasPet = req.HasPet;
        user.CleanlinessLevel = req.CleanlinessLevel;
        user.UpdatedAt = _clock.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<ProfileDto> UploadAvatarAsync(long userId, FileUpload file, CancellationToken ct = default)
    {
        var user = await _users.GetAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy tài khoản.");
        var maxMb = await _settings.GetIntAsync(ConfigKeys.ImageMaxSizeMb, ct);
        if (file.Length <= 0 || file.Length > maxMb * 1024L * 1024L)
            throw BusinessException.Validation($"Ảnh đại diện phải nhỏ hơn {maxMb} MB.");
        var ext = await ImageValidator.DetectExtensionAsync(file.Content, ct)
                  ?? throw BusinessException.Validation("Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.");

        var oldUrl = user.AvatarUrl;
        user.AvatarUrl = await _files.SaveAsync(file.Content, ext, "avatars", ct);
        user.UpdatedAt = _clock.UtcNow;
        await _uow.SaveChangesAsync(ct);
        if (oldUrl != null) await _files.DeleteAsync(oldUrl, ct);
        return await GetAsync(userId, ct);
    }

    public async Task<PublicProfileDto> GetPublicAsync(long userId, CancellationToken ct = default) =>
        await _users.GetPublicProfileAsync(userId, ct) ?? throw BusinessException.NotFound("Không tìm thấy người dùng.");

    public Task<PagedResult<ReviewDto>> GetReviewsAsync(long userId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize, 10);
        return _reviews.GetForUserAsync(userId, includeHidden: false, page, pageSize, ct);
    }

    public Task<List<PostSummaryDto>> GetPublicPostsAsync(long userId, CancellationToken ct = default) =>
        _posts.GetPublicByOwnerAsync(userId, _clock.UtcNow, ct);

    private static void EnsureIn(string? value, string[] allowed, string field)
    {
        if (value != null && !allowed.Contains(value)) throw BusinessException.Validation($"{field} không hợp lệ.");
    }
}
