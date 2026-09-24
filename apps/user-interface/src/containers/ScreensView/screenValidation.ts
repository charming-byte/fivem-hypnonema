import { slug } from "./screenId.ts";

// Mirrors Hypnonema.Shared.ScreenValidator.MaxNameLength — the server re-checks authoritatively.
export const MAX_SCREEN_NAME_LENGTH = 100;

// True when the string holds a control char (C0 incl. tab / newline, DEL, or a C1 char) — the
// set char.IsControl rejects server-side.
const hasControlChar = (value: string): boolean => {
  for (let i = 0; i < value.length; i++) {
    const code = value.charCodeAt(i);
    if (code < 0x20 || (code >= 0x7f && code <= 0x9f)) return true;
  }
  return false;
};

/**
 * Client-side pre-flight for a brand-new screen, run before `CreateScreen` goes to the server
 * (which re-validates authoritatively). Runs the same checks as `ScreenValidator.ValidateName`
 * — empty, too long, control characters — plus a local id-duplicate check against the screen ids
 * this client already knows. Ticket 07.
 */
export const validateNewScreenName = (
  name: string,
  existingIds: string[],
): string[] => {
  const errors: string[] = [];
  const trimmed = name.trim();

  if (trimmed.length === 0) {
    errors.push("Name must not be empty.");
    return errors;
  }

  if (trimmed.length > MAX_SCREEN_NAME_LENGTH)
    errors.push(`Name must not exceed ${MAX_SCREEN_NAME_LENGTH} characters.`);

  if (hasControlChar(name))
    errors.push("Name must not contain control characters or line breaks.");

  const id = slug(trimmed);
  if (existingIds.includes(id))
    errors.push(`A screen with id “${id}” already exists.`);

  return errors;
};
