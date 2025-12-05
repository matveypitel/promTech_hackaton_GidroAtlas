using System.Runtime.Serialization;

namespace GidroAtlas.Shared.Enums;

public enum WaterType
{
    [EnumMember(Value = "Пресная")]
    Fresh = 0,
    [EnumMember(Value = "Непресная")]
    NonFresh = 1,
}
