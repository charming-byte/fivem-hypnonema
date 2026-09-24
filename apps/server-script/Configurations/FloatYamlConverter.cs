using System;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Hypnonema.Server.Configurations;

/// <summary>
///     YamlDotNet formats <see cref="float" /> with the "G" specifier, which keeps only ~7 significant digits and
///     does not round-trip on this runtime. <see cref="Media.ScreenRepository" /> re-parses every write and refuses
///     it when the result differs bit-for-bit from the in-memory data, so an editor-supplied coordinate such as
///     <c>-1678.94897</c> would make every screen save fail. Emit the shortest representation that actually
///     round-trips instead.
/// </summary>
public sealed class FloatYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(float);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        return float.Parse(parser.Consume<Scalar>().Value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(ToRoundTrippableString((float)value!)));
    }

    /// <summary>
    ///     "R" gives the shortest round-trippable form on modern runtimes; where it doesn't (the historical
    ///     .NET Framework "R" bug), fall back to "G9", which always round-trips a <see cref="float" />.
    /// </summary>
    public static string ToRoundTrippableString(float value)
    {
        var shortest = value.ToString("R", CultureInfo.InvariantCulture);

        return float.Parse(shortest, NumberStyles.Float, CultureInfo.InvariantCulture) == value
            ? shortest
            : value.ToString("G9", CultureInfo.InvariantCulture);
    }
}
