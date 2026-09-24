import type { Screen } from "@hypnonema/generated-types";

export const slug = (value: string): string =>
  value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "") || "screen";

export const screenId = (screen: Screen): string =>
  screen.id ?? slug(screen.name);
