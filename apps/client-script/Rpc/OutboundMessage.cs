using System;
using System.Collections.Generic;
using CitizenFX.Core;

namespace Hypnonema.Client.Rpc;

public class OutboundMessage
{
    public string Event { get; set; } = default!;

    public Guid Id { get; set; }

    public List<string> Payloads { get; set; } = [];

    public int Source { get; set; } = Game.Player.ServerId;

    public byte[] Pack()
    {
        return RpcPacker.Pack(this);
    }
}