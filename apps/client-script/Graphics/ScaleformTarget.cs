using System;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using Hypnonema.Client.Dui;
using Hypnonema.Client.Media;
using Hypnonema.Client.ObjectPools;
using Hypnonema.Shared.Media;
using Vector3 = Hypnonema.Shared.Vector3;

namespace Hypnonema.Client.Graphics;

public sealed class ScaleformTarget : IDrawable
{
    // Also used by the screen editor's scale handles - one source of truth for the quad's world size.
    internal const float WorldUnitsPerScaleUnit = 2f;

    private const float StageAspectRatio = 16f / 9f;

    private const float DefaultDepthScale = -0.1f;

    private readonly TextureRenderer textureRenderer;

    private ScaleformTarget(TextureRenderer textureRenderer, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        this.textureRenderer = textureRenderer ?? throw new ArgumentNullException(nameof(textureRenderer));
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public Vector3 Position { get; set; }

    public Vector3 Rotation { get; set; }

    public Vector3 Scale { get; set; }

    public void Dispose()
    {
        textureRenderer.Dispose();
    }

    public void Draw()
    {
        textureRenderer.Render3D(Position, Rotation, Scale);
    }

    public static async Task<ScaleformTarget> Build(MediaPlayer mediaPlayer, Browser browser)
    {
        var settings = mediaPlayer.Scaleform;

        var position = settings.Transform.Position;
        var rotation = settings.Transform.Rotation;
        var scale = settings.Transform.Scale;

        if (mediaPlayer.Target is ModelTarget modelTarget)
        {
            var entity = mediaPlayer.GetEntity() ?? throw new EntityNotFoundException(
                $"No prop matching model '{modelTarget.Model.Label}' is streamed in near the local player.");

            var modelMinimum = CitizenFX.Core.Vector3.Zero;
            var modelMaximum = CitizenFX.Core.Vector3.Zero;

            API.GetModelDimensions((uint)API.GetHashKey(modelTarget.Model.Prop), ref modelMinimum, ref modelMaximum);

            // Model targets range from a handheld radio to a clubhouse jukebox, so a fixed default fits none of them:
            // size the screen to the prop's own width and keep it at the stage's 16:9.
            var derivedScaleX = (modelMaximum.X - modelMinimum.X) / WorldUnitsPerScaleUnit;
            var derivedScaleY = derivedScaleX / StageAspectRatio;

            scale = new Vector3(scale.X == 0f ? derivedScaleX : scale.X, scale.Y == 0f ? derivedScaleY : scale.Y,
                scale.Z == 0f ? DefaultDepthScale : scale.Z);

            // Render3D places the quad by its centre, so lifting it by the prop's height alone would sink half the
            // screen into the prop.
            var quadHalfHeight = scale.Y * WorldUnitsPerScaleUnit / 2f;

            position = new Vector3(entity.Position.X + settings.Transform.Position.X,
                entity.Position.Y + settings.Transform.Position.Y,
                entity.Position.Z + modelMaximum.Z + quadHalfHeight + settings.Transform.Position.Z);

            rotation = new Vector3(settings.Transform.Rotation.X, settings.Transform.Rotation.Y,
                -entity.Heading + settings.Transform.Rotation.Z);
        }

        var texture = new Texture(browser.TextureDictionaryName, Browser.TextureName);

        var scaleform =
            await ScaleformPool.Self.Checkout(
                new ScaleformPoolOptions(TimeSpan.FromSeconds(5))); // TODO: Make timeout configurable

        var dimension = settings.Texture.Dimension;

        if (dimension.Width <= 0 || dimension.Height <= 0) dimension = ScaleformTexture.StageDimension;

        var textureRenderer = new TextureRenderer(scaleform, texture, settings.Texture.Position, dimension);

        return new ScaleformTarget(textureRenderer, position, rotation, scale);
    }
}