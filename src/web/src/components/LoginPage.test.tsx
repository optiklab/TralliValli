/**
 * LoginPage Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import * as services from '@services';
import { LoginPage } from './LoginPage';

// Mock the API service
vi.mock('@services', () => ({
  api: {
    requestMagicLink: vi.fn(),
  },
}));

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('renders the login form', () => {
    render(<LoginPage />);

    expect(screen.getByText('Sign in to your account')).toBeInTheDocument();
    expect(screen.getByText("We'll send you a magic link to sign in")).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Email address')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /send magic link/i })).toBeInTheDocument();
  });

  it('updates email input when user types', () => {
    render(<LoginPage />);

    const emailInput = screen.getByPlaceholderText('Email address') as HTMLInputElement;
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

    expect(emailInput.value).toBe('test@example.com');
  });

  it('shows error when submitting empty email', async () => {
    const mockOnError = vi.fn();
    render(<LoginPage onError={mockOnError} />);

    const form = screen.getByRole('button', { name: /send magic link/i }).closest('form');

    // Trigger form submit event directly (bypasses HTML5 validation)
    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(mockOnError).toHaveBeenCalledWith('Please enter your email address');
    });

    expect(services.api.requestMagicLink).not.toHaveBeenCalled();
  });

  it('shows error when submitting invalid email', async () => {
    const mockOnError = vi.fn();
    render(<LoginPage onError={mockOnError} />);

    const emailInput = screen.getByPlaceholderText('Email address');
    const form = screen.getByRole('button', { name: /send magic link/i }).closest('form');

    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });

    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(mockOnError).toHaveBeenCalledWith('Please enter a valid email address');
    });

    expect(services.api.requestMagicLink).not.toHaveBeenCalled();
  });

  it('calls API and stores email on successful submission', async () => {
    const mockOnMagicLinkSent = vi.fn();
    vi.mocked(services.api.requestMagicLink).mockResolvedValue({ message: 'Magic link sent' });

    render(<LoginPage onMagicLinkSent={mockOnMagicLinkSent} />);

    const emailInput = screen.getByPlaceholderText('Email address');
    const submitButton = screen.getByRole('button', { name: /send magic link/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(services.api.requestMagicLink).toHaveBeenCalledWith({
        email: 'test@example.com',
        deviceId: expect.any(String),
      });
    });

    expect(sessionStorage.getItem('pendingEmail')).toBe('test@example.com');
    expect(mockOnMagicLinkSent).toHaveBeenCalled();
  });

  it('shows loading state while submitting', async () => {
    vi.mocked(services.api.requestMagicLink).mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve({ message: 'Sent' }), 100))
    );

    render(<LoginPage />);

    const emailInput = screen.getByPlaceholderText('Email address');
    const submitButton = screen.getByRole('button', { name: /send magic link/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Sending...')).toBeInTheDocument();
      expect(submitButton).toBeDisabled();
    });
  });

  it('shows error message on API failure', async () => {
    const mockOnError = vi.fn();
    vi.mocked(services.api.requestMagicLink).mockRejectedValue(new Error('Network error'));

    render(<LoginPage onError={mockOnError} />);

    const emailInput = screen.getByPlaceholderText('Email address');
    const submitButton = screen.getByRole('button', { name: /send magic link/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument();
    });

    expect(mockOnError).toHaveBeenCalledWith('Network error');
  });

  it('creates and stores deviceId if not present', async () => {
    vi.mocked(services.api.requestMagicLink).mockResolvedValue({ message: 'Sent' });

    render(<LoginPage />);

    const emailInput = screen.getByPlaceholderText('Email address');
    const submitButton = screen.getByRole('button', { name: /send magic link/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(services.api.requestMagicLink).toHaveBeenCalled();
    });

    const deviceId = localStorage.getItem('deviceId');
    expect(deviceId).toBeTruthy();
    expect(typeof deviceId).toBe('string');
  });

  it('reuses existing deviceId if present', async () => {
    const existingDeviceId = 'existing-device-123';
    localStorage.setItem('deviceId', existingDeviceId);

    vi.mocked(services.api.requestMagicLink).mockResolvedValue({ message: 'Sent' });

    render(<LoginPage />);

    const emailInput = screen.getByPlaceholderText('Email address');
    const submitButton = screen.getByRole('button', { name: /send magic link/i });

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(services.api.requestMagicLink).toHaveBeenCalledWith({
        email: 'test@example.com',
        deviceId: existingDeviceId,
      });
    });

    expect(localStorage.getItem('deviceId')).toBe(existingDeviceId);
  });
});
