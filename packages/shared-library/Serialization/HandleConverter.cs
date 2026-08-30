using System;
using Hypnonema.Shared.Media;
using Newtonsoft.Json;

namespace Hypnonema.Shared.Serialization;

public sealed class HandleConverter : JsonConverter<Handle>
{
    public override Handle ReadJson(JsonReader reader, Type objectType, Handle existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        int parsedInt;

        if (reader.TokenType == JsonToken.String)
            int.TryParse((string)reader.Value, out parsedInt);
        else if (reader.TokenType == JsonToken.Integer)
            parsedInt = Convert.ToInt32(reader.Value);
        else
            throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");

        return new Handle(parsedInt);
    }

    public override void WriteJson(JsonWriter writer, Handle value, JsonSerializer serializer)
    {
        var timespanFormatted = $"{value.Value}";

        writer.WriteValue(timespanFormatted);
    }
}