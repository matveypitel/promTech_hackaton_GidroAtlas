namespace GidroAtlas.Api.Infrastructure.Chat;

/// <summary>
/// Service for generating text embeddings using Ollama.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding vector for a text.
    /// </summary>
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple texts in batch.
    /// </summary>
    Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the embedding service is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
