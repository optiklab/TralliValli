/**
 * SettingsPage Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { useThemeStore } from '@stores';
import { SettingsPage } from './SettingsPage';

describe('SettingsPage', () => {
  beforeEach(() => {
    useThemeStore.setState({ theme: 'light' });
    localStorage.clear();
  });

  it('renders the settings page', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Settings')).toBeInTheDocument();
    expect(screen.getByText('Appearance')).toBeInTheDocument();
    expect(screen.getByText('Notifications')).toBeInTheDocument();
  });

  it('displays theme toggle', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Dark Mode')).toBeInTheDocument();
    expect(screen.getByText('Switch between light and dark theme')).toBeInTheDocument();
    expect(screen.getByRole('switch')).toBeInTheDocument();
  });

  it('displays current theme', () => {
    render(<SettingsPage />);

    expect(screen.getByText(/Current theme:/)).toBeInTheDocument();
    expect(screen.getByText('light')).toBeInTheDocument();
  });

  it('toggles theme from light to dark', () => {
    render(<SettingsPage />);

    const toggle = screen.getByRole('switch');
    expect(toggle).toHaveAttribute('aria-checked', 'false');

    fireEvent.click(toggle);

    const { theme } = useThemeStore.getState();
    expect(theme).toBe('dark');
    expect(toggle).toHaveAttribute('aria-checked', 'true');
  });

  it('toggles theme from dark to light', () => {
    useThemeStore.setState({ theme: 'dark' });

    render(<SettingsPage />);

    const toggle = screen.getByRole('switch');
    expect(toggle).toHaveAttribute('aria-checked', 'true');

    fireEvent.click(toggle);

    const { theme } = useThemeStore.getState();
    expect(theme).toBe('light');
    expect(toggle).toHaveAttribute('aria-checked', 'false');
  });

  it('calls onThemeChange callback when theme is toggled', () => {
    const mockOnThemeChange = vi.fn();

    render(<SettingsPage onThemeChange={mockOnThemeChange} />);

    const toggle = screen.getByRole('switch');
    fireEvent.click(toggle);

    expect(mockOnThemeChange).toHaveBeenCalledWith('dark');
  });

  it('updates current theme text when toggled', () => {
    const { rerender } = render(<SettingsPage />);

    expect(screen.getByText('light')).toBeInTheDocument();

    const toggle = screen.getByRole('switch');
    fireEvent.click(toggle);

    rerender(<SettingsPage />);

    expect(screen.getByText('dark')).toBeInTheDocument();
  });

  it('displays notification preferences placeholder', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Email Notifications')).toBeInTheDocument();
    expect(screen.getByText('Receive email notifications for new messages')).toBeInTheDocument();

    expect(screen.getByText('Push Notifications')).toBeInTheDocument();
    expect(screen.getByText('Receive push notifications for new messages')).toBeInTheDocument();

    expect(screen.getByText('Sound Notifications')).toBeInTheDocument();
    expect(screen.getByText('Play a sound when you receive a new message')).toBeInTheDocument();

    // All notification options should show "Coming soon"
    const comingSoonTexts = screen.getAllByText('Coming soon');
    expect(comingSoonTexts).toHaveLength(3);
  });

  it('applies correct CSS classes for light theme', () => {
    render(<SettingsPage />);

    const toggle = screen.getByRole('switch');
    expect(toggle).toHaveClass('bg-gray-200');
  });

  it('applies correct CSS classes for dark theme', () => {
    useThemeStore.setState({ theme: 'dark' });

    render(<SettingsPage />);

    const toggle = screen.getByRole('switch');
    expect(toggle).toHaveClass('bg-indigo-600');
  });

  it('has accessible labels for theme toggle', () => {
    render(<SettingsPage />);

    const toggle = screen.getByRole('switch');
    expect(toggle).toHaveAttribute('id', 'theme-toggle');

    const label = screen.getByText('Dark Mode');
    expect(label).toHaveAttribute('for', 'theme-toggle');
  });
});
