using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GidroAtlas.Api.Entities;
using GidroAtlas.Api.Infrastructure.Database;
using GidroAtlas.Api.Options;
using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace GidroAtlas.Api.Infrastructure.Chat;

/// <summary>
/// RAG-based chat service using Ollama and pgvector.
/// </summary>
public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<ChatService> _logger;

    private const int TopK = 15; // Number of similar documents to retrieve
    private const int MaxContentLength = 500; // Max length of content snippet

    public ChatService(
        ApplicationDbContext context,
        IEmbeddingService embeddingService,
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<ChatService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<ChatResponseDto> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Generate embedding for the question
            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question, cancellationToken);
            if (questionEmbedding == null)
            {
                return new ChatResponseDto
                {
                    Answer = "Извините, сервис эмбеддингов временно недоступен. Попробуйте позже.",
                    Error = "Embedding service unavailable",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    UsedRag = false
                };
            }

            // 2. Search for similar documents using pgvector
            var queryVector = new Vector(questionEmbedding);
            var similarDocs = await _context.WaterObjectEmbeddings
                .OrderBy(e => e.Embedding.CosineDistance(queryVector))
                .Take(TopK)
                .Include(e => e.WaterObject)
                .Select(e => new
                {
                    e.WaterObjectId,
                    e.Content,
                    e.ContentType,
                    WaterObjectName = e.WaterObject!.Name,
                    WaterObjectRegion = e.WaterObject.Region,
                    Distance = e.Embedding.CosineDistance(queryVector)
                })
                .ToListAsync(cancellationToken);

            // 3. Build context from retrieved documents
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Информация о водных объектах Казахстана:\n");

            var sources = new List<ChatSourceDto>();
            var seenIds = new HashSet<Guid>();

            foreach (var doc in similarDocs)
            {
                contextBuilder.AppendLine($"---\n{doc.Content}\n");

                if (!seenIds.Contains(doc.WaterObjectId))
                {
                    seenIds.Add(doc.WaterObjectId);
                    sources.Add(new ChatSourceDto
                    {
                        Id = doc.WaterObjectId,
                        Name = doc.WaterObjectName,
                        Region = doc.WaterObjectRegion,
                        Relevance = Math.Round(1 - doc.Distance, 3), // Convert distance to similarity
                        ContentSnippet = doc.Content.Length > MaxContentLength 
                            ? doc.Content[..MaxContentLength] + "..." 
                            : doc.Content
                    });
                }
            }

            // 4. Generate answer using LLM
            var systemPrompt = """
                Ты - эксперт по водным ресурсам и гидротехническим сооружениям Казахстана.
                Для ответа можешь использовать контекст который тебе дан. Если контекста нет или он неприавльный, то отвечай сам на этот вопрос.
                Отвечай на русском языке на любой вопрос. /no-thinking
                """;

            var userPrompt = $"""
                {contextBuilder}
                
                Вопрос пользователя: {question}
                
                Ответь на вопрос, используя только информацию выше:
                """;

            var answer = await GenerateLlmResponseAsync(systemPrompt, userPrompt, cancellationToken);

            stopwatch.Stop();

            return new ChatResponseDto
            {
                Answer = answer ?? "Не удалось сгенерировать ответ. Попробуйте переформулировать вопрос.",
                Sources = sources,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                UsedRag = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request: {Question}", question);
            stopwatch.Stop();

            return new ChatResponseDto
            {
                Answer = "Произошла ошибка при обработке запроса. Попробуйте позже.",
                Error = ex.Message,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                UsedRag = false
            };
        }
    }

    public async Task<int> IndexAllWaterObjectsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting indexing of all water objects...");

        var waterObjects = await _context.WaterObjects.ToListAsync(cancellationToken);
        var indexedCount = 0;

        // Clear existing embeddings
        await _context.WaterObjectEmbeddings.ExecuteDeleteAsync(cancellationToken);

        foreach (var obj in waterObjects)
        {
            try
            {
                // Generate full content for embedding
                var content = GenerateFullContent(obj);
                var embedding = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken);

                if (embedding != null)
                {
                    var embeddingEntity = new WaterObjectEmbedding
                    {
                        WaterObjectId = obj.Id,
                        ChunkIndex = 0,
                        ContentType = "main",
                        Content = content,
                        Embedding = new Vector(embedding),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.WaterObjectEmbeddings.Add(embeddingEntity);
                    indexedCount++;

                    _logger.LogDebug("Indexed water object: {Name}", obj.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index water object: {Id}", obj.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Indexing complete. Indexed {Count} water objects", indexedCount);

        return indexedCount;
    }

    public async Task<ChatStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new ChatStatusDto
        {
            ModelName = _settings.ChatModel
        };

        try
        {
            // Check embeddings availability
            status.EmbeddingsAvailable = await _embeddingService.IsAvailableAsync(cancellationToken);

            // Check LLM availability
            status.LlmAvailable = await CheckLlmAvailableAsync(cancellationToken);

            // Count indexed objects
            status.IndexedObjectsCount = await _context.WaterObjectEmbeddings
                .Select(e => e.WaterObjectId)
                .Distinct()
                .CountAsync(cancellationToken);

            status.IsAvailable = status.EmbeddingsAvailable && status.LlmAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking chat service status");
            status.Error = ex.Message;
            status.IsAvailable = false;
        }

        return status;
    }

    private string GenerateFullContent(WaterObject obj)
    {
        var conditionText = obj.TechnicalCondition switch
        {
            1 => "критическое (требует немедленного обследования)",
            2 => "плохое (требует обследования)",
            3 => "удовлетворительное",
            4 => "хорошее",
            5 => "отличное",
            _ => "неизвестно"
        };

        var resourceTypeText = obj.ResourceType.GetDisplayName();
        var waterTypeText = obj.WaterType.GetDisplayName();
        var passportAge = (DateTime.UtcNow - obj.PassportDate).Days / 365;

        // Calculate priority
        var priority = (6 - obj.TechnicalCondition) * 3 + passportAge;
        var priorityLevel = priority switch
        {
            >= 12 => "высокий",
            >= 6 => "средний",
            _ => "низкий"
        };

        return $"""
            Название объекта: {obj.Name}
            Область/Регион: {obj.Region}
            Тип водного ресурса: {resourceTypeText}
            Тип воды: {waterTypeText}
            Наличие фауны: {(obj.HasFauna ? "да, присутствует" : "нет, отсутствует")}
            Техническое состояние: {obj.TechnicalCondition} из 5 - {conditionText}
            Дата паспорта: {obj.PassportDate:dd.MM.yyyy}
            Возраст паспорта: {passportAge} лет
            Приоритет обследования: {priorityLevel} (score: {priority})
            Координаты: широта {obj.Latitude}, долгота {obj.Longitude}
            Ссылка на паспорт: {obj.PdfUrl}
            """;
    }

    private async Task<string?> GenerateLlmResponseAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var request = new
            {
                model = _settings.ChatModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                stream = false,
                options = new
                {
                    temperature = _settings.Temperature,
                    num_predict = _settings.MaxTokens
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LLM request failed. Status: {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
            var rawContent = result?.Message?.Content;
            return CleanLlmResponse(rawContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response");
            return null;
        }
    }

    private async Task<bool> CheckLlmAvailableAsync(CancellationToken cancellationToken)
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
        catch
        {
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
        var cleaned = Regex.Replace(response, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase);
        
        // Remove any remaining opening/closing think tags
        cleaned = Regex.Replace(cleaned, @"</?think>", "", RegexOptions.IgnoreCase);
        
        // Remove other common thinking markers
        cleaned = Regex.Replace(cleaned, @"\[thinking\][\s\S]*?\[/thinking\]", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\*\*Thinking\*\*:?[\s\S]*?(?=\n\n|\z)", "", RegexOptions.IgnoreCase);
        
        // Trim whitespace and extra newlines
        cleaned = Regex.Replace(cleaned, @"^\s+", "");
        cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");
        
        return cleaned.Trim();
    }

    private class OllamaChatResponse
    {
        public OllamaChatMessage? Message { get; set; }
    }

    private class OllamaChatMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }
}
