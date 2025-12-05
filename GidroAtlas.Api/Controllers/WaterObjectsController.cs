using GidroAtlas.Api.Abstractions;
using GidroAtlas.Shared.Constants;
using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GidroAtlas.Api.Controllers;

/// <summary>
/// Controller for water objects management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WaterObjectsController : ControllerBase
{
    private readonly IWaterObjectService _waterObjectService;
    private readonly ILogger<WaterObjectsController> _logger;

    public WaterObjectsController(IWaterObjectService waterObjectService, ILogger<WaterObjectsController> logger)
    {
        _waterObjectService = waterObjectService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all water objects with filtering, sorting and pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<WaterObjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<WaterObjectDto>>> GetAll(
        [FromQuery] string? region = null,
        [FromQuery] ResourceType? resourceType = null,
        [FromQuery] WaterType? waterType = null,
        [FromQuery] bool? hasFauna = null,
        [FromQuery] DateTime? passportDateFrom = null,
        [FromQuery] DateTime? passportDateTo = null,
        [FromQuery] int? technicalCondition = null,
        [FromQuery] string? search = null,
        [FromQuery] SortField? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new WaterObjectFilterDto
        {
            Region = region,
            ResourceType = resourceType,
            WaterType = waterType,
            HasFauna = hasFauna,
            PassportDateFrom = passportDateFrom,
            PassportDateTo = passportDateTo,
            TechnicalCondition = technicalCondition,
            SearchQuery = search,
            SortBy = sortBy,
            SortDescending = sortDesc,
            Page = page,
            PageSize = pageSize
        };

        var result = await _waterObjectService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific water object by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WaterObjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WaterObjectDto>> GetById(Guid id)
    {
        var result = await _waterObjectService.GetByIdAsync(id);
        
        if (result == null)
        {
            return NotFound(new { message = "Water object not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all available regions for filtering
    /// </summary>
    [HttpGet("regions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetRegions()
    {
        var regions = await _waterObjectService.GetRegionsAsync();
        return Ok(regions);
    }

    /// <summary>
    /// Gets prioritized water objects for inspection (Expert only)
    /// </summary>
    [HttpGet("priorities")]
    [Authorize(Policy = AuthPolicies.ExpertOnly)]
    [ProducesResponseType(typeof(PagedResponseDto<WaterObjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponseDto<WaterObjectDto>>> GetPriorities(
        [FromQuery] SortField? sortBy = SortField.Priority,
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new WaterObjectFilterDto
        {
            SortBy = sortBy ?? SortField.Priority,
            SortDescending = sortDesc,
            Page = page,
            PageSize = pageSize
        };

        var result = await _waterObjectService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Gets priority statistics summary (Expert only)
    /// </summary>
    /// TODO: Deprecate for AI usage
    [HttpGet("priorities/summary")]
    [Authorize(Policy = AuthPolicies.ExpertOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPrioritySummary()
    {
        var allObjects = await _waterObjectService.GetAllAsync(new WaterObjectFilterDto { PageSize = 1000 });
        
        var summary = new
        {
            Total = allObjects.TotalCount,
            High = allObjects.Items.Count(x => x.PriorityLevel == PriorityLevel.High),
            Medium = allObjects.Items.Count(x => x.PriorityLevel == PriorityLevel.Medium),
            Low = allObjects.Items.Count(x => x.PriorityLevel == PriorityLevel.Low)
        };

        return Ok(summary);
    }
}
