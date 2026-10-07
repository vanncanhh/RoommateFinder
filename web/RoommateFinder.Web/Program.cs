using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RoommateFinder.Web;
using RoommateFinder.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Địa chỉ backend lấy từ wwwroot/appsettings.json (ApiBaseUrl).
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBase.TrimEnd('/') + "/") });

// Blazor WASM chạy một người dùng/tab → Scoped tương đương Singleton trong vòng đời ứng dụng.
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<RealtimeService>();
builder.Services.AddScoped<AuthApi>();
builder.Services.AddScoped<ProfileApi>();
builder.Services.AddScoped<CatalogApi>();
builder.Services.AddScoped<PostsApi>();
builder.Services.AddScoped<ModerationApi>();
builder.Services.AddScoped<ConnectionsApi>();
builder.Services.AddScoped<ChatApi>();
builder.Services.AddScoped<FeedbackApi>();
builder.Services.AddScoped<NotificationsApi>();
builder.Services.AddScoped<AdminApi>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());

var host = builder.Build();
await host.Services.GetRequiredService<AuthSession>().InitializeAsync();
await host.Services.GetRequiredService<RealtimeService>().SyncAsync();
await host.RunAsync();
