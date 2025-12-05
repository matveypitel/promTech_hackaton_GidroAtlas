using System.Reflection;
using GidroAtlas.Shared.Attributes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GidroAtlas.Api.Infrastructure.Database.Converters;

public class EnumDisplayNameConverter<TEnum> : ValueConverter<TEnum, string> where TEnum : struct, Enum
{
    public EnumDisplayNameConverter() : base(
        v => ToDisplayName(v),
        v => FromDisplayName(v))
    {
    }

    private static string ToDisplayName(TEnum value)
    {
        var memberInfo = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
        var attribute = memberInfo?.GetCustomAttribute<DisplayNameAttribute>();
        return attribute?.Name ?? value.ToString();
    }

    private static TEnum FromDisplayName(string value)
    {
        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            var memberInfo = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
            var attribute = memberInfo?.GetCustomAttribute<DisplayNameAttribute>();
            if (attribute?.Name == value || enumValue.ToString() == value)
            {
                return enumValue;
            }
        }
        
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : default;
    }
}
