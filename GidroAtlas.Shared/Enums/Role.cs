using System.Runtime.Serialization;

namespace GidroAtlas.Shared.Enums;

public enum Role
{
    [EnumMember(Value = "Гость")]
    Guest = 0,
    [EnumMember(Value = "Эксперт")]
    Expert = 1
}
