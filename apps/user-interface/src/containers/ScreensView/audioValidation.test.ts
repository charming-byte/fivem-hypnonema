import { describe, expect, it } from "vitest";
import {
  renderDistanceWarning,
  validateScreenAudio,
} from "./audioValidation.ts";

const valid = { minDistance: 5, maxDistance: 40, occludedVolume: 0.4 };

describe("validateScreenAudio", () => {
  it("accepts a well-formed attenuation", () => {
    expect(validateScreenAudio(valid)).toEqual([]);
  });

  it("rejects a negative min distance", () => {
    expect(validateScreenAudio({ ...valid, minDistance: -1 })).toEqual([
      "Min distance must not be negative.",
    ]);
  });

  it.each([40, 10])(
    "rejects a max distance not greater than min (%s)",
    (maxDistance) => {
      expect(
        validateScreenAudio({ ...valid, minDistance: 40, maxDistance }),
      ).toEqual(["Max distance must be greater than min distance."]);
    },
  );

  it.each([-0.1, 1.1])(
    "rejects an occluded volume outside 0..1 (%s)",
    (occludedVolume) => {
      expect(validateScreenAudio({ ...valid, occludedVolume })).toEqual([
        "Occluded volume must be between 0 and 1.",
      ]);
    },
  );

  it("rejects non-finite numbers", () => {
    expect(
      validateScreenAudio({
        minDistance: NaN,
        maxDistance: NaN,
        occludedVolume: NaN,
      }),
    ).toHaveLength(3);
  });

  it("flags each violation independently", () => {
    expect(
      validateScreenAudio({
        minDistance: -1,
        maxDistance: -5,
        occludedVolume: 2,
      }),
    ).toHaveLength(3);
  });
});

describe("renderDistanceWarning", () => {
  it("warns when audio outreaches the render distance", () => {
    expect(renderDistanceWarning(50, 40)).toMatch(/50m/);
  });

  it("is silent at the boundary", () => {
    expect(renderDistanceWarning(40, 40)).toBeUndefined();
  });

  it("is silent when audio is within the render distance", () => {
    expect(renderDistanceWarning(30, 40)).toBeUndefined();
  });
});
