using GidroAtlas.Shared.Attributes;

namespace GidroAtlas.Shared.Enums;

public enum SortField
{
    [DisplayName("Название")]
    Name = 0,
    
    [DisplayName("Регион")]
    Region = 1,
    
    [DisplayName("Тип ресурса")]
    ResourceType = 2,
    
    [DisplayName("Тип воды")]
    WaterType = 3,
    
    [DisplayName("Наличие фауны")]
    HasFauna = 4,
    
    [DisplayName("Дата паспорта")]
    PassportDate = 5,
    
    [DisplayName("Техническое состояние")]
    TechnicalCondition = 6,
    
    [DisplayName("Приоритет")]
    Priority = 7
}
