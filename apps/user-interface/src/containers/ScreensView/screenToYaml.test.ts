import { describe, expect, it } from "vitest";
import { parse } from "yaml";
import {
  AudioMode,
  type DefaultAudioConfiguration,
  type Screen,
} from "@hypnonema/generated-types";
import { screenToYaml } from "./screenToYaml.ts";

const screen: Screen = {
  id: "bar-screen",
  name: "Bar Screen",
  renderDistance: 180,
  audio: {
    mode: AudioMode.Default,
    attenuation: { minDistance: 20, maxDistance: 150, occludedVolume: 0.4 },
  } as DefaultAudioConfiguration,
  scaleform: {
    transform: {
      position: { x: -1678.949, y: -928.3431, z: 21.129 },
      rotation: { x: 0, y: 0, z: -140 },
      scale: { x: 0.945, y: 0.55, z: -0.1 },
    },
    texture: {
      dimension: { width: 1280, height: 720 },
      position: { x: 0, y: 0 },
    },
  },
};

// The block is a single `screens.yaml` list entry; splice it under an empty list to parse it.
const parseEntry = (block: string): Record<string, unknown> => {
  const [entry] = parse(`screens:\n${block}`).screens as Record<
    string,
    unknown
  >[];
  return entry;
};

describe("screenToYaml", () => {
  it("round-trips a full screen through a YAML parse", () => {
    const entry = parseEntry(screenToYaml(screen));

    expect(entry).toEqual({
      id: "bar-screen",
      name: "Bar Screen",
      renderDistance: 180,
      audio: {
        mode: "default",
        attenuation: { minDistance: 20, maxDistance: 150, occludedVolume: 0.4 },
      },
      scaleform: screen.scaleform,
    });
  });

  it("resolves a missing id from the name", () => {
    const entry = parseEntry(screenToYaml({ ...screen, id: undefined }));

    expect(entry.id).toBe("bar-screen");
  });

  it("quotes a name that would otherwise break the YAML", () => {
    const entry = parseEntry(screenToYaml({ ...screen, name: "Screen: #1" }));

    expect(entry.name).toBe("Screen: #1");
  });

  it("pastes at the file's two-space list indent", () => {
    expect(screenToYaml(screen)).toMatch(/^ {2}- id: /);
  });
});
