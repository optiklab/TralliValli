/**
 * Form validation utilities
 */

/**
 * Email validation using a standard regex pattern
 * @param email - The email address to validate
 * @returns true if email is valid, false otherwise
 */
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

/**
 * Debounce timeout for invite validation (in milliseconds)
 */
export const INVITE_VALIDATION_DEBOUNCE_MS = 500;
