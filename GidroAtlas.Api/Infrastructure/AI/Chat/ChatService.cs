using System.Diagnostics;
using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Infrastructure.Documents.Abstractions;
using GidroAtlas.Shared.DTOs;

namespace GidroAtlas.Api.Infrastructure.AI.Chat;

/// <summary>
/// RAG-based chat service that orchestrates LLM and RAG components.
/// Uses separate services for LLM generation, embedding search, and document indexing.
/// </summary>
public class ChatService : IChatService
{
    private readonly ILlmService _llmService;
    private readonly IRagService _ragService;
    private readonly IDocumentIndexingService _indexingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<ChatService> _logger;

    private const int DefaultTopK = 3; // Number of similar documents to retrieve (lower = faster)
    private const double HighRelevanceThreshold = 0.6; // Consider context highly relevant above this

    public ChatService(
        ILlmService llmService,
        IRagService ragService,
        IDocumentIndexingService indexingService,
        IEmbeddingService embeddingService,
        ILogger<ChatService> logger)
    {
        _llmService = llmService;
        _ragService = ragService;
        _indexingService = indexingService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<ChatResponseDto> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Search for relevant context using RAG
            var ragResult = await _ragService.SearchAsync(question, DefaultTopK, cancellationToken);
            
            var usedRag = ragResult.HasRelevantContext && ragResult.AverageRelevance >= HighRelevanceThreshold;
            
            _logger.LogDebug(
                "RAG search completed. Found {Count} sources, avg relevance: {Relevance:F3}, using RAG: {UseRag}",
                ragResult.Sources.Count, 
                ragResult.AverageRelevance,
                usedRag);

            // 2. Build prompts based on context availability
            var systemPrompt = PromptTemplates.WaterExpertSystemPrompt;
            var userPrompt = usedRag 
                ? PromptTemplates.BuildUserPrompt(question, ragResult.Context)
                : PromptTemplates.BuildUserPrompt(question, null);

            // 3. Generate response using LLM
            var answer = await _llmService.GenerateResponseAsync(systemPrompt, userPrompt, cancellationToken);

            if (string.IsNullOrEmpty(answer))
            {
                _logger.LogWarning("LLM returned empty response for question: {Question}", 
                    question.Length > 100 ? question[..100] + "..." : question);
                
                return CreateErrorResponse(
                    "Не удалось сгенерировать ответ. Попробуйте переформулировать вопрос.",
                    stopwatch.ElapsedMilliseconds,
                    usedRag);
            }

            stopwatch.Stop();

            return new ChatResponseDto
            {
                Answer = answer,
                Sources = usedRag ? ragResult.Sources : [],
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                UsedRag = usedRag
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request: {Question}", 
                question.Length > 100 ? question[..100] + "..." : question);
            stopwatch.Stop();

            return CreateErrorResponse(
                "Произошла ошибка при обработке запроса. Попробуйте позже.",
                stopwatch.ElapsedMilliseconds,
                false,
                ex.Message);
        }
    }

    public async Task<int> IndexAllWaterObjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _indexingService.IndexAllWaterObjectsAsync(cancellationToken);
    }

    public async Task<ChatStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new ChatStatusDto
        {
            ModelName = _llmService.ModelName
        };

        try
        {
            // Check services availability in parallel
            var embeddingsTask = _embeddingService.IsAvailableAsync(cancellationToken);
            var llmTask = _llmService.IsAvailableAsync(cancellationToken);
            var indexedCountTask = _ragService.GetIndexedDocumentCountAsync(cancellationToken);

            await Task.WhenAll(embeddingsTask, llmTask, indexedCountTask);

            status.EmbeddingsAvailable = await embeddingsTask;
            status.LlmAvailable = await llmTask;
            status.IndexedObjectsCount = await indexedCountTask;

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

    private static ChatResponseDto CreateErrorResponse(
        string message, 
        long processingTimeMs, 
        bool usedRag,
        string? error = null)
    {
        return new ChatResponseDto
        {
            Answer = message,
            Error = error,
            ProcessingTimeMs = processingTimeMs,
            UsedRag = usedRag
        };
    }
}
