/** Mirrors ASP.NET Identity password options in DependencyInjection (API). */

export const PASSWORD_MIN_LENGTH = 8;

/** Returns a user-facing error message, or null if the password meets policy. */
export function passwordStrengthMessage(password: string): string | null {
  if (!password) return 'Password is required.';
  if (password.length < PASSWORD_MIN_LENGTH) {
    return `Password must be at least ${PASSWORD_MIN_LENGTH} characters.`;
  }
  if (!/\d/.test(password)) {
    return 'Password must contain at least one digit.';
  }
  if (!/[a-z]/.test(password)) {
    return 'Password must contain at least one lowercase letter.';
  }
  if (!/[A-Z]/.test(password)) {
    return 'Password must contain at least one uppercase letter.';
  }
  if (!/[^A-Za-z0-9]/.test(password)) {
    return 'Password must contain at least one special character (for example !@#$%).';
  }
  return null;
}
