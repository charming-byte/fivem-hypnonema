using System;
using Hypnonema.Shared.Media.Audio;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hypnonema.Shared.Serialization;

/// <summary>
///     Deserializes the abstract <see cref="AudioConfiguration" /> to its concrete type. The immutable
///     <see cref="DistanceAttenuation" /> record is rebuilt explicitly through its constructor — Newtonsoft's
///     default member handling reuses the existing default instance and can't assign its get-only properties.
/// </summary>
public sealed class AudioConfigurationConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(AudioConfiguration);
    }

    public override bool CanWrite => false;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (JToken.ReadFrom(reader) is not JObject jobj) return null;

        // "default" is the only mode today. An unrecognized mode yields null so ScreenValidator can reject it with a
        // clear message instead of it being silently coerced to "default".
        if (!IsKnownMode(Prop(jobj, "mode"))) return null;

        // Branch on the mode here as concrete types are added.
        return new DefaultAudioConfiguration
        {
            Attenuation = ReadAttenuation(Prop(jobj, "attenuation") as JObject)
        };
    }

    private static bool IsKnownMode(JToken? mode)
    {
        if (mode == null || mode.Type == JTokenType.Null) return true; // absent → default

        return mode.Type switch
        {
            JTokenType.Integer => mode.Value<int>() == (int)AudioMode.Default,
            JTokenType.String => string.Equals(mode.Value<string>(), "default", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static DistanceAttenuation ReadAttenuation(JObject? attenuation)
    {
        if (attenuation == null) return DistanceAttenuation.Default;

        var fallback = DistanceAttenuation.Default;

        return new DistanceAttenuation(
            Prop(attenuation, "minDistance")?.Value<float>() ?? fallback.MinDistance,
            Prop(attenuation, "maxDistance")?.Value<float>() ?? fallback.MaxDistance,
            Prop(attenuation, "occludedVolume")?.Value<float>() ?? fallback.OccludedVolume);
    }

    // Payloads reach us in camelCase (NUI) and PascalCase (internal RPC), so accept either.
    private static JToken? Prop(JObject obj, string camelCaseName)
    {
        var pascalCaseName = char.ToUpperInvariant(camelCaseName[0]) + camelCaseName.Substring(1);
        return obj[camelCaseName] ?? obj[pascalCaseName];
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }
}
