namespace GidroAtlas.Api.Infrastructure.ML;

using Microsoft.ML.Data;

/// <summary>
/// Output from ML model prediction.
/// </summary>
public class PriorityPrediction
{
    [ColumnName("PredictedLabel")]
    public bool RequiresAttention { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}
