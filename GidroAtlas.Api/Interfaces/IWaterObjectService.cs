using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Api.Abstractions;

/// <summary>
/// Service for managing water objects.
/// </summary>
public interface IWaterObjectService
{
    /// <summary>
    /// Retrieves a paged list of water objects based on the provided filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>A paged response containing water object DTOs.</returns>
    Task<PagedResponseDto<WaterObjectDto>> GetAllAsync(WaterObjectFilterDto filter);

    /// <summary>
    /// Retrieves a specific water object by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the water object.</param>
    /// <returns>The water object DTO if found; otherwise, null.</returns>
    Task<WaterObjectDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a list of all available regions.
    /// </summary>
    /// <returns>A list of region names.</returns>
    Task<List<string>> GetRegionsAsync();

    /// <summary>
    /// Calculates the priority score based on technical condition and passport date.
    /// </summary>
    /// <param name="technicalCondition">The technical condition score.</param>
    /// <param name="passportDate">The date of the passport issuance.</param>
    /// <returns>The calculated priority score.</returns>
    int CalculatePriority(int technicalCondition, DateTime passportDate);

    /// <summary>
    /// Determines the priority level based on the priority score.
    /// </summary>
    /// <param name="priority">The priority score.</param>
    /// <returns>The priority level (High, Medium, Low).</returns>
    PriorityLevel GetPriorityLevel(int priority);

    /// <summary>
    /// Gets detailed priority information for a specific water object.
    /// Includes calculated priority, ML prediction, and calculation details.
    /// </summary>
    /// <param name="id">The water object ID.</param>
    /// <returns>Object priority DTO with ML prediction, or null if not found.</returns>
    Task<ObjectPriorityDto?> GetObjectPriorityAsync(Guid id);

    /// <summary>
    /// Updates an existing water object by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the water object.</param>
    /// <param name="updateDto">The updated water object data.</param>
    /// <returns>The updated water object DTO if found; otherwise, null.</returns>
    Task<WaterObjectDto?> UpdateAsync(Guid id, UpdateWaterObjectDto updateDto);
}
