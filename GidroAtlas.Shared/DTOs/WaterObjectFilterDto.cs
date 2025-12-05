using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

public class WaterObjectFilterDto
{
    public string? Region { get; set; }
    public ResourceType? ResourceType { get; set; }
    public WaterType? WaterType { get; set; }
    public bool? HasFauna { get; set; }
    public DateTime? PassportDateFrom { get; set; }
    public DateTime? PassportDateTo { get; set; }
    public int? TechnicalCondition { get; set; }
    public string? SearchQuery { get; set; }
    public SortField? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
