using System.Reflection;
using GidroAtlas.Shared.Attributes;

namespace GidroAtlas.Shared.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var memberInfo = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
        var attribute = memberInfo?.GetCustomAttribute<DisplayNameAttribute>();
        return attribute?.Name ?? value.ToString();
    }

    public static TEnum? FromDisplayName<TEnum>(string displayName) where TEnum : struct, Enum
    {
        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (value.GetDisplayName() == displayName || value.ToString() == displayName)
            {
                return value;
            }
        }
        
        return Enum.TryParse<TEnum>(displayName, true, out var parsed) ? parsed : null;
    }

    public static Dictionary<TEnum, string> GetAllDisplayNames<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>().ToDictionary(v => v, v => v.GetDisplayName());
    }
}
