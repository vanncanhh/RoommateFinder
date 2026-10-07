using RoommateFinder.Application.Services;

namespace RoommateFinder.Api.BackgroundJobs;

/// <summary>BR-03: chạy mỗi giờ (và một lần sau khi khởi động); mỗi lần tạo scope + DbContext mới.</summary>
public class MaintenanceBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<MaintenanceBackgroundService> _logger;

    public MaintenanceBackgroundService(IServiceScopeFactory scopes, ILogger<MaintenanceBackgroundService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // ứng dụng đang dừng
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<MaintenanceService>();
            var (expired, reactivated) = await service.RunAsync(ct);
            if (expired + reactivated > 0)
                _logger.LogInformation("Tác vụ nền: {Expired} tin hết hạn, {Reactivated} tài khoản được mở khóa.", expired, reactivated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Tác vụ nền lỗi, sẽ thử lại ở lần chạy sau.");
        }
    }
}
