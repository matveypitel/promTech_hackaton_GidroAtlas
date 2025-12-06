using GidroAtlas.Shared.DTOs;

namespace GidroAtlas.Api.Infrastructure.Chat;

/// <summary>
/// Service for RAG-based chat functionality.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Process a user question and generate an AI response using RAG.
    /// </summary>
    Task<ChatResponseDto> AskAsync(string question, CancellationToken cancellationToken = default);

    /// <summary>
    /// Index all water objects for vector search.
    /// </summary>
    Task<int> IndexAllWaterObjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the chat service is available.
    /// </summary>
    Task<ChatStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
