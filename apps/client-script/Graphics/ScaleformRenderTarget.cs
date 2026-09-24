using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Dui;
using Hypnonema.Client.Media;
using Hypnonema.Client.ObjectPools;
using Hypnonema.Shared.Media;
using Vector2 = Hypnonema.Shared.Vector2;

namespace Hypnonema.Client.Graphics;

public class ScaleformRenderTarget : RenderTarget
{
    private static readonly Vector2 StagePosition = Vector2.Zero;

    private readonly TextureRenderer textureRenderer;

    private bool disposed;

    private ScaleformRenderTarget(Prop entity, string name, Texture texture, TextureRenderer textureRenderer) : base(
        entity, name, texture)
    {
        this.textureRenderer = textureRenderer ?? throw new ArgumentNullException(nameof(textureRenderer));
    }

    public new static async Task<ScaleformRenderTarget> Build(MediaPlayer mediaPlayer, Browser browser)
    {
        if (mediaPlayer.Target is not ModelTarget modelTarget)
            throw new InvalidOperationException(
                "MediaPlayer target is not model-based. 'scaleform on renderTarget' renderMode requires a model target.");

        var entity = mediaPlayer.GetEntity() ?? throw new EntityNotFoundException(
            $"No prop matching model '{modelTarget.Model.Label}' is streamed in near the local player.");

        var scaleform = await ScaleformPool.Self.Checkout(new ScaleformPoolOptions(TimeSpan.FromSeconds(5)));

        var texture = new Texture(browser.TextureDictionaryName, Browser.TextureName);

        // Deliberately ignoring mediaPlayer.ScaleformSettings.Texture here: those coordinates are measured for
        // the 3D scaleform mode, where the movie is placed in the world. On a render target the movie has to
        // cover the whole stage, and writing them back onto the (server-authoritative) settings would leak
        // this render mode's values into the 3D mode.
        var textureRenderer =
            new TextureRenderer(scaleform, texture, StagePosition, ScaleformTexture.StageDimension);

        return new ScaleformRenderTarget(entity, modelTarget.Model.RenderTarget, texture, textureRenderer);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        base.Dispose(disposing);

        if (disposing) textureRenderer.Dispose();
    }

    public override void Draw()
    {
        if (Handle == 0)
            return;

        API.SetTextRenderId(Handle);

        API.Set_2dLayer(4);

        API.SetScriptGfxDrawBehindPausemenu(true);

        API.SetScriptGfxAlign(73, 73);
        API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);

        API.DrawRect(0.5f, 0.5f, 1.0f, 1.0f, 0, 0, 0, 255);

        API.SetScaleformFitRendertarget(textureRenderer.Scaleform.Handle, true);

        textureRenderer.Render2D();

        API.SetTextRenderId(API.GetDefaultScriptRendertargetRenderId());

        API.ResetScriptGfxAlign();

        API.SetScriptGfxDrawBehindPausemenu(false);
    }
}