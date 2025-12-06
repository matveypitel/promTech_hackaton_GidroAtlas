namespace GidroAtlas.ML.Services;

using System.Text;

/// <summary>
/// Formula types for priority calculation.
/// </summary>
public enum FormulaType
{
    /// <summary>
    /// Basic linear formula from ТЗ.
    /// </summary>
    Basic,
    
    /// <summary>
    /// Advanced formula with non-linear effects and interactions.
    /// </summary>
    Advanced
}

/// <summary>
/// Generates synthetic training data based on different priority formulas.
/// </summary>
public static class DataGenerator
{
    /// <summary>
    /// Generates training data CSV file using specified formula.
    /// </summary>
    /// <param name="outputPath">Path to save CSV file.</param>
    /// <param name="formulaType">Formula type to use for label generation.</param>
    /// <param name="count">Number of samples to generate.</param>
    public static void GenerateTrainingData(string outputPath, FormulaType formulaType, int count = 10000)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var sb = new StringBuilder();
        
        // CSV header
        sb.AppendLine("TechnicalCondition,PassportAgeYears,ResourceType,WaterType,HasFauna,RequiresAttention");

        for (int i = 0; i < count; i++)
        {
            // Random features
            var techCondition = random.Next(1, 6);              // 1-5
            var passportAge = random.NextDouble() * 15;          // 0-15 years
            var resourceType = random.Next(0, 3);                // 0=Lake, 1=Canal, 2=Reservoir
            var waterType = random.Next(0, 2);                   // 0=Fresh, 1=NonFresh
            var hasFauna = random.Next(0, 2);                    // 0 or 1

            // Calculate priority based on formula type
            var priority = formulaType switch
            {
                FormulaType.Basic => CalculateBasicPriority(techCondition, passportAge, resourceType, waterType, hasFauna, random),
                FormulaType.Advanced => CalculateAdvancedPriority(techCondition, passportAge, resourceType, waterType, hasFauna, random),
                _ => throw new ArgumentException($"Unknown formula type: {formulaType}")
            };

            // Label: requires attention if priority >= threshold
            var threshold = formulaType == FormulaType.Basic ? 10.0 : 8.0;
            var requiresAttention = priority >= threshold;

            sb.AppendLine($"{techCondition},{passportAge:F2},{resourceType},{waterType},{hasFauna},{requiresAttention}");
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"Generated {count} training samples using {formulaType} formula to {outputPath}");
    }

    /// <summary>
    /// Basic linear formula from ТЗ.
    /// Priority = (6 - состояние) * 3 + возраст_паспорта + небольшие бонусы
    /// </summary>
    private static double CalculateBasicPriority(int techCondition, double passportAge, int resourceType, int waterType, int hasFauna, Random random)
    {
        // Base formula from ТЗ
        var priority = (6 - techCondition) * 3 + passportAge;
        
        // Add noise for realistic variation (±1)
        priority += (random.NextDouble() * 2 - 1);

        // Additional factors (small linear weights)
        if (waterType == 1) priority += 0.5;      // Non-fresh water
        if (hasFauna == 1) priority += 0.3;       // Fauna presence
        if (resourceType == 2) priority += 0.5;   // Reservoirs

        return priority;
    }

    /// <summary>
    /// Advanced formula with non-linear effects and feature interactions.
    /// More realistic for production use.
    /// </summary>
    private static double CalculateAdvancedPriority(int techCondition, double passportAge, int resourceType, int waterType, int hasFauna, Random random)
    {
        // 1. Non-linear technical condition (exponential growth for bad conditions)
        // Condition 1 → 11.2, Condition 3 → 5.2, Condition 5 → 2.0
        var techScore = Math.Pow(6 - techCondition, 1.5) * 2;
        
        // 2. Passport age with diminishing returns (logarithmic)
        // 1 year → 2.1, 5 years → 5.4, 10 years → 7.2, 15 years → 8.4
        var ageScore = Math.Log(passportAge + 1) * 3;
        
        // 3. Interaction: bad condition + old passport = critical
        var interactionBonus = 0.0;
        if (techCondition >= 4 && passportAge >= 5)
        {
            interactionBonus = 2.5;
        }
        else if (techCondition >= 3 && passportAge >= 8)
        {
            interactionBonus = 1.5;
        }
        
        // 4. Resource type weights (reservoirs are critical infrastructure)
        var resourceBonus = resourceType switch
        {
            2 => 1.5,  // Reservoir - critical
            1 => 0.5,  // Canal - moderate
            _ => 0.0   // Lake - base
        };
        
        // 5. Environmental factors
        var envBonus = 0.0;
        if (waterType == 1) envBonus += 0.8;       // Non-fresh water harder to maintain
        if (hasFauna == 1) envBonus += 0.5;        // Fauna requires better conditions
        
        // 6. Critical combinations (multiplicative effects)
        var criticalBonus = 0.0;
        if (resourceType == 2 && techCondition >= 4)
        {
            // Critical: Reservoir in bad condition
            criticalBonus += 3.0;
        }
        if (waterType == 1 && hasFauna == 1 && techCondition >= 3)
        {
            // Critical: Non-fresh water with fauna and degrading condition
            criticalBonus += 2.0;
        }
        
        // Add noise (smaller than basic formula)
        var noise = random.NextDouble() * 1.5 - 0.75;  // ±0.75
        
        // Total priority
        return techScore + ageScore + interactionBonus + resourceBonus + envBonus + criticalBonus + noise;
    }
}
