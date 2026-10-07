using Microsoft.Extensions.DependencyInjection;
using RoommateFinder.Application.Services;

namespace RoommateFinder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<NotificationPublisher>();
        services.AddScoped<AccountSanctions>();
        services.AddScoped<AuthService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<PostService>();
        services.AddScoped<ModerationService>();
        services.AddScoped<ConnectionService>();
        services.AddScoped<ChatService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<ReportService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<AdminService>();
        services.AddScoped<MaintenanceService>();
        return services;
    }
}
