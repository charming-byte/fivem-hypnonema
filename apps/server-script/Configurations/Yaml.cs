using System;
using System.Collections.Generic;
using Hypnonema.Shared.Media.Audio;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Hypnonema.Server.Configurations;

public static class Yaml
{
    public static T Deserialize<T>(string yaml)
    {
        return Deserializer().Deserialize<T>(yaml);
    }

    public static string Serialize<T>(T value)
    {
        return Serializer().Serialize(value);
    }

    private static IDeserializer Deserializer()
    {
        return new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new YamlStringEnumConverter())
            .WithTypeConverter(new FloatYamlConverter())
            .WithTypeConverter(new DistanceAttenuationYamlConverter())
            .IgnoreUnmatchedProperties()
            .WithTypeDiscriminatingNodeDeserializer(options =>
            {
                options.AddKeyValueTypeDiscriminator<AudioConfiguration>(
                    "mode",
                    new Dictionary<string, Type> { { "default", typeof(DefaultAudioConfiguration) } });
            })
            .Build();
    }

    private static ISerializer Serializer()
    {
        return new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new YamlStringEnumConverter())
            .WithTypeConverter(new FloatYamlConverter())
            .WithTypeConverter(new DistanceAttenuationYamlConverter())
            .Build();
    }
}