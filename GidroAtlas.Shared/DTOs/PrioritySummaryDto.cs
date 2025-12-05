namespace GidroAtlas.Shared.DTOs;

/// <summary>
/// Priority summary statistics DTO
/// </summary>
public class PrioritySummaryDto
{
    /// <summary>
    /// Total number of water objects
    /// </summary>
    public int Total { get; set; }
    
    /// <summary>
    /// Number of high priority objects
    /// </summary>
    public int High { get; set; }
    
    /// <summary>
    /// Number of medium priority objects
    /// </summary>
    public int Medium { get; set; }
    
    /// <summary>
    /// Number of low priority objects
    /// </summary>
    public int Low { get; set; }
}
