using System.Net.Http.Json;
using Blazored.LocalStorage;
using LeaveManagement.Shared.Common;
using Microsoft.AspNetCore.Components;

namespace LeaveManagement.Web.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;

    public ApiService(HttpClient httpClient, ILocalStorageService localStorage, NavigationManager navigation)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _navigation = navigation;
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
    {
        await SetAuthHeader();

        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object? data = null)
    {
        await SetAuthHeader();

        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object data)
    {
        await SetAuthHeader();

        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse?> DeleteAsync(string endpoint)
    {
        await SetAuthHeader();

        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _navigation.NavigateTo("/login");
                return ApiResponse.Fail("Unauthorized");
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse.Fail(ex.Message);
        }
    }

    private async Task SetAuthHeader()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task<ApiResponse<T>?> HandleResponse<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _navigation.NavigateTo("/login");
            return ApiResponse<T>.Fail("Unauthorized");
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        return result;
    }
}
