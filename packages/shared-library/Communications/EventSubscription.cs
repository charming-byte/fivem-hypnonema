using System;

namespace Hypnonema.Shared.Communications;

public sealed class EventSubscription
{
    public EventSubscription(string @event, Delegate handler)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public string Event { get; }

    public Delegate Handler { get; }

    public void Deconstruct(out string @event, out Delegate handler)
    {
        @event = Event;
        handler = Handler;
    }
}