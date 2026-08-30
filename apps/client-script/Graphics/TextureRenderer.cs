using System;
using CitizenFX.Core;
using Hypnonema.Client.Extensions;
using Hypnonema.Client.ObjectPools;
using Hypnonema.Shared;
using Vector2 = Hypnonema.Shared.Vector2;
using Vector3 = Hypnonema.Shared.Vector3;

namespace Hypnonema.Client.Graphics;

public sealed class TextureRenderer : IDisposable
{
    /// <summary>
    ///     Factor between the movie's stage coordinates and the coordinate space <c>SET_TEXTURE</c> expects.
    /// </summary>
    /// <remarks>
    ///     The stage of <c>hypnonema_texture_renderer*.gfx</c> is 1280x720, but the exported <c>CONTENT</c> symbol is
    ///     intrinsically 51.2x28.8 units and the movie's constructor stretches it to the stage via
    ///     <c>contentMC._width = 1280</c> — a scale factor of 25. <c>SET_TEXTURE</c> writes <c>_x/_y/_width/_height</c>
    ///     of a child of <c>contentMC</c>, so its arguments live in that unscaled space: the full stage is 51.2x28.8.
    ///     Passing stage pixels directly makes the texture 25 times oversized, leaving only its top-left corner visible.
    /// </remarks>
    private const float ContentScale = 25f;

    private const int WarmupFrames = 5;

    private int warmupFramesLeft = WarmupFrames;

    public TextureRenderer(Scaleform scaleform, Texture texture, Vector2 texturePosition, Size2 size)
    {
        Scaleform = scaleform ?? throw new ArgumentNullException(nameof(scaleform));
        if (!scaleform.IsValid || !scaleform.IsLoaded) throw new ArgumentException("Scaleform is invalid");

        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
        TexturePosition = texturePosition;
        TextureSize = size;

        SetTexture(TexturePosition, TextureSize);
    }

    public Scaleform Scaleform { get; }

    public Texture Texture { get; }

    public Vector2 TexturePosition { get; private set; }

    public Size2 TextureSize { get; private set; }

    public void Dispose()
    {
        ScaleformPool.Self.CheckIn(Scaleform);
    }

    public void SetTexturePosition(Vector2 position)
    {
        TexturePosition = position;
        SetTexture(TexturePosition, TextureSize);
    }

    public void SetTextureSize(Size2 size)
    {
        TextureSize = size;
        SetTexture(TexturePosition, TextureSize);
    }

    public void Render2D()
    {
        Warmup();

        Scaleform.Render2D();
    }

    public void Render3D(Vector3 position, Vector3 rotation, Vector3 scale)
    {
        Warmup();

        Scaleform.Render3D(position.ToFxVector3(), rotation.ToFxVector3(), scale.ToFxVector3());
    }

    /// <summary>
    ///     Re-applies the texture for the first <see cref="WarmupFrames" /> rendered frames. See that constant.
    /// </summary>
    private void Warmup()
    {
        if (warmupFramesLeft <= 0) return;

        warmupFramesLeft--;

        SetTexture(TexturePosition, TextureSize);
    }

    private void SetTexture(Vector2 position, Size2 size)
    {
        Scaleform.CallFunction("SET_TEXTURE", Texture.DictionaryName, Texture.Name, position.X / ContentScale,
            position.Y / ContentScale, size.Width / ContentScale, size.Height / ContentScale);
    }
}