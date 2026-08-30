namespace Hypnonema.Client.Graphics;

public interface IClearableDrawable : IDrawable
{
    /// <summary>
    ///     Draws a single opaque black frame onto the surface. Must be called from within a tick; a single call is not
    ///     enough, because the render thread does not necessarily rasterize it before the surface is torn down.
    /// </summary>
    void DrawBlank();
}