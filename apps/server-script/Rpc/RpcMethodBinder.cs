using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hypnonema.Server.Communications;
using Hypnonema.Shared.Communications;
using Serilog;

namespace Hypnonema.Server.Rpc;

public static class RpcMethodBinder
{
    private static readonly Dictionary<Type, RpcMethodBinding[]> BindingCache = [];

    private static readonly Dictionary<int, MethodInfo> BindHandlerOverloads =
        typeof(RpcMethodBinder)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name == nameof(BindHandler))
            .ToDictionary(method => method.GetGenericArguments().Length);

    private static bool permissionsEnabled = true;

    /// <summary>Applies the resource configuration. Call once at startup, before anything binds.</summary>
    public static void Init(bool permissionsEnabled, ILogger logger)
    {
        RpcMethodBinder.permissionsEnabled = permissionsEnabled;

        if (permissionsEnabled) return;

        logger.Warning(
            "Permission checks are disabled by configuration: every rpc handler accepts any client, regardless of its ACE requirement.");
    }

    public static List<EventSubscription> Bind(object instance, ILogger logger, Func<string, string>? scope = null)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        var subscriptions = new List<EventSubscription>();

        foreach (var binding in GetBindings(instance.GetType()))
        {
            var @event = scope != null ? scope(binding.Event) : binding.Event;

            subscriptions.Add((EventSubscription)binding.Binder.Invoke(null,
                new object?[] { @event, instance, binding.Method, binding.Permission, logger })!);
        }

        logger.Verbose("Bound {count} rpc handler(s) of {type}", subscriptions.Count, instance.GetType().Name);

        return subscriptions;
    }

    public static void Unbind(IEnumerable<EventSubscription> subscriptions)
    {
        foreach (var (@event, handler) in subscriptions) RpcManager.Off(@event, handler);
    }

    private static RpcMethodBinding[] GetBindings(Type type)
    {
        if (BindingCache.TryGetValue(type, out var cached)) return cached;

        // inherit: true so a handler annotated on a virtual base method still binds through its override — the walk
        // below yields the override, which carries neither attribute of its own. It also keeps an override from
        // silently shedding the ACE requirement its base method declared.
        var bindings = (from method in GetDeclaredMethods(type)
            from attribute in method.GetCustomAttributes<RpcMethodAttribute>(true)
            select new RpcMethodBinding(attribute.Event, method, ResolveBinder(method),
                method.GetCustomAttribute<PermissionRequiredAttribute>(true)?.RequiredPermission)).ToArray();

        BindingCache[type] = bindings;

        return bindings;
    }

    private static List<MethodInfo> GetDeclaredMethods(Type type)
    {
        // Walked by hand because GetMethods() does not return private methods of base types, and handlers are private.
        // Materialized rather than returned with 'yield return': the state machine Roslyn generates for an iterator
        // reads Environment.CurrentManagedThreadId in its constructor, which FiveM's CLR host rejects as inaccessible
        // when it verifies the type.
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

        if (parameters.Length < 1 || parameters[0].ParameterType != typeof(CommunicationMessage))
            throw new InvalidOperationException(
                $"[RpcMethod] handler '{Describe(method)}' must take a {nameof(CommunicationMessage)} as its first parameter.");

        var payloadTypes = parameters.Skip(1).Select(parameter => parameter.ParameterType).ToArray();

        if (!BindHandlerOverloads.TryGetValue(payloadTypes.Length, out var binder))
            throw new InvalidOperationException(
                $"[RpcMethod] handler '{Describe(method)}' takes {payloadTypes.Length} payloads, but at most " +
                $"{BindHandlerOverloads.Count - 1} are supported.");

        return payloadTypes.Length == 0 ? binder : binder.MakeGenericMethod(payloadTypes);
    }

    private static bool IsAllowed(CommunicationMessage message, string permission, MethodInfo method, ILogger logger)
    {
        // Checked per call rather than skipping the wrapper at bind time, so the gate reflects the current
        // configuration instead of whatever it was when this media player happened to be created.
        if (!permissionsEnabled) return true;

        if (message.Client == null)
        {
            logger.Warning(
                "Rejected '{event}': handler {handler} requires permission '{permission}' but the message carried no client.",
                message.Event, Describe(method), permission);

            return false;
        }

        if (message.Client.HasPermission(permission)) return true;

        logger.Warning("Rejected '{event}' from client '{client}': missing permission '{permission}'.",
            message.Event, message.Client, permission);

        var deniedEvent = method.GetCustomAttribute<PermissionRequiredAttribute>(true)?.DeniedEvent;

        if (deniedEvent is { Length: > 0 })
            RpcManager.Emit(deniedEvent, message.Client, $"You don't have permission for this action ('{permission}').");

        return false;
    }

    private static string Describe(MethodInfo method)
    {
        return $"{method.DeclaringType?.Name}.{method.Name}";
    }

    private static EventSubscription BindHandler(string @event, object instance, MethodInfo method, string? permission,
        ILogger logger)
    {
        var handler = (Action<CommunicationMessage>)Delegate.CreateDelegate(
            typeof(Action<CommunicationMessage>), instance, method);

        if (permission != null)
        {
            var inner = handler;

            handler = message =>
            {
                if (IsAllowed(message, permission, method, logger)) inner(message);
            };
        }

        RpcManager.On(@event, null, handler);

        return new EventSubscription(@event, handler);
    }

    private static EventSubscription BindHandler<T>(string @event, object instance, MethodInfo method,
        string? permission, ILogger logger)
    {
        var handler = (Action<CommunicationMessage, T>)Delegate.CreateDelegate(
            typeof(Action<CommunicationMessage, T>), instance, method);

        if (permission != null)
        {
            var inner = handler;

            handler = (message, payload) =>
            {
                if (IsAllowed(message, permission, method, logger)) inner(message, payload);
            };
        }

        RpcManager.On(@event, null, handler);

        return new EventSubscription(@event, handler);
    }

    private static EventSubscription BindHandler<T1, T2>(string @event, object instance, MethodInfo method,
        string? permission, ILogger logger)
    {
        var handler = (Action<CommunicationMessage, T1, T2>)Delegate.CreateDelegate(
            typeof(Action<CommunicationMessage, T1, T2>), instance, method);

        if (permission != null)
        {
            var inner = handler;

            handler = (message, first, second) =>
            {
                if (IsAllowed(message, permission, method, logger)) inner(message, first, second);
            };
        }

        RpcManager.On(@event, null, handler);

        return new EventSubscription(@event, handler);
    }

    private static EventSubscription BindHandler<T1, T2, T3>(string @event, object instance, MethodInfo method,
        string? permission, ILogger logger)
    {
        var handler = (Action<CommunicationMessage, T1, T2, T3>)Delegate.CreateDelegate(
            typeof(Action<CommunicationMessage, T1, T2, T3>), instance, method);

        if (permission != null)
        {
            var inner = handler;

            handler = (message, first, second, third) =>
            {
                if (IsAllowed(message, permission, method, logger)) inner(message, first, second, third);
            };
        }

        RpcManager.On(@event, null, handler);

        return new EventSubscription(@event, handler);
    }

    private static EventSubscription BindHandler<T1, T2, T3, T4>(string @event, object instance, MethodInfo method,
        string? permission, ILogger logger)
    {
        var handler = (Action<CommunicationMessage, T1, T2, T3, T4>)Delegate.CreateDelegate(
            typeof(Action<CommunicationMessage, T1, T2, T3, T4>), instance, method);

        if (permission != null)
        {
            var inner = handler;

            handler = (message, first, second, third, fourth) =>
            {
                if (IsAllowed(message, permission, method, logger)) inner(message, first, second, third, fourth);
            };
        }

        RpcManager.On(@event, null, handler);

        return new EventSubscription(@event, handler);
    }

    /// <summary>One annotated handler: the event it serves, the method, the closed binder, and its ACE requirement.</summary>
    private sealed class RpcMethodBinding(string @event, MethodInfo method, MethodInfo binder, string? permission)
    {
        public string Event { get; } = @event;

        public MethodInfo Method { get; } = method;

        public MethodInfo Binder { get; } = binder;

        public string? Permission { get; } = permission;
    }
}