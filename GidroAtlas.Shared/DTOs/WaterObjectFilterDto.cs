using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

/// <summary>
/// Filter criteria for querying water objects.
/// </summary>
public class WaterObjectFilterDto
{
    /// <summary>
    /// Filter by region name.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Filter by resource type.
    /// </summary>
    public ResourceType? ResourceType { get; set; }

    /// <summary>
    /// Filter by water type.
    /// </summary>
    public WaterType? WaterType { get; set; }

    /// <summary>
    /// Filter by presence of fauna.
    /// </summary>
    public bool? HasFauna { get; set; }

    /// <summary>
    /// Filter records with passport date from this date.
    /// </summary>
    public DateTime? PassportDateFrom { get; set; }

    /// <summary>
    /// Filter records with passport date up to this date.
    /// </summary>
    public DateTime? PassportDateTo { get; set; }

    /// <summary>
    /// Filter by technical condition score.
    /// </summary>
    public int? TechnicalCondition { get; set; }

    /// <summary>
    /// General search query for text fields (e.g., Name).
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public SortField? SortBy { get; set; }

    /// <summary>
    /// Sort direction (true for descending).
    /// </summary>
    public bool SortDescending { get; set; } = false;

    /// <summary>
    /// Page number (1-based index).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;
}
