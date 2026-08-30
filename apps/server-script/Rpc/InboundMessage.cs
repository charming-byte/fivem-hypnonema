using System;
using System.Collections.Generic;

namespace Hypnonema.Server.Rpc;

public class InboundMessage
{
    public string Event { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public List<string> Payloads { get; set; } = [];

    public string Source { get; set; }

    public static InboundMessage From(byte[] data)
    {
        return RpcPacker.Unpack(data);
    }
}