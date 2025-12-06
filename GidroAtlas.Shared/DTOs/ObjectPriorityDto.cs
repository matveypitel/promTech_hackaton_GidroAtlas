using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

/// <summary>
/// DTO for priority information of a specific water object.
/// </summary>
public class ObjectPriorityDto
{
    /// <summary>
    /// Water object ID.
    /// </summary>
    public Guid ObjectId { get; set; }

    /// <summary>
    /// Water object name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Calculated priority score using formula:
    /// PriorityScore = (6 - TechnicalCondition) * 3 + PassportAgeYears
    /// </summary>
    public int PriorityScore { get; set; }

    /// <summary>
    /// Priority level classification (High, Medium, Low).
    /// High: >= 12, Medium: 6-11, Low: < 6
    /// </summary>
    public PriorityLevel PriorityLevel { get; set; }

    /// <summary>
    /// ML-predicted probability that object requires attention (0.0 - 1.0).
    /// Null if ML model is not available.
    /// </summary>
    public double? AttentionProbability { get; set; }

    /// <summary>
    /// Technical condition score (1-5).
    /// </summary>
    public int TechnicalCondition { get; set; }

    /// <summary>
    /// Passport age in years (used in priority calculation).
    /// </summary>
    public int PassportAgeYears { get; set; }

    /// <summary>
    /// Date of the passport issuance.
    /// </summary>
    public DateTime PassportDate { get; set; }

    /// <summary>
    /// Indicates if ML model was used for prediction.
    /// </summary>
    public bool IsMlPredictionAvailable { get; set; }
}
