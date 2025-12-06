using System.Net.Http.Json;
using System.Text.Json;
using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Options;
using Microsoft.Extensions.Options;

namespace GidroAtlas.Api.Infrastructure.AI.Ollama;

/// <summary>
/// Ollama-based embedding service implementation.
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = _settings.EmbeddingModel,
                prompt = text
            };

            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to generate embedding. Status: {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);
            return result?.Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text: {TextPreview}", text.Length > 50 ? text[..50] + "..." : text);
            return null;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            if (embedding != null)
            {
                results.Add(embedding);
            }
        }
        
        return results;
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
            return content.Contains(_settings.EmbeddingModel, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama embedding service is not available");
            return false;
        }
    }

    private class OllamaEmbeddingResponse
    {
        public float[]? Embedding { get; set; }
    }
}
