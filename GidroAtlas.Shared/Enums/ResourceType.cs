using GidroAtlas.Shared.Attributes;

namespace GidroAtlas.Shared.Enums;

public enum ResourceType
{
    [DisplayName("Озеро")]
    Lake = 0,
    
    [DisplayName("Канал")]
    Canal = 1,
    
    [DisplayName("Водохранилище")]
    Reservoir = 2
}
