namespace GidroAtlas.ML.Services;

using GidroAtlas.ML.Models;
using Microsoft.ML;

/// <summary>
/// Trains and saves the ML model for priority prediction.
/// </summary>
public class ModelTrainer
{
    private readonly MLContext _mlContext;

    public ModelTrainer()
    {
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Trains the model and saves it to the specified path.
    /// </summary>
    /// <param name="dataPath">Path to training data CSV.</param>
    /// <param name="modelPath">Path to save trained model.</param>
    public void Train(string dataPath, string modelPath)
    {
        Console.WriteLine("Loading training data...");
        
        // Load data from CSV
        var data = _mlContext.Data.LoadFromTextFile<WaterObjectInput>(
            dataPath,
            hasHeader: true,
            separatorChar: ',');

        // Split into train (80%) and test (20%) sets
        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        Console.WriteLine("Building ML pipeline...");
        
        // Build the training pipeline
        var pipeline = _mlContext.Transforms
            // Combine all features into a single vector
            .Concatenate("Features",
                nameof(WaterObjectInput.TechnicalCondition),
                nameof(WaterObjectInput.PassportAgeYears),
                nameof(WaterObjectInput.ResourceType),
                nameof(WaterObjectInput.WaterType),
                nameof(WaterObjectInput.HasFauna))
            // Normalize features to 0-1 range
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            // Use Logistic Regression for binary classification
            .Append(_mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        Console.WriteLine("Training model...");
        
        // Train the model
        var model = pipeline.Fit(split.TrainSet);

        Console.WriteLine("Evaluating model...");
        
        // Evaluate on test set
        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        Console.WriteLine();
        Console.WriteLine("===== Model Metrics =====");
        Console.WriteLine($"Accuracy:     {metrics.Accuracy:P2}");
        Console.WriteLine($"AUC:          {metrics.AreaUnderRocCurve:F4}");
        Console.WriteLine($"F1 Score:     {metrics.F1Score:F4}");
        Console.WriteLine($"Precision:    {metrics.PositivePrecision:P2}");
        Console.WriteLine($"Recall:       {metrics.PositiveRecall:P2}");
        Console.WriteLine();

        // Ensure output directory exists
        var modelDirectory = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrEmpty(modelDirectory) && !Directory.Exists(modelDirectory))
        {
            Directory.CreateDirectory(modelDirectory);
        }

        // Save the model
        _mlContext.Model.Save(model, data.Schema, modelPath);
        Console.WriteLine($"Model saved to: {modelPath}");
    }

    /// <summary>
    /// Tests the model with sample predictions.
    /// </summary>
    /// <param name="modelPath">Path to trained model.</param>
    public void TestPredictions(string modelPath)
    {
        Console.WriteLine();
        Console.WriteLine("===== Test Predictions =====");

        var model = _mlContext.Model.Load(modelPath, out _);
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<WaterObjectInput, PriorityPrediction>(model);

        // Test case 1: Bad condition (5), old passport (10 years) - should be HIGH priority
        var badCase = new WaterObjectInput
        {
            TechnicalCondition = 5,
            PassportAgeYears = 10,
            ResourceType = 2, // Reservoir
            WaterType = 1,    // NonFresh
            HasFauna = 1
        };
        var badPrediction = predictionEngine.Predict(badCase);
        Console.WriteLine($"Bad condition (5), 10 years old:");
        Console.WriteLine($"  Requires Attention: {badPrediction.RequiresAttention}");
        Console.WriteLine($"  Probability: {badPrediction.Probability:P1}");

        // Test case 2: Good condition (1), new passport (1 year) - should be LOW priority
        var goodCase = new WaterObjectInput
        {
            TechnicalCondition = 1,
            PassportAgeYears = 1,
            ResourceType = 0, // Lake
            WaterType = 0,    // Fresh
            HasFauna = 0
        };
        var goodPrediction = predictionEngine.Predict(goodCase);
        Console.WriteLine($"Good condition (1), 1 year old:");
        Console.WriteLine($"  Requires Attention: {goodPrediction.RequiresAttention}");
        Console.WriteLine($"  Probability: {goodPrediction.Probability:P1}");

        // Test case 3: Medium condition (3), medium age (5 years)
        var mediumCase = new WaterObjectInput
        {
            TechnicalCondition = 3,
            PassportAgeYears = 5,
            ResourceType = 1, // Canal
            WaterType = 0,    // Fresh
            HasFauna = 1
        };
        var mediumPrediction = predictionEngine.Predict(mediumCase);
        Console.WriteLine($"Medium condition (3), 5 years old:");
        Console.WriteLine($"  Requires Attention: {mediumPrediction.RequiresAttention}");
        Console.WriteLine($"  Probability: {mediumPrediction.Probability:P1}");
    }
}
