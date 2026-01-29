/**
 * InviteModal Component Tests
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { InviteModal } from './InviteModal';

// Mock QRCode library
vi.mock('qrcode', () => ({
  default: {
    toCanvas: vi.fn((canvas, _text, _options, callback) => {
      // Simulate successful QR code generation
      callback?.(null);
    }),
  },
}));

describe('InviteModal', () => {
  let clipboardWriteText: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    // Mock clipboard API
    clipboardWriteText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', {
      value: {
        writeText: clipboardWriteText,
      },
      writable: true,
      configurable: true,
    });

    // Mock window.location
    Object.defineProperty(window, 'location', {
      value: {
        origin: 'http://localhost:3000',
      },
      writable: true,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders the modal', () => {
    render(<InviteModal />);

    expect(screen.getByText('Invite Friends')).toBeInTheDocument();
    expect(screen.getByLabelText('Link Expiry')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /generate invite link/i })).toBeInTheDocument();
  });

  it('renders close button', () => {
    render(<InviteModal />);

    expect(screen.getByLabelText('Close modal')).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', () => {
    const mockOnClose = vi.fn();
    render(<InviteModal onClose={mockOnClose} />);

    const closeButton = screen.getByLabelText('Close modal');
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it('displays expiry options', () => {
    render(<InviteModal />);

    const select = screen.getByLabelText('Link Expiry') as HTMLSelectElement;
    expect(select).toBeInTheDocument();

    expect(screen.getByRole('option', { name: '1 hour' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: '6 hours' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: '24 hours' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: '3 days' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: '1 week' })).toBeInTheDocument();
  });

  it('defaults to 24 hours expiry', () => {
    render(<InviteModal />);

    const select = screen.getByLabelText('Link Expiry') as HTMLSelectElement;
    expect(select.value).toBe('24');
  });

  it('allows changing expiry selection', () => {
    render(<InviteModal />);

    const select = screen.getByLabelText('Link Expiry') as HTMLSelectElement;
    fireEvent.change(select, { target: { value: '72' } });

    expect(select.value).toBe('72');
  });

  it('generates invite link when button is clicked', async () => {
    render(<InviteModal />);

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      expect(screen.getByLabelText('Invite Link')).toBeInTheDocument();
    });

    const inviteLinkInput = screen.getByLabelText('Invite Link') as HTMLInputElement;
    expect(inviteLinkInput.value).toContain('http://localhost:3000/invite/');
  });

  it('calls onGenerate callback with expiry hours', async () => {
    const mockOnGenerate = vi.fn();
    render(<InviteModal onGenerate={mockOnGenerate} />);

    const select = screen.getByLabelText('Link Expiry') as HTMLSelectElement;
    fireEvent.change(select, { target: { value: '72' } });

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      expect(mockOnGenerate).toHaveBeenCalledWith(72);
    });
  });

  it('displays QR code after generating link', async () => {
    render(<InviteModal />);

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      const canvas = document.querySelector('canvas');
      expect(canvas).toBeInTheDocument();
    });
  });

  it('displays copy button after generating link', async () => {
    render(<InviteModal />);

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /copy/i })).toBeInTheDocument();
    });
  });

  it('copies invite link to clipboard when copy button is clicked', async () => {
    render(<InviteModal />);

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /copy/i })).toBeInTheDocument();
    });

    const copyButton = screen.getByRole('button', { name: /copy/i });
    fireEvent.click(copyButton);

    await waitFor(() => {
      expect(clipboardWriteText).toHaveBeenCalled();
      expect(screen.getByText('Copied!')).toBeInTheDocument();
    });
  });

  it('displays expiry information', async () => {
    render(<InviteModal />);

    const select = screen.getByLabelText('Link Expiry') as HTMLSelectElement;
    fireEvent.change(select, { target: { value: '6' } });

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      const inviteLinkInput = screen.queryByLabelText('Invite Link');
      expect(inviteLinkInput).toBeInTheDocument();
    });

    // Check expiry text - should be in gray-600 div, not in the select options
    const expiryText = screen.getByText(/This link will expire in/).parentElement;
    expect(expiryText).toHaveTextContent('6');
    expect(expiryText).toHaveTextContent('hours');
  });

  it('handles singular hour in expiry message', async () => {
    render(<InviteModal />);

    const select = screen.getByLabelText('Link Expiry') as HTMLSelectElement;
    fireEvent.change(select, { target: { value: '1' } });

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      const inviteLinkInput = screen.queryByLabelText('Invite Link');
      expect(inviteLinkInput).toBeInTheDocument();
    });

    // Check for singular form - should be in gray-600 div
    const expiryText = screen.getByText(/This link will expire in/).parentElement;
    expect(expiryText).toHaveTextContent('1');
    expect(expiryText).toHaveTextContent('hour');
    // Should not have the 's' for plural
    expect(expiryText?.textContent).not.toMatch(/1 hours/);
  });

  it('disables expiry selection after link is generated', async () => {
    render(<InviteModal />);

    const select = screen.getByLabelText('Link Expiry') as HTMLSelectElement;
    expect(select).not.toBeDisabled();

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      const inviteLinkInput = screen.queryByLabelText('Invite Link');
      expect(inviteLinkInput).toBeInTheDocument();
    });

    expect(select).toBeDisabled();
  });

  it('allows generating a new link', async () => {
    render(<InviteModal />);

    // Generate first link
    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      expect(screen.getByLabelText('Invite Link')).toBeInTheDocument();
    });

    const firstLink = (screen.getByLabelText('Invite Link') as HTMLInputElement).value;

    // Click generate new link
    const generateNewButton = screen.getByRole('button', { name: /generate new link/i });
    fireEvent.click(generateNewButton);

    // Should show generate button again
    expect(screen.getByRole('button', { name: /generate invite link/i })).toBeInTheDocument();

    // Generate second link
    const generateButton2 = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton2);

    await waitFor(() => {
      const inviteInput = screen.getByLabelText('Invite Link') as HTMLInputElement;
      expect(inviteInput.value).toBeTruthy();
      expect(inviteInput.value).not.toBe(firstLink);
    });
  });

  it('generates unique invite tokens', async () => {
    const { unmount } = render(<InviteModal />);

    const generateButton = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton);

    await waitFor(() => {
      expect(screen.getByLabelText('Invite Link')).toBeInTheDocument();
    });

    const firstLink = (screen.getByLabelText('Invite Link') as HTMLInputElement).value;

    unmount();

    // Render again and generate another link
    render(<InviteModal />);

    const generateButton2 = screen.getByRole('button', { name: /generate invite link/i });
    fireEvent.click(generateButton2);

    await waitFor(() => {
      const inviteInput = screen.getByLabelText('Invite Link') as HTMLInputElement;
      expect(inviteInput.value).toBeTruthy();
      expect(inviteInput.value).not.toBe(firstLink);
    });
  });
});
