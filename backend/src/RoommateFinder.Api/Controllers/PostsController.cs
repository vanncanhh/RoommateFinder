using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;

namespace RoommateFinder.Api.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    /// <summary>10 ảnh × 5 MB + dữ liệu form; giới hạn thật do POST_MAX_IMAGES / IMAGE_MAX_SIZE_MB kiểm tra ở Service.</summary>
    private const long MaxUploadBytes = 120 * 1024 * 1024;
    private readonly PostService _posts;

    public PostsController(PostService posts) => _posts = posts;

    [HttpGet("search")]
    public Task<PagedResult<PostSummaryDto>> Search([FromQuery] PostSearchQuery query, CancellationToken ct) =>
        _posts.SearchAsync(query, ct);

    [HttpGet("latest")]
    public Task<List<PostSummaryDto>> Latest([FromQuery] int count = 8, CancellationToken ct = default) =>
        _posts.GetLatestAsync(count, ct);

    [HttpGet("{id:long}")]
    public Task<PostDetailDto> Get(long id, CancellationToken ct) =>
        _posts.GetDetailAsync(User.ToCurrentUserOrNull(), id, countView: true, ct);

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<ActionResult<PostDetailDto>> Create([FromForm] PostFormData form, [FromForm] List<IFormFile>? images, CancellationToken ct)
    {
        var uploads = ToUploads(images);
        try
        {
            var dto = await _posts.CreateAsync(User.ToCurrentUser(), form, uploads, ct);
            return CreatedAtAction(nameof(Get), new { id = dto.PostId }, dto);
        }
        finally
        {
            foreach (var u in uploads) await u.Content.DisposeAsync();
        }
    }

    [Authorize]
    [HttpPut("{id:long}")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<PostDetailDto> Update(long id, [FromForm] PostFormData form, [FromForm] List<IFormFile>? images, CancellationToken ct)
    {
        var uploads = ToUploads(images);
        try
        {
            return await _posts.UpdateAsync(User.ToCurrentUser(), id, form, uploads, ct);
        }
        finally
        {
            foreach (var u in uploads) await u.Content.DisposeAsync();
        }
    }

    [Authorize]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _posts.DeleteAsync(User.ToCurrentUser(), id, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPut("{id:long}/extend")]
    public Task<PostDetailDto> Extend(long id, CancellationToken ct) => _posts.ExtendAsync(User.ToCurrentUser(), id, ct);

    [Authorize]
    [HttpPut("{id:long}/close")]
    public Task<PostDetailDto> Close(long id, CancellationToken ct) => _posts.CloseAsync(User.ToCurrentUser(), id, ct);

    [Authorize]
    [HttpGet("mine")]
    public Task<List<PostSummaryDto>> Mine([FromQuery] string? status, CancellationToken ct) =>
        _posts.GetMineAsync(User.ToCurrentUser(), status, ct);

    [Authorize]
    [HttpGet("saved")]
    public Task<List<PostSummaryDto>> Saved(CancellationToken ct) => _posts.GetSavedAsync(User.ToCurrentUser(), ct);

    [Authorize]
    [HttpPost("{id:long}/save")]
    public async Task<IActionResult> Save(long id, CancellationToken ct)
    {
        await _posts.SaveAsync(User.ToCurrentUser(), id, ct);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:long}/save")]
    public async Task<IActionResult> Unsave(long id, CancellationToken ct)
    {
        await _posts.UnsaveAsync(User.ToCurrentUser(), id, ct);
        return NoContent();
    }

    private static List<FileUpload> ToUploads(List<IFormFile>? files) =>
        (files ?? new List<IFormFile>())
            .Where(f => f.Length > 0)
            .Select(f => new FileUpload(f.FileName, f.ContentType, f.Length, f.OpenReadStream()))
            .ToList();
}
