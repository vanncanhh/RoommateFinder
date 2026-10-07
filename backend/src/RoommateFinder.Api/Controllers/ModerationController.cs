using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.ModeratorOrAdmin)]
[ServiceFilter(typeof(AuditLogFilter))]
[Route("api/moderation")]
public class ModerationController : ControllerBase
{
    private readonly ModerationService _moderation;
    private readonly PostService _posts;

    public ModerationController(ModerationService moderation, PostService posts)
    {
        _moderation = moderation;
        _posts = posts;
    }

    [HttpGet("posts")]
    public Task<PagedResult<PostSummaryDto>> Posts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        _moderation.GetPostsAsync(status, page, pageSize, ct);

    [HttpPost("posts/{id:long}/approve")]
    public async Task<PostDetailDto> Approve(long id, CancellationToken ct)
    {
        await _moderation.ApproveAsync(User.ToCurrentUser(), id, ct);
        return await _posts.GetDetailAsync(User.ToCurrentUser(), id, countView: false, ct);
    }

    [HttpPost("posts/{id:long}/reject")]
    public async Task<PostDetailDto> Reject(long id, RejectPostRequest req, CancellationToken ct)
    {
        await _moderation.RejectAsync(User.ToCurrentUser(), id, req, ct);
        return await _posts.GetDetailAsync(User.ToCurrentUser(), id, countView: false, ct);
    }

    [HttpGet("reports")]
    public Task<PagedResult<ReportDto>> Reports([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        _moderation.GetReportsAsync(User.ToCurrentUser(), status, page, pageSize, ct);

    [HttpPost("reports/{id:long}/handle")]
    public Task<ReportDto> Handle(long id, HandleReportRequest req, CancellationToken ct) =>
        _moderation.HandleReportAsync(User.ToCurrentUser(), id, req, ct);

    [HttpGet("violations")]
    public Task<PagedResult<ViolationDto>> Violations([FromQuery] long? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        _moderation.GetViolationsAsync(userId, page, pageSize, ct);

    [HttpPut("reviews/{id:long}/hide")]
    public async Task<IActionResult> HideReview(long id, CancellationToken ct)
    {
        await _moderation.SetReviewHiddenAsync(User.ToCurrentUser(), id, true, ct);
        return NoContent();
    }

    [HttpPut("reviews/{id:long}/unhide")]
    public async Task<IActionResult> UnhideReview(long id, CancellationToken ct)
    {
        await _moderation.SetReviewHiddenAsync(User.ToCurrentUser(), id, false, ct);
        return NoContent();
    }
}
