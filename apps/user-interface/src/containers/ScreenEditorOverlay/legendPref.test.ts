import { beforeEach, describe, expect, it } from "vitest";
import { persistLegendPref, readLegendPref } from "./legendPref.ts";

describe("legendPref", () => {
  beforeEach(() => localStorage.clear());

  it("defaults to shown on the first ever open", () => {
    expect(readLegendPref()).toBe(true);
  });

  it("remembers an explicit hide across a restart (fresh read)", () => {
    persistLegendPref(false);
    expect(readLegendPref()).toBe(false);

    persistLegendPref(true);
    expect(readLegendPref()).toBe(true);
  });
});
