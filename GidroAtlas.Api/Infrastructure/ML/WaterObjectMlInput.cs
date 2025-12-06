namespace GidroAtlas.Api.Infrastructure.ML;

/// <summary>
/// Input data for ML model prediction.
/// </summary>
public class WaterObjectMlInput
{
    public float TechnicalCondition { get; set; }
    public float PassportAgeYears { get; set; }
    public float ResourceType { get; set; }
    public float WaterType { get; set; }
    public float HasFauna { get; set; }
}
