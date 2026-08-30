using System;
using System.Collections.Generic;
using Hypnonema.Shared.Communications;
using Newtonsoft.Json;

namespace Hypnonema.Server.Rpc;

public class OutboundMessage
{
    public string Event { get; set; } = default!;

    public Guid Id { get; set; }

    public List<string> Payloads { get; set; } = [];

    [JsonIgnore] public IClient? Target { get; set; }

    public byte[] Pack()
    {
        return RpcPacker.Pack(this);
    }
}