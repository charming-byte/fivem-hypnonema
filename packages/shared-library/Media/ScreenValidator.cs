using System.Collections.Generic;
using System.Linq;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Shared.Media;

/// <summary>
///     Value rules for a <see cref="Screen" /> before it is written to screens.yaml (plan §2.5). Shared between
///     client (pre-flight UX) and server (authoritative gate) so both sides agree on what "valid" means.
/// </summary>
public static class ScreenValidator
{
    public const int MaxNameLength = 100;

    public const float MaxCoordinate = 100_000f;

    // Generous enough for many full turns (a legitimate use), tight enough that a stray garbage float still gets
    // caught rather than silently accepted as "some large rotation".
    public const float MaxRotationDegrees = 3_600f;

    public const float MaxScale = 100f;

    public const float MaxRenderDistance = 10_000f;

    public static IReadOnlyList<string> Validate(Screen screen)
    {
        var errors = new List<string>();

        ValidateName(screen.Name, errors);
        ValidateId(screen.Id, errors);
        ValidateComponents(screen.Scaleform.Transform.Position, "scaleform.position", MaxCoordinate, errors);
        ValidateComponents(screen.Scaleform.Transform.Rotation, "scaleform.rotation", MaxRotationDegrees, errors);
        ValidateComponents(screen.Scaleform.Transform.Scale, "scaleform.scale", MaxScale, errors);
        ValidateRenderDistance(screen.RenderDistance, errors);
        ValidateAudio(screen.Audio, errors);

        return errors;
    }

    private static void ValidateAudio(AudioConfiguration? audio, ICollection<string> errors)
    {
        // The audio json converter yields null for a mode string it doesn't recognize (see AudioConfigurationConverter).
        if (audio == null)
        {
            errors.Add("audio.mode must be a known audio mode.");
            return;
        }

        if (audio is not DefaultAudioConfiguration { Attenuation: var attenuation }) return;

        foreach (var error in DistanceAttenuation.Validate(attenuation.MinDistance, attenuation.MaxDistance,
                     attenuation.OccludedVolume))
            errors.Add($"audio.{error}");
    }

    private static void ValidateName(string name, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("name must not be empty.");
            return;
        }

        if (name.Length > MaxNameLength) errors.Add($"name must not exceed {MaxNameLength} characters.");

        // Rejects control characters (including newlines and tabs) so a screen name can't smuggle formatting into
        // the YAML file the server writes and later re-parses on startup.
        if (name.Any(char.IsControl))
            errors.Add("name must not contain control characters or line breaks.");
    }

    private static void ValidateId(string? id, ICollection<string> errors)
    {
        if (id == null) return;

        var isValidSlug = id.Length > 0 && id[0] != '-' && id[id.Length - 1] != '-' && !id.Contains("--") &&
                          id.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

        if (!isValidSlug)
            errors.Add("id must be a lowercase slug (letters, digits, single hyphens between segments).");
    }

    private static void ValidateComponents(Vector3 value, string field, float max, ICollection<string> errors)
    {
        ValidateComponent(value.X, $"{field}.x", max, errors);
        ValidateComponent(value.Y, $"{field}.y", max, errors);
        ValidateComponent(value.Z, $"{field}.z", max, errors);
    }

    private static void ValidateComponent(float value, string field, float max, ICollection<string> errors)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            errors.Add($"{field} must be a finite number.");
            return;
        }

        if (value < -max || value > max) errors.Add($"{field} must be between {-max} and {max}.");
    }

    private static void ValidateRenderDistance(float value, ICollection<string> errors)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            errors.Add("renderDistance must be a finite number.");
            return;
        }

        if (value <= 0 || value > MaxRenderDistance)
            errors.Add($"renderDistance must be between 0 (exclusive) and {MaxRenderDistance}.");
    }
}
