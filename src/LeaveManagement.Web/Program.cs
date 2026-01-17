using Blazored.LocalStorage;
using LeaveManagement.Web;
using LeaveManagement.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient
builder.Services.AddScoped(sp =>
{
    var client = new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000") };
    return client;
});

// Add MudBlazor
builder.Services.AddMudServices();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Application Services
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IActivityTypeService, ActivityTypeService>();
builder.Services.AddScoped<IUserService, UserService>();

await builder.Build().RunAsync();
