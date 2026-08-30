using CitizenFX.Core;

namespace Hypnonema.Client.Extensions;

public static class VectorExtensions
{
    public static Vector2 ToFxVector2(this Shared.Vector2 v)
    {
        return new Vector2(v.X, v.Y);
    }

    public static Vector3 ToFxVector3(this Shared.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }
}