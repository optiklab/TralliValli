/**
 * VerifyMagicLink Component
 *
 * Handles the magic link verification callback from email.
 * Shows loading state while verifying the token and authenticating the user.
 */

import { useEffect, useState } from 'react';
import { api } from '@services';
import { useAuthStore } from '@stores/useAuthStore';

export interface VerifyMagicLinkProps {
  token: string;
  onSuccess?: () => void;
  onError?: (error: string) => void;
}

export function VerifyMagicLink({ token, onSuccess, onError }: VerifyMagicLinkProps) {
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [error, setError] = useState<string | null>(null);
  const login = useAuthStore((state) => state.login);

  useEffect(() => {
    const verifyToken = async () => {
      if (!token) {
        const errorMsg = 'No verification token provided';
        setError(errorMsg);
        setStatus('error');
        onError?.(errorMsg);
        return;
      }

      try {
        setStatus('loading');
        const response = await api.verifyMagicLink({ token });

        // Extract user info from token (in a real app, this would come from the API response)
        // For now, we'll get it from session storage or use a placeholder
        const email = sessionStorage.getItem('pendingEmail') || 'user@example.com';
        const user = {
          id: 'user-' + Date.now(), // Placeholder - should come from API
          email,
          displayName: email.split('@')[0], // Placeholder - should come from API
        };

        // Login with the received tokens
        login(user, {
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
          expiresAt: response.expiresAt,
          refreshExpiresAt: response.refreshExpiresAt,
        });

        // Clean up pending email
        sessionStorage.removeItem('pendingEmail');

        setStatus('success');
        onSuccess?.();
      } catch (err) {
        const errorMsg =
          err instanceof Error ? err.message : 'Failed to verify magic link. Please try again.';
        setError(errorMsg);
        setStatus('error');
        onError?.(errorMsg);
      }
    };

    verifyToken();
  }, [token, login, onSuccess, onError]);

  if (status === 'loading') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
        <div className="max-w-md w-full space-y-8 text-center">
          <div>
            {/* Loading Spinner */}
            <div className="mx-auto flex items-center justify-center h-12 w-12">
              <svg
                className="animate-spin h-12 w-12 text-indigo-600"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                ></circle>
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
            </div>

            <h2 className="mt-6 text-3xl font-extrabold text-gray-900">Verifying your link</h2>
            <p className="mt-2 text-sm text-gray-600">Please wait while we sign you in...</p>
          </div>
        </div>
      </div>
    );
  }

  if (status === 'success') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
        <div className="max-w-md w-full space-y-8 text-center">
          <div>
            {/* Success Checkmark */}
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100">
              <svg
                className="h-6 w-6 text-green-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                xmlns="http://www.w3.org/2000/svg"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </div>

            <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
              Successfully signed in!
            </h2>
            <p className="mt-2 text-sm text-gray-600">Redirecting you to the app...</p>
          </div>
        </div>
      </div>
    );
  }

  // Error state
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          {/* Error Icon */}
          <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100">
            <svg
              className="h-6 w-6 text-red-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </div>

          <h2 className="mt-6 text-3xl font-extrabold text-gray-900">Verification failed</h2>
          <p className="mt-2 text-sm text-gray-600">{error}</p>
        </div>

        <div className="mt-8">
          <div className="bg-red-50 border border-red-200 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg
                  className="h-5 w-5 text-red-400"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    fillRule="evenodd"
                    d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-red-700">
                  The magic link may have expired or is invalid. Please request a new one.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
