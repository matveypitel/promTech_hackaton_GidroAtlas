using GidroAtlas.Shared.Attributes;

namespace GidroAtlas.Shared.Enums;

public enum WaterType
{
    [DisplayName("Пресная")]
    Fresh = 0,
    
    [DisplayName("Непресная")]
    NonFresh = 1
}
