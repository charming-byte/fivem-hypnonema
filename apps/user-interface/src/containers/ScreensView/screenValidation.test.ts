import { describe, expect, it } from "vitest";
import { validateNewScreenName } from "./screenValidation.ts";

describe("validateNewScreenName", () => {
  it("accepts a fresh, well-formed name", () => {
    expect(validateNewScreenName("Lounge TV", ["bar-screen"])).toEqual([]);
  });

  it.each(["", "   ", "\t"])("rejects an empty / blank name (%j)", (name) => {
    expect(validateNewScreenName(name, [])).toEqual([
      "Name must not be empty.",
    ]);
  });

  it("rejects control characters and line breaks", () => {
    expect(validateNewScreenName("Lounge\nTV", [])).toContain(
      "Name must not contain control characters or line breaks.",
    );
  });

  it("rejects a name past the length cap", () => {
    expect(validateNewScreenName("x".repeat(101), [])).toContain(
      "Name must not exceed 100 characters.",
    );
  });

  it("rejects an id that collides with a known screen", () => {
    expect(validateNewScreenName("Lounge TV!", ["lounge-tv"])).toEqual([
      "A screen with id “lounge-tv” already exists.",
    ]);
  });

  it("flags each violation independently", () => {
    expect(
      validateNewScreenName("bad\tname", ["bad-name"]),
    ).toHaveLength(2);
  });
});
