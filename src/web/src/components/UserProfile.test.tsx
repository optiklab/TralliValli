/**
 * UserProfile Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { useAuthStore } from '@stores';
import { UserProfile } from './UserProfile';

describe('UserProfile', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      token: null,
      refreshToken: null,
      expiresAt: null,
      refreshExpiresAt: null,
      isAuthenticated: false,
    });
  });

  it('renders nothing when user is not logged in', () => {
    const { container } = render(<UserProfile />);
    expect(container.firstChild).toBeNull();
  });

  it('renders user profile when user is logged in', () => {
    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'Test User',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile />);

    expect(screen.getByText('Test User')).toBeInTheDocument();
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /logout/i })).toBeInTheDocument();
  });

  it('displays initials in avatar', () => {
    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'John Doe',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile />);

    expect(screen.getByText('JD')).toBeInTheDocument();
  });

  it('displays initials for single word names', () => {
    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'Alice',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile />);

    expect(screen.getByText('A')).toBeInTheDocument();
  });

  it('displays only first two initials for multi-word names', () => {
    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'John Paul Smith',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile />);

    expect(screen.getByText('JP')).toBeInTheDocument();
  });

  it('calls logout when logout button is clicked', () => {
    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'Test User',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile />);

    const logoutButton = screen.getByRole('button', { name: /logout/i });
    fireEvent.click(logoutButton);

    const { user, isAuthenticated } = useAuthStore.getState();
    expect(user).toBeNull();
    expect(isAuthenticated).toBe(false);
  });

  it('calls onLogout callback when logout button is clicked', () => {
    const mockOnLogout = vi.fn();

    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'Test User',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile onLogout={mockOnLogout} />);

    const logoutButton = screen.getByRole('button', { name: /logout/i });
    fireEvent.click(logoutButton);

    expect(mockOnLogout).toHaveBeenCalled();
  });

  it('truncates long display names', () => {
    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'A Very Long Display Name That Should Be Truncated',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile />);

    const displayName = screen.getByText('A Very Long Display Name That Should Be Truncated');
    expect(displayName).toHaveClass('truncate');
  });

  it('truncates long email addresses', () => {
    useAuthStore.setState({
      user: {
        id: 'user-1',
        email: 'verylongemailaddress@verylongdomainname.com',
        displayName: 'Test User',
      },
      isAuthenticated: true,
      token: 'fake-token',
      refreshToken: 'fake-refresh-token',
      expiresAt: new Date(),
      refreshExpiresAt: new Date(),
    });

    render(<UserProfile />);

    const email = screen.getByText('verylongemailaddress@verylongdomainname.com');
    expect(email).toHaveClass('truncate');
  });
});
