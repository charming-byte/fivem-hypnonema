import {
  AudioMode,
  type DefaultAudioConfiguration,
  type DistanceAttenuation,
  type Screen,
} from "@hypnonema/generated-types";
import { setClipboard } from "@/utils/misc.ts";
import { DEFAULT_ATTENUATION } from "./audioValidation.ts";
import { screenId } from "./screenId.ts";

// ponytail: String() is enough for world coords / rotations / scales (magnitudes well above 1e-4,
// so never exponential); toFixed(6) + parseFloat just trims binary-float noise. A genuine
// sub-1e-6 component would round to 0 — not a real screen transform.
const num = (value: number): string =>
  Number.isInteger(value) ? String(value) : String(parseFloat(value.toFixed(6)));

// AudioMode is written as its EnumMember string in screens.yaml (see Hypnonema.Shared.AudioMode).
const AUDIO_MODE_YAML: Record<number, string> = {
  [AudioMode.Default]: "default",
};

const attenuationOf = (screen: Screen): DistanceAttenuation =>
  (screen.audio as DefaultAudioConfiguration | undefined)?.attenuation ??
  DEFAULT_ATTENUATION;

/**
 * A paste-ready `config/screens.yaml` list entry — a faithful, complete copy of the screen
 * (`id`, `name`, `renderDistance`, `audio`, `scaleform`) at the file's 2-space indent, so it can
 * be dropped into the file unchanged. `id` is the resolved id (`screenId`), which pins identity
 * regardless of later name edits. Ticket 07.
 */
export const screenToYaml = (screen: Screen): string => {
  const { position, rotation, scale } = screen.scaleform.transform;
  const { dimension } = screen.scaleform.texture;
  const texturePosition = screen.scaleform.texture.position;
  const attenuation = attenuationOf(screen);

  return (
    [
      `  - id: ${screenId(screen)}`,
      `    name: ${JSON.stringify(screen.name)}`,
      `    renderDistance: ${num(screen.renderDistance)}`,
      `    audio:`,
      `      mode: ${AUDIO_MODE_YAML[screen.audio.mode] ?? "default"}`,
      `      attenuation:`,
      `        minDistance: ${num(attenuation.minDistance)}`,
      `        maxDistance: ${num(attenuation.maxDistance)}`,
      `        occludedVolume: ${num(attenuation.occludedVolume)}`,
      `    scaleform:`,
      `      texture:`,
      `        dimension:`,
      `          width: ${num(dimension.width)}`,
      `          height: ${num(dimension.height)}`,
      `        position:`,
      `          x: ${num(texturePosition.x)}`,
      `          y: ${num(texturePosition.y)}`,
      `      transform:`,
      `        position:`,
      `          x: ${num(position.x)}`,
      `          y: ${num(position.y)}`,
      `          z: ${num(position.z)}`,
      `        rotation:`,
      `          x: ${num(rotation.x)}`,
      `          y: ${num(rotation.y)}`,
      `          z: ${num(rotation.z)}`,
      `        scale:`,
      `          x: ${num(scale.x)}`,
      `          y: ${num(scale.y)}`,
      `          z: ${num(scale.z)}`,
    ].join("\n") + "\n"
  );
};

/** Put a screen's YAML block on the clipboard and confirm it. Shared by the finish dialog and the detail view. */
export const copyScreenYaml = (
  screen: Screen,
  notify: (message: string) => void,
): void => {
  setClipboard(screenToYaml(screen));
  notify("YAML copied to the clipboard.");
};
