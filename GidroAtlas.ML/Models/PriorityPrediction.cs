namespace GidroAtlas.ML.Models;

using Microsoft.ML.Data;

/// <summary>
/// Output from ML model prediction.
/// </summary>
public class PriorityPrediction
{
    /// <summary>
    /// Predicted label: whether object requires attention.
    /// </summary>
    [ColumnName("PredictedLabel")]
    public bool RequiresAttention { get; set; }

    /// <summary>
    /// Probability that object requires attention (0.0 - 1.0).
    /// </summary>
    [ColumnName("Probability")]
    public float Probability { get; set; }

    /// <summary>
    /// Raw prediction score.
    /// </summary>
    [ColumnName("Score")]
    public float Score { get; set; }
}
