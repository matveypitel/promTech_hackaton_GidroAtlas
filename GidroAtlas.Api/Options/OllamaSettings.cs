namespace GidroAtlas.Api.Options;

/// <summary>
/// Settings for Ollama LLM integration.
/// </summary>
public class OllamaSettings
{
    /// <summary>
    /// Base URL for Ollama API (e.g., http://localhost:11434)
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name for chat/generation (e.g., qwen3:4b)
    /// </summary>
    public string ChatModel { get; set; } = "qwen3:4b";

    /// <summary>
    /// Model name for embeddings (e.g., nomic-embed-text)
    /// </summary>
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Temperature for generation (0.0 - 1.0). Lower = faster, more deterministic
    /// </summary>
    public float Temperature { get; set; } = 0.5f;

    /// <summary>
    /// Maximum number of tokens to generate. Lower = faster responses
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Number of context tokens for the model
    /// </summary>
    public int NumCtx { get; set; } = 8192;
}
