using System;
using System.Globalization;
using Hypnonema.Shared.Media.Audio;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Hypnonema.Server.Configurations;

public sealed class DistanceAttenuationYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(DistanceAttenuation);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        parser.Consume<MappingStart>();

        var minDistance = DistanceAttenuation.Default.MinDistance;
        var maxDistance = DistanceAttenuation.Default.MaxDistance;
        var occludedVolume = DistanceAttenuation.Default.OccludedVolume;

        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>().Value;
            var value = parser.Consume<Scalar>().Value;

            switch (key)
            {
                case "minDistance":
                    minDistance = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "maxDistance":
                    maxDistance = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "occludedVolume":
                    occludedVolume = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        // DistanceAttenuation no longer self-validates (ScreenValidator is the gate for the RPC path). A
        // hand-edited screens.yaml is still rejected here so a corrupt range can't be loaded at startup.
        var errors = DistanceAttenuation.Validate(minDistance, maxDistance, occludedVolume);
        if (errors.Count > 0) throw new ArgumentException(string.Join("; ", errors));

        return new DistanceAttenuation(minDistance, maxDistance, occludedVolume);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not DistanceAttenuation attenuation) return;

        emitter.Emit(new MappingStart());
        emitter.Emit(new Scalar("minDistance"));
        emitter.Emit(new Scalar(FloatYamlConverter.ToRoundTrippableString(attenuation.MinDistance)));
        emitter.Emit(new Scalar("maxDistance"));
        emitter.Emit(new Scalar(FloatYamlConverter.ToRoundTrippableString(attenuation.MaxDistance)));
        emitter.Emit(new Scalar("occludedVolume"));
        emitter.Emit(new Scalar(FloatYamlConverter.ToRoundTrippableString(attenuation.OccludedVolume)));
        emitter.Emit(new MappingEnd());
    }
}
