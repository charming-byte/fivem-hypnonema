using System;
using System.Collections.Generic;

namespace Hypnonema.Client.Rpc;

public class InboundMessage
{
    public string Event { get; set; } = default!;

    public Guid Id { get; set; }

    public List<string> Payloads { get; set; } = [];

    public static InboundMessage From(byte[] data)
    {
        return RpcPacker.Unpack(data);
    }
}