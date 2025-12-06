using Pgvector;

namespace GidroAtlas.Api.Entities;

/// <summary>
/// Entity for storing document embeddings for vector search.
/// Used for PDF documents, reference materials, and other standalone content.
/// </summary>
public class DocumentEmbedding
{
    /// <summary>
    /// Unique identifier for this embedding record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Document name/title (e.g., "Справочник водохранилищ Казахстана")
    /// </summary>
    public required string DocumentName { get; set; }

    /// <summary>
    /// Original file name if uploaded from file
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Chunk index (0, 1, 2, ... for multi-chunk documents)
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Type of content (e.g., "pdf", "reference", "manual", "regulation")
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Original text content used for embedding
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Vector embedding (768 dimensions for nomic-embed-text)
    /// </summary>
    public required Vector Embedding { get; set; }

    /// <summary>
    /// When the embedding was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
