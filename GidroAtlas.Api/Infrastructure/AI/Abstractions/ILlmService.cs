namespace GidroAtlas.Api.Infrastructure.AI.Abstractions;

/// <summary>
/// Service for interacting with Large Language Models (LLM).
/// Handles text generation and model availability checks.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Generate a response from the LLM given system and user prompts.
    /// </summary>
    /// <param name="systemPrompt">System instruction for the model.</param>
    /// <param name="userPrompt">User's message/question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated response text, or null if generation failed.</returns>
    Task<string?> GenerateResponseAsync(
        string systemPrompt, 
        string userPrompt, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the LLM service is available and the model is loaded.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the current model name.
    /// </summary>
    string ModelName { get; }
}
