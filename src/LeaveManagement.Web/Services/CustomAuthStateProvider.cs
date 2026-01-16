using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace LeaveManagement.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            return _anonymous;
        }

        var claims = ParseClaimsFromJwt(token);

        // Check if token is expired
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
        {
            var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
            if (expDate <= DateTimeOffset.UtcNow)
            {
                await _localStorage.RemoveItemAsync("authToken");
                return _anonymous;
            }
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(_anonymous);
        NotifyAuthenticationStateChanged(authState);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            claims.AddRange(token.Claims);

            // Add role claims properly
            var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role");
            foreach (var roleClaim in roleClaims)
            {
                if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                }
            }
        }
        catch
        {
            // Invalid token
        }

        return claims;
    }
}
