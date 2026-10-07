using Microsoft.EntityFrameworkCore;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Landmark> Landmarks => Set<Landmark>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<ReportReason> ReportReasons => Set<ReportReason>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostImage> PostImages => Set<PostImage>();
    public DbSet<PostAmenity> PostAmenities => Set<PostAmenity>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<ConnectionRequest> ConnectionRequests => Set<ConnectionRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ViolationHistory> ViolationHistory => Set<ViolationHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // CSDL lưu UTC (quyết định f). Hậu tố "Z" khi trả JSON do UtcDateTimeJsonConverter ở tầng Api đảm nhận,
        // không dùng value converter ở đây để EF vẫn dịch được AddHours/Date sang SQL.
        builder.Properties<DateTime>().HaveColumnType("datetime2");
        builder.Properties<DateTime?>().HaveColumnType("datetime2");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Quyết định (b): tin đăng và người dùng chỉ xóa mềm → mọi khóa ngoại Restrict,
        // trừ PostImages/PostAmenities (cấu hình Cascade riêng trong PostConfigurations).
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            var dependent = fk.DeclaringEntityType.ClrType;
            if (dependent != typeof(PostImage) && dependent != typeof(PostAmenity) || fk.PrincipalEntityType.ClrType != typeof(Post))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
