using GidroAtlas.Shared.DTOs;
using GidroAtlas.Web.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GidroAtlas.Web.Services;

/// <summary>
/// Implementation of chat API service for AI-powered conversations
/// </summary>
public class ChatApiService : IChatApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatApiService> _logger;
    private string? _authToken;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ChatApiService(HttpClient httpClient, ILogger<ChatApiService> logger)
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

    public async Task<ChatResponseDto?> AskAsync(string message)
    {
        try
        {
            _logger.LogInformation("Sending chat message to API");
            
            var request = new ChatRequestDto { Message = message };
            var response = await _httpClient.PostAsJsonAsync("api/chat", request);
            
            _logger.LogInformation("Chat response status: {StatusCode}", response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Response JSON: {Json}", jsonContent);
                
                var result = JsonSerializer.Deserialize<ChatResponseDto>(jsonContent, JsonOptions);
                return result;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Chat request failed with status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            return new ChatResponseDto 
            { 
                Answer = "Ошибка при отправке сообщения. Попробуйте позже.",
                Error = $"HTTP {(int)response.StatusCode}: {response.StatusCode}"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during chat request");
            return new ChatResponseDto 
            { 
                Answer = "Не удалось подключиться к серверу. Проверьте соединение.",
                Error = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Chat request timed out");
            return new ChatResponseDto 
            { 
                Answer = "Превышено время ожидания ответа. Попробуйте позже.",
                Error = "Timeout"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during chat request");
            return new ChatResponseDto 
            { 
                Answer = "Произошла непредвиденная ошибка.",
                Error = ex.Message
            };
        }
    }

    public async Task<ChatStatusDto?> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/chat/status");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ChatStatusDto>(jsonContent, JsonOptions);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat status");
            return null;
        }
    }
}
