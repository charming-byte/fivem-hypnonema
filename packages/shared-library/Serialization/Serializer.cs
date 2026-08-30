using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hypnonema.Shared.Serialization;

public static class Serializer
{
    /// <summary>
    ///     The nui serializer settings used for serialization of nui values
    /// </summary>
    public static readonly JsonSerializerSettings NuiSerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
        DefaultValueHandling = DefaultValueHandling.Include,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        },
        Converters = new List<JsonConverter>
        {
            new DateTimeConverter(),
            new TargetConverter(),
            new HandleConverter(),
            new AudioConfigurationConverter()
        }
    };

    public static Formatting Formatting { get; set; } = Formatting.None;

    public static JsonSerializerSettings Settings { get; set; } = new()
    {
        Converters = new List<JsonConverter>
        {
            new HandleConverter(), new TargetConverter(), new AudioConfigurationConverter()
        }
    };

    public static T Deserialize<T>(string json, JsonSerializerSettings settings)
    {
        return JsonConvert.DeserializeObject<T>(json, settings);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }

    public static string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting, Settings);
    }

    public static string Serialize(object obj, JsonSerializerSettings settings)
    {
        return JsonConvert.SerializeObject(obj, Formatting, settings);
    }
}