/**
 * App Component Tests
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import App from './App';

describe('App', () => {
  it('renders without crashing', () => {
    render(<App />);
    // App should render RegisterPage by default
    expect(screen.getByText('Create your account')).toBeInTheDocument();
  });

  it('redirects to register page by default', () => {
    render(<App />);
    // Should show registration form
    expect(screen.getByLabelText('Invite Link')).toBeInTheDocument();
    expect(screen.getByLabelText('Email address')).toBeInTheDocument();
  });
});
