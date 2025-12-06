using GidroAtlas.Shared.DTOs;
using GidroAtlas.Web.Interfaces;
using System.Net.Http.Headers;

namespace GidroAtlas.Web.Services;

/// <summary>
/// Implementation of authentication API service
/// </summary>
public class AuthApiService : IAuthApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiService> _logger;
    private string? _authToken;

    public AuthApiService(HttpClient httpClient, ILogger<AuthApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken()
    {
        _authToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        try
        {
            _logger.LogInformation("Sending login request to API for user: {Login}", request.Login);
            _logger.LogInformation("API Base URL: {BaseUrl}", _httpClient.BaseAddress);
            
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            _logger.LogInformation("Login response status: {StatusCode}", response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                // Читаем JSON как строку для логирования
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response JSON: {Json}", jsonContent);
                
                // Десериализуем из строки с поддержкой enum как строк
                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                
                var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponseDto>(jsonContent, options);
                
                _logger.LogInformation("Deserialization result - IsNull: {IsNull}", result == null);
                
                if (result?.Token != null)
                {
                    _logger.LogInformation("Token received successfully, length: {Length}, Role: {Role}", 
                        result.Token.Length, result.Role);
                    SetAuthToken(result.Token);
                }
                else
                {
                    _logger.LogWarning("Response was successful but token is null. Result object is null: {IsNull}", result == null);
                }
                return result;
            }
            
            // Читаем содержимое ответа для отладки
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Login failed with status code: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during login. Message: {Message}", ex.Message);
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error during login. Message: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login. Type: {Type}, Message: {Message}", 
                ex.GetType().Name, ex.Message);
            return null;
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/auth/logout", null);
            ClearAuthToken();
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            ClearAuthToken();
            return false;
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/me");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserDto>();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }
}
