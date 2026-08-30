using System;
using Newtonsoft.Json;

namespace Hypnonema.Shared.Serialization;

public sealed class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        DateTime.TryParse((string)reader.Value, out var parsedTimeSpan);

        return parsedTimeSpan;
    }

    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
    {
        var timespanFormatted = $"{value.Ticks}";

        writer.WriteValue(timespanFormatted);
    }
}