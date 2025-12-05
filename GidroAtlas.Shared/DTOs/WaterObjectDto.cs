using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

/// <summary>
/// Data Transfer Object representing a Water Object.
/// </summary>
public class WaterObjectDto
{
    /// <summary>
    /// Unique identifier of the water object.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the water object.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Region where the water object is located.
    /// </summary>
    public required string Region { get; set; }

    /// <summary>
    /// Type of resource (e.g., Surface, Underground).
    /// </summary>
    public ResourceType ResourceType { get; set; }

    /// <summary>
    /// Specific water type (e.g., River, Lake).
    /// </summary>
    public WaterType WaterType { get; set; }

    /// <summary>
    /// Indicates if there are any specific fauna associated.
    /// </summary>
    public bool HasFauna { get; set; }

    /// <summary>
    /// Date of the passport issuance.
    /// </summary>
    public DateTime PassportDate { get; set; }

    /// <summary>
    /// Technical condition score (lower might be worse).
    /// </summary>
    public int TechnicalCondition { get; set; }

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    public float Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    public float Longitude { get; set; }

    /// <summary>
    /// URL to the PDF document.
    /// </summary>
    public required string PdfUrl { get; set; }

    /// <summary>
    /// Calculated priority score.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// priority level classification (High, Medium, Low).
    /// </summary>
    public PriorityLevel PriorityLevel { get; set; }
}
