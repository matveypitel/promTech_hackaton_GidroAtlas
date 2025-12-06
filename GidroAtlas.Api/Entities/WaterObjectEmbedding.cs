using Pgvector;

namespace GidroAtlas.Api.Entities;

/// <summary>
/// Entity for storing water object embeddings for vector search.
/// </summary>
public class WaterObjectEmbedding
{
    /// <summary>
    /// Water object ID (foreign key to WaterObject)
    /// </summary>
    public Guid WaterObjectId { get; set; }

    /// <summary>
    /// Chunk index (0 for main content, 1+ for additional chunks like PDF)
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Type of content (e.g., "main", "pdf", "technical")
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
    /// When the embedding was created/updated
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to WaterObject
    /// </summary>
    public WaterObject? WaterObject { get; set; }
}
