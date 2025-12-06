namespace GidroAtlas.ML.Models;

using Microsoft.ML.Data;

/// <summary>
/// Input data for ML model training and prediction.
/// </summary>
public class WaterObjectInput
{
    /// <summary>
    /// Technical condition (1-5, where 5 is worst).
    /// </summary>
    [LoadColumn(0)]
    public float TechnicalCondition { get; set; }

    /// <summary>
    /// Age of passport in years.
    /// </summary>
    [LoadColumn(1)]
    public float PassportAgeYears { get; set; }

    /// <summary>
    /// Resource type (0=Lake, 1=Canal, 2=Reservoir).
    /// </summary>
    [LoadColumn(2)]
    public float ResourceType { get; set; }

    /// <summary>
    /// Water type (0=Fresh, 1=NonFresh).
    /// </summary>
    [LoadColumn(3)]
    public float WaterType { get; set; }

    /// <summary>
    /// Whether the object has fauna.
    /// </summary>
    [LoadColumn(4)]
    public float HasFauna { get; set; }

    /// <summary>
    /// Target label: whether the object requires attention.
    /// </summary>
    [LoadColumn(5), ColumnName("Label")]
    public bool RequiresAttention { get; set; }
}
