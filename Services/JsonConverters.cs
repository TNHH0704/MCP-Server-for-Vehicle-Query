using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace McpVersionVer2.Services;

/// <summary>
/// Custom DateTime converter for consistent date formatting across all JSON responses
/// </summary>
public class CustomDateTimeConverter : JsonConverter<DateTime>
{
    private readonly string _format = "dd-MM-yyyy HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        return DateTime.ParseExact(reader.GetString()!, _format, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(_format));
    }
}

/// <summary>
/// Custom nullable DateTime converter for consistent date formatting
/// </summary>
public class CustomNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly string _format = "dd-MM-yyyy HH:mm:ss";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrEmpty(value) ? null : DateTime.ParseExact(value, _format, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, System.Text.Json.JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(_format));
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Global JSON serializer options for consistent formatting across the application
/// </summary>
public static class AppJsonSerializerOptions
{
    public static System.Text.Json.JsonSerializerOptions Default => new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = 
        {
            new CustomDateTimeConverter(),
            new CustomNullableDateTimeConverter()
        }
    };
}