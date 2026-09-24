using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Diagnostics;
using Hypnonema.Client.Extensions;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Serialization;

namespace Hypnonema.Client.Dui;

public sealed class Browser : IDisposable
{
    private const string MouseButtonLeft = "left";

    private const int MouseSettleDelayMs = 50;

    private readonly long handle;
    public readonly string Id;

    private readonly ILogger logger;

    private readonly List<EventSubscription> nuiSubscriptions;

    private bool isClicking;

    private RuntimeTexture? runtimeTexture;

    private Browser(string id, string url, Size2 dimension)
    {
        Id = id;
        logger = new Logger($"Browser:{Id}");

        var aspectRatio = Math.Round((float)dimension.Height / dimension.Width * 100, 2);

        var uriBuilder = new UriBuilder(url);
        var query =
            $"browserId={Uri.EscapeDataString(Id)}&aspectRatio={aspectRatio.ToString(CultureInfo.InvariantCulture)}&resourceName={Uri.EscapeDataString(API.GetCurrentResourceName())}";
        uriBuilder.Query = uriBuilder.Query.Length > 1 ? $"{uriBuilder.Query.TrimStart('?')}&{query}" : query;
        url = uriBuilder.Uri.ToString();

        nuiSubscriptions = NuiCallbackBinder.Bind(this, GetScopedEvent);

        handle = API.CreateDui(url, dimension.Width, dimension.Height);
    }

    public static string TextureName => "browser_texture";

    public string TextureDictionaryName => $"hypnonema_dui_{Id}";

    public bool IsReady { get; private set; }

    private bool IsAvailable => API.IsDuiAvailable(handle);

    public void Dispose()
    {
        if (handle != 0) API.DestroyDui(handle);

        NuiCallbackBinder.Unbind(nuiSubscriptions);
    }

    public override string ToString()
    {
        return $"Browser:{Id}";
    }

    private string GetScopedEvent(string @event)
    {
        return $"browser_{Id}_{@event}";
    }

    [NuiCallback(Shared.Dui.DuiCallbacks.OnPlayerEnd)]
    private void OnPlayerEnd()
    {
        PlayerEnd?.Invoke(this, EventArgs.Empty);
    }

    [NuiCallback(Shared.Dui.DuiCallbacks.OnDocumentReady)]
    private void OnDocumentReady()
    {
        IsReady = true;
    }

    [NuiCallback(Shared.Dui.DuiCallbacks.OnPlayerDuration)]
    private void OnPlayerDuration(int duration)
    {
        PlayerDuration?.Invoke(this, new DurationEventArgs(duration));
    }

    [NuiCallback(Shared.Dui.DuiCallbacks.OnYoutubeAdStatus)]
    private void OnYoutubeAdStatus(YoutubeAdStatus status)
    {
        YoutubeAdStatus?.Invoke(this, new YoutubeAdStatusEventArgs(status));
    }

    [NuiCallback(Shared.Dui.DuiCallbacks.OnRequestClick)]
    private void OnRequestClick(Shared.Dui.DuiClickRequest request)
    {
        _ = ClickAsync(request);
    }

    private async Task ClickAsync(Shared.Dui.DuiClickRequest request)
    {
        if (handle == 0 || isClicking) return;

        isClicking = true;

        API.SendDuiMouseMove(handle, request.X, request.Y);
        await BaseScript.Delay(MouseSettleDelayMs);

        API.SendDuiMouseDown(handle, MouseButtonLeft);
        await BaseScript.Delay(MouseSettleDelayMs);

        API.SendDuiMouseUp(handle, MouseButtonLeft);

        isClicking = false;
    }

    public void SetVolume(float volume)
    {
        SendMessage(Shared.Dui.DuiEvents.SetVolume, volume);
    }

    public void Reset()
    {
        SendMessage(Shared.Dui.DuiEvents.Reset);
    }

    ~Browser()
    {
        Dispose();
    }

    public event EventHandler<DurationEventArgs>? PlayerDuration;

    public event EventHandler<ErrorEventArgs>? PlayerError;

    public event EventHandler? PlayerEnd;

    public event EventHandler<YoutubeAdStatusEventArgs>? YoutubeAdStatus;


    public void SendMessage(string type, object? data = null)
    {
        data ??= new
            { };

        var message = new BrowserMessage(type, data);

        if (handle != 0)
            API.SendDuiMessage(handle, Serializer.Serialize(message, Serializer.NuiSerializerSettings));
    }

    private async Task CreateRuntimeTextureAsync(DateTime startTime, TimeSpan timeout)
    {
        timeout = timeout != TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(10);

        while (!IsAvailable)
        {
            await BaseScript.Delay(1);

            if (startTime.HasTimedOut(timeout))
                throw new TimeoutException(
                    $"Failed to create DuiBrowser RuntimeTexture within {timeout.TotalSeconds}s.");
        }

        runtimeTexture = new RuntimeTexture(handle, TextureDictionaryName, TextureName);
    }

    public static async Task<Browser> CreateAsync(BrowserOptions options)
    {
        var startTime = DateTime.UtcNow;
        var browser = new Browser(options.Id, options.Url, options.Dimension);

        while (!browser.IsReady)
        {
            await BaseScript.Delay(1);

            if (startTime.HasTimedOut(options.LoadTimeout))
            {
                browser.logger.Log(
                    $"Browser '{options.Id}' timed out after {options.LoadTimeout.TotalSeconds}s while loading.",
                    LogLevel.Error);

                throw new TimeoutException(
                    $"Browser '{options.Id}' timed out after {options.LoadTimeout.TotalSeconds}s while loading.");
            }
        }

        await browser.CreateRuntimeTextureAsync(startTime, options.LoadTimeout);

        return browser;
    }

    public async Task LoadTrackAsync(ITrack track)
    {
        try
        {
            await LoadUrlAsync(track.Url, track.ThumbnailUrl);

            SendMessage(Shared.Dui.DuiEvents.Seek, track.Position);
        }
        catch (Exception exception)
        {
            PlayerError?.Invoke(this, new ErrorEventArgs(exception.Message));

            throw;
        }
    }

    private async Task LoadUrlAsync(string url, string thumbnailUrl)
    {
        if (!url.IsValidUrl()) throw new ArgumentException($"Invalid URL \"{url}\"");

        var tcs = new TaskCompletionSource<bool>();

        NuiManager.Once(GetScopedEvent(Shared.Dui.DuiCallbacks.OnPlayerLoaded), OnLoaded);
        NuiManager.Once(GetScopedEvent(Shared.Dui.DuiCallbacks.OnPlayerError), OnError);

        void OnLoaded()
        {
            logger.Debug($"Play URL: {url}");

            tcs.TrySetResult(true);
        }

        void OnError()
        {
            var exception = new Exception($"Could not load URL: {url}");

            tcs.SetException(exception);
        }

        SendMessage(Shared.Dui.DuiEvents.Load, new { url, thumbnailUrl });

        await tcs.Task;
    }
}

public sealed class DurationEventArgs(int duration) : EventArgs
{
    public int Duration { get; } = duration;
}

public sealed class ErrorEventArgs(string reason) : EventArgs
{
    public string Reason { get; } = reason;
}

public sealed class YoutubeAdStatusEventArgs(YoutubeAdStatus status) : EventArgs
{
    public YoutubeAdStatus Status { get; } = status;
}

public sealed record BrowserMessage
{
    public BrowserMessage(string type, object? data)
    {
        Type = type;
        Data = data;
    }

    public object? Data { get; }

    public string Type { get; }
}