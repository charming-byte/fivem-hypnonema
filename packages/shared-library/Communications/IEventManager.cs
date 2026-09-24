using System;

namespace Hypnonema.Shared.Communications;

public interface IEventManager : IDisposable
{
    void Emit(string @event, params object[] args);

    void Off(string @event, Delegate action);

    // TODO: Consider adding a generic version of On() to avoid using Delegate directly, which can be error-prone.
    void On(string @event, Delegate action);
}