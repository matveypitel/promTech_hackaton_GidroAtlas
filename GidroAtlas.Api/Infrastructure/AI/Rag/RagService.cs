using System.Text;
using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Infrastructure.Database;
using GidroAtlas.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace GidroAtlas.Api.Infrastructure.AI.Rag;

/// <summary>
/// RAG service implementation using pgvector for semantic search.
/// Searches across both water object embeddings and document embeddings.
/// </summary>
public class RagService : IRagService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<RagService> _logger;

    private const int MaxContentSnippetLength = 500;
    private const double MinRelevanceThreshold = 0.3; // Minimum relevance to include in results

    public RagService(
        ApplicationDbContext context,
        IEmbeddingService embeddingService,
        ILogger<RagService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<RagSearchResult> SearchAsync(
        string query,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate embedding for the query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            
            if (queryEmbedding == null)
            {
                _logger.LogWarning("Failed to generate embedding for query: {Query}", 
                    query.Length > 50 ? query[..50] + "..." : query);
                return new RagSearchResult();
            }

            var queryVector = new Vector(queryEmbedding);

            // Search water object embeddings
            var waterObjectResults = await _context.WaterObjectEmbeddings
                .OrderBy(e => e.Embedding.CosineDistance(queryVector))
                .Take(topK)
                .Include(e => e.WaterObject)
                .Select(e => new SearchResultItem
                {
                    SourceId = e.WaterObjectId,
                    Content = e.Content,
                    ContentType = e.ContentType,
                    SourceName = e.WaterObject != null ? e.WaterObject.Name : "Unknown",
                    SourceRegion = e.WaterObject != null ? e.WaterObject.Region : null,
                    Distance = e.Embedding.CosineDistance(queryVector),
                    IsWaterObject = true
                })
                .ToListAsync(cancellationToken);

            // Search document embeddings
            var documentResults = await _context.DocumentEmbeddings
                .OrderBy(e => e.Embedding.CosineDistance(queryVector))
                .Take(topK)
                .Select(e => new SearchResultItem
                {
                    SourceId = e.Id,
                    Content = e.Content,
                    ContentType = e.ContentType,
                    SourceName = e.DocumentName,
                    SourceRegion = null,
                    Distance = e.Embedding.CosineDistance(queryVector),
                    IsWaterObject = false
                })
                .ToListAsync(cancellationToken);

            // Merge and sort by distance
            var allResults = waterObjectResults
                .Concat(documentResults)
                .OrderBy(r => r.Distance)
                .Take(topK)
                .ToList();

            // Build context and sources
            var contextBuilder = new StringBuilder();
            var sources = new List<ChatSourceDto>();
            var seenIds = new HashSet<Guid>();

            foreach (var result in allResults)
            {
                var relevance = Math.Round(1 - result.Distance, 3);
                
                // Skip documents with very low relevance
                if (relevance < MinRelevanceThreshold)
                    continue;

                contextBuilder.AppendLine($"---\n{result.Content}\n");

                if (!seenIds.Contains(result.SourceId))
                {
                    seenIds.Add(result.SourceId);
                    sources.Add(new ChatSourceDto
                    {
                        Id = result.SourceId,
                        Name = result.SourceName,
                        Region = result.SourceRegion,
                        Relevance = relevance,
                        ContentSnippet = TruncateContent(result.Content)
                    });
                }
            }

            _logger.LogDebug("RAG search found {Count} relevant documents for query " +
                            "(water objects: {WoCount}, documents: {DocCount})", 
                sources.Count, 
                waterObjectResults.Count(r => 1 - r.Distance >= MinRelevanceThreshold),
                documentResults.Count(r => 1 - r.Distance >= MinRelevanceThreshold));

            return new RagSearchResult
            {
                Context = contextBuilder.ToString(),
                Sources = sources
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RAG search for query: {Query}", 
                query.Length > 50 ? query[..50] + "..." : query);
            return new RagSearchResult();
        }
    }

    public async Task<int> GetIndexedDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        var waterObjectCount = await _context.WaterObjectEmbeddings
            .Select(e => e.WaterObjectId)
            .Distinct()
            .CountAsync(cancellationToken);

        var documentCount = await _context.DocumentEmbeddings
            .Select(e => e.DocumentName)
            .Distinct()
            .CountAsync(cancellationToken);

        return waterObjectCount + documentCount;
    }

    private static string TruncateContent(string content)
    {
        return content.Length > MaxContentSnippetLength
            ? content[..MaxContentSnippetLength] + "..."
            : content;
    }

    private class SearchResultItem
    {
        public Guid SourceId { get; init; }
        public required string Content { get; init; }
        public required string ContentType { get; init; }
        public required string SourceName { get; init; }
        public string? SourceRegion { get; init; }
        public double Distance { get; init; }
        public bool IsWaterObject { get; init; }
    }
}
