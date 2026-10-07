using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoommateFinder.Application;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.Infrastructure;
using RoommateFinder.Infrastructure.Data;
using Xunit;

namespace RoommateFinder.IntegrationTests;

/// <summary>
/// Kế hoạch 5.2 / NFR-08: chạy trên CSDL thật riêng (RoommateFinderDB_Test) vì UPDATE có điều kiện và
/// filtered unique index chỉ kiểm chứng được trên SQL Server. Đặt biến môi trường ROOMMATE_TEST_DB để đổi chuỗi kết nối.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    public const string DefaultConnection =
        @"Server=.\SQLEXPRESS;Database=RoommateFinderDB_Test;Trusted_Connection=True;TrustServerCertificate=True";

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = Environment.GetEnvironmentVariable("ROOMMATE_TEST_DB") ?? DefaultConnection,
            ["Jwt:Key"] = new string('k', 40),
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IWebHostEnvironment>(new TestEnvironment());
        services.AddApplication();
        services.AddInfrastructure(config);
        services.AddScoped<IRealtimeNotifier, NoopRealtime>();
        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        using (var scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        await Services.DisposeAsync();
    }

    private sealed class NoopRealtime : IRealtimeNotifier
    {
        public Task NotificationsCreatedAsync(IReadOnlyList<NotificationDto> notifications, CancellationToken ct = default) => Task.CompletedTask;
        public Task MessageSentAsync(MessageDto message, IReadOnlyList<long> recipientIds, CancellationToken ct = default) => Task.CompletedTask;
        public Task MessagesReadAsync(long conversationId, long readerId, long otherUserId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "rf-test-wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "RoommateFinder.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}

public class ConcurrencyTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fx;
    public ConcurrencyTests(DatabaseFixture fx) => _fx = fx;

    /// <summary>Tạo chủ tin + một tin approved còn <paramref name="slots"/> chỗ + <paramref name="senders"/> yêu cầu pending.</summary>
    private async Task<(long OwnerId, long PostId, List<long> RequestIds)> ArrangeAsync(int slots, int senders)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tag = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        var area = new Area { Name = "Khu test " + tag, City = "Hà Nội", Latitude = 21m, Longitude = 105.8m };
        var owner = NewUser("owner", tag);
        var others = Enumerable.Range(1, senders).Select(i => NewUser("s" + i, tag)).ToList();
        db.AddRange(area, owner);
        db.AddRange(others);
        await db.SaveChangesAsync();

        var post = new Post
        {
            UserId = owner.UserId, PostType = PostType.HasRoom, Title = "Tin kiểm thử tranh chấp " + tag, Price = 1_000_000,
            AreaId = area.AreaId, NeededOccupants = slots, Status = PostStatus.Approved, CreatedAt = now, ExpiredAt = now.AddDays(30),
        };
        db.Add(post);
        await db.SaveChangesAsync();

        var requests = others.Select(s => new ConnectionRequest
        {
            PostId = post.PostId, SenderId = s.UserId, ReceiverId = owner.UserId, Status = RequestStatus.Pending, CreatedAt = now,
        }).ToList();
        db.AddRange(requests);
        await db.SaveChangesAsync();
        return (owner.UserId, post.PostId, requests.Select(r => r.RequestId).ToList());
    }

    private static User NewUser(string name, string tag) => new()
    {
        FullName = $"{name} {tag}", Email = $"{name}.{tag}@test.vn", PasswordHash = "x", Role = Roles.User,
        Status = UserStatus.Active, CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task FiveParallelAccepts_OnTwoSlots_ExactlyTwoSucceed()
    {
        var (ownerId, postId, requestIds) = await ArrangeAsync(slots: 2, senders: 5);
        var owner = new CurrentUser(ownerId, Roles.User);

        // Mỗi lời gọi một scope riêng (DbContext + kết nối riêng), bắt đầu cùng lúc.
        using var gate = new ManualResetEventSlim(false);
        var tasks = requestIds.Select(id => Task.Run(async () =>
        {
            gate.Wait();
            using var scope = _fx.Services.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<ConnectionService>().AcceptAsync(owner, id);
                return "ok";
            }
            catch (BusinessException ex) when (ex.Code == ErrorCode.Conflict)
            {
                return "409";
            }
            catch (Exception ex)
            {
                return "unexpected: " + ex.GetType().Name + " " + ex.Message;
            }
        })).ToList();
        gate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.DoesNotContain(results, r => r.StartsWith("unexpected"));
        Assert.Equal(2, results.Count(r => r == "ok"));
        Assert.Equal(3, results.Count(r => r == "409"));

        using var check = _fx.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await db.Posts.AsNoTracking().SingleAsync(p => p.PostId == postId);
        Assert.Equal(0, post.NeededOccupants);
        Assert.Equal(PostStatus.Closed, post.Status);

        var statuses = await db.ConnectionRequests.AsNoTracking().Where(r => r.PostId == postId).Select(r => r.Status).ToListAsync();
        Assert.Equal(2, statuses.Count(s => s == RequestStatus.Accepted));
        Assert.Equal(3, statuses.Count(s => s == RequestStatus.Expired));
        Assert.Equal(2, await db.Conversations.CountAsync(c => c.Request.PostId == postId));
    }

    /// <summary>Chạy song song các thao tác, trả về "ok" / "409" / "unexpected: ...".</summary>
    private async Task<string[]> RunParallelAsync(params Func<IServiceProvider, Task>[] actions)
    {
        using var gate = new ManualResetEventSlim(false);
        var tasks = actions.Select(action => Task.Run(async () =>
        {
            gate.Wait();
            using var scope = _fx.Services.CreateScope();
            try { await action(scope.ServiceProvider); return "ok"; }
            catch (BusinessException ex) when (ex.Code == ErrorCode.Conflict) { return "409"; }
            catch (Exception ex) { return "unexpected: " + ex.GetType().Name + " " + ex.Message; }
        })).ToList();
        gate.Set();
        return await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task CloseAndAcceptAtSameTime_NoDeadlock()
    {
        for (var round = 0; round < 5; round++)
        {
            var (ownerId, postId, requestIds) = await ArrangeAsync(slots: 3, senders: 3);
            var owner = new CurrentUser(ownerId, Roles.User);
            var results = await RunParallelAsync(
                sp => sp.GetRequiredService<PostService>().CloseAsync(owner, postId),
                sp => sp.GetRequiredService<ConnectionService>().AcceptAsync(owner, requestIds[0]),
                sp => sp.GetRequiredService<ConnectionService>().AcceptAsync(owner, requestIds[1]));
            Assert.DoesNotContain(results, r => r.StartsWith("unexpected"));
        }
    }

    [Fact]
    public async Task TwoModeratorsHandleSameReport_OnlyOneSucceeds()
    {
        var (ownerId, _, _) = await ArrangeAsync(slots: 1, senders: 1);
        long reportId, mod1, mod2;
        using (var scope = _fx.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tag = Guid.NewGuid().ToString("N")[..8];
            var m1 = NewUser("mod1", tag); m1.Role = Roles.Moderator;
            var m2 = NewUser("mod2", tag); m2.Role = Roles.Moderator;
            var reporter = NewUser("rep", tag);
            db.AddRange(m1, m2, reporter);
            if (!await db.ReportReasons.AnyAsync(r => r.ReasonId == 2)) throw new InvalidOperationException("thiếu seed ReportReasons");
            await db.SaveChangesAsync();
            var report = new Report { ReporterId = reporter.UserId, ReportedUserId = ownerId, ReasonId = 2, Status = ReportStatus.Pending, CreatedAt = DateTime.UtcNow };
            db.Add(report);
            await db.SaveChangesAsync();
            (reportId, mod1, mod2) = (report.ReportId, m1.UserId, m2.UserId);
        }

        var results = await RunParallelAsync(
            sp => sp.GetRequiredService<ModerationService>().HandleReportAsync(new CurrentUser(mod1, Roles.Moderator), reportId,
                new HandleReportRequest { Decision = ReportDecision.Suspend, SuspendDays = 3 }),
            sp => sp.GetRequiredService<ModerationService>().HandleReportAsync(new CurrentUser(mod2, Roles.Moderator), reportId,
                new HandleReportRequest { Decision = ReportDecision.Suspend, SuspendDays = 3 }));

        Assert.DoesNotContain(results, r => r.StartsWith("unexpected"));
        Assert.Equal(1, results.Count(r => r == "ok"));
        using var check = _fx.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db2.ViolationHistory.CountAsync(v => v.ReportId == reportId));
        Assert.Equal(UserStatus.Suspended, (await db2.Users.SingleAsync(u => u.UserId == ownerId)).Status); // không bị nâng thành ban
    }

    [Fact]
    public async Task DuplicatePendingRequest_IsBlockedByFilteredUniqueIndex_2601()
    {
        var (ownerId, postId, requestIds) = await ArrangeAsync(slots: 1, senders: 1);
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var senderId = await db.ConnectionRequests.Where(r => r.RequestId == requestIds[0]).Select(r => r.SenderId).SingleAsync();

        // Ghi thẳng qua repository (bỏ qua bước kiểm tra trước của Service) để chắc chắn DB là hàng rào cuối.
        var repo = scope.ServiceProvider.GetRequiredService<IConnectionRequestRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        repo.Add(new ConnectionRequest { PostId = postId, SenderId = senderId, ReceiverId = ownerId, Status = RequestStatus.Pending, CreatedAt = DateTime.UtcNow });
        var ex = await Assert.ThrowsAsync<DuplicateKeyException>(() => uow.SaveChangesAsync());
        Assert.True(ex.Is("UQ_Request_SenderPost_Active"), ex.IndexName);
    }

    [Fact]
    public async Task RejectedRequest_CanBeSentAgain()
    {
        var (ownerId, postId, requestIds) = await ArrangeAsync(slots: 1, senders: 1);
        using var scope = _fx.Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<ConnectionService>().RejectAsync(new CurrentUser(ownerId, Roles.User), requestIds[0]);

        var db = sp.GetRequiredService<AppDbContext>();
        var senderId = await db.ConnectionRequests.Where(r => r.RequestId == requestIds[0]).Select(r => r.SenderId).SingleAsync();
        var again = await sp.GetRequiredService<ConnectionService>()
            .SendAsync(new CurrentUser(senderId, Roles.User), new CreateConnectionRequest { PostId = postId, Message = "Gửi lại" });
        Assert.Equal(RequestStatus.Pending, again.Status);
    }

    [Fact]
    public async Task ParallelDuplicateSends_OnlyOneCreated_OthersConflict()
    {
        var (_, postId, requestIds) = await ArrangeAsync(slots: 3, senders: 1);
        long senderId;
        using (var s = _fx.Services.CreateScope())
        {
            var db = s.ServiceProvider.GetRequiredService<AppDbContext>();
            senderId = await db.ConnectionRequests.Where(r => r.RequestId == requestIds[0]).Select(r => r.SenderId).SingleAsync();
            await db.ConnectionRequests.Where(r => r.RequestId == requestIds[0])
                .ExecuteUpdateAsync(x => x.SetProperty(r => r.Status, RequestStatus.Cancelled));
        }

        using var gate = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            gate.Wait();
            using var scope = _fx.Services.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<ConnectionService>()
                    .SendAsync(new CurrentUser(senderId, Roles.User), new CreateConnectionRequest { PostId = postId });
                return "ok";
            }
            catch (BusinessException ex) when (ex.Code == ErrorCode.Conflict)
            {
                return "409";
            }
            catch (Exception ex)
            {
                return "unexpected: " + ex.GetType().Name + " " + ex.Message;
            }
        })).ToList();
        gate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.DoesNotContain(results, r => r.StartsWith("unexpected"));
        Assert.Equal(1, results.Count(r => r == "ok"));
    }
}
