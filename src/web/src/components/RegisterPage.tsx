/**
 * RegisterPage Component
 *
 * Displays a registration form with optional invite link input, email, and display name.
 * Validates the invite token before allowing registration if provided.
 * If system is not bootstrapped, allows admin setup. Otherwise, invite link is optional for open registration.
 */

import { useState, type FormEvent, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
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
  // Read invite token from URL query parameter if not provided via props
  const [searchParams] = useSearchParams();
  const inviteFromUrl = searchParams.get('invite');

  const [inviteToken, setInviteToken] = useState(initialInviteToken || inviteFromUrl || '');
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isValidating, setIsValidating] = useState(false);
  const [isCheckingStatus, setIsCheckingStatus] = useState(true);
  const [isBootstrapped, setIsBootstrapped] = useState(true);
  const [inviteValid, setInviteValid] = useState<boolean | null>(null);
  const [error, setError] = useState<string | null>(null);
  const login = useAuthStore((state) => state.login);

  // Check system status on mount
  useEffect(() => {
    const checkSystemStatus = async () => {
      try {
        const status = await api.getSystemStatus();
        setIsBootstrapped(status.isBootstrapped);

        // If not bootstrapped and no invite token, skip invite validation
        if (!status.isBootstrapped && !inviteFromUrl && !initialInviteToken) {
          setInviteValid(true); // Not needed for bootstrap
        }
      } catch (error) {
        console.error('Failed to check system status:', error);
        // Assume bootstrapped on error to be safe
        setIsBootstrapped(true);
      } finally {
        setIsCheckingStatus(false);
      }
    };

    checkSystemStatus();
  }, [inviteFromUrl, initialInviteToken]);

  // Validate invite token when it changes
  useEffect(() => {
    const validateInvite = async () => {
      if (!inviteToken) {
        setInviteValid(null);
        setError(null);
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

    // Validation - invite token is now optional
    if (!!inviteToken && inviteValid === false) {
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
        inviteToken: inviteToken || undefined,
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

  if (isCheckingStatus) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
          <p className="mt-2 text-sm text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {isBootstrapped ? 'Create your account' : 'Create Admin Account'}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {isBootstrapped
              ? 'Enter your details to get started. Invite link is optional.'
              : 'Set up the first administrator account'}
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="rounded-md shadow-sm space-y-4">
            {isBootstrapped && (
              <div>
                <label
                  htmlFor="invite-token"
                  className="block text-sm font-medium text-gray-700 mb-1"
                >
                  Invite Link <span className="text-gray-500 text-xs">(optional)</span>
                </label>
                <input
                  id="invite-token"
                  name="inviteToken"
                  type="text"
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
            )}

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
                disabled={isLoading || (!!inviteToken && !inviteValid)}
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
                disabled={isLoading || (!!inviteToken && !inviteValid)}
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

          {!isBootstrapped && (
            <div className="rounded-md bg-blue-50 p-4">
              <div className="flex">
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-blue-800">
                    You are creating the first administrator account. After registration, you'll be
                    able to generate invite links for other users.
                  </h3>
                </div>
              </div>
            </div>
          )}

          <div>
            <button
              type="submit"
              disabled={isLoading || (!!inviteToken && !inviteValid)}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading
                ? 'Creating account...'
                : isBootstrapped
                  ? 'Create account'
                  : 'Create Admin Account'}
            </button>
          </div>

          {isBootstrapped && (
            <div className="text-center mt-4">
              <p className="text-sm text-gray-600">
                Already have an account?{' '}
                <a href="/login" className="font-medium text-indigo-600 hover:text-indigo-500">
                  Sign in
                </a>
              </p>
            </div>
          )}
        </form>
      </div>
    </div>
  );
}
