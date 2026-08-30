using System;

namespace Hypnonema.Client.Graphics;

public interface IDrawable : IDisposable
{
    void Draw();
}