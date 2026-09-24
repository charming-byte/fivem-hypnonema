using System;
using Hypnonema.Shared.Communications;
using Newtonsoft.Json.Converters;

namespace Hypnonema.Client.Serialization;

public sealed class ClientConverter : CustomCreationConverter<IClient>
{
    public override IClient Create(Type objectType)
    {
        return new Communications.Client();
    }
}