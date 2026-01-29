/**
 * RegisterPage Component
 *
 * Displays a registration form with invite link input, email, and display name.
 * Validates the invite token before allowing registration.
 */

import { useState, type FormEvent, useEffect } from 'react';
import { api } from '@services/index';
import { useAuthStore } from '@stores/useAuthStore';
import { isValidEmail, INVITE_VALIDATION_DEBOUNCE_MS } from '@utils/validation';

export interface RegisterPageProps {
  inviteToken?: string;
  onSuccess?: () => void;
  onError?: (error: string) => void;
}

export function RegisterPage({
  inviteToken: initialInviteToken,
  onSuccess,
  onError,
}: RegisterPageProps) {
  const [inviteToken, setInviteToken] = useState(initialInviteToken || '');
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isValidating, setIsValidating] = useState(false);
  const [inviteValid, setInviteValid] = useState<boolean | null>(null);
  const [error, setError] = useState<string | null>(null);
  const login = useAuthStore((state) => state.login);

  // Validate invite token when it changes
  useEffect(() => {
    const validateInvite = async () => {
      if (!inviteToken) {
        setInviteValid(null);
        return;
      }

      setIsValidating(true);
      setError(null);

      try {
        const response = await api.validateInvite(inviteToken);
        setInviteValid(response.isValid);
        if (!response.isValid) {
          setError(response.message || 'Invalid or expired invite link');
        }
      } catch (error) {
        setInviteValid(false);
        const errorMsg = error instanceof Error ? error.message : 'Failed to validate invite link';
        setError(errorMsg);
      } finally {
        setIsValidating(false);
      }
    };

    // Debounce validation
    const timeoutId = setTimeout(validateInvite, INVITE_VALIDATION_DEBOUNCE_MS);
    return () => clearTimeout(timeoutId);
  }, [inviteToken]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    // Validation
    if (!inviteToken) {
      const errorMsg = 'Please enter an invite link';
      setError(errorMsg);
      onError?.(errorMsg);
      return;
    }

    if (!inviteValid) {
      const errorMsg = 'Please enter a valid invite link';
      setError(errorMsg);
      onError?.(errorMsg);
      return;
    }

    if (!email) {
      const errorMsg = 'Please enter your email address';
      setError(errorMsg);
      onError?.(errorMsg);
      return;
    }

    // Basic email validation
    if (!isValidEmail(email)) {
      const errorMsg = 'Please enter a valid email address';
      setError(errorMsg);
      onError?.(errorMsg);
      return;
    }

    if (!displayName || displayName.trim().length === 0) {
      const errorMsg = 'Please enter your display name';
      setError(errorMsg);
      onError?.(errorMsg);
      return;
    }

    setIsLoading(true);

    try {
      const response = await api.register({
        inviteToken,
        email,
        displayName: displayName.trim(),
      });

      // Create user object
      const user = {
        id: 'user-' + Date.now(), // Placeholder - should come from API
        email,
        displayName: displayName.trim(),
      };

      // Login with the received tokens
      login(user, {
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        expiresAt: response.expiresAt,
        refreshExpiresAt: response.refreshExpiresAt,
      });

      onSuccess?.();
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to register. Please try again.';
      setError(errorMsg);
      onError?.(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Create your account
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Enter your invite link and details to get started
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="rounded-md shadow-sm space-y-4">
            <div>
              <label
                htmlFor="invite-token"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Invite Link
              </label>
              <input
                id="invite-token"
                name="inviteToken"
                type="text"
                required
                value={inviteToken}
                onChange={(e) => setInviteToken(e.target.value)}
                className="appearance-none rounded-md relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Enter your invite link or token"
                disabled={isLoading}
              />
              {isValidating && <p className="mt-1 text-sm text-gray-500">Validating invite...</p>}
              {inviteValid === true && (
                <p className="mt-1 text-sm text-green-600">✓ Valid invite link</p>
              )}
              {inviteValid === false && (
                <p className="mt-1 text-sm text-red-600">✗ Invalid or expired invite link</p>
              )}
            </div>

            <div>
              <label
                htmlFor="email-address"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Email address
              </label>
              <input
                id="email-address"
                name="email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="appearance-none rounded-md relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Email address"
                disabled={isLoading || !inviteValid}
              />
            </div>

            <div>
              <label
                htmlFor="display-name"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Display Name
              </label>
              <input
                id="display-name"
                name="displayName"
                type="text"
                autoComplete="name"
                required
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                className="appearance-none rounded-md relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Your name"
                disabled={isLoading || !inviteValid}
              />
            </div>
          </div>

          {error && (
            <div className="rounded-md bg-red-50 p-4">
              <div className="flex">
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-red-800">{error}</h3>
                </div>
              </div>
            </div>
          )}

          <div>
            <button
              type="submit"
              disabled={isLoading || !inviteValid}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Creating account...' : 'Create account'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
