using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using Hypnonema.Client.Diagnostics;

namespace Hypnonema.Client.ObjectPools;

public sealed class ScaleformPool : ObjectPool<Scaleform, ScaleformPoolOptions>
{
    public static string ScaleformNameBase = "hypnonema_texture_renderer";

    private static ScaleformPool? _instance;
    private static int scaleformLimit;

    private ScaleformPool() : base(new Logger("ScaleformPool"))
    {
    }

    public static ScaleformPool Self => _instance ??= new ScaleformPool();

    public static void Init(int scaleformLimit)
    {
        ScaleformPool.scaleformLimit = scaleformLimit;
    }

    public override void Expire(Scaleform scaleform)
    {
        Logger.Debug($"Expiring Scaleform ({scaleform.Handle})");

        scaleform.Dispose();
    }

    public string GetScaleformFileName(int index)
    {
        return $"{ScaleformNameBase}{index.ToString("N0").PadLeft(2, '0')}";
    }

    public override bool Validate(Scaleform scaleform)
    {
        return scaleform is { IsValid: true, IsLoaded: true };
    }

    protected override async Task<Scaleform?> Create(ScaleformPoolOptions options)
    {
        try
        {
            var currentIndex = Locked.Count + Unlocked.Count + 1;
            var limitReached = currentIndex >= scaleformLimit;

            if (limitReached)
                throw new InvalidOperationException(
                    $"Maximum Scaleform limit ({scaleformLimit}) reached. Close an existing Scaleform before opening a new one.");

            var scaleformName = GetScaleformFileName(currentIndex);
            var scaleform = await LoadScaleform(scaleformName, options.LoadTimeout);

            Logger.Debug($"Created Scaleform ({scaleform.Handle})");

            return scaleform;
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Creating new Scaleform failed");

            return null;
        }
    }

    private static async Task<Scaleform> LoadScaleform(string scaleformName, TimeSpan timeout)
    {
        var scaleform = new Scaleform(scaleformName);
        var startTime = DateTime.UtcNow;

        while (!scaleform.IsLoaded)
        {
            if (DateTime.UtcNow.Subtract(startTime) >= timeout)
                throw new TimeoutException(
                    $"Failed to load scaleform \"{scaleformName}\" within {timeout.TotalMilliseconds}ms.");

            await BaseScript.Delay(1);
        }

        return scaleform;
    }
}