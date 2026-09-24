using System;

namespace Hypnonema.Shared;

public sealed class Size2 : IEquatable<Size2>
{
    public Size2()
    {
    }

    public Size2(int width, int height)
    {
        Height = height;

        Width = width;
    }

    public int Height { get; set; }

    public int Width { get; set; }

    public static Size2 Default => new(0, 0);

    public bool Equals(Size2? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return Width == other.Width && Height == other.Height;
    }

    public override string ToString()
    {
        return $"[Width: {Width} Height: {Height}]";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Size2);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Width;
            hash = hash * 31 + Height;
            return hash;
        }
    }

    public static bool operator ==(Size2? left, Size2? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(Size2? left, Size2? right)
    {
        return !(left == right);
    }
}