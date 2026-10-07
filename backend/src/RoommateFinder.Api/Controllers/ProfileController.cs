using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;

namespace RoommateFinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profiles;
    public ProfileController(ProfileService profiles) => _profiles = profiles;

    [HttpGet]
    public Task<ProfileDto> Get(CancellationToken ct) => _profiles.GetAsync(User.GetUserId(), ct);

    [HttpPut]
    public Task<ProfileDto> Update(UpdateProfileRequest req, CancellationToken ct) => _profiles.UpdateAsync(User.GetUserId(), req, ct);

    [HttpPost("avatar")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ProfileDto> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return await _profiles.UploadAvatarAsync(User.GetUserId(), new FileUpload(file.FileName, file.ContentType, file.Length, stream), ct);
    }
}

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ProfileService _profiles;
    public UsersController(ProfileService profiles) => _profiles = profiles;

    [HttpGet("{id:long}")]
    public Task<PublicProfileDto> Get(long id, CancellationToken ct) => _profiles.GetPublicAsync(id, ct);

    [HttpGet("{id:long}/reviews")]
    public Task<PagedResult<ReviewDto>> Reviews(long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        _profiles.GetReviewsAsync(id, page, pageSize, ct);

    [HttpGet("{id:long}/posts")]
    public Task<List<PostSummaryDto>> Posts(long id, CancellationToken ct) => _profiles.GetPublicPostsAsync(id, ct);
}
