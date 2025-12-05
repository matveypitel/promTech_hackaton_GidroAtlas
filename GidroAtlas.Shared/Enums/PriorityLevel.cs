using GidroAtlas.Shared.Attributes;

namespace GidroAtlas.Shared.Enums;

public enum PriorityLevel
{
    [DisplayName("Низкий")]
    Low = 0,
    
    [DisplayName("Средний")]
    Medium = 1,
    
    [DisplayName("Высокий")]
    High = 2
}
