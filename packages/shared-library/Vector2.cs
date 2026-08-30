using System;

namespace Hypnonema.Shared;

public sealed class Vector2 : IEquatable<Vector2>
{
    public Vector2()
    {
    }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; }

    public float Y { get; set; }

    public static Vector2 Zero => new(0, 0);

    public bool Equals(Vector2? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override string ToString()
    {
        return $"[X:{X} Y:{Y}]";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Vector2);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Vector2? left, Vector2? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(Vector2? left, Vector2? right)
    {
        return !(left == right);
    }
}