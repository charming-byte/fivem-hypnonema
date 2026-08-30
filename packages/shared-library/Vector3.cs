using System;

namespace Hypnonema.Shared;

public sealed class Vector3 : IEquatable<Vector3>
{
    public Vector3()
    {
    }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public bool Equals(Vector3? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    public override string ToString()
    {
        return $"[X:{X} Y:{Y} Z:{Z}]";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Vector3);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            hash = hash * 31 + Z.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Vector3? left, Vector3? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Vector3? left, Vector3? right)
    {
        return !(left == right);
    }
}