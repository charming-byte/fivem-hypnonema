using System;
using Hypnonema.Server.Rpc;
using Hypnonema.Shared.Communications;

namespace Hypnonema.Server.Communications;

public class CommunicationMessage
{
    private readonly EventManager eventManager;

    public CommunicationMessage(string @event)
    {
        Event = @event;
    }

    public CommunicationMessage(string @event, EventManager eventManager) : this(@event)
    {
        this.eventManager = eventManager;
    }

    public CommunicationMessage(string @event, IClient client) : this(@event)
    {
        Client = client;
    }

    public CommunicationMessage(string @event, Guid id, IClient client) : this(@event, client)
    {
        Id = id;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string Event { get; }

    public IClient Client { get; }

    public void Reply(params object[] payloads)
    {
        if (Client == null)
            eventManager.Emit($"{Id}:{Event}", payloads);
        else
            RpcManager.Emit($"{Id}:{Event}", Client, payloads);
    }
}