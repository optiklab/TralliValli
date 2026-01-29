/**
 * MagicLinkSent Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MagicLinkSent } from './MagicLinkSent';

describe('MagicLinkSent', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('renders the confirmation message', () => {
    render(<MagicLinkSent email="test@example.com" />);

    expect(screen.getByText('Check your email')).toBeInTheDocument();
    expect(screen.getByText(/We've sent a magic link to/)).toBeInTheDocument();
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
  });

  it('displays email from props', () => {
    render(<MagicLinkSent email="user@test.com" />);

    expect(screen.getByText('user@test.com')).toBeInTheDocument();
  });

  it('retrieves email from sessionStorage when not provided', () => {
    sessionStorage.setItem('pendingEmail', 'stored@example.com');

    render(<MagicLinkSent />);

    expect(screen.getByText('stored@example.com')).toBeInTheDocument();
  });

  it('displays fallback email when neither props nor sessionStorage provide email', () => {
    render(<MagicLinkSent />);

    expect(screen.getByText('your email')).toBeInTheDocument();
  });

  it('renders resend link when onResend is provided', () => {
    const mockOnResend = vi.fn();

    render(<MagicLinkSent email="test@example.com" onResend={mockOnResend} />);

    const resendButton = screen.getByText('resend the link');
    expect(resendButton).toBeInTheDocument();
  });

  it('calls onResend when resend link is clicked', () => {
    const mockOnResend = vi.fn();

    render(<MagicLinkSent email="test@example.com" onResend={mockOnResend} />);

    const resendButton = screen.getByText('resend the link');
    fireEvent.click(resendButton);

    expect(mockOnResend).toHaveBeenCalledTimes(1);
  });

  it('does not render resend button when onResend is not provided', () => {
    render(<MagicLinkSent email="test@example.com" />);

    expect(screen.queryByText('resend the link')).not.toBeInTheDocument();
  });

  it('displays expiration information', () => {
    render(<MagicLinkSent email="test@example.com" />);

    expect(screen.getByText(/The link will expire in 15 minutes/)).toBeInTheDocument();
  });

  it('displays instructions about spam folder', () => {
    render(<MagicLinkSent email="test@example.com" />);

    expect(
      screen.getByText(/Didn't receive an email\? Check your spam folder/)
    ).toBeInTheDocument();
  });

  it('renders email icon', () => {
    const { container } = render(<MagicLinkSent email="test@example.com" />);

    // Check for SVG email icon
    const svgElement = container.querySelector('svg');
    expect(svgElement).toBeInTheDocument();
  });
});
