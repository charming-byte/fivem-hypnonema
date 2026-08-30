using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hypnonema.Shared.Diagnostics;

namespace Hypnonema.Client.ObjectPools;

public abstract class ObjectPool<T, TOptions>(ILogger logger) where T : class where TOptions : class
{
    protected List<T> Locked = [];

    protected ILogger Logger = logger;

    protected List<T> Unlocked = [];

    public void CheckIn(T instance)
    {
        // Checking the same instance in twice (e.g. because its owner got disposed twice) would put it into the
        // unlocked pool multiple times, allowing two consumers to check out the very same instance afterwards.
        if (Unlocked.Contains(instance))
        {
            Logger.Warning(
                $"Instance of type {typeof(T)} is already in the unlocked pool. Ignoring duplicate check-in");

            return;
        }

        if (!Locked.Remove(instance))
        {
            Logger.Warning(
                $"Instance of type {typeof(T)} was not checked out from this pool. Ignoring check-in");

            return;
        }

        Logger.Verbose($"Returning instance of type {typeof(T)} back to the pool");

        Unlocked.Add(instance);
    }

    public async Task<T> Checkout(TOptions options)
    {
        var element = TryGetFromUnlockedPool();

        if (element != null)
        {
            Logger.Debug($"{typeof(T)} successfully retrieved from unlocked pool");
            return element;
        }

        // Create a new instance if no valid object is found
        return await CreateNewInstanceAsync(options);
    }

    public abstract void Expire(T o);

    public abstract bool Validate(T instance);

    protected abstract Task<T?> Create(TOptions options);

    private async Task<T> CreateNewInstanceAsync(TOptions options)
    {
        var newT = await Create(options);

        if (newT == null) throw new ArgumentNullException(nameof(newT), "Failed to create a new instance");

        Logger.Debug($"Created new instance of {typeof(T)}");

        Locked.Add(newT);

        return newT;
    }

    private T? TryGetFromUnlockedPool()
    {
        if (Unlocked.Count == 0)
        {
            Logger.Debug("Unlocked pool is empty");
            return null;
        }

        foreach (var element in new List<T>(Unlocked))
        {
            if (Validate(element))
            {
                Unlocked.Remove(element);
                Locked.Add(element);
                return element;
            }

            Unlocked.Remove(element);
            Expire(element);
        }

        return null;
    }
}