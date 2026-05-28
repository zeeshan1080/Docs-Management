/**
 * HTML `pattern` and programmatic format check (practical; not full RFC 5322).
 * Allows subdomains, e.g. name@mail.example.co.uk.
 */
export const EMAIL_PATTERN =
  '^[a-zA-Z0-9._+\\-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,}$';

const emailRegex = new RegExp(EMAIL_PATTERN);

export function isWellFormedEmail(email: string): boolean {
  const s = email.trim();
  if (!s || s.length > 254) return false;
  return emailRegex.test(s);
}

/**
 * @param whenEmpty Message when the value is empty or whitespace-only.
 * @returns `null` if valid, otherwise a user-facing error string.
 */
export function emailValidationMessage(raw: string, whenEmpty: string): string | null {
  const s = raw.trim();
  if (!s) return whenEmpty;
  if (!isWellFormedEmail(s)) return 'Enter a valid email address.';
  return null;
}
