using GidroAtlas.Shared.DTOs;
using GidroAtlas.Web.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GidroAtlas.Web.Components.Pages;

/// <summary>
/// Code-behind for Login page
/// </summary>
public partial class Login : ComponentBase
{
    [Inject]
    private IAuthApiService AuthApiService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<Login> Logger { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    // Properties for UI state
    protected LoginModel LoginModel { get; set; } = new();
    protected bool ShowPassword { get; set; } = false;
    protected bool IsLoading { get; set; } = false;
    protected string ErrorMessage { get; set; } = string.Empty;
    protected string SuccessMessage { get; set; } = string.Empty;

    /// <summary>
    /// Toggles password visibility
    /// </summary>
    protected void TogglePasswordVisibility()
    {
        ShowPassword = !ShowPassword;
    }

    /// <summary>
    /// Handles login form submission
    /// </summary>
    protected async Task HandleLoginAsync()
    {
        Logger.LogInformation("=== HandleLoginAsync START ===");
        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            Logger.LogInformation("Attempting login for user: {Login}", LoginModel.Login);

            // Create login request DTO
            var loginRequest = new LoginRequestDto
            {
                Login = LoginModel.Login,
                Password = LoginModel.Password
            };

            Logger.LogInformation("Created LoginRequestDto, calling AuthApiService.LoginAsync...");

            // Call authentication service
            var response = await AuthApiService.LoginAsync(loginRequest);

            Logger.LogInformation("AuthApiService.LoginAsync returned. Response is null: {IsNull}", response == null);

            if (response != null)
            {
                Logger.LogInformation("Login successful for user: {Login}, Token length: {TokenLength}",
                    LoginModel.Login, response.Token?.Length ?? 0);

                // Save to LocalStorage
                await JS.InvokeVoidAsync("localStorage.setItem", "authToken", response.Token);
                await JS.InvokeVoidAsync("localStorage.setItem", "userRole", response.Role.ToString());
                await JS.InvokeVoidAsync("localStorage.setItem", "userName", LoginModel.Login);

                SuccessMessage = "Вход выполнен успешно! Перенаправление...";

                Logger.LogInformation("Setting success message and waiting 1 second...");

                // Wait a moment to show success message
                await Task.Delay(1000);

                Logger.LogInformation("Navigating to /dashboard...");

                // Navigate to dashboard or home page
                NavigationManager.NavigateTo("/map", forceLoad: true);

                Logger.LogInformation("Navigation completed");
            }
            else
            {
                Logger.LogWarning("Login failed for user: {Login} - response is NULL", LoginModel.Login);
                ErrorMessage = "Неверный логин или пароль";
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error during login for user: {Login}", LoginModel.Login);
            ErrorMessage = "Ошибка подключения к серверу. Проверьте соединение с интернетом.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during login for user: {Login}. Type: {Type}",
                LoginModel.Login, ex.GetType().Name);
            ErrorMessage = "Произошла непредвиденная ошибка. Попробуйте позже.";
        }
        finally
        {
            IsLoading = false;
            Logger.LogInformation("=== HandleLoginAsync END === IsLoading: {IsLoading}, ErrorMessage: {ErrorMessage}",
                IsLoading, ErrorMessage);
        }
    }
}
