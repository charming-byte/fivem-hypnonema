using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hypnonema.Shared.Communications;
using Serilog;

namespace Hypnonema.Server.Communications;

public class EventManager : IEventManager
{
    private readonly ILogger logger;
    private readonly Dictionary<string, List<Delegate>> subscriptions = new();

    public EventManager(ILogger logger)
    {
        this.logger = logger;
    }

    public void Off(string @event, Delegate action)
    {
        lock (subscriptions)
        {
            if (subscriptions.ContainsKey(@event) && subscriptions[@event].Contains(action))
                subscriptions[@event].Remove(action);
        }
    }

    public void Emit(string @event, params object[] args)
    {
        lock (subscriptions)
        {
            if (!subscriptions.ContainsKey(@event)) return;

            var message = new CommunicationMessage(@event, this);

            logger.Verbose(args.Length > 0
                ? $"Emit: \"{@event}\" with {args.Length} payload(s): {string.Join(", ", args.Select(a => a?.ToString() ?? "NULL"))}"
                : $"Emit: \"{@event}\" without payload");

            foreach (var subscription in subscriptions[@event].ToList())
            {
                var payload = new List<object> { message };
                payload.AddRange(args);

                subscription.DynamicInvoke(payload.ToArray());
            }
        }
    }

    public void On(string @event, Delegate action)
    {
        lock (subscriptions)
        {
            if (!subscriptions.ContainsKey(@event)) subscriptions.Add(@event, new List<Delegate>());

            subscriptions[@event].Add(action);

            logger.Verbose(
                $"On: \"{@event}\" attached to \"{action.Method.DeclaringType?.Name}.{action.Method.Name}({string.Join(", ", action.Method.GetParameters().Select(p => p.ParameterType + " " + p.Name))})\"");
        }
    }

    public void Dispose()
    {
        foreach (var subscription in subscriptions) subscription.Value.ForEach(d => Off(subscription.Key, d));
    }

    internal void OnRequest(string @event, Delegate action)
    {
        lock (subscriptions)
        {
            if (!subscriptions.ContainsKey(@event)) subscriptions.Add(@event, new List<Delegate>());

            subscriptions[@event].Add(action);

            logger.Verbose(
                $"On: \"{@event}\" attached to \"{action.Method.DeclaringType?.Name}.{action.Method.Name}({string.Join(", ", action.Method.GetParameters().Select(p => p.ParameterType + " " + p.Name))})\"");
        }
    }

    internal async Task<TReturn> Request<TReturn>(string @event, params object[] args)
    {
        var message = new CommunicationMessage(@event, this);
        var tcs = new TaskCompletionSource<TReturn>();

        try
        {
            On($"{message.Id}:{@event}", new Action<CommunicationMessage, TReturn>((e, data) =>
            {
                logger.Verbose(
                    $"Request Reply: \"{@event}\" with {args.Length} payload(s): {string.Join(", ", args.Select(a => a?.ToString() ?? "NULL"))}");

                tcs.SetResult(data);
            }));

            logger.Verbose(args.Length > 0
                ? $"Request Emit: \"{@event}\" with {args.Length} payload(s): {string.Join(", ", args.Select(a => a?.ToString() ?? "NULL"))}"
                : $"Request Emit: \"{@event}\" without payload");

            lock (subscriptions)
            {
                var payload = new List<object> { message };
                payload.AddRange(args);

                subscriptions.Single(s => s.Key == @event).Value.Single().DynamicInvoke(payload.ToArray());
            }

            return await tcs.Task;
        }
        finally
        {
            lock (subscriptions)
            {
                subscriptions.Remove($"{message.Id}:{@event}");
            }
        }
    }
}