using GidroAtlas.ML.Services;

// Paths
var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var dataDir = Path.Combine(projectRoot, "GidroAtlas.ML", "Data");
var modelDir = Path.Combine(projectRoot, "GidroAtlas.Api", "Infrastructure", "ML", "MLModels");

Console.WriteLine("========================================");
Console.WriteLine("  GidroAtlas ML Model Training");
Console.WriteLine("========================================");
Console.WriteLine();

var trainer = new ModelTrainer();

// ========== BASIC MODEL ==========
Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║  Training BASIC Model                ║");
Console.WriteLine("╚══════════════════════════════════════╝");
Console.WriteLine();

var basicDataPath = Path.Combine(dataDir, "training_data_basic.csv");
var basicModelPath = Path.Combine(modelDir, "priority_model_basic.zip");

Console.WriteLine("Step 1: Generating training data (Basic formula)...");
DataGenerator.GenerateTrainingData(basicDataPath, FormulaType.Basic, count: 10000);
Console.WriteLine();

Console.WriteLine("Step 2: Training model...");
trainer.Train(basicDataPath, basicModelPath);

Console.WriteLine("Step 3: Testing predictions...");
trainer.TestPredictions(basicModelPath);
Console.WriteLine();

// ========== ADVANCED MODEL ==========
Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║  Training ADVANCED Model             ║");
Console.WriteLine("╚══════════════════════════════════════╝");
Console.WriteLine();

var advancedDataPath = Path.Combine(dataDir, "training_data_advanced.csv");
var advancedModelPath = Path.Combine(modelDir, "priority_model_advanced.zip");

Console.WriteLine("Step 1: Generating training data (Advanced formula)...");
DataGenerator.GenerateTrainingData(advancedDataPath, FormulaType.Advanced, count: 10000);
Console.WriteLine();

Console.WriteLine("Step 2: Training model...");
trainer.Train(advancedDataPath, advancedModelPath);

Console.WriteLine("Step 3: Testing predictions...");
trainer.TestPredictions(advancedModelPath);
Console.WriteLine();

// ========== SUMMARY ==========
Console.WriteLine("========================================");
Console.WriteLine("  Training Complete!");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("Models saved:");
Console.WriteLine($"  Basic:    {basicModelPath}");
Console.WriteLine($"  Advanced: {advancedModelPath}");
Console.WriteLine();
Console.WriteLine("To switch models in API, set environment variable:");
Console.WriteLine("  ML_MODEL_TYPE=Basic    (default)");
Console.WriteLine("  ML_MODEL_TYPE=Advanced");
