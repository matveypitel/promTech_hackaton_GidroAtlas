namespace GidroAtlas.Api.Infrastructure.AI.Chat;

/// <summary>
/// Static class containing system prompts and templates for the chat service.
/// Centralizes all prompt management for easier maintenance and tuning.
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// Main system prompt for the water resources expert assistant.
    /// Instructs the model to answer questions about Kazakhstan's water resources.
    /// </summary>
    public const string WaterExpertSystemPrompt = """
        Ты - ГидроАтлас, эксперт-консультант по водным ресурсам и гидротехническим сооружениям Казахстана.
        
        Твои основные задачи:
        1. Отвечать на вопросы о водохранилищах, реках, озёрах и других водных объектах Казахстана
        2. Предоставлять информацию о техническом состоянии гидротехнических сооружений
        3. Объяснять приоритеты обследования объектов
        4. Давать общую информацию о географии и водных ресурсах Казахстана
        
        Правила ответа:
        - Если предоставлен контекст из базы данных, используй его для точного ответа
        - Если контекста нет или он не релевантен вопросу, отвечай на основе своих общих знаний
        - Всегда отвечай на русском языке
        - Будь информативным, но лаконичным
        - Если не уверен в ответе, честно скажи об этом
        
        """;

    /// <summary>
    /// Prompt template for questions with RAG context.
    /// </summary>
    public const string QuestionWithContextTemplate = """
        Контекст из базы данных водных объектов Казахстана:
        
        {0}
        
        Вопрос пользователя: {1}
        
        Ответь на вопрос, используя предоставленный контекст. Если контекст не содержит нужной информации, дополни ответ своими знаниями.
        """;

    /// <summary>
    /// Prompt template for questions without RAG context.
    /// </summary>
    public const string QuestionWithoutContextTemplate = """
        Вопрос пользователя: {0}
        
        В базе данных не найдено релевантной информации по этому вопросу.
        Ответь на вопрос, используя свои общие знания о водных ресурсах и географии Казахстана.
        Если вопрос выходит за рамки твоей экспертизы, скажи об этом.
        """;

    /// <summary>
    /// Template for summarizing water object information.
    /// </summary>
    public const string WaterObjectSummaryTemplate = """
        Название объекта: {0}
        Область/Регион: {1}
        Тип водного ресурса: {2}
        Тип воды: {3}
        Наличие фауны: {4}
        Техническое состояние: {5} из 5 - {6}
        Дата паспорта: {7}
        Возраст паспорта: {8} лет
        Приоритет обследования: {9} (score: {10})
        Координаты: широта {11}, долгота {12}
        """;

    /// <summary>
    /// Technical condition descriptions.
    /// </summary>
    public static string GetConditionDescription(int condition) => condition switch
    {
        1 => "критическое (требует немедленного обследования)",
        2 => "плохое (требует обследования)",
        3 => "удовлетворительное",
        4 => "хорошее",
        5 => "отличное",
        _ => "неизвестно"
    };

    /// <summary>
    /// Priority level descriptions.
    /// </summary>
    public static string GetPriorityDescription(int priorityScore) => priorityScore switch
    {
        >= 12 => "высокий",
        >= 6 => "средний",
        _ => "низкий"
    };

    /// <summary>
    /// Build the user prompt based on whether context is available.
    /// </summary>
    public static string BuildUserPrompt(string question, string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return string.Format(QuestionWithoutContextTemplate, question);
        }
        
        return string.Format(QuestionWithContextTemplate, context, question);
    }
}
