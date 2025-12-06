using GidroAtlas.Api.Entities;
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
    /// Determines the priority level based on the priority score (1-5).
    /// </summary>
    /// <param name="priority">The priority score (1-5).</param>
    /// <returns>The priority level (High, Medium, Low).</returns>
    PriorityLevel GetPriorityLevel(int priority);

    /// <summary>
    /// Calculates priority (1-5) using ML model's attention probability.
    /// 5 = highest priority (needs attention), 1 = lowest priority.
    /// If ML model is unavailable, falls back to formula-based calculation.
    /// </summary>
    /// <param name="entity">The water object entity.</param>
    /// <returns>Priority score from 1 to 5.</returns>
    int CalculateMlPriority(WaterObject entity);

    /// <summary>
    /// Gets detailed priority information for a specific water object.
    /// Includes calculated priority, ML prediction, and calculation details.
    /// </summary>
    /// <param name="id">The water object ID.</param>
    /// <returns>Object priority DTO with ML prediction, or null if not found.</returns>
    Task<ObjectPriorityDto?> GetObjectPriorityAsync(Guid id);

    /// <summary>
    /// Updates an existing water object by its unique identifier.
    /// Priority is automatically recalculated based on the new values.
    /// </summary>
    /// <param name="id">The unique identifier of the water object.</param>
    /// <param name="updateDto">The updated water object data.</param>
    /// <returns>The updated water object DTO if found; otherwise, null.</returns>
    Task<WaterObjectDto?> UpdateAsync(Guid id, UpdateWaterObjectDto updateDto);

    /// <summary>
    /// Creates a new water object.
    /// Priority is automatically calculated based on technical condition and passport date.
    /// </summary>
    /// <param name="createDto">The data for creating the water object.</param>
    /// <returns>The created water object DTO.</returns>
    Task<WaterObjectDto> CreateAsync(CreateWaterObjectDto createDto);

    /// <summary>
    /// Deletes a water object by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the water object.</param>
    /// <returns>True if the water object was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id);
}
