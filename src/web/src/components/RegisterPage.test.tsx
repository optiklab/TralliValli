/**
 * RegisterPage Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import * as services from '@services';
import * as authStore from '@stores/useAuthStore';
import { RegisterPage } from './RegisterPage';

// Mock the API service
vi.mock('@services', () => ({
  api: {
    register: vi.fn(),
    validateInvite: vi.fn(),
  },
}));

// Mock the auth store
vi.mock('@stores/useAuthStore', () => ({
  useAuthStore: vi.fn(),
}));

describe('RegisterPage', () => {
  const mockLogin = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(authStore.useAuthStore).mockReturnValue(mockLogin);
  });

  it('renders the registration form', () => {
    render(<RegisterPage />);

    expect(screen.getByText('Create your account')).toBeInTheDocument();
    expect(screen.getByLabelText('Invite Link')).toBeInTheDocument();
    expect(screen.getByLabelText('Email address')).toBeInTheDocument();
    expect(screen.getByLabelText('Display Name')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create account/i })).toBeInTheDocument();
  });

  it('updates input fields when user types', () => {
    render(<RegisterPage />);

    const inviteInput = screen.getByLabelText('Invite Link') as HTMLInputElement;
    const emailInput = screen.getByLabelText('Email address') as HTMLInputElement;
    const displayNameInput = screen.getByLabelText('Display Name') as HTMLInputElement;

    fireEvent.change(inviteInput, { target: { value: 'invite-token-123' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(displayNameInput, { target: { value: 'Test User' } });

    expect(inviteInput.value).toBe('invite-token-123');
    expect(emailInput.value).toBe('test@example.com');
    expect(displayNameInput.value).toBe('Test User');
  });

  it('validates invite token when entered', async () => {
    vi.mocked(services.api.validateInvite).mockResolvedValue({
      isValid: true,
    });

    render(<RegisterPage />);

    const inviteInput = screen.getByLabelText('Invite Link');
    fireEvent.change(inviteInput, { target: { value: 'valid-token' } });

    await waitFor(
      () => {
        expect(screen.getByText('✓ Valid invite link')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );
  });

  it('shows error for invalid invite token', async () => {
    vi.mocked(services.api.validateInvite).mockResolvedValue({
      isValid: false,
      message: 'Invite has expired',
    });

    render(<RegisterPage />);

    const inviteInput = screen.getByLabelText('Invite Link');
    fireEvent.change(inviteInput, { target: { value: 'invalid-token' } });

    await waitFor(
      () => {
        expect(screen.getByText('✗ Invalid or expired invite link')).toBeInTheDocument();
        expect(screen.getByText('Invite has expired')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );
  });

  it('disables form fields when invite is invalid', async () => {
    vi.mocked(services.api.validateInvite).mockResolvedValue({
      isValid: false,
    });

    render(<RegisterPage />);

    const inviteInput = screen.getByLabelText('Invite Link');
    fireEvent.change(inviteInput, { target: { value: 'invalid-token' } });

    await waitFor(
      () => {
        expect(screen.getByLabelText('Email address')).toBeDisabled();
        expect(screen.getByLabelText('Display Name')).toBeDisabled();
        expect(screen.getByRole('button', { name: /create account/i })).toBeDisabled();
      },
      { timeout: 1000 }
    );
  });

  it('shows error when submitting without invite token', async () => {
    const mockOnError = vi.fn();
    render(<RegisterPage onError={mockOnError} />);

    const form = screen.getByRole('button', { name: /create account/i }).closest('form');

    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(mockOnError).toHaveBeenCalledWith('Please enter an invite link');
    });

    expect(services.api.register).not.toHaveBeenCalled();
  });

  it('shows error when submitting with invalid email', async () => {
    const mockOnError = vi.fn();
    vi.mocked(services.api.validateInvite).mockResolvedValue({ isValid: true });

    render(<RegisterPage inviteToken="valid-token" onError={mockOnError} />);

    await waitFor(
      () => {
        expect(screen.getByText('✓ Valid invite link')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );

    const emailInput = screen.getByLabelText('Email address');
    const displayNameInput = screen.getByLabelText('Display Name');
    const form = screen.getByRole('button', { name: /create account/i }).closest('form');

    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
    fireEvent.change(displayNameInput, { target: { value: 'Test User' } });

    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(mockOnError).toHaveBeenCalledWith('Please enter a valid email address');
    });

    expect(services.api.register).not.toHaveBeenCalled();
  });

  it('successfully registers user and logs in', async () => {
    const mockOnSuccess = vi.fn();
    const mockRegisterResponse = {
      accessToken: 'access-token-123',
      refreshToken: 'refresh-token-123',
      expiresAt: '2026-12-31T23:59:59Z',
      refreshExpiresAt: '2027-12-31T23:59:59Z',
    };

    vi.mocked(services.api.validateInvite).mockResolvedValue({ isValid: true });
    vi.mocked(services.api.register).mockResolvedValue(mockRegisterResponse);

    render(<RegisterPage inviteToken="valid-token" onSuccess={mockOnSuccess} />);

    await waitFor(
      () => {
        expect(screen.getByText('✓ Valid invite link')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );

    const emailInput = screen.getByLabelText('Email address');
    const displayNameInput = screen.getByLabelText('Display Name');
    const submitButton = screen.getByRole('button', { name: /create account/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(displayNameInput, { target: { value: 'Test User' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(services.api.register).toHaveBeenCalledWith({
        inviteToken: 'valid-token',
        email: 'test@example.com',
        displayName: 'Test User',
      });
    });

    expect(mockLogin).toHaveBeenCalledWith(
      expect.objectContaining({
        email: 'test@example.com',
        displayName: 'Test User',
      }),
      mockRegisterResponse
    );
    expect(mockOnSuccess).toHaveBeenCalled();
  });

  it('trims whitespace from display name', async () => {
    vi.mocked(services.api.validateInvite).mockResolvedValue({ isValid: true });
    vi.mocked(services.api.register).mockResolvedValue({
      accessToken: 'token',
      refreshToken: 'refresh',
      expiresAt: '2026-12-31T23:59:59Z',
      refreshExpiresAt: '2027-12-31T23:59:59Z',
    });

    render(<RegisterPage inviteToken="valid-token" />);

    await waitFor(
      () => {
        expect(screen.getByText('✓ Valid invite link')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );

    const emailInput = screen.getByLabelText('Email address');
    const displayNameInput = screen.getByLabelText('Display Name');
    const submitButton = screen.getByRole('button', { name: /create account/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(displayNameInput, { target: { value: '  Test User  ' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(services.api.register).toHaveBeenCalledWith({
        inviteToken: 'valid-token',
        email: 'test@example.com',
        displayName: 'Test User',
      });
    });
  });

  it('shows error on registration failure', async () => {
    const mockOnError = vi.fn();

    vi.mocked(services.api.validateInvite).mockResolvedValue({ isValid: true });
    vi.mocked(services.api.register).mockRejectedValue(new Error('Registration failed'));

    render(<RegisterPage inviteToken="valid-token" onError={mockOnError} />);

    await waitFor(
      () => {
        expect(screen.getByText('✓ Valid invite link')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );

    const emailInput = screen.getByLabelText('Email address');
    const displayNameInput = screen.getByLabelText('Display Name');
    const submitButton = screen.getByRole('button', { name: /create account/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(displayNameInput, { target: { value: 'Test User' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Registration failed')).toBeInTheDocument();
    });

    expect(mockOnError).toHaveBeenCalledWith('Registration failed');
    expect(mockLogin).not.toHaveBeenCalled();
  });

  it('shows loading state during registration', async () => {
    vi.mocked(services.api.validateInvite).mockResolvedValue({ isValid: true });
    vi.mocked(services.api.register).mockImplementation(
      () =>
        new Promise((resolve) =>
          setTimeout(
            () =>
              resolve({
                accessToken: 'token',
                refreshToken: 'refresh',
                expiresAt: '2026-12-31T23:59:59Z',
                refreshExpiresAt: '2027-12-31T23:59:59Z',
              }),
            100
          )
        )
    );

    render(<RegisterPage inviteToken="valid-token" />);

    await waitFor(
      () => {
        expect(screen.getByText('✓ Valid invite link')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );

    const emailInput = screen.getByLabelText('Email address');
    const displayNameInput = screen.getByLabelText('Display Name');
    const submitButton = screen.getByRole('button', { name: /create account/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(displayNameInput, { target: { value: 'Test User' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Creating account...')).toBeInTheDocument();
      expect(submitButton).toBeDisabled();
    });
  });

  it('uses provided inviteToken prop', async () => {
    vi.mocked(services.api.validateInvite).mockResolvedValue({ isValid: true });

    render(<RegisterPage inviteToken="preset-token" />);

    const inviteInput = screen.getByLabelText('Invite Link') as HTMLInputElement;
    expect(inviteInput.value).toBe('preset-token');

    await waitFor(
      () => {
        expect(services.api.validateInvite).toHaveBeenCalledWith('preset-token');
      },
      { timeout: 1000 }
    );
  });
});
