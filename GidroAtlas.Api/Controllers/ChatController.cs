using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Infrastructure.Documents.Abstractions;
using GidroAtlas.Shared.Constants;
using GidroAtlas.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GidroAtlas.Api.Controllers;

/// <summary>
/// Controller for AI-powered chat functionality.
/// Uses RAG (Retrieval-Augmented Generation) with water objects data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(AppConstants.ContentTypes.ApplicationJson)]
[Authorize(Policy = AuthPolicies.ExpertOnly)]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IDocumentIndexingService _indexingService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IDocumentIndexingService indexingService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _indexingService = indexingService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question about water objects using AI.
    /// The AI will use RAG to find relevant context from the database,
    /// but can also answer general questions about Kazakhstan's water resources.
    /// </summary>
    /// <param name="request">Chat request with user's question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI-generated response with sources.</returns>
    /// <response code="200">Returns AI response</response>
    /// <response code="400">If message is empty</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not an expert</response>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ChatResponseDto>> Ask(
        [FromBody] ChatRequestDto request, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Сообщение не может быть пустым" });
        }

        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Chat request from {User}: {Message}", 
            userName, 
            request.Message.Length > 100 ? request.Message[..100] + "..." : request.Message);

        var response = await _chatService.AskAsync(request.Message, cancellationToken);

        _logger.LogInformation(
            "Chat response generated in {Time}ms, Used RAG: {UsedRag}, Sources: {SourceCount}", 
            response.ProcessingTimeMs, 
            response.UsedRag,
            response.Sources.Count);

        return Ok(response);
    }

    /// <summary>
    /// Get chat service status including LLM and embedding availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service availability and statistics.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ChatStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _chatService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Trigger indexing of all water objects for vector search.
    /// This will re-index all water objects from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of indexed objects.</returns>
    /// <response code="200">Returns count of indexed objects</response>
    [HttpPost("index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> IndexWaterObjects(CancellationToken cancellationToken)
    {
        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Water objects indexing triggered by {User}", userName);

        var count = await _chatService.IndexAllWaterObjectsAsync(cancellationToken);

        return Ok(new { 
            message = $"Успешно проиндексировано {count} водных объектов",
            indexedCount = count 
        });
    }

    /// <summary>
    /// Upload and index a PDF document for RAG search.
    /// The PDF will be split into chunks and indexed for semantic search.
    /// </summary>
    /// <param name="file">PDF file to upload.</param>
    /// <param name="documentName">Optional name for the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of indexed chunks.</returns>
    [HttpPost("index/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<IActionResult> IndexPdfDocument(
        IFormFile file,
        [FromQuery] string? documentName,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Файл не загружен" });
        }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Поддерживаются только PDF файлы" });
        }

        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("PDF indexing triggered by {User}: {FileName} ({Size} bytes)", 
            userName, file.FileName, file.Length);

        // Save file temporarily
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var name = documentName ?? Path.GetFileNameWithoutExtension(file.FileName);
            var chunksIndexed = await _indexingService.IndexPdfDocumentAsync(
                tempPath, 
                name, 
                cancellationToken: cancellationToken);

            return Ok(new
            {
                message = $"PDF успешно проиндексирован: {chunksIndexed} фрагментов",
                documentName = name,
                chunksIndexed,
                originalFileName = file.FileName,
                fileSizeBytes = file.Length
            });
        }
        finally
        {
            // Clean up temp file
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Index plain text content for RAG search.
    /// </summary>
    /// <param name="request">Text content and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of indexed chunks.</returns>
    [HttpPost("index/text")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IndexTextContent(
        [FromBody] IndexTextRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Контент не может быть пустым" });
        }

        if (string.IsNullOrWhiteSpace(request.DocumentName))
        {
            return BadRequest(new { message = "Название документа обязательно" });
        }

        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Text indexing triggered by {User}: {DocumentName} ({Length} chars)", 
            userName, request.DocumentName, request.Content.Length);

        var chunksIndexed = await _indexingService.IndexTextContentAsync(
            request.Content,
            request.DocumentName,
            request.ContentType ?? "reference",
            cancellationToken);

        return Ok(new
        {
            message = $"Текст успешно проиндексирован: {chunksIndexed} фрагментов",
            documentName = request.DocumentName,
            chunksIndexed
        });
    }

    /// <summary>
    /// Clear indexed documents of a specific type.
    /// </summary>
    /// <param name="contentType">Type of content to clear (e.g., "pdf", "reference"). Leave empty to clear all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearIndex(
        [FromQuery] string? contentType,
        CancellationToken cancellationToken)
    {
        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Index clear triggered by {User}, type: {Type}", 
            userName, contentType ?? "all");

        await _indexingService.ClearIndexAsync(contentType, cancellationToken);

        return Ok(new { message = $"Индекс очищен (тип: {contentType ?? "все"})" });
    }
}

/// <summary>
/// Request DTO for indexing text content.
/// </summary>
public class IndexTextRequest
{
    /// <summary>
    /// Text content to index.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Name of the document.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <summary>
    /// Type of content (e.g., "reference", "manual", "regulation").
    /// </summary>
    public string? ContentType { get; init; }
}
