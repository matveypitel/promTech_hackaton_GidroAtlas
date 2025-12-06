namespace GidroAtlas.Api.Infrastructure.ML;

using GidroAtlas.Api.Entities;
using Microsoft.ML;

/// <summary>
/// Service for ML-based priority predictions.
/// Supports switching between Basic and Advanced models.
/// </summary>
public class PredictionService : IDisposable
{
    private readonly PredictionEngine<WaterObjectMlInput, PriorityPrediction>? _predictionEngine;
    private readonly ILogger<PredictionService> _logger;
    private readonly bool _isModelLoaded;
    private readonly MlModelType _modelType;

    public PredictionService(ILogger<PredictionService> logger, IWebHostEnvironment env, IConfiguration configuration)
    {
        _logger = logger;

        // Get model type from configuration (default: Basic)
        var modelTypeStr = configuration["ML:ModelType"] ?? "Basic";
        _modelType = Enum.TryParse<MlModelType>(modelTypeStr, ignoreCase: true, out var parsed) 
            ? parsed 
            : MlModelType.Basic;

        var modelFileName = _modelType switch
        {
            MlModelType.Advanced => "priority_model_advanced.zip",
            _ => "priority_model_basic.zip"
        };

        var modelPath = Path.Combine(env.ContentRootPath, "Infrastructure", "ML", "MLModels", modelFileName);

        if (!File.Exists(modelPath))
        {
            // Fallback: try without suffix for backward compatibility
            var fallbackPath = Path.Combine(env.ContentRootPath, "Infrastructure", "ML", "MLModels", "priority_model.zip");
            if (File.Exists(fallbackPath))
            {
                modelPath = fallbackPath;
                _logger.LogWarning("Model {ModelType} not found, using fallback model at {FallbackPath}", _modelType, fallbackPath);
            }
            else
            {
                _logger.LogWarning("ML model not found at {ModelPath}. Predictions will return null.", modelPath);
                _isModelLoaded = false;
                return;
            }
        }

        try
        {
            var mlContext = new MLContext();
            var model = mlContext.Model.Load(modelPath, out _);
            _predictionEngine = mlContext.Model.CreatePredictionEngine<WaterObjectMlInput, PriorityPrediction>(model);
            _isModelLoaded = true;
            _logger.LogInformation("ML model ({ModelType}) loaded successfully from {ModelPath}", _modelType, modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ML model from {ModelPath}", modelPath);
            _isModelLoaded = false;
        }
    }

    /// <summary>
    /// Gets the probability that a water object requires attention.
    /// </summary>
    /// <param name="waterObject">The water object to evaluate.</param>
    /// <returns>Probability (0.0-1.0) or null if model is not available.</returns>
    public double? GetAttentionProbability(WaterObject waterObject)
    {
        if (!_isModelLoaded || _predictionEngine == null)
        {
            return null;
        }

        try
        {
            var input = new WaterObjectMlInput
            {
                TechnicalCondition = waterObject.TechnicalCondition,
                PassportAgeYears = (float)(DateTime.UtcNow - waterObject.PassportDate).TotalDays / 365f,
                ResourceType = (float)waterObject.ResourceType,
                WaterType = (float)waterObject.WaterType,
                HasFauna = waterObject.HasFauna ? 1f : 0f
            };

            var prediction = _predictionEngine.Predict(input);
            return Math.Round(prediction.Probability, 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting attention probability for water object {Id}", waterObject.Id);
            return null;
        }
    }

    /// <summary>
    /// Checks if the ML model is loaded and available.
    /// </summary>
    public bool IsModelAvailable => _isModelLoaded;

    /// <summary>
    /// Gets the currently loaded model type.
    /// </summary>
    public MlModelType ModelType => _modelType;

    public void Dispose()
    {
        (_predictionEngine as IDisposable)?.Dispose();
    }
}
