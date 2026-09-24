using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Extensions;

namespace Hypnonema.Shared;

/// <seealso cref="ITickManager" />
public sealed class TickManager : ITickManager
{
    private readonly ILogger logger;
    private readonly Action<Func<Task>> attachCallback;
    private readonly Action<Func<Task>> detachCallback;
    private readonly Dictionary<Delegate, Func<Task>> registeredTasks = new();

    public TickManager(ILogger logger, Action<Func<Task>> attachCallback, Action<Func<Task>> detachCallback)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.attachCallback = attachCallback ?? throw new ArgumentNullException(nameof(attachCallback));
        this.detachCallback = detachCallback ?? throw new ArgumentNullException(nameof(detachCallback));
    }

    public void Add(Func<Task>? task)
    {
        if (task == null)
        {
            logger.Warning("Attempted to add a null task to the Tick registry.");
            return;
        }

        logger.Information(
            $"Registering new Tick task. Callback: {task.PrintCallback()}. Source: {task.Method.DeclaringType?.FullName ?? "<UnknownType>"}.{task.Method.Name}");

        var wrapped = ExceptionWrapper(task);

        attachCallback(wrapped);
        registeredTasks.Add(task, wrapped);

        logger.Debug($"Tick successfully registered. Total registered ticks: {registeredTasks.Count}.");
    }


    public Task<bool> RunAsync(Func<Task> task)
    {
        var tcs = new TaskCompletionSource<bool>();
        attachCallback(Wrapped);

        return tcs.Task;

        async Task Wrapped()
        {
            try
            {
                await task();
                tcs.SetResult(true);
            }
            catch (Exception exception)
            {
                logger.Error(exception, $"An error occurred while processing {task.PrintCallback()}");
                tcs.SetException(exception);
            }
            finally
            {
                detachCallback(Wrapped);
            }
        }
    }

    public void Remove(Func<Task>? task)
    {
        if (task == null)
        {
            logger.Warning("Attempted to remove a null task from the Tick registry.");
            return;
        }

        var callbackInfo = task.PrintCallback();

        logger.Information($"Attempting to remove Tick task. Callback: {callbackInfo}.");

        if (!registeredTasks.TryGetValue(task, out var registeredTask) || registeredTask == null)
        {
            logger.Warning($"No registered Tick found for callback {callbackInfo}. Nothing removed.");
            return;
        }

        detachCallback(registeredTask);
        registeredTasks.Remove(task);

        logger.Debug($"Tick successfully removed. Remaining registered ticks: {registeredTasks.Count}.");
    }

    private Func<Task> ExceptionWrapper(Func<Task> task)
    {
        return WrappedTask;

        async Task WrappedTask()
        {
            try
            {
                await task();
            }
            catch (Exception exception)
            {
                detachCallback(WrappedTask);

                logger.Error(exception, $"An error occurred while processing tick for {task.PrintCallback()}");
            }
        }
    }
}
