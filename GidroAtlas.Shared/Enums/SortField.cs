using GidroAtlas.Shared.Attributes;

namespace GidroAtlas.Shared.Enums;

public enum SortField
{
    [DisplayName("Название")]
    Name,
    
    [DisplayName("Регион")]
    Region,
    
    [DisplayName("Тип ресурса")]
    ResourceType,
    
    [DisplayName("Тип воды")]
    WaterType,
    
    [DisplayName("Наличие фауны")]
    HasFauna,
    
    [DisplayName("Дата паспорта")]
    PassportDate,
    
    [DisplayName("Техническое состояние")]
    TechnicalCondition,
    
    [DisplayName("Приоритет")]
    Priority
}
