using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Web.Interfaces;

/// <summary>
/// Service for water objects operations with the API
/// </summary>
public interface IWaterObjectApiService : IApiClient
{
    /// <summary>
    /// Gets all water objects with filtering and pagination
    /// </summary>
    Task<PagedResponseDto<WaterObjectDto>?> GetAllAsync(WaterObjectFilterDto filter);
    
    /// <summary>
    /// Gets a specific water object by ID
    /// </summary>
    Task<WaterObjectDto?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Gets all available regions
    /// </summary>
    Task<List<string>?> GetRegionsAsync();
    
    /// <summary>
    /// Gets prioritized water objects (requires Expert role)
    /// </summary>
    Task<PagedResponseDto<WaterObjectDto>?> GetPrioritiesAsync(SortField? sortBy = null, bool sortDesc = true, int page = 1, int pageSize = 20);
    
    /// <summary>
    /// Gets priority statistics summary (requires Expert role)
    /// </summary>
    Task<PrioritySummaryDto?> GetPrioritySummaryAsync();

    /// <summary>
    /// Gets priority details for a specific water object (requires Expert role)
    /// Includes ML-based attention probability prediction
    /// </summary>
    Task<ObjectPriorityDto?> GetObjectPriorityAsync(Guid id);
    
    /// <summary>
    /// Gets the passport PDF for a specific water object (requires Expert role)
    /// </summary>
    /// <param name="id">Water object ID</param>
    /// <returns>Tuple with PDF bytes, content type and file name, or null if not found</returns>
    Task<(byte[] Content, string ContentType, string FileName)?> GetPassportAsync(Guid id);
    
    /// <summary>
    /// Updates an existing water object (requires Expert role)
    /// </summary>
    /// <param name="id">Water object ID</param>
    /// <param name="updateDto">Updated water object data</param>
    /// <returns>Updated water object DTO, or null if not found</returns>
    Task<WaterObjectDto?> UpdateAsync(Guid id, UpdateWaterObjectDto updateDto);
}
