using GidroAtlas.Shared.DTOs;

namespace GidroAtlas.Web.Interfaces;

/// <summary>
/// Service for chat operations with the AI API
/// </summary>
public interface IChatApiService : IApiClient
{
    /// <summary>
    /// Sends a message to the chat AI and gets a response
    /// </summary>
    Task<ChatResponseDto?> AskAsync(string message);
    
    /// <summary>
    /// Gets the status of the chat service
    /// </summary>
    Task<ChatStatusDto?> GetStatusAsync();
}
