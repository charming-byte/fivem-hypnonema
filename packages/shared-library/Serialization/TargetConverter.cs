using System;
using Hypnonema.Shared.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Hypnonema.Shared.Serialization;

public sealed class TargetConverter : CustomCreationConverter<Target>
{
    private Model? model;

    private Screen? screen;
    private TargetType? targetType;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jobj = JObject.ReadFrom(reader);
        if (jobj == null) throw new JsonSerializationException("Invalid JSON received. JObject is null");

        targetType = jobj["type"]?.ToObject<TargetType>(serializer) ?? jobj["Type"]?.ToObject<TargetType>(serializer) ?? null;
        screen = jobj["screen"]?.ToObject<Screen>(serializer) ?? jobj["Screen"]?.ToObject<Screen>(serializer) ?? null;
        model = jobj["model"]?.ToObject<Model>(serializer) ?? jobj["Model"]?.ToObject<Model>(serializer) ?? null;

        // Create() already builds a fully-formed target from the parsed screen/model. A follow-up serializer.Populate
        // (what base.ReadJson does) re-assigns nested members in place and mangles immutable ones like the
        // DistanceAttenuation record, so don't.
        return Create(objectType);
    }

    public override Target Create(Type objectType)
    {
        switch (targetType)
        {
            case TargetType.Screen:
                if (screen == null)
                    throw new InvalidOperationException("Invalid JSON received. Screen can not be null for Screen");
                return new ScreenTarget(screen);
            case TargetType.Model:
                if (model == null)
                    throw new InvalidOperationException("Invalid JSON received. Model can not be null for Model");
                return new ModelTarget(model);
            default:
                throw new NotImplementedException();
        }
    }
}