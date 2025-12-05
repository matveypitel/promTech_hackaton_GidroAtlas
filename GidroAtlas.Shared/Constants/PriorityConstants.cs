namespace GidroAtlas.Shared.Constants;

/// <summary>
/// Constants used for priority calculation and classification.
/// </summary>
public static class PriorityConstants
{
    /// <summary>
    /// Base value for technical condition calculation.
    /// </summary>
    public const int TechnicalConditionBase = 6;

    /// <summary>
    /// Multiplier for technical condition impact.
    /// </summary>
    public const int TechnicalConditionMultiplier = 3;

    /// <summary>
    /// Threshold for High priority level.
    /// </summary>
    public const int HighPriorityThreshold = 12;

    /// <summary>
    /// Threshold for Medium priority level.
    /// </summary>
    public const int MediumPriorityThreshold = 6;
}
