using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using Hypnonema.Client.Dui;
using Hypnonema.Client.Media;
using Hypnonema.Shared;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;

namespace Hypnonema.Client.Graphics;

public sealed class DrawableSlot
{
    private const int BlankFrameCount = 3;

    private readonly Browser browser;
    private readonly ILogger logger;
    private readonly MediaPlayer mediaPlayer;
    private readonly ITickManager tickManager;

    private IDrawable? drawable;
    private int generation;
    private bool isDrawTickRegistered;

    public DrawableSlot(MediaPlayer mediaPlayer, Browser browser, ITickManager tickManager, ILogger logger)
    {
        this.mediaPlayer = mediaPlayer ?? throw new ArgumentNullException(nameof(mediaPlayer));
        this.browser = browser ?? throw new ArgumentNullException(nameof(browser));
        this.tickManager = tickManager ?? throw new ArgumentNullException(nameof(tickManager));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool HasDrawable => drawable != null;

    /// <summary>
    ///     Hot-apply a screen geometry edit (ticket 08): push the new transform straight onto a running
    ///     <see cref="ScaleformTarget" /> - <c>Draw()</c> reads it next frame, so playback never blinks. Any
    ///     other drawable type ignores it and picks the change up on its next rebuild.
    /// </summary>
    public void ApplyTransform(ScaleformTransform transform)
    {
        if (transform == null || drawable is not ScaleformTarget target) return;

        target.Position = transform.Position;
        target.Rotation = transform.Rotation;
        target.Scale = transform.Scale;
    }

    public void Adopt(IDrawable builtDrawable)
    {
        if (builtDrawable == null) throw new ArgumentNullException(nameof(builtDrawable));

        Release();

        Publish(builtDrawable);
    }

    public async Task RebuildAsync()
    {
        var token = Release();

        try
        {
            var built = await DrawableBuilder.BuildDrawable(mediaPlayer, browser);

            // Another rebuild or a release happened while we were building — this drawable is already obsolete.
            if (token != generation)
            {
                built.Dispose();

                logger.Debug($"Discarded a stale drawable (generation {token}, current {generation})");
                return;
            }

            Publish(built);

            logger.Debug($"Rebuilt drawable (RenderMode: {mediaPlayer.RenderMode})");
        }
        catch (Exception exception)
        {
            logger.Error(exception, $"Failed to build drawable (RenderMode: {mediaPlayer.RenderMode})");
        }
    }

    public int Release()
    {
        // Bump first: any build still in flight has to observe a stale token and discard itself.
        generation++;

        RemoveDrawTick();

        var released = drawable;
        drawable = null;

        switch (released)
        {
            case IClearableDrawable clearable:
                tickManager.RunAsync(() => ClearAsync(clearable));
                break;
            default:
                released?.Dispose();
                break;
        }

        return generation;
    }

    private async Task ClearAsync(IClearableDrawable clearable)
    {
        try
        {
            for (var frame = 0; frame < BlankFrameCount && drawable == null; frame++)
            {
                clearable.DrawBlank();

                await BaseScript.Delay(0);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Failed to blank a drawable surface before disposing it");
        }
        finally
        {
            // Dispose either way, even when a new drawable took over: the reference count in NamedRenderTargetRegistry
            // keeps the underlying render target alive for its new owner.
            clearable.Dispose();
        }
    }

    private void Publish(IDrawable builtDrawable)
    {
        drawable = builtDrawable;

        AddDrawTick();
    }

    private void AddDrawTick()
    {
        if (isDrawTickRegistered) return;

        tickManager.Add(OnDraw);
        isDrawTickRegistered = true;
    }

    private void RemoveDrawTick()
    {
        if (!isDrawTickRegistered) return;

        tickManager.Remove(OnDraw);
        isDrawTickRegistered = false;
    }

    private async Task OnDraw()
    {
        drawable?.Draw();
    }
}