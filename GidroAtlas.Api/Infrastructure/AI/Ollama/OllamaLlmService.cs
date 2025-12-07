using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Options;
using Microsoft.Extensions.Options;

namespace GidroAtlas.Api.Infrastructure.AI.Ollama;

/// <summary>
/// Ollama-based LLM service implementation.
/// Handles text generation using Ollama API.
/// </summary>
public partial class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaLlmService> _logger;

    public string ModelName => _settings.ChatModel;

    public OllamaLlmService(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaLlmService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<string?> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OllamaChatRequest
            {
                Model = _settings.ChatModel,
                Messages =
                [
                    new OllamaChatMessage { Role = "system", Content = systemPrompt },
                    new OllamaChatMessage { Role = "user", Content = userPrompt }
                ],
                Stream = false,
                Options = new OllamaOptions
                {
                    Temperature = _settings.Temperature,
                    NumPredict = _settings.MaxTokens,
                    NumCtx = _settings.NumCtx
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LLM request failed. Status: {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                cancellationToken: cancellationToken);
            
            return CleanLlmResponse(result?.Message?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response");
            return null;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content.Contains(_settings.ChatModel, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama LLM service is not available");
            return false;
        }
    }

    /// <summary>
    /// Cleans LLM response from thinking tags and other artifacts.
    /// Removes &lt;think&gt;...&lt;/think&gt; blocks that Qwen3 sometimes adds.
    /// </summary>
    private static string? CleanLlmResponse(string? response)
    {
        if (string.IsNullOrEmpty(response))
            return response;

        // Remove <think>...</think> blocks (including multiline)
        var cleaned = ThinkTagRegex().Replace(response, "");
        
        // Remove any remaining opening/closing think tags
        cleaned = PartialThinkTagRegex().Replace(cleaned, "");
        
        // Remove other common thinking markers
        cleaned = ThinkingBracketRegex().Replace(cleaned, "");
        cleaned = ThinkingBoldRegex().Replace(cleaned, "");
        
        // Trim whitespace and extra newlines
        cleaned = LeadingWhitespaceRegex().Replace(cleaned, "");
        cleaned = MultipleNewlinesRegex().Replace(cleaned, "\n\n");
        
        return cleaned.Trim();
    }

    // Source-generated regex for better performance
    [GeneratedRegex(@"<think>[\s\S]*?</think>", RegexOptions.IgnoreCase)]
    private static partial Regex ThinkTagRegex();
    
    [GeneratedRegex(@"</?think>", RegexOptions.IgnoreCase)]
    private static partial Regex PartialThinkTagRegex();
    
    [GeneratedRegex(@"\[thinking\][\s\S]*?\[/thinking\]", RegexOptions.IgnoreCase)]
    private static partial Regex ThinkingBracketRegex();
    
    [GeneratedRegex(@"\*\*Thinking\*\*:?[\s\S]*?(?=\n\n|\z)", RegexOptions.IgnoreCase)]
    private static partial Regex ThinkingBoldRegex();
    
    [GeneratedRegex(@"^\s+")]
    private static partial Regex LeadingWhitespaceRegex();
    
    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleNewlinesRegex();

    #region Request/Response Models

    private class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }
        
        [JsonPropertyName("messages")]
        public required List<OllamaChatMessage> Messages { get; init; }
        
        [JsonPropertyName("stream")]
        public bool Stream { get; init; }
        
        [JsonPropertyName("think")]
        public bool Think { get; init; } = false; // Disable thinking mode for faster responses
        
        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; init; }
    }

    private class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }
        
        [JsonPropertyName("content")]
        public required string Content { get; init; }
    }

    private class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; init; }
        
        [JsonPropertyName("num_predict")]
        public int NumPredict { get; init; }
        
        [JsonPropertyName("num_ctx")]
        public int NumCtx { get; init; }
    }

    private class OllamaChatResponse
    {
        public OllamaChatMessageResponse? Message { get; set; }
    }

    private class OllamaChatMessageResponse
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    #endregion
}
