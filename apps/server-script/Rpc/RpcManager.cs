using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Hypnonema.Server.Communications;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Serialization;
using Serilog;

namespace Hypnonema.Server.Rpc;

#pragma warning disable CS8601 // Possible null reference assignment.

public sealed class RpcManager
{
    private static ILogger? logger;
    private static PlayerList? playerList;

    private static EventHandlerDictionary? eventManager;

    private static readonly Dictionary<Tuple<string, Delegate>, Action<byte[]>> subscriptions = [];

    public static void Init(ILogger logger, PlayerList playerList, EventHandlerDictionary eventManager)
    {
        RpcManager.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RpcManager.playerList = playerList ?? throw new ArgumentNullException(nameof(playerList));
        RpcManager.eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
    }

    public static void Emit(string @event, IClient? target, params object[] payloads)
    {
        Emit(new OutboundMessage
        {
            Id = Guid.NewGuid(),
            Target = target,
            Event = @event,
            Payloads = payloads.Select(Serializer.Serialize).ToList()
        });
    }

    public static void On(string @event, IClient? target, Action<CommunicationMessage> callback)
    {
        InternalOn(@event, target, callback, m => Array.Empty<object>());
    }

    public static void On<T>(string @event, IClient? target, Action<CommunicationMessage, T> callback)
    {
        InternalOn(@event, target, callback, m => [Serializer.Deserialize<T>(m.Payloads[0])]);
    }

    public static void On<T1, T2>(string @event, IClient? target, Action<CommunicationMessage, T1, T2> callback)
    {
        InternalOn(@event, target, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1])
        ]);
    }

    public static void On<T1, T2, T3>(string @event, IClient? target, Action<CommunicationMessage, T1, T2, T3> callback)
    {
        InternalOn(@event, target, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2])
        ]);
    }

    public static void On<T1, T2, T3, T4>(string @event, IClient? target,
        Action<CommunicationMessage, T1, T2, T3, T4> callback)
    {
        InternalOn(@event, target, callback, m =>
        [
            Serializer.Deserialize<T1>(m.Payloads[0]),
            Serializer.Deserialize<T2>(m.Payloads[1]),
            Serializer.Deserialize<T3>(m.Payloads[2]),
            Serializer.Deserialize<T4>(m.Payloads[3])
        ]);
    }

    public static void Off(string @event, Delegate callback)
    {
        if (eventManager == null)
            throw new InvalidOperationException("Cannot unregister callback. EventHandlerDictionary is null.");

        var key = Tuple.Create(@event, callback);

        if (!subscriptions.TryGetValue(key, out var handler))
        {
            logger!.Warning("Failed to unregister callback {callback} for \"{event}\"", callback.PrintCallback(),
                @event);

            return;
        }

        eventManager[@event] -= handler;

        subscriptions.Remove(key);
    }

    public static void OnRequest(string @event, IClient? target, Action<CommunicationMessage> callback)
    {
        InternalOnRequest(@event, target, callback, m => Array.Empty<object>());
    }

    private static void Emit(OutboundMessage message)
    {
        logger?.Verbose($"Fire: \"{PrintOutboundMessage(message)}\"");

        if (message.Target != null)
        {
            logger?.Verbose($"TriggerClientEvent: Using PlayerList with {playerList?.Count()} player(s)");

            var player = playerList?.FirstOrDefault(p => p.Handle == message.Target.Handle);
            if (player == null)
            {
                logger?.Warning($"TriggerClientEvent: Target {message.Target.Handle} is not in playerList");
                return;
            }

            var packed = message.Pack();
            logger?.Verbose($"TriggerClientEvent: Sending {packed.Length} packed bytes for \"{message.Event}\"");
            BaseScript.TriggerClientEvent(player, message.Event, packed);
        }
        else
        {
            logger?.Verbose("TriggerClientEvent: All clients");
            var packed = message.Pack();
            logger?.Verbose($"TriggerClientEvent: Sending {packed.Length} packed bytes for \"{message.Event}\"");
            BaseScript.TriggerClientEvent(message.Event, packed);
        }
    }

    private static string PrintInboundMessage(InboundMessage message)
    {
        var str = $"\"{message.Event}\" with ";

        if (message.Payloads.Count < 1) return str + "no payloads";

        return str +
               $"{"payload".Pluralize(message.Payloads.Count)}:{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", message.Payloads)}";
    }

    private static string PrintOutboundMessage(OutboundMessage message)
    {
        var str = $"\"{message.Event}\" with ";

        if (message.Target != null) str += $"to {message.Target.Handle} ";

        if (message.Payloads.Count < 1) return str + "no payloads";

        return str +
               $"{"payload".Pluralize(message.Payloads.Count)}:{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", message.Payloads)}";
    }

    private static void InternalOn(string @event, IClient? target, Delegate callback,
        Func<InboundMessage, IEnumerable<object>> func)
    {
        if (eventManager == null)
            throw new InvalidOperationException("Cannot unregister callback. EventHandlerDictionary is null.");

        var key = Tuple.Create(@event, callback);

        if (subscriptions.ContainsKey(key))
        {
            logger?.Warning("Ignoring duplicate registration of callback {callback} for \"{event}\"",
                callback.PrintCallback(), @event);

            return;
        }

        logger?.Verbose($"On: \"{@event}\" attached to \"{callback.PrintCallback()}\"");

        var handler = (byte[] data) =>
        {
            var message = InboundMessage.From(data);

            if (target != null && message.Source != target.Handle)
            {
                logger?.Verbose($"Ignoring event {@event} triggered by: {message.Source} | expected: {target.Handle}");
                return;
            }

            var client = new Client(message.Source);
            var communicationMessage = new CommunicationMessage(@event, message.Id, client);

            var args = new List<object>
            {
                communicationMessage
            };

            args.AddRange(func(message));

            callback.DynamicInvoke(args.ToArray());
        };

        eventManager[@event] += handler;

        subscriptions.Add(key, handler);
    }

    private static void InternalOnRequest(string @event, IClient? target, Delegate callback,
        Func<InboundMessage, IEnumerable<object>> func)
    {
        if (eventManager == null)
            throw new InvalidOperationException("Cannot unregister callback. EventHandlerDictionary is null.");

        var key = Tuple.Create(@event, callback);

        if (subscriptions.ContainsKey(key))
        {
            logger?.Warning("Ignoring duplicate registration of callback {callback} for \"{event}\"",
                callback.PrintCallback(), @event);

            return;
        }

        logger?.Verbose($"OnRequest: \"{@event}\" attached to \"{callback.PrintCallback()}\"");

        var handler = (byte[] data) =>
        {
            var message = InboundMessage.From(data);

            if (target != null && message.Source != target.Handle)
            {
                logger?.Verbose(
                    $"Ignoring event {@event} triggered by: {message.Source} | expected: {target.Handle}");
                return;
            }

            var client = new Client(message.Source);
            var communicationMessage = new CommunicationMessage(@event, message.Id, client);

            var args = new List<object>
            {
                communicationMessage
            };

            args.AddRange(func(message));

            logger?.Verbose($"DynamicInvoke: {callback.PrintCallback()} with {string.Join(", ", args.ToString())}");

            callback.DynamicInvoke(args.ToArray());
        };

        eventManager[@event] += handler;

        subscriptions.Add(key, handler);
    }
}