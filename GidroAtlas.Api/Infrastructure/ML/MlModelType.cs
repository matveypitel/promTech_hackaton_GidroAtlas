namespace GidroAtlas.Api.Infrastructure.ML;

/// <summary>
/// ML model type for priority prediction.
/// </summary>
public enum MlModelType
{
    /// <summary>
    /// Basic model trained with linear formula from ТЗ.
    /// </summary>
    Basic,

    /// <summary>
    /// Advanced model with non-linear effects and interactions.
    /// </summary>
    Advanced
}
