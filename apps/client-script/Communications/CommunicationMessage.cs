using System;
using Hypnonema.Client.Rpc;

namespace Hypnonema.Client.Communications;

public sealed class CommunicationMessage
{
    private readonly EventManager eventManager;

    private readonly bool networked;

    public CommunicationMessage(string @event)
    {
        Event = @event;
    }

    public CommunicationMessage(string @event, EventManager eventManager) : this(@event)
    {
        this.eventManager = eventManager;
    }

    public CommunicationMessage(string @event, Guid id, bool networked = false) : this(@event)
    {
        Id = id;
        this.networked = networked;
    }

    public string Event { get; }

    public Guid Id { get; } = Guid.NewGuid();

    public void Reply(params object[] payloads)
    {
        if (networked)
            RpcManager.Emit($"{Id}:{Event}", payloads);
        else
            eventManager.Emit($"{Id}:{Event}", payloads);
    }
}