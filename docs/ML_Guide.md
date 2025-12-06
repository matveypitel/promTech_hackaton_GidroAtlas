# ML.NET: Обучающий документ

Как работает ML система приоритизации на C# с ML.NET.

---

## Обзор ML.NET

**ML.NET** — это open-source ML фреймворк от Microsoft для .NET. Позволяет обучать и использовать ML модели полностью на C#.

```
Обучение → Сохранение модели (.zip) → Загрузка в API → Предсказания
```

---

## Архитектура системы

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  GidroAtlas.ML  │     │  GidroAtlas.Api │     │  GidroAtlas.Web │
│  (Training)     │────▶│  (Inference)    │◀────│  (UI)           │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │
        ▼                       ▼
┌─────────────────┐     ┌─────────────────┐
│ training_data   │     │ priority_model  │
│ .csv            │     │ .zip            │
└─────────────────┘     └─────────────────┘
```

---

## Структура данных

### Input (входные данные)

```csharp
public class WaterObjectInput
{
    [LoadColumn(0)]
    public float TechnicalCondition { get; set; }  // 1-5
    
    [LoadColumn(1)]
    public float PassportAgeYears { get; set; }    // 0-20
    
    [LoadColumn(2)]
    public float ResourceType { get; set; }        // 0/1/2
    
    [LoadColumn(3)]
    public float WaterType { get; set; }           // 0/1
    
    [LoadColumn(4)]
    public float HasFauna { get; set; }            // 0/1
    
    [LoadColumn(5), ColumnName("Label")]
    public bool RequiresAttention { get; set; }    // Target variable
}
```

### Output (предсказание)

```csharp
public class PriorityPrediction
{
    [ColumnName("PredictedLabel")]
    public bool RequiresAttention { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }         // 0.0 - 1.0
    
    [ColumnName("Score")]
    public float Score { get; set; }               // Raw score
}
```

---

## Логистическая регрессия

### Что это?

Логистическая регрессия — алгоритм **бинарной классификации**, который выдаёт вероятность принадлежности к классу.

```
P(требует внимания) = σ(w₁·x₁ + w₂·x₂ + ... + wₙ·xₙ + b)
```

Где:
- `σ(z) = 1 / (1 + e^(-z))` — сигмоидная функция
- `wᵢ` — обученные веса признаков
- `xᵢ` — значения признаков объекта
- `b` — смещение (bias)

### Почему логистическая регрессия?

1. **Простота** — легко обучить и интерпретировать
2. **Скорость** — мгновенный инференс (< 1ms)
3. **Вероятность** — выдаёт калиброванную вероятность
4. **ML.NET** — встроенный алгоритм `LbfgsLogisticRegression`

---

## Генерация обучающих данных

Поскольку реальных размеченных данных нет, генерируем синтетические на основе формулы из ТЗ:

```csharp
// Формула приоритета из ТЗ
var priority = (6 - techCondition) * 3 + passportAgeYears;

// Добавляем шум для реалистичности
priority += random.NextDouble() * 2 - 1;  // ±1

// Дополнительные факторы
if (waterType == 1) priority += 0.5;      // Непресная вода
if (hasFauna == 1) priority += 0.3;       // Наличие фауны
if (resourceType == 2) priority += 0.5;   // Водохранилище

// Label: требует внимания если приоритет >= 10
var requiresAttention = priority >= 10;
```

---

## Обучение модели

### Pipeline

```csharp
var pipeline = mlContext.Transforms
    // 1. Объединение признаков в вектор
    .Concatenate("Features",
        "TechnicalCondition", "PassportAgeYears", 
        "ResourceType", "WaterType", "HasFauna")
    // 2. Нормализация (0-1)
    .Append(mlContext.Transforms.NormalizeMinMax("Features"))
    // 3. Алгоритм
    .Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression());
```

### Запуск обучения

```bash
cd GidroAtlas.ML
dotnet run
```

Вывод:
```
===== Model Metrics =====
Accuracy:     95.20%
AUC:          0.9850
F1 Score:     0.9412
Precision:    93.50%
Recall:       94.75%

Model saved to: .../GidroAtlas.Api/Infrastructure/ML/MLModels/priority_model.zip
```

---

## Использование в API

### PredictionService

```csharp
public class PredictionService
{
    private readonly PredictionEngine<WaterObjectMlInput, PriorityPrediction> _engine;
    
    public PredictionService(IWebHostEnvironment env)
    {
        var modelPath = Path.Combine(env.ContentRootPath, "Infrastructure", "ML", "MLModels", "priority_model.zip");
        var mlContext = new MLContext();
        var model = mlContext.Model.Load(modelPath, out _);
        _engine = mlContext.Model.CreatePredictionEngine<...>(model);
    }
    
    public double? GetAttentionProbability(WaterObject obj)
    {
        var input = new WaterObjectMlInput
        {
            TechnicalCondition = obj.TechnicalCondition,
            PassportAgeYears = (float)(DateTime.UtcNow - obj.PassportDate).TotalDays / 365f,
            ResourceType = (float)obj.ResourceType,
            WaterType = (float)obj.WaterType,
            HasFauna = obj.HasFauna ? 1f : 0f
        };
        
        return _engine.Predict(input).Probability;
    }
}
```

### Результат в API

```json
{
  "id": "...",
  "name": "Озеро Балхаш",
  "technicalCondition": 4,
  "priority": 14,
  "priorityLevel": "High",
  "attentionProbability": 0.847
}
```

---

## Интерпретация результатов

| Вероятность | Уровень | Рекомендация |
|-------------|---------|--------------|
| ≥ 0.7 | Высокий | Требует срочного обследования |
| 0.4 - 0.7 | Средний | Плановое обследование |
| < 0.4 | Низкий | Может подождать |

---

## Переобучение на реальных данных

Когда появятся реальные данные с метками:

1. Подготовить CSV:
   ```
   TechnicalCondition,PassportAgeYears,ResourceType,WaterType,HasFauna,RequiresAttention
   4,8.5,2,1,1,True
   1,1.2,0,0,0,False
   ...
   ```

2. Изменить путь в `Program.cs`
3. Запустить `dotnet run`
4. Модель автоматически сохранится в API

---

## FAQ

**Q: Что если ML сервис недоступен?**

A: Поле `AttentionProbability` будет `null`. Формула приоритета продолжит работать.

**Q: Как добавить новый признак?**

1. Добавить колонку в CSV
2. Добавить поле в `WaterObjectInput`
3. Добавить в `Concatenate("Features", ...)`
4. Переобучить модель

**Q: Можно заменить алгоритм?**

Да, ML.NET поддерживает:
- `SdcaLogisticRegression` — быстрее на больших данных
- `FastTree` — gradient boosting
- `LightGbm` — ещё точнее
