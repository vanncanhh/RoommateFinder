using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoommateFinder.Api.BackgroundJobs;
using RoommateFinder.Api.Hubs;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Infrastructure;
using RoommateFinder.Infrastructure.Data;
using RoommateFinder.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Logging (NFR-12)
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/roommatefinder-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

// ---------------------------------------------------------------- Tầng nghiệp vụ & hạ tầng
// Controller/Service chỉ phụ thuộc interface; đây là nơi duy nhất biết đến lớp cài đặt (composition root).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddScoped<AuditLogFilter>();
builder.Services.AddHostedService<MaintenanceBackgroundService>();

// ---------------------------------------------------------------- Xác thực JWT
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Thiếu Jwt:Key (tối thiểu 32 ký tự). Đặt bằng: dotnet user-secrets set \"Jwt:Key\" \"<chuỗi ngẫu nhiên>\"");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        // SignalR gửi token qua query string ?access_token=...
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// ---------------------------------------------------------------- MVC, JSON, SignalR
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        // Lỗi binding/validation trả cùng định dạng { code, message } như lỗi nghiệp vụ.
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var first = ctx.ModelState.Where(kv => kv.Value?.Errors.Count > 0)
                .Select(kv => kv.Key).FirstOrDefault();
            var message = first == null ? "Dữ liệu không hợp lệ." : $"Trường \"{first.TrimStart('$', '.')}\" không hợp lệ.";
            return new BadRequestObjectResult(new ApiError("VALIDATION", message));
        };
    });
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RoommateFinder API", Version = "v1", Description = "API hệ thống tìm người ở ghép" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Dán access token nhận từ POST /api/auth/login",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>(),
    });
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
});

var app = builder.Build();

// ---------------------------------------------------------------- Khởi tạo CSDL (chỉ Development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
}

// ---------------------------------------------------------------- Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DocumentTitle = "RoommateFinder API");
}

app.UseStaticFiles(); // wwwroot/uploads
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<AccountStatusMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

/// <summary>Cho phép project IntegrationTests tham chiếu.</summary>
public partial class Program;
