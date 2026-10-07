using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Infrastructure.Data;
using RoommateFinder.Infrastructure.Repositories;
using RoommateFinder.Infrastructure.Services;

namespace RoommateFinder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Thiếu ConnectionStrings:Default. Đặt bằng: dotnet user-secrets set \"ConnectionStrings:Default\" \"...\"");

        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.Section));
        services.Configure<FrontendOptions>(config.GetSection(FrontendOptions.Section));
        services.AddMemoryCache();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ISavedPostRepository, SavedPostRepository>();
        services.AddScoped<IConnectionRequestRepository, ConnectionRequestRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IViolationRepository, ViolationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IAppLinks, AppLinks>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<ISystemSettings, SystemSettings>();
        services.AddScoped<DemoDataSeeder>();
        return services;
    }
}
