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
[Produces(AppConstants.ContentTypes.ApplicationJson)]
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
            return NotFound(new { message = AppConstants.ErrorMessages.WaterObjectNotFound });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all available regions for filtering
    /// </summary>
    [HttpGet(AppConstants.Routes.Regions)]
    [Authorize(Policy = AuthPolicies.ExpertOnly)]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetRegions()
    {
        var regions = await _waterObjectService.GetRegionsAsync();
        return Ok(regions);
    }

    /// <summary>
    /// Gets the passport PDF file for a specific water object
    /// </summary>
    /// <param name="id">Water object ID</param>
    /// <returns>PDF file of the water object passport</returns>
    [HttpGet("{id:guid}/" + AppConstants.Routes.Passport)]
    [Authorize(Policy = AuthPolicies.ExpertOnly)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPassportPdf(Guid id)
    {
        var waterObject = await _waterObjectService.GetByIdAsync(id);
        
        if (waterObject == null)
        {
            return NotFound(new { message = AppConstants.ErrorMessages.WaterObjectNotFound });
        }

        var basePath = Environment.GetEnvironmentVariable("PASPORTS_PATH") ?? Path.Combine(Directory.GetCurrentDirectory(), "..");
        
        // Remove leading slash from PdfUrl if present (e.g., "/pasports/file.pdf" -> "pasports/file.pdf")
        var relativePath = waterObject.PdfUrl.TrimStart('/');
        
        // Construct full path to the PDF file
        var passportPath = Path.Combine(basePath, relativePath);

        if (!System.IO.File.Exists(passportPath))
        {
            _logger.LogWarning("Passport PDF not found for water object {ObjectId} at path {Path}", id, passportPath);
            return NotFound(new { message = "Passport PDF file not found", path = relativePath });
        }

        try
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(passportPath);
            
            // Create a user-friendly filename
            var downloadFileName = $"Паспорт_{waterObject.Name}.pdf";
            
            return File(fileBytes, AppConstants.ContentTypes.ApplicationPdf, downloadFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading passport PDF for water object {ObjectId} from path {Path}", id, passportPath);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Error reading passport PDF file" });
        }
    }

    /// <summary>
    /// Gets prioritized water objects for inspection (Expert only)
    /// </summary>
    [HttpGet(AppConstants.Routes.Priorities)]
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
    /// Gets priority information for a specific water object (Expert only)
    /// Includes ML-based attention probability prediction
    /// </summary>
    /// <param name="id">Water object ID</param>
    /// <returns>Priority details with ML prediction</returns>
    [HttpGet("{id:guid}/priority")]
    [Authorize(Policy = AuthPolicies.ExpertOnly)]
    [ProducesResponseType(typeof(ObjectPriorityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ObjectPriorityDto>> GetObjectPriority(Guid id)
    {
        var result = await _waterObjectService.GetObjectPriorityAsync(id);
        
        if (result == null)
        {
            return NotFound(new { message = AppConstants.ErrorMessages.WaterObjectNotFound });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets priority statistics summary (Expert only)
    /// </summary>
    /// TODO: Deprecate for AI usage
    [HttpGet(AppConstants.Routes.PrioritiesSummary)]
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

    /// <summary>
    /// Updates a water object by ID (Expert only)
    /// </summary>
    /// <param name="id">Water object ID</param>
    /// <param name="updateDto">Updated water object data</param>
    /// <returns>Updated water object</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.ExpertOnly)]
    [ProducesResponseType(typeof(WaterObjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WaterObjectDto>> Update(Guid id, [FromBody] UpdateWaterObjectDto updateDto)
    {
        _logger.LogInformation("Updating water object {Id}", id);
        
        var result = await _waterObjectService.UpdateAsync(id, updateDto);
        
        if (result == null)
        {
            return NotFound(new { message = AppConstants.ErrorMessages.WaterObjectNotFound });
        }

        _logger.LogInformation("Water object {Id} updated successfully", id);
        return Ok(result);
    }
}
