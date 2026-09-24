using System;

namespace Hypnonema.Shared.Media;

public sealed class ScaleformRenderConfiguration : IEquatable<ScaleformRenderConfiguration>
{
    public ScaleformTransform Transform { get; set; } = new();

    public ScaleformTexture Texture { get; set; } = new();

    public static ScaleformRenderConfiguration Default => new()
    {
        Transform = new ScaleformTransform(),
        Texture = new ScaleformTexture()
    };

    public bool Equals(ScaleformRenderConfiguration? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return Transform == other.Transform && Texture == other.Texture;
    }

    public override string ToString()
    {
        return $"[Transform: {Transform} Texture: {Texture}]";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ScaleformRenderConfiguration);
    }

    public static bool operator ==(ScaleformRenderConfiguration? left, ScaleformRenderConfiguration? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(ScaleformRenderConfiguration? left, ScaleformRenderConfiguration? right)
    {
        return !(left == right);
    }
}

public sealed class ScaleformTransform : IEquatable<ScaleformTransform>
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.Zero;

    public bool Equals(ScaleformTransform? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Position.Equals(other.Position) && Rotation.Equals(other.Rotation) && Scale.Equals(other.Scale);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ScaleformTransform other && Equals(other));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Position.GetHashCode();
            hash = hash * 31 + Rotation.GetHashCode();
            hash = hash * 31 + Scale.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(ScaleformTransform? left, ScaleformTransform? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(ScaleformTransform? left, ScaleformTransform? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"[Position: {Position} Rotation: {Rotation} Scale: {Scale}]";
    }
}

public sealed class ScaleformTexture : IEquatable<ScaleformTexture>
{
    //TODO: Move to client-side configuration, as this is only used for scaleform rendering on the client.
    public static Size2 StageDimension => new(1280, 720);

    public Size2 Dimension { get; set; } = StageDimension;

    public Vector2 Position { get; set; } = Vector2.Zero;

    public bool Equals(ScaleformTexture? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return Dimension == other.Dimension && Position == other.Position;
    }

    public override string ToString()
    {
        return $"[Dimension: {Dimension} Position: {Position}]";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ScaleformTexture);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Dimension.GetHashCode();
            hash = hash * 31 + Position.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(ScaleformTexture? left, ScaleformTexture? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(ScaleformTexture? left, ScaleformTexture? right)
    {
        return !(left == right);
    }
}