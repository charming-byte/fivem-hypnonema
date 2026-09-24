using System;

namespace Hypnonema.Shared.Communications;

/// <summary>
///     Marks a method as the handler for an RPC event, so it is registered (and unregistered) automatically instead of
///     by a hand-maintained list of <c>RegisterEventHandler</c> calls.
/// </summary>
/// <remarks>
///     The annotated method must be an instance method taking a <c>CommunicationMessage</c> as its first parameter,
///     followed by one parameter per payload. The event name is the unscoped one — media players scope it by handle
///     when they bind.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class RpcMethodAttribute(string @event) : Attribute
{
    public string Event { get; } = @event;
}