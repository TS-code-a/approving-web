using System.Net.Http.Json;
using Blazored.LocalStorage;
using LeaveManagement.Shared.Common;
using LeaveManagement.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace LeaveManagement.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    private const string TokenKey = "authToken";
    private const string UserKey = "currentUser";

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginDto);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        if (result?.Success == true && result.Data != null)
        {
            await _localStorage.SetItemAsync(TokenKey, result.Data.Token);
            await _localStorage.SetItemAsync(UserKey, result.Data.User);

            ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Data.Token);

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Data.Token);

            return result.Data;
        }

        return null;
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(UserKey);

        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        return await _localStorage.GetItemAsync<UserDto>(UserKey);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenKey);
    }
}
