using System;

namespace Hypnonema.Client;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class NuiCallbackAttribute(string @event) : Attribute
{
    public string Event { get; } = @event;
}
