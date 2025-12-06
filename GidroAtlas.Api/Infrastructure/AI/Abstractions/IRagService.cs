using GidroAtlas.Shared.DTOs;

namespace GidroAtlas.Api.Infrastructure.AI.Abstractions;

/// <summary>
/// Service for Retrieval-Augmented Generation (RAG) operations.
/// Handles semantic search over indexed documents.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Search for relevant documents based on a query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Number of top results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Relevant context and source information.</returns>
    Task<RagSearchResult> SearchAsync(
        string query, 
        int topK = 10, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the count of indexed documents.
    /// </summary>
    Task<int> GetIndexedDocumentCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of RAG search containing context and sources.
/// </summary>
public class RagSearchResult
{
    /// <summary>
    /// Combined context text from relevant documents.
    /// </summary>
    public string Context { get; init; } = string.Empty;
    
    /// <summary>
    /// Source documents with relevance scores.
    /// </summary>
    public List<ChatSourceDto> Sources { get; init; } = [];
    
    /// <summary>
    /// Whether any relevant documents were found.
    /// </summary>
    public bool HasRelevantContext => Sources.Count > 0;
    
    /// <summary>
    /// Average relevance score of found documents.
    /// </summary>
    public double AverageRelevance => Sources.Count > 0 
        ? Sources.Average(s => s.Relevance) 
        : 0;
}
