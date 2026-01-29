/**
 * Theme Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useThemeStore } from './useThemeStore';

describe('useThemeStore', () => {
  beforeEach(() => {
    // Reset the store before each test
    useThemeStore.setState({ theme: 'light' });
    localStorage.clear();
  });

  it('initializes with light theme', () => {
    const { theme } = useThemeStore.getState();
    expect(theme).toBe('light');
  });

  it('sets theme to dark', () => {
    const { setTheme } = useThemeStore.getState();
    setTheme('dark');

    const { theme } = useThemeStore.getState();
    expect(theme).toBe('dark');
  });

  it('sets theme to light', () => {
    const { setTheme } = useThemeStore.getState();
    setTheme('dark');
    setTheme('light');

    const { theme } = useThemeStore.getState();
    expect(theme).toBe('light');
  });

  it('toggles theme from light to dark', () => {
    const { toggleTheme } = useThemeStore.getState();
    toggleTheme();

    const { theme } = useThemeStore.getState();
    expect(theme).toBe('dark');
  });

  it('toggles theme from dark to light', () => {
    const { setTheme, toggleTheme } = useThemeStore.getState();
    setTheme('dark');
    toggleTheme();

    const { theme } = useThemeStore.getState();
    expect(theme).toBe('light');
  });

  it('toggles theme multiple times', () => {
    const { toggleTheme } = useThemeStore.getState();

    toggleTheme();
    expect(useThemeStore.getState().theme).toBe('dark');

    toggleTheme();
    expect(useThemeStore.getState().theme).toBe('light');

    toggleTheme();
    expect(useThemeStore.getState().theme).toBe('dark');
  });
});
