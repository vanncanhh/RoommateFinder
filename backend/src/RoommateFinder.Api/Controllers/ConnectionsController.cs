using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;

namespace RoommateFinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/connections")]
public class ConnectionsController : ControllerBase
{
    private readonly ConnectionService _connections;
    public ConnectionsController(ConnectionService connections) => _connections = connections;

    [HttpPost]
    public async Task<ActionResult<ConnectionRequestDto>> Send(CreateConnectionRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _connections.SendAsync(User.ToCurrentUser(), req, ct));

    [HttpGet("incoming")]
    public Task<List<ConnectionRequestDto>> Incoming([FromQuery] string? status, CancellationToken ct) =>
        _connections.GetIncomingAsync(User.ToCurrentUser(), status, ct);

    [HttpGet("outgoing")]
    public Task<List<ConnectionRequestDto>> Outgoing([FromQuery] string? status, CancellationToken ct) =>
        _connections.GetOutgoingAsync(User.ToCurrentUser(), status, ct);

    [HttpPut("{id:long}/accept")]
    public Task<ConnectionRequestDto> Accept(long id, CancellationToken ct) => _connections.AcceptAsync(User.ToCurrentUser(), id, ct);

    [HttpPut("{id:long}/reject")]
    public Task<ConnectionRequestDto> Reject(long id, CancellationToken ct) => _connections.RejectAsync(User.ToCurrentUser(), id, ct);

    [HttpPut("{id:long}/cancel")]
    public Task<ConnectionRequestDto> Cancel(long id, CancellationToken ct) => _connections.CancelAsync(User.ToCurrentUser(), id, ct);
}

[ApiController]
[Authorize]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly ChatService _chat;
    public ConversationsController(ChatService chat) => _chat = chat;

    [HttpGet]
    public Task<List<ConversationDto>> List(CancellationToken ct) => _chat.GetConversationsAsync(User.GetUserId(), ct);

    [HttpGet("{id:long}/messages")]
    public Task<List<MessageDto>> Messages(long id, [FromQuery] long? before, [FromQuery] int limit = 30, CancellationToken ct = default) =>
        _chat.GetMessagesAsync(User.GetUserId(), id, before, limit, ct);

    [HttpPost("{id:long}/messages")]
    public Task<MessageDto> Send(long id, SendMessageRequest req, CancellationToken ct) =>
        _chat.SendAsync(User.GetUserId(), id, req.Content, ct);

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        await _chat.MarkReadAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}

[ApiController]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly ReviewService _reviews;
    private readonly ReportService _reports;
    private readonly NotificationService _notifications;

    public FeedbackController(ReviewService reviews, ReportService reports, NotificationService notifications)
    {
        _reviews = reviews;
        _reports = reports;
        _notifications = notifications;
    }

    [HttpPost("api/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(CreateReviewRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _reviews.CreateAsync(User.ToCurrentUser(), req, ct));

    [HttpPost("api/reports")]
    public async Task<ActionResult<MessageResponse>> CreateReport(CreateReportRequest req, CancellationToken ct)
    {
        await _reports.CreateAsync(User.ToCurrentUser(), req, ct);
        return StatusCode(StatusCodes.Status201Created, new MessageResponse("Đã gửi báo cáo. Kiểm duyệt viên sẽ xem xét sớm nhất."));
    }

    [HttpGet("api/notifications")]
    public Task<PagedResult<NotificationDto>> Notifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        _notifications.GetAsync(User.GetUserId(), page, pageSize, ct);

    [HttpGet("api/notifications/unread-count")]
    public async Task<object> UnreadCount(CancellationToken ct) => new { count = await _notifications.CountUnreadAsync(User.GetUserId(), ct) };

    [HttpPut("api/notifications/{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPut("api/notifications/read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
