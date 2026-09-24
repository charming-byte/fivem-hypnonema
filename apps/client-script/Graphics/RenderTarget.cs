using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client;
using Hypnonema.Client.Dui;
using Hypnonema.Client.Graphics;
using Hypnonema.Client.Media;
using Hypnonema.Shared.Media;

public class RenderTarget : IClearableDrawable
{
    private readonly string name;
    private readonly Texture texture;

    private bool disposed;

    protected RenderTarget(Prop entity, string name, Texture texture)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.texture = texture ?? throw new ArgumentNullException(nameof(texture));

        Handle = NamedRenderTargetRegistry.Acquire(name, entity.Model);
    }

    public int Handle { get; private set; }

    public virtual void Draw()
    {
        if (Handle == 0)
            return;

        API.SetTextRenderId(Handle);

        API.Set_2dLayer(4);
        API.SetScriptGfxDrawBehindPausemenu(true);

        API.DrawRect(0.5f, 0.5f, 1.0f, 1.0f, 0, 0, 0, 255);

        API.DrawSprite(
            texture.DictionaryName,
            texture.Name,
            0.5f,
            0.5f,
            1.0f,
            1.0f,
            0.0f,
            255,
            255,
            255,
            255);

        API.SetTextRenderId(API.GetDefaultScriptRendertargetRenderId());
        API.SetScriptGfxDrawBehindPausemenu(false);
    }

    public void DrawBlank()
    {
        if (Handle == 0)
            return;

        API.SetTextRenderId(Handle);

        API.Set_2dLayer(4);
        API.SetScriptGfxDrawBehindPausemenu(true);

        API.DrawRect(0.5f, 0.5f, 1.0f, 1.0f, 0, 0, 0, 255);

        API.SetTextRenderId(API.GetDefaultScriptRendertargetRenderId());
        API.SetScriptGfxDrawBehindPausemenu(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (Handle == 0)
            return;

        Handle = 0;

        NamedRenderTargetRegistry.Release(name);
    }

    public static RenderTarget Build(MediaPlayer mediaPlayer, Browser browser)
    {
        if (mediaPlayer.Target is not ModelTarget modelTarget)
            throw new InvalidOperationException(
                "MediaPlayer target is not model-based. 'renderTarget' renderMode requires a model target.");
        var entity = mediaPlayer.GetEntity() ?? throw new EntityNotFoundException(
            $"No prop matching model '{modelTarget.Model.Label}' is streamed in near the local player.");

        var texture = new Texture(browser.TextureDictionaryName, Browser.TextureName);
        return new RenderTarget(entity, modelTarget.Model.RenderTarget, texture);
    }
}