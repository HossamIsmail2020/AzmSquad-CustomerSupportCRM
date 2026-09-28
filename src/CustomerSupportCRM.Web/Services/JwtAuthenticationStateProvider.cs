using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace CustomerSupportCRM.Web.Services;

/// <summary>Reads claims out of the JWT payload client-side purely to drive UI (show/hide nav, [Authorize] pages). The API independently validates and enforces the token on every request -- this provider is not a security boundary by itself.</summary>
public class JwtAuthenticationStateProvider(TokenStorageService tokenStorage) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStorage.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        try
        {
            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            await tokenStorage.ClearTokenAsync();
            return Anonymous;
        }
    }

    public async Task NotifyUserAuthenticationAsync(string token)
    {
        await tokenStorage.SetTokenAsync(token);
        var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
    }

    public async Task NotifyUserLogoutAsync()
    {
        await tokenStorage.ClearTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var bytes = Convert.FromBase64String(PadBase64Url(payload));
        var json = JsonDocument.Parse(bytes);

        foreach (var property in json.RootElement.EnumerateObject())
        {
            yield return new Claim(property.Name, property.Value.ToString());
        }
    }

    private static string PadBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    }
}
