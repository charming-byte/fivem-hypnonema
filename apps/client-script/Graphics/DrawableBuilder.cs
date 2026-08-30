using System;
using System.Threading.Tasks;
using Hypnonema.Client.Dui;
using Hypnonema.Client.Media;
using Hypnonema.Shared.Media;

namespace Hypnonema.Client.Graphics;

public class DrawableBuilder
{
    public static async Task<IDrawable> BuildDrawable(MediaPlayer mediaPlayer, Browser browser)
    {
        if (mediaPlayer == null) throw new InvalidOperationException("MediaPlayer instance must not be null.");
        if (browser == null) throw new InvalidOperationException("Browser instance must not be null.");

        return mediaPlayer.RenderMode switch
        {
            RenderMode.RenderTarget => RenderTarget.Build(mediaPlayer, browser),
            RenderMode.Scaleform => await ScaleformTarget.Build(mediaPlayer, browser),
            RenderMode.ScaleformRenderTarget => await ScaleformRenderTarget.Build(mediaPlayer, browser),
            _ => throw new NotSupportedException($"Render mode '{mediaPlayer.RenderMode}' is not supported.")
        };
    }
}