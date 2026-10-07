using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace RoommateFinder.Web.Services;

/// <summary>Cầu nối AuthSession → hệ thống phân quyền của Blazor ([Authorize], &lt;AuthorizeView Roles="..."&gt;).</summary>
public class JwtAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private readonly AuthSession _session;

    public JwtAuthStateProvider(AuthSession session)
    {
        _session = session;
        _session.Changed += OnChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _session.InitializeAsync();
        var user = _session.User;
        if (user == null) return Anonymous;
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        }, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void OnChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() => _session.Changed -= OnChanged;
}
