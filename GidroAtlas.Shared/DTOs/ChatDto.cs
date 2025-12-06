using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

/// <summary>
/// Request for chat endpoint.
/// </summary>
public class ChatRequestDto
{
    /// <summary>
    /// User's question or message.
    /// </summary>
    public required string Message { get; set; }
}

/// <summary>
/// Response from chat endpoint.
/// </summary>
public class ChatResponseDto
{
    /// <summary>
    /// AI-generated answer.
    /// </summary>
    public required string Answer { get; set; }

    /// <summary>
    /// Source water objects used for the answer.
    /// </summary>
    public List<ChatSourceDto> Sources { get; set; } = [];

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Whether ML was used for the response.
    /// </summary>
    public bool UsedRag { get; set; }

    /// <summary>
    /// Error message if any.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Source document used in the RAG response.
/// </summary>
public class ChatSourceDto
{
    /// <summary>
    /// Water object ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Water object name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Region of the water object.
    /// </summary>
    public required string Region { get; set; }

    /// <summary>
    /// Similarity score (0.0 - 1.0).
    /// </summary>
    public double Relevance { get; set; }

    /// <summary>
    /// Content snippet used.
    /// </summary>
    public string? ContentSnippet { get; set; }
}

/// <summary>
/// Status of the chat service.
/// </summary>
public class ChatStatusDto
{
    /// <summary>
    /// Whether the chat service is available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Whether embeddings are available.
    /// </summary>
    public bool EmbeddingsAvailable { get; set; }

    /// <summary>
    /// Whether LLM is available.
    /// </summary>
    public bool LlmAvailable { get; set; }

    /// <summary>
    /// Number of indexed water objects.
    /// </summary>
    public int IndexedObjectsCount { get; set; }

    /// <summary>
    /// LLM model name.
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Error message if service is not available.
    /// </summary>
    public string? Error { get; set; }
}
