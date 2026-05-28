/** @returns `null` if `value` has non-whitespace content, else a required-field message. */
export function requiredTrimmed(value: string, displayName: string): string | null {
  if (!value.trim()) return `${displayName} is required.`;
  return null;
}
