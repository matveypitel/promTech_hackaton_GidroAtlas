using System.Runtime.Serialization;

namespace GidroAtlas.Shared.Enums;

public enum ResourceType
{
    [EnumMember(Value = "Озеро")]
    Lake = 0,
    [EnumMember(Value = "Канал")]
    Canal = 1,
    [EnumMember(Value = "Водохранилище")]
    Reservoir = 2,
}
