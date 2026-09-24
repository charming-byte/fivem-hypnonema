using System;
using Hypnonema.Client.Media;
using Hypnonema.Shared.Media;
using Newtonsoft.Json.Converters;

namespace Hypnonema.Client.Serialization;

public sealed class TrackConverter : CustomCreationConverter<ITrack>
{
    public override ITrack Create(Type objectType)
    {
        return new Track();
    }
}