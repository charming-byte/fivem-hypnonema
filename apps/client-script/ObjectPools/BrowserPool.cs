using System;
using System.Threading.Tasks;
using Hypnonema.Client.Diagnostics;
using Hypnonema.Client.Dui;

namespace Hypnonema.Client.ObjectPools;

public sealed class BrowserPool : ObjectPool<Browser, BrowserPoolOptions>
{
    private static BrowserPool? _instance;

    private BrowserPool() : base(new Logger("BrowserPool"))
    {
    }

    public static BrowserPool Self => _instance ??= new BrowserPool();

    public override void Expire(Browser browser)
    {
        Logger.Verbose($"Expiring {browser}");

        browser.Dispose();
    }

    public override bool Validate(Browser browser)
    {
        // TODO: Implement validation per sendMessage and test if response was received
        return browser.IsReady;
    }

    protected override async Task<Browser?> Create(BrowserPoolOptions options)
    {
        var id = Locked.Count + Unlocked.Count + 1;

        var browserOptions = new BrowserOptions(id.ToString(), options.LoadTimeout, options.Dimension, options.Url);

        try
        {
            return await Browser.CreateAsync(browserOptions);
        }
        catch (TimeoutException exception)
        {
            Logger.Error(exception,
                $"Browser creation timed out (ID: {id}, URL: {options.Url}, Timeout: {options.LoadTimeout.TotalMilliseconds}ms).");
            return null;
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Failed to create browser (ID: {id}, URL: {options.Url}): {exception.Message}");
            return null;
        }
    }
}