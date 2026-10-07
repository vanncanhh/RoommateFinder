using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.UnitTests.Fakes;
using Xunit;

namespace RoommateFinder.UnitTests;

/// <summary>BR-05, BR-06: đánh giá sau kết nối.</summary>
public class ReviewServiceTests
{
    private readonly FakeUsers _users = new();
    private readonly FakeRequests _requests = new();
    private readonly FakeReviews _reviews = new();
    private readonly FakeClock _clock = new();
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _users.Add(1);
        _users.Add(2);
        _users.Add(3);
        _requests.Add(new ConnectionRequest
        {
            RequestId = 10, PostId = 100, SenderId = 1, ReceiverId = 2, Status = RequestStatus.Accepted,
            CreatedAt = _clock.UtcNow.AddDays(-10), RespondedAt = _clock.UtcNow.AddDays(-8),
        });
        var notifier = new NotificationPublisher(new FakeNotifications(), new FakeRealtime(), _clock);
        _service = new ReviewService(_reviews, _requests, _users, new FakeUnitOfWork(), new FakeSettings(), _clock, notifier);
    }

    private Task<ReviewDto> Review(long reviewer, byte rating = 5) =>
        _service.CreateAsync(new CurrentUser(reviewer, Roles.User), new CreateReviewRequest { RequestId = 10, Rating = rating });

    [Fact]
    public async Task BothSides_CanReviewEachOther_AndRatingIsRecalculated()
    {
        await Review(1, 4); // người gửi đánh giá chủ tin
        await Review(2, 5); // chủ tin đánh giá người gửi
        Assert.Equal(4m, _users.Items.Single(u => u.UserId == 2).AvgRating);
        Assert.Equal(1, _users.Items.Single(u => u.UserId == 2).ReviewCount);
        Assert.Equal(5m, _users.Items.Single(u => u.UserId == 1).AvgRating);
    }

    [Fact]
    public async Task BeforeMinDays_Conflict()
    {
        _requests.Items[0].RespondedAt = _clock.UtcNow.AddDays(-3);
        Assert.Equal(ErrorCode.Conflict, (await Assert.ThrowsAsync<BusinessException>(() => Review(1))).Code);
    }

    [Fact]
    public async Task SecondReviewSameConnection_Conflict()
    {
        await Review(1);
        Assert.Equal(ErrorCode.Conflict, (await Assert.ThrowsAsync<BusinessException>(() => Review(1))).Code);
    }

    [Fact]
    public async Task OutsiderCannotReview() =>
        Assert.Equal(ErrorCode.Forbidden, (await Assert.ThrowsAsync<BusinessException>(() => Review(3))).Code);

    [Fact]
    public async Task NotAcceptedRequest_Conflict()
    {
        _requests.Items[0].Status = RequestStatus.Rejected;
        Assert.Equal(ErrorCode.Conflict, (await Assert.ThrowsAsync<BusinessException>(() => Review(1))).Code);
    }

    [Fact]
    public async Task InvalidRating_Validation() =>
        Assert.Equal(ErrorCode.Validation, (await Assert.ThrowsAsync<BusinessException>(() => Review(1, 6))).Code);
}

/// <summary>BR-07: tự ẩn tin khi đủ số người báo cáo khác nhau trong cửa sổ thời gian.</summary>
public class ReportServiceTests
{
    private readonly FakeUsers _users = new();
    private readonly FakePosts _posts = new();
    private readonly FakeReports _reports = new();
    private readonly FakeClock _clock = new();
    private readonly FakeNotifications _notifications = new();
    private readonly ReportService _service;
    private readonly Post _post;

    public ReportServiceTests()
    {
        _users.Add(1);
        for (var i = 2; i <= 8; i++) _users.Add(i);
        _users.Add(99, Roles.Moderator);
        _post = new Post { PostId = 50, UserId = 1, Title = "Phòng test", Status = PostStatus.Approved };
        _posts.Items.Add(_post);
        var notifier = new NotificationPublisher(_notifications, new FakeRealtime(), _clock);
        _service = new ReportService(_reports, new FakeCatalog(), _posts, _users, new FakeUnitOfWork(), new FakeSettings(), _clock, notifier, new FakeRequests());
    }

    private Task ReportPost(long reporter) =>
        _service.CreateAsync(new CurrentUser(reporter, Roles.User), new CreateReportRequest { PostId = 50, ReasonId = 1 });

    [Fact]
    public async Task FourReporters_PostStaysApproved()
    {
        for (var i = 2; i <= 5; i++) await ReportPost(i);
        Assert.Equal(PostStatus.Approved, _post.Status);
    }

    [Fact]
    public async Task FiveDistinctReporters_AutoHidden_OwnerNotified()
    {
        for (var i = 2; i <= 6; i++) await ReportPost(i);
        Assert.Equal(PostStatus.Hidden, _post.Status);
        Assert.Equal(ReportService.AutoHideReason, _post.RejectReason);
        Assert.Contains(_notifications.Items, n => n.UserId == 1 && n.Type == NotificationType.PostHidden);
        Assert.Contains(_notifications.Items, n => n.UserId == 99 && n.Type == NotificationType.ReportNew);
    }

    [Fact]
    public async Task ReportsOutsideWindow_NotCounted()
    {
        for (var i = 2; i <= 5; i++) await ReportPost(i);
        foreach (var r in _reports.Items) r.CreatedAt = _clock.UtcNow.AddHours(-30); // ngoài cửa sổ 24 giờ
        await ReportPost(6);
        Assert.Equal(PostStatus.Approved, _post.Status);
    }

    [Fact]
    public async Task DismissedReportsAreNotCountedAgain_SingleReporterCannotReHide()
    {
        for (var i = 2; i <= 6; i++) await ReportPost(i);
        Assert.Equal(PostStatus.Hidden, _post.Status);
        // Kiểm duyệt viên bỏ qua toàn bộ báo cáo và hiện lại tin
        foreach (var r in _reports.Items) r.Status = ReportStatus.Dismissed;
        _post.Status = PostStatus.Approved;
        await ReportPost(2); // một người báo cáo lại trong 24 giờ
        Assert.Equal(PostStatus.Approved, _post.Status);
    }

    [Fact]
    public async Task CannotReportNonPublicPost()
    {
        _post.Status = PostStatus.Pending;
        Assert.Equal(ErrorCode.NotFound, (await Assert.ThrowsAsync<BusinessException>(() => ReportPost(2))).Code);
    }

    [Fact]
    public async Task CannotReportOwnPost() =>
        Assert.Equal(ErrorCode.Validation, (await Assert.ThrowsAsync<BusinessException>(() => ReportPost(1))).Code);

    [Fact]
    public async Task MustTargetExactlyOneObject() =>
        Assert.Equal(ErrorCode.Validation, (await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CreateAsync(new CurrentUser(2, Roles.User), new CreateReportRequest { PostId = 50, ReportedUserId = 1, ReasonId = 2 }))).Code);

    [Fact]
    public async Task ReasonMustApplyToTarget() =>
        // Lý do 5 chỉ dành cho báo cáo người dùng
        Assert.Equal(ErrorCode.Validation, (await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CreateAsync(new CurrentUser(2, Roles.User), new CreateReportRequest { PostId = 50, ReasonId = 5 }))).Code);
}

/// <summary>BR-02, BR-08, BR-09: duyệt tin và xử lý vi phạm.</summary>
public class ModerationServiceTests
{
    private readonly FakeUsers _users = new();
    private readonly FakePosts _posts = new();
    private readonly FakeReports _reports = new();
    private readonly FakeViolations _violations = new();
    private readonly FakeClock _clock = new();
    private readonly ModerationService _service;
    private readonly CurrentUser _mod = new(99, Roles.Moderator);

    public ModerationServiceTests()
    {
        _users.Add(1);
        _users.Add(2);
        _users.Add(99, Roles.Moderator);
        var notifier = new NotificationPublisher(new FakeNotifications(), new FakeRealtime(), _clock);
        var requests = new FakeRequests();
        var sanctions = new AccountSanctions(_posts, _violations, notifier, requests);
        _service = new ModerationService(_posts, _reports, _violations, _users, new FakeReviews(), new FakeUnitOfWork(),
            new FakeSettings(), _clock, notifier, sanctions, requests);
    }

    private Report AddUserReport(long id, long reportedUserId)
    {
        var report = new Report
        {
            ReportId = id, ReporterId = 2, ReportedUserId = reportedUserId, ReasonId = 2,
            Reason = new ReportReason { ReasonId = 2, Name = "Lừa đảo" }, Status = ReportStatus.Pending, CreatedAt = _clock.UtcNow,
        };
        _reports.Add(report);
        return report;
    }

    [Fact]
    public async Task Approve_SetsExpiryFromApprovalTime()
    {
        _posts.Items.Add(new Post { PostId = 1, UserId = 1, Title = "Tin chờ duyệt", Status = PostStatus.Pending, CreatedAt = _clock.UtcNow.AddDays(-2) });
        await _service.ApproveAsync(_mod, 1);
        var post = _posts.Items[0];
        Assert.Equal(PostStatus.Approved, post.Status);
        Assert.Equal(_clock.UtcNow.AddDays(30), post.ExpiredAt); // BR-02: tính từ lúc duyệt, không phải lúc tạo
        Assert.Equal(99, post.ModeratedBy);
    }

    [Fact]
    public async Task CannotApprovePostOfLockedOwner()
    {
        _users.Items.Single(u => u.UserId == 1).Status = UserStatus.Banned;
        _posts.Items.Add(new Post { PostId = 9, UserId = 1, Title = "Tin của người bị khóa", Status = PostStatus.Pending });
        Assert.Equal(ErrorCode.Conflict, (await Assert.ThrowsAsync<BusinessException>(() => _service.ApproveAsync(_mod, 9))).Code);
        Assert.Equal(PostStatus.Pending, _posts.Items.Single(p => p.PostId == 9).Status);
    }

    [Fact]
    public async Task SuspendDecision_DoesNotDowngradeBannedUser()
    {
        _users.Items.Single(u => u.UserId == 1).Status = UserStatus.Banned;
        AddUserReport(20, reportedUserId: 1);
        Assert.Equal(ErrorCode.Conflict, (await Assert.ThrowsAsync<BusinessException>(() =>
            _service.HandleReportAsync(_mod, 20, new HandleReportRequest { Decision = ReportDecision.Suspend, SuspendDays = 1 }))).Code);
        Assert.Equal(UserStatus.Banned, _users.Items.Single(u => u.UserId == 1).Status);
    }

    [Fact]
    public async Task PriorBan_CountsAsSevere_NextSuspendEscalatesToBan()
    {
        _violations.Add(new ViolationHistory { UserId = 1, Action = ViolationAction.BanAccount, Level = ViolationLevel.Severe });
        AddUserReport(21, reportedUserId: 1);
        await _service.HandleReportAsync(_mod, 21, new HandleReportRequest { Decision = ReportDecision.Suspend, SuspendDays = 2 });
        Assert.Equal(UserStatus.Banned, _users.Items.Single(u => u.UserId == 1).Status);
    }

    [Fact]
    public async Task LongNote_OnEscalation_IsTruncatedTo500()
    {
        _violations.Add(new ViolationHistory { UserId = 1, Action = ViolationAction.SuspendAccount, Level = ViolationLevel.Severe, SuspendDays = 1 });
        AddUserReport(22, reportedUserId: 1);
        await _service.HandleReportAsync(_mod, 22, new HandleReportRequest { Decision = ReportDecision.Suspend, SuspendDays = 2, Note = new string('x', 450) });
        Assert.All(_violations.Items, v => Assert.True((v.Note?.Length ?? 0) <= 500));
    }

    [Fact]
    public async Task Warning_RestoresAutoHiddenPost()
    {
        var post = new Post { PostId = 30, UserId = 1, Title = "Tin tự ẩn", Status = PostStatus.Hidden, RejectReason = ReportService.AutoHideReason, ExpiredAt = _clock.UtcNow.AddDays(5) };
        _posts.Items.Add(post);
        _reports.Add(new Report { ReportId = 30, ReporterId = 2, PostId = 30, Post = post, ReasonId = 1, Reason = new ReportReason { ReasonId = 1, Name = "Tin ảo" }, Status = ReportStatus.Pending });
        await _service.HandleReportAsync(_mod, 30, new HandleReportRequest { Decision = ReportDecision.Warning });
        Assert.Equal(PostStatus.Approved, post.Status);
    }

    [Fact]
    public async Task ModeratorCannotHideReviewAboutThemself()
    {
        var reviews = new FakeReviews();
        reviews.Add(new Review { ReviewId = 5, ReviewerId = 1, RevieweeId = 99, Rating = 1 });
        var notifier = new NotificationPublisher(new FakeNotifications(), new FakeRealtime(), _clock);
        var requests = new FakeRequests();
        var svc = new ModerationService(_posts, _reports, _violations, _users, reviews, new FakeUnitOfWork(), new FakeSettings(),
            _clock, notifier, new AccountSanctions(_posts, _violations, notifier, requests), requests);
        Assert.Equal(ErrorCode.Forbidden, (await Assert.ThrowsAsync<BusinessException>(() => svc.SetReviewHiddenAsync(_mod, 5, true))).Code);
    }

    [Fact]
    public async Task ModeratorCannotApproveOwnPost()
    {
        _posts.Items.Add(new Post { PostId = 2, UserId = 99, Title = "Tin của mod", Status = PostStatus.Pending });
        Assert.Equal(ErrorCode.Forbidden, (await Assert.ThrowsAsync<BusinessException>(() => _service.ApproveAsync(_mod, 2))).Code);
    }

    [Fact]
    public async Task Suspend_HidesApprovedPostsAndRecordsViolation()
    {
        _posts.Items.Add(new Post { PostId = 3, UserId = 1, Title = "Tin đang hiển thị", Status = PostStatus.Approved });
        AddUserReport(1, reportedUserId: 1);
        await _service.HandleReportAsync(_mod, 1, new HandleReportRequest { Decision = ReportDecision.Suspend, SuspendDays = 7 });

        var user = _users.Items.Single(u => u.UserId == 1);
        Assert.Equal(UserStatus.Suspended, user.Status);
        Assert.Equal(_clock.UtcNow.AddDays(7), user.SuspendedUntil);
        Assert.Equal(PostStatus.Hidden, _posts.Items.Single(p => p.PostId == 3).Status);
        Assert.Contains(_violations.Items, v => v.UserId == 1 && v.Action == ViolationAction.SuspendAccount && v.SuspendDays == 7);
    }

    [Fact]
    public async Task SecondSevereViolation_EscalatesToBan()
    {
        _violations.Add(new ViolationHistory { UserId = 1, Action = ViolationAction.SuspendAccount, Level = ViolationLevel.Severe, SuspendDays = 3 });
        AddUserReport(2, reportedUserId: 1);
        await _service.HandleReportAsync(_mod, 2, new HandleReportRequest { Decision = ReportDecision.Suspend, SuspendDays = 7 });
        Assert.Equal(UserStatus.Banned, _users.Items.Single(u => u.UserId == 1).Status); // BR-08
    }

    [Fact]
    public async Task ModeratorCannotHandleReportAgainstThemself()
    {
        AddUserReport(3, reportedUserId: 99);
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.HandleReportAsync(_mod, 3, new HandleReportRequest { Decision = ReportDecision.Dismiss }));
        Assert.Equal(ErrorCode.Forbidden, ex.Code); // BR-09
    }

    [Fact]
    public async Task Dismiss_RestoresAutoHiddenPost()
    {
        var post = new Post { PostId = 4, UserId = 1, Title = "Tin bị tự ẩn", Status = PostStatus.Hidden, ExpiredAt = _clock.UtcNow.AddDays(10) };
        _posts.Items.Add(post);
        _reports.Add(new Report
        {
            ReportId = 4, ReporterId = 2, PostId = 4, Post = post, ReasonId = 1,
            Reason = new ReportReason { ReasonId = 1, Name = "Tin ảo" }, Status = ReportStatus.Pending,
        });
        await _service.HandleReportAsync(_mod, 4, new HandleReportRequest { Decision = ReportDecision.Dismiss });
        Assert.Equal(PostStatus.Approved, post.Status);
    }

    [Fact]
    public async Task HandledReportCannotBeHandledAgain()
    {
        AddUserReport(5, reportedUserId: 1).Status = ReportStatus.Resolved;
        Assert.Equal(ErrorCode.Conflict, (await Assert.ThrowsAsync<BusinessException>(() =>
            _service.HandleReportAsync(_mod, 5, new HandleReportRequest { Decision = ReportDecision.Warning }))).Code);
    }
}

public class PostVisibilityTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(PostStatus.Approved, 1, true)]
    [InlineData(PostStatus.Approved, -1, false)]  // quá hạn nhưng tác vụ nền chưa chạy
    [InlineData(PostStatus.Pending, 1, false)]
    [InlineData(PostStatus.Hidden, 1, false)]
    [InlineData(PostStatus.Closed, 1, false)]
    public void IsPublic(string status, int daysToExpiry, bool expected) =>
        Assert.Equal(expected, PostService.IsPublic(status, Now.AddDays(daysToExpiry), Now));
}
