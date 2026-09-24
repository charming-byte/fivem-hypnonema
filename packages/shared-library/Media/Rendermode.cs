using System;

namespace Hypnonema.Shared.Media;

[Flags]
public enum RenderMode
{
    RenderTarget = 1,
    Scaleform,
    ScaleformRenderTarget = RenderTarget | Scaleform
}