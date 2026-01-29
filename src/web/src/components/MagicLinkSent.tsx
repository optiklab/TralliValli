/**
 * MagicLinkSent Component
 *
 * Displays a confirmation message after a magic link has been sent.
 * Shows instructions to check email.
 */

export interface MagicLinkSentProps {
  email?: string;
  onResend?: () => void;
}

export function MagicLinkSent({ email, onResend }: MagicLinkSentProps) {
  // Get email from session storage if not provided
  const displayEmail = email || sessionStorage.getItem('pendingEmail') || 'your email';

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          {/* Email Icon */}
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
                d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
              />
            </svg>
          </div>

          <h2 className="mt-6 text-3xl font-extrabold text-gray-900">Check your email</h2>
          <p className="mt-2 text-sm text-gray-600">
            We've sent a magic link to
            <span className="font-medium text-gray-900"> {displayEmail}</span>
          </p>
          <p className="mt-4 text-sm text-gray-600">
            Click the link in the email to sign in to your account. The link will expire in 15
            minutes.
          </p>
        </div>

        <div className="mt-8">
          <div className="bg-blue-50 border border-blue-200 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg
                  className="h-5 w-5 text-blue-400"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    fillRule="evenodd"
                    d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-blue-700">
                  Didn't receive an email? Check your spam folder or{' '}
                  {onResend && (
                    <button
                      onClick={onResend}
                      className="font-medium underline hover:text-blue-800"
                    >
                      resend the link
                    </button>
                  )}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
