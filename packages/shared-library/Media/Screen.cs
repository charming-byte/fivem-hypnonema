using System;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Media.Audio;
using Newtonsoft.Json;

namespace Hypnonema.Shared.Media;

public sealed class Screen : IEquatable<Screen>
{
    public Screen()
    {

    }

  
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ScaleformRenderConfiguration Scaleform { get; set; } = ScaleformRenderConfiguration.Default;

    public float RenderDistance { get; set; } = 200f;

    // Replace, not populate-in-place: Audio is polymorphic (needs AudioConfigurationConverter) and wraps an
    // immutable DistanceAttenuation record, so reusing the default instance would silently drop the incoming values.
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public AudioConfiguration Audio { get; set; } = DefaultAudioConfiguration.Default;

    /// <summary>
    ///     The stable identity of this screen: the frozen <see cref="Id" /> slug, or — when it was never set —
    ///     a slug derived from <see cref="Name" /> (<c>null</c> → <see cref="StringExtensions.ToSlug" />).
    ///     Never position-dependent, so moving a screen in the world keeps its identity. Consistent with
    ///     <see cref="Target.AreEqual" /> for <see cref="ScreenTarget" /> and with the server's frozen screen ids.
    /// </summary>
    public string ResolveId()
    {
        return string.IsNullOrWhiteSpace(Id) ? Name.ToSlug() : Id!;
    }

    public bool Equals(Screen? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return Id == other.Id && Name == other.Name && Scaleform == other.Scaleform &&
               RenderDistance.Equals(other.RenderDistance) && AudioConfiguration.AreEqual(Audio, other.Audio);
    }

    public override string ToString()
    {
        return $"[Id: {Id} Name: {Name} Scaleform: {Scaleform} RenderDistance: {RenderDistance} Audio: {Audio}]";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Screen);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Id?.GetHashCode() ?? 0);
            hash = hash * 31 + Name.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Screen? left, Screen? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Screen? left, Screen? right)
    {
        return !(left == right);
    }
}