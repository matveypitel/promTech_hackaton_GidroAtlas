using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

/// <summary>
/// Data Transfer Object for updating a Water Object.
/// </summary>
public class UpdateWaterObjectDto
{
    /// <summary>
    /// Name of the water object.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Region where the water object is located.
    /// </summary>
    public required string Region { get; set; }

    /// <summary>
    /// Type of resource (e.g., Lake, Canal, Reservoir).
    /// </summary>
    public ResourceType ResourceType { get; set; }

    /// <summary>
    /// Specific water type (e.g., Fresh, NonFresh).
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
    /// Technical condition score (1-5, where 5 is best).
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
}
