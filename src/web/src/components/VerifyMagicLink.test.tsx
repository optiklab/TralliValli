/**
 * VerifyMagicLink Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { VerifyMagicLink } from './VerifyMagicLink';
import * as services from '@services';
import * as authStore from '@stores/useAuthStore';

// Mock the API service
vi.mock('@services', () => ({
  api: {
    verifyMagicLink: vi.fn(),
  },
}));

// Mock the auth store
vi.mock('@stores/useAuthStore', () => ({
  useAuthStore: vi.fn(),
}));

describe('VerifyMagicLink', () => {
  const mockLogin = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    vi.mocked(authStore.useAuthStore).mockReturnValue(mockLogin);
  });

  it('shows loading state initially', () => {
    vi.mocked(services.api.verifyMagicLink).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<VerifyMagicLink token="test-token" />);

    expect(screen.getByText('Verifying your link')).toBeInTheDocument();
    expect(screen.getByText('Please wait while we sign you in...')).toBeInTheDocument();
  });

  it('shows error when token is not provided', async () => {
    const mockOnError = vi.fn();

    render(<VerifyMagicLink token="" onError={mockOnError} />);

    await waitFor(() => {
      expect(screen.getByText('Verification failed')).toBeInTheDocument();
      expect(screen.getByText('No verification token provided')).toBeInTheDocument();
    });

    expect(mockOnError).toHaveBeenCalledWith('No verification token provided');
  });

  it('successfully verifies token and logs in user', async () => {
    const mockOnSuccess = vi.fn();
    const mockTokenResponse = {
      accessToken: 'access-token-123',
      refreshToken: 'refresh-token-123',
      expiresAt: '2026-12-31T23:59:59Z',
      refreshExpiresAt: '2027-12-31T23:59:59Z',
    };

    sessionStorage.setItem('pendingEmail', 'test@example.com');

    vi.mocked(services.api.verifyMagicLink).mockResolvedValue(mockTokenResponse);

    render(<VerifyMagicLink token="valid-token" onSuccess={mockOnSuccess} />);

    await waitFor(() => {
      expect(screen.getByText('Successfully signed in!')).toBeInTheDocument();
    });

    expect(services.api.verifyMagicLink).toHaveBeenCalledWith({ token: 'valid-token' });
    expect(mockLogin).toHaveBeenCalledWith(
      expect.objectContaining({
        email: 'test@example.com',
      }),
      mockTokenResponse
    );
    expect(mockOnSuccess).toHaveBeenCalled();
    expect(sessionStorage.getItem('pendingEmail')).toBeNull();
  });

  it('shows error on verification failure', async () => {
    const mockOnError = vi.fn();

    vi.mocked(services.api.verifyMagicLink).mockRejectedValue(
      new Error('Invalid token')
    );

    render(<VerifyMagicLink token="invalid-token" onError={mockOnError} />);

    await waitFor(() => {
      expect(screen.getByText('Verification failed')).toBeInTheDocument();
      expect(screen.getByText('Invalid token')).toBeInTheDocument();
    });

    expect(mockOnError).toHaveBeenCalledWith('Invalid token');
    expect(mockLogin).not.toHaveBeenCalled();
  });

  it('uses fallback email if not in session storage', async () => {
    const mockTokenResponse = {
      accessToken: 'access-token-123',
      refreshToken: 'refresh-token-123',
      expiresAt: '2026-12-31T23:59:59Z',
      refreshExpiresAt: '2027-12-31T23:59:59Z',
    };

    vi.mocked(services.api.verifyMagicLink).mockResolvedValue(mockTokenResponse);

    render(<VerifyMagicLink token="valid-token" />);

    await waitFor(() => {
      expect(screen.getByText('Successfully signed in!')).toBeInTheDocument();
    });

    expect(mockLogin).toHaveBeenCalledWith(
      expect.objectContaining({
        email: 'user@example.com',
      }),
      mockTokenResponse
    );
  });

  it('displays loading spinner during verification', () => {
    vi.mocked(services.api.verifyMagicLink).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    const { container } = render(<VerifyMagicLink token="test-token" />);

    // Check for spinner SVG
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('displays success checkmark after successful verification', async () => {
    vi.mocked(services.api.verifyMagicLink).mockResolvedValue({
      accessToken: 'token',
      refreshToken: 'refresh',
      expiresAt: '2026-12-31T23:59:59Z',
      refreshExpiresAt: '2027-12-31T23:59:59Z',
    });

    const { container } = render(<VerifyMagicLink token="valid-token" />);

    await waitFor(() => {
      expect(screen.getByText('Successfully signed in!')).toBeInTheDocument();
    });

    // Check for success checkmark icon
    const checkmark = container.querySelector('svg path[d*="M5 13l4 4L19 7"]');
    expect(checkmark).toBeInTheDocument();
  });

  it('displays error icon on verification failure', async () => {
    vi.mocked(services.api.verifyMagicLink).mockRejectedValue(
      new Error('Token expired')
    );

    const { container } = render(<VerifyMagicLink token="expired-token" />);

    await waitFor(() => {
      expect(screen.getByText('Verification failed')).toBeInTheDocument();
    });

    // Check for error X icon
    const errorIcon = container.querySelector('svg path[d*="M6 18L18 6M6 6l12 12"]');
    expect(errorIcon).toBeInTheDocument();
  });

  it('shows helpful message about expired links on error', async () => {
    vi.mocked(services.api.verifyMagicLink).mockRejectedValue(
      new Error('Token expired')
    );

    render(<VerifyMagicLink token="expired-token" />);

    await waitFor(() => {
      expect(
        screen.getByText(/The magic link may have expired or is invalid/)
      ).toBeInTheDocument();
    });
  });
});
