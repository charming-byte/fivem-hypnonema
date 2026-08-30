import type { DistanceAttenuation } from '@hypnonema/generated-types';

// Mirrors DistanceAttenuation.Default in packages/shared-library; only a fallback for a screen
// definition that somehow reaches the UI without attenuation values.
export const DEFAULT_ATTENUATION: DistanceAttenuation = {
  minDistance: 3,
  maxDistance: 35,
  occludedVolume: 0.4,
};

// Client pre-flight for the audio fields, mirroring the server's ScreenValidator rules
// (packages/shared-library/Media/ScreenValidator.cs + DistanceAttenuation.Validate). The server
// stays authoritative; this only spares a round-trip on the obvious range mistakes.
export const validateScreenAudio = (
  attenuation: DistanceAttenuation,
): string[] => {
  const { minDistance, maxDistance, occludedVolume } = attenuation;
  const errors: string[] = [];

  if (!Number.isFinite(minDistance)) errors.push('Min distance must be a number.');
  else if (minDistance < 0) errors.push('Min distance must not be negative.');

  if (!Number.isFinite(maxDistance)) errors.push('Max distance must be a number.');
  else if (Number.isFinite(minDistance) && maxDistance <= minDistance)
    errors.push('Max distance must be greater than min distance.');

  if (!Number.isFinite(occludedVolume))
    errors.push('Occluded volume must be a number.');
  else if (occludedVolume < 0 || occludedVolume > 1)
    errors.push('Occluded volume must be between 0 and 1.');

  return errors;
};

// Soft, non-blocking hint: audio would keep playing past the point where the screen stops
// rendering. Not an error - the user may want it that way.
export const renderDistanceWarning = (
  maxDistance: number,
  renderDistance: number,
): string | undefined =>
  Number.isFinite(maxDistance) &&
  Number.isFinite(renderDistance) &&
  maxDistance > renderDistance
    ? `Audio reaches ${maxDistance}m but the screen only renders to ${renderDistance}m.`
    : undefined;
