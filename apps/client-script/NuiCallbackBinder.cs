using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hypnonema.Client.Diagnostics;
using Hypnonema.Shared.Communications;

namespace Hypnonema.Client;

public static class NuiCallbackBinder
{
    private static readonly Logger Logger = new("NuiBinder");

    private static readonly Dictionary<Type, NuiCallbackBinding[]> BindingCache = [];

    private static readonly Dictionary<int, MethodInfo> BindHandlerOverloads =
        typeof(NuiCallbackBinder)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name == nameof(BindHandler))
            .ToDictionary(method => method.GetGenericArguments().Length);

    public static List<EventSubscription> Bind(object instance, Func<string, string>? scope = null)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));

        var subscriptions = new List<EventSubscription>();

        foreach (var binding in GetBindings(instance.GetType()))
        {
            var @event = scope != null ? scope(binding.Event) : binding.Event;

            subscriptions.Add((EventSubscription)binding.Binder.Invoke(null,
                new object[] { @event, instance, binding.Method })!);
        }

        Logger.Verbose($"Bound {subscriptions.Count} nui callback(s) of {instance.GetType().Name}");

        return subscriptions;
    }

    public static void Unbind(IEnumerable<EventSubscription> subscriptions)
    {
        foreach (var (@event, _) in subscriptions) NuiManager.UnregisterNuiCallback(@event);
    }

    private static NuiCallbackBinding[] GetBindings(Type type)
    {
        if (BindingCache.TryGetValue(type, out var cached)) return cached;

        // inherit: true so a handler annotated on a virtual base method still binds through its override — the walk
        // below yields the override, which carries no attribute of its own.
        var bindings = (from method in GetDeclaredMethods(type)
            from attribute in method.GetCustomAttributes<NuiCallbackAttribute>(true)
            select new NuiCallbackBinding(attribute.Event, method, ResolveBinder(method))).ToArray();

        BindingCache[type] = bindings;

        return bindings;
    }

    private static List<MethodInfo> GetDeclaredMethods(Type type)
    {
        var methods = new List<MethodInfo>();
        var seen = new HashSet<MethodInfo>();

        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        foreach (var method in current.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                  BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            // GetBaseDefinition() collapses an override onto the method it overrides, so a handler declared in a base
            // type and overridden here binds once rather than twice.
            if (seen.Add(method.GetBaseDefinition()))
                methods.Add(method);

        return methods;
    }

    private static MethodInfo ResolveBinder(MethodInfo method)
    {
        var parameters = method.GetParameters();

        if (parameters.Length > 1)
            throw new InvalidOperationException(
                $"[NuiCallback] handler '{Describe(method)}' takes {parameters.Length} parameters, but at most 1 is supported.");

        var payloadTypes = parameters.Select(parameter => parameter.ParameterType).ToArray();

        if (!BindHandlerOverloads.TryGetValue(payloadTypes.Length, out var binder))
            throw new InvalidOperationException(
                $"[NuiCallback] handler '{Describe(method)}' takes {payloadTypes.Length} parameters, but at most " +
                $"{BindHandlerOverloads.Count - 1} are supported.");

        return payloadTypes.Length == 0 ? binder : binder.MakeGenericMethod(payloadTypes);
    }

    private static string Describe(MethodInfo method)
    {
        return $"{method.DeclaringType?.Name}.{method.Name}";
    }

    private static EventSubscription BindHandler(string @event, object instance, MethodInfo method)
    {
        var handler = (Action)Delegate.CreateDelegate(typeof(Action), instance, method);

        NuiManager.RegisterNuiCallback(@event, handler);

        return new EventSubscription(@event, handler);
    }

    private static EventSubscription BindHandler<T>(string @event, object instance, MethodInfo method)
    {
        var handler = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), instance, method);

        NuiManager.RegisterNuiCallback(@event, handler);

        return new EventSubscription(@event, handler);
    }

    private sealed class NuiCallbackBinding(string @event, MethodInfo method, MethodInfo binder)
    {
        public string Event { get; } = @event;

        public MethodInfo Method { get; } = method;

        public MethodInfo Binder { get; } = binder;
    }
}
