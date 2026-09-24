using System.Collections.Generic;

namespace Hypnonema.Shared.Media.Audio;

public sealed record DistanceAttenuation
{
    public DistanceAttenuation(float minDistance, float maxDistance, float occludedVolume = 0.4f)
    {
        MinDistance = minDistance;
        MaxDistance = maxDistance;
        OccludedVolume = occludedVolume;
    }

    public float MinDistance { get; }
    public float MaxDistance { get; }

    /// <summary>
    ///     The share of the volume that remains when the line of sight to the media player is fully blocked.
    /// </summary>
    public float OccludedVolume { get; }

    public static DistanceAttenuation Default => new(3f, 35f);

    /// <summary>
    ///     The value rules for an attenuation, shared by the screens.yaml load path and by ScreenValidator. The
    ///     record itself no longer enforces them so ScreenValidator can see the offending values and report them;
    ///     callers that persist an attenuation must run this first.
    /// </summary>
    public static IReadOnlyList<string> Validate(float minDistance, float maxDistance, float occludedVolume)
    {
        var errors = new List<string>();

        if (!IsFinite(minDistance)) errors.Add("minDistance must be a finite number.");
        else if (minDistance < 0f) errors.Add("minDistance must not be negative.");

        if (!IsFinite(maxDistance)) errors.Add("maxDistance must be a finite number.");
        else if (IsFinite(minDistance) && maxDistance <= minDistance)
            errors.Add("maxDistance must be greater than minDistance.");

        if (!IsFinite(occludedVolume)) errors.Add("occludedVolume must be a finite number.");
        else if (occludedVolume is < 0f or > 1f) errors.Add("occludedVolume must be between 0 and 1.");

        return errors;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <param name="distance">Distance between the listener and the media player, in meters.</param>
    /// <param name="occlusion">0 = clear line of sight, 1 = fully blocked.</param>
    public float GetVolume(float distance, float occlusion = 0f)
    {
        if (distance >= MaxDistance || MaxDistance <= MinDistance) return 0f;

        // Inverse distance law: amplitude is proportional to 1/d (-6 dB per doubling of distance). The linear
        // fade makes the curve reach exactly 0 at MaxDistance instead of being cut off at an audible level.
        var gain = distance <= MinDistance
            ? 1f
            : Clamp01(MinDistance / distance * (1f - (distance - MinDistance) / (MaxDistance - MinDistance)));

        return gain * Lerp(1f, Clamp01(OccludedVolume), Clamp01(occlusion));
    }

    private static float Lerp(float from, float to, float amount)
    {
        return from + (to - from) * amount;
    }

    private static float Clamp01(float value)
    {
        return value switch
        {
            < 0f => 0f,
            > 1f => 1f,
            _ => value
        };
    }
}
