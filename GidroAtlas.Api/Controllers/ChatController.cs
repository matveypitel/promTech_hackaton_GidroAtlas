using GidroAtlas.Api.Infrastructure.Chat;
using GidroAtlas.Shared.Constants;
using GidroAtlas.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GidroAtlas.Api.Controllers;

/// <summary>
/// Controller for AI-powered chat functionality (Expert only).
/// Uses RAG (Retrieval-Augmented Generation) with water objects data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(AppConstants.ContentTypes.ApplicationJson)]
[Authorize(Policy = AuthPolicies.ExpertOnly)]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question about water objects using AI.
    /// </summary>
    /// <param name="request">Chat request with user's question.</param>
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
    public async Task<ActionResult<ChatResponseDto>> Ask([FromBody] ChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Сообщение не может быть пустым" });
        }

        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Chat request from {User}: {Message}", userName, request.Message);

        var response = await _chatService.AskAsync(request.Message);

        _logger.LogInformation("Chat response generated in {Time}ms, Used RAG: {UsedRag}", 
            response.ProcessingTimeMs, response.UsedRag);

        return Ok(response);
    }

    /// <summary>
    /// Get chat service status.
    /// </summary>
    /// <returns>Service availability and statistics.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ChatStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatStatusDto>> GetStatus()
    {
        var status = await _chatService.GetStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// Trigger indexing of all water objects for vector search.
    /// </summary>
    /// <returns>Number of indexed objects.</returns>
    /// <response code="200">Returns count of indexed objects</response>
    [HttpPost("index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> IndexWaterObjects()
    {
        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Indexing triggered by {User}", userName);

        var count = await _chatService.IndexAllWaterObjectsAsync();

        return Ok(new { 
            message = $"Успешно проиндексировано {count} водных объектов",
            indexedCount = count 
        });
    }
}
