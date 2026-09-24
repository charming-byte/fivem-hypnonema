import type { ScaleformRenderConfiguration } from "@hypnonema/generated-types";

export const defaultScaleformSettings: ScaleformRenderConfiguration = {
  transform: {
    position: { x: 0, y: 0, z: 0 },
    rotation: { x: 0, y: 0, z: 0 },
    scale: { x: 0, y: 0, z: 0 },
  },
  texture: {
    dimension: { width: 1280, height: 720 },
    position: { x: 0, y: 0 },
  },
};

export type ScaleformSteps = {
  position: number;
  rotation: number;
  scale: number;
};

export const defaultScaleformSteps: ScaleformSteps = {
  position: 0.5,
  rotation: 1,
  scale: 0.1,
};
