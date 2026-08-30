using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Communications;
using Hypnonema.Client.Diagnostics;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Serialization;

#pragma warning disable CS8601 // Possible null reference assignment.

namespace Hypnonema.Client.Rpc;

public static class RpcManager
{
    private static readonly Logger Logger = new("RPC");

    private static EventHandlerDictionary? events;

    private static readonly Dictionary<Tuple<string, Delegate>, Action<byte[]>> subscriptions = [];


    public static void Init(EventHandlerDictionary events)
    {
        RpcManager.events = events;
    }

    public static void Emit(string @event, params object[] payloads)
    {
        Emit(new OutboundMessage
        {
            Id = Guid.NewGuid(),
            Source = API.GetPlayerServerId(API.PlayerId()),
            Event = @event,
            Payloads = payloads.Select(Serializer.Serialize).ToList()
        });
    }

    public static void Off(string @event, Delegate callback)
    {
        if (events == null) throw new InvalidOperationException("Events handler is not initialized.");

        Logger.Verbose(
            $"Off: \"{@event}\" detached from \"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}({string.Join(", ", callback.Method.GetParameters().Select(p => p.ParameterType + " " + p.Name))})\"");

        var key = Tuple.Create(@event, callback);

        if (!subscriptions.TryGetValue(key, out var handler))
        {
            Logger.Warning(
                $"Failed to unregister callback {callback.PrintCallback()} for event {@event}. Callback was not registered");

            return;
        }

        events[@event] -= handler;

        subscriptions.Remove(key);
    }

    public static void On(string @event, Action<CommunicationMessage> callback)
    {
        InternalOn(@event, callback, m => []);
    }

    public static void On<T>(string @event, Action<CommunicationMessage, T> callback)
    {
        InternalOn(@event, callback, m =>
        [
            Serializer.Deserialize<T>(m.Payloads[0])
        ]);
    }

    public static void On<T1, T2>(string @event, Action<CommunicationMessage, T1, T2> callback)
    {
        InternalOn(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1])
        ]);
    }

    public static void On<T1, T2, T3>(string @event, Action<CommunicationMessage, T1, T2, T3> callback)
    {
        InternalOn(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2])
        ]);
    }

    public static void On<T1, T2, T3, T4>(string @event, Action<CommunicationMessage, T1, T2, T3, T4> callback)
    {
        InternalOn(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2]),
            Serializer.Deserialize<T4>(m.Payloads[3])
        ]);
    }

    public static void On<T1, T2, T3, T4, T5>(string @event, Action<CommunicationMessage, T1, T2, T3, T4, T5> callback)
    {
        InternalOn(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2]),
            Serializer.Deserialize<T4>(m.Payloads[3]),
            Serializer.Deserialize<T5>(m.Payloads[4])
        ]);
    }

    public static void OnRaw(string @event, Delegate callback)
    {
        if (events == null) throw new InvalidOperationException("Events handler is not initialized.");

        Logger.Verbose(
            $"OnRaw: \"{@event}\" attached to \"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}({string.Join(", ", callback.Method.GetParameters().Select(p => p.ParameterType + " " + p.Name))})\"");

        events[@event] += callback;
    }

    public static void OnRequest(string @event, Action<CommunicationMessage> callback)
    {
        InternalOnRequest(@event, callback, m => []);
    }

    public static void OnRequest<T>(string @event, Action<CommunicationMessage, T> callback)
    {
        InternalOnRequest(@event, callback, m =>
        [
            Serializer.Deserialize<T>(m.Payloads[0])
        ]);
    }

    public static void OnRequest<T1, T2>(string @event, Action<CommunicationMessage, T1, T2> callback)
    {
        InternalOnRequest(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1])
        ]);
    }

    public static void OnRequest<T1, T2, T3>(string @event, Action<CommunicationMessage, T1, T2, T3> callback)
    {
        InternalOnRequest(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2])
        ]);
    }

    public static void OnRequest<T1, T2, T3, T4>(string @event, Action<CommunicationMessage, T1, T2, T3, T4> callback)
    {
        InternalOnRequest(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2]),
            Serializer.Deserialize<T4>(m.Payloads[3])
        ]);
    }

    public static void OnRequest<T1, T2, T3, T4, T5>(string @event,
        Action<CommunicationMessage, T1, T2, T3, T4, T5> callback)
    {
        InternalOnRequest(@event, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2]),
            Serializer.Deserialize<T4>(m.Payloads[3]),
            Serializer.Deserialize<T5>(m.Payloads[4])
        ]);
    }

    public static async Task Request(string @event, params object[] payloads)
    {
        await InternalRequest(@event, payloads);
    }

    public static async Task<T> Request<T>(string @event, params object[] payloads)
    {
        var results = await InternalRequest(@event, payloads);

        return Serializer.Deserialize<T>(results.Payloads[0]);
    }

    public static async Task<Tuple<T1, T2>> Request<T1, T2>(string @event, params object[] payloads)
    {
        var results = await InternalRequest(@event, payloads);

        return Tuple.Create(Serializer.Deserialize<T1>(results.Payloads[0]),
            Serializer.Deserialize<T2>(results.Payloads[1]));
    }

    public static async Task<Tuple<T1, T2, T3>> Request<T1, T2, T3>(string @event, params object[] payloads)
    {
        var results = await InternalRequest(@event, payloads);

        return Tuple.Create(Serializer.Deserialize<T1>(results.Payloads[0]),
            Serializer.Deserialize<T2>(results.Payloads[1]), Serializer.Deserialize<T3>(results.Payloads[2]));
    }

    public static async Task<Tuple<T1, T2, T3, T4>> Request<T1, T2, T3, T4>(string @event, params object[] payloads)
    {
        var results = await InternalRequest(@event, payloads);

        return Tuple.Create(Serializer.Deserialize<T1>(results.Payloads[0]),
            Serializer.Deserialize<T2>(results.Payloads[1]), Serializer.Deserialize<T3>(results.Payloads[2]),
            Serializer.Deserialize<T4>(results.Payloads[3]));
    }

    public static async Task<Tuple<T1, T2, T3, T4, T5>> Request<T1, T2, T3, T4, T5>(string @event,
        params object[] payloads)
    {
        var results = await InternalRequest(@event, payloads);

        return Tuple.Create(Serializer.Deserialize<T1>(results.Payloads[0]),
            Serializer.Deserialize<T2>(results.Payloads[1]), Serializer.Deserialize<T3>(results.Payloads[2]),
            Serializer.Deserialize<T4>(results.Payloads[3]), Serializer.Deserialize<T5>(results.Payloads[4]));
    }

    private static void Emit(OutboundMessage message)
    {
        Logger.Verbose(message.Payloads.Count > 0
            ? $"Emit: \"{message.Event}\" with {message.Payloads.Count} payload{(message.Payloads.Count > 1 ? "s" : string.Empty)}:{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", message.Payloads)}"
            : $"Emit: \"{message.Event}\" with no payloads");

        BaseScript.TriggerServerEvent(message.Event, message.Pack());
    }

    private static void InternalOn(string @event, Delegate callback, Func<InboundMessage, IEnumerable<object>> func)
    {
        if (events == null) throw new InvalidOperationException("Events handler is not initialized.");

        var key = Tuple.Create(@event, callback);

        if (subscriptions.ContainsKey(key))
        {
            Logger.Warning(
                $"Ignoring duplicate registration of callback {callback.PrintCallback()} for event {@event}. Callback is already registered");

            return;
        }

        Logger.Verbose(
            $"On: \"{@event}\" attached to \"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}({string.Join(", ", callback.Method.GetParameters().Select(p => p.ParameterType + " " + p.Name))})\"");

        var handler = (byte[] data) =>
        {
            try
            {
                var message = InboundMessage.From(data);

                Logger.Verbose(message.Payloads.Count > 0
                    ? $"On Received: \"{message.Event}\" with {message.Payloads.Count} payload{(message.Payloads.Count > 1 ? "s" : string.Empty)}:{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", message.Payloads)}"
                    : $"On Received: \"{message.Event}\" with no payloads");

                var args = new List<object>
                {
                    new CommunicationMessage(@event)
                };

                args.AddRange(func(message));

                callback.DynamicInvoke(args.ToArray());
            }
            catch (TargetInvocationException exception)
            {
                // DynamicInvoke always wraps whatever the handler threw; the outer frame carries no useful
                // information, so log the inner exception instead.
                Logger.Error(exception.InnerException ?? exception,
                    $"On: unhandled exception in handler {callback.PrintCallback()} for event \"{@event}\"");
            }
            catch (Exception exception)
            {
                // Covers payload deserialization (InboundMessage.From / func) and argument building.
                Logger.Error(exception,
                    $"On: failed to dispatch event \"{@event}\" to {callback.PrintCallback()}");
            }
        };
        events[@event] += handler;

        subscriptions.Add(key, handler);
    }

    private static void InternalOnRequest(string @event, Delegate callback,
        Func<InboundMessage, IEnumerable<object>> func)
    {
        if (events == null) throw new InvalidOperationException("Events handler is not initialized.");

        var key = Tuple.Create(@event, callback);

        if (subscriptions.ContainsKey(key))
        {
            Logger.Warning(
                $"Ignoring duplicate registration of callback {callback.PrintCallback()} for event {@event}. Callback is already registered");

            return;
        }

        Logger.Verbose(
            $"OnRequest: \"{@event}\" attached to \"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}({string.Join(", ", callback.Method.GetParameters().Select(p => p.ParameterType + " " + p.Name))})\"");

        var handler = (byte[] data) =>
        {
            try
            {
                var message = InboundMessage.From(data);

                Logger.Verbose(message.Payloads.Count > 0
                    ? $"OnRequest Received: \"{message.Event}\" with {message.Payloads.Count} payload{(message.Payloads.Count > 1 ? "s" : string.Empty)}:{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", message.Payloads)}"
                    : $"OnRequest Received: \"{message.Event}\" with no payloads");

                var args = new List<object>
                {
                    new CommunicationMessage(@event, message.Id, true)
                };

                args.AddRange(func(message));

                callback.DynamicInvoke(args.ToArray());
            }
            catch (TargetInvocationException exception)
            {
                // DynamicInvoke always wraps whatever the handler threw; the outer frame carries no useful
                // information, so log the inner exception instead.
                Logger.Error(exception.InnerException ?? exception,
                    $"OnRequest: unhandled exception in handler {callback.PrintCallback()} for event \"{@event}\"");
            }
            catch (Exception exception)
            {
                // Covers payload deserialization (InboundMessage.From / func) and argument building.
                Logger.Error(exception,
                    $"OnRequest: failed to dispatch event \"{@event}\" to {callback.PrintCallback()}");
            }
        };

        events[@event] += handler;

        subscriptions.Add(key, handler);
    }

    private static async Task<InboundMessage> InternalRequest(string @event, params object[] payloads)
    {
        if (events == null) throw new InvalidOperationException("Events handler is not initialized.");

        var tcs = new TaskCompletionSource<InboundMessage>();

        var callback = new Action<byte[]>(data =>
        {
            try
            {
                var message = InboundMessage.From(data);

                Logger.Verbose(message.Payloads.Count > 0
                    ? $"Request Received: \"{message.Event}\" with {message.Payloads.Count} payload{(message.Payloads.Count > 1 ? "s" : string.Empty)}:{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", message.Payloads)}"
                    : $"Request Received: \"{message.Event}\" with no payloads");

                tcs.SetResult(message);
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }
        });

        var msg = new OutboundMessage
        {
            Id = Guid.NewGuid(),
            Source = API.GetPlayerServerId(API.PlayerId()),
            Event = @event,
            Payloads = payloads.Select(Serializer.Serialize).ToList()
        };

        var key = $"{msg.Id}:{@event}";

        try
        {
            events[key] += callback;

            Emit(msg);

            return await tcs.Task;
        }
        finally
        {
            events[key] -= callback;
        }
    }
}