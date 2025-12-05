using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using GidroAtlas.Shared.Attributes;

namespace GidroAtlas.Shared.Converters;

public class EnumDisplayNameJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(EnumDisplayNameJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class EnumDisplayNameJsonConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private readonly Dictionary<T, string> _enumToString = new();
    private readonly Dictionary<string, T> _stringToEnum = new();

    public EnumDisplayNameJsonConverter()
    {
        var type = typeof(T);
        foreach (var value in Enum.GetValues<T>())
        {
            var memberInfo = type.GetMember(value.ToString()).FirstOrDefault();
            var attribute = memberInfo?.GetCustomAttribute<DisplayNameAttribute>();
            var stringValue = attribute?.Name ?? value.ToString();
            
            _enumToString[value] = stringValue;
            _stringToEnum[stringValue] = value;
            _stringToEnum[value.ToString()] = value;
        }
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (stringValue != null && _stringToEnum.TryGetValue(stringValue, out var enumValue))
        {
            return enumValue;
        }
        
        if (Enum.TryParse<T>(stringValue, true, out var parsed))
        {
            return parsed;
        }
        
        throw new JsonException($"Unable to convert \"{stringValue}\" to enum {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(_enumToString[value]);
    }
}
