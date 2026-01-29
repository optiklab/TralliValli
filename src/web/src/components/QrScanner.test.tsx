/**
 * QrScanner Component Tests
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QrScanner } from './QrScanner';

// Mock the qr-scanner library
vi.mock('qr-scanner', () => {
  const mockStart = vi.fn().mockResolvedValue(undefined);
  const mockStop = vi.fn();
  const mockDestroy = vi.fn();
  const mockHasCamera = vi.fn().mockResolvedValue(true);

  const MockQrScannerClass = vi.fn(function (this: any) {
    this.start = mockStart;
    this.stop = mockStop;
    this.destroy = mockDestroy;
    return this;
  });

  MockQrScannerClass.hasCamera = mockHasCamera;

  // Export mocks for access in tests
  (MockQrScannerClass as any)._mockStart = mockStart;
  (MockQrScannerClass as any)._mockStop = mockStop;
  (MockQrScannerClass as any)._mockDestroy = mockDestroy;
  (MockQrScannerClass as any)._mockHasCamera = mockHasCamera;

  return {
    default: MockQrScannerClass,
  };
});

// Import after mocking
import QrScannerLib from 'qr-scanner';

describe('QrScanner', () => {
  const mockOnScan = vi.fn();
  const mockOnError = vi.fn();
  const mockOnClose = vi.fn();

  // Get the mock functions
  const mockStart = (QrScannerLib as any)._mockStart;
  const mockStop = (QrScannerLib as any)._mockStop;
  const mockDestroy = (QrScannerLib as any)._mockDestroy;
  const mockHasCamera = (QrScannerLib as any)._mockHasCamera;

  beforeEach(() => {
    vi.clearAllMocks();
    mockHasCamera.mockResolvedValue(true);
    mockStart.mockResolvedValue(undefined);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders the scanner modal', () => {
    render(<QrScanner onScan={mockOnScan} />);

    expect(screen.getByText('Scan QR Code')).toBeInTheDocument();
    expect(screen.getByText('Position the QR code within the frame')).toBeInTheDocument();
  });

  it('renders close button', () => {
    render(<QrScanner onScan={mockOnScan} onClose={mockOnClose} />);

    const closeButton = screen.getByLabelText('Close scanner');
    expect(closeButton).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', () => {
    render(<QrScanner onScan={mockOnScan} onClose={mockOnClose} />);

    const closeButton = screen.getByLabelText('Close scanner');
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it('shows loading state initially', () => {
    render(<QrScanner onScan={mockOnScan} />);

    expect(screen.getByText('Initializing camera...')).toBeInTheDocument();
  });

  it('initializes QR scanner on mount', async () => {
    render(<QrScanner onScan={mockOnScan} />);

    await waitFor(() => {
      expect(mockHasCamera).toHaveBeenCalled();
      expect(mockStart).toHaveBeenCalled();
    });
  });

  it('shows error when no camera is available', async () => {
    mockHasCamera.mockResolvedValue(false);

    render(<QrScanner onScan={mockOnScan} onError={mockOnError} />);

    await waitFor(() => {
      expect(screen.getByText('Camera Error')).toBeInTheDocument();
      expect(screen.getByText('No camera found on this device')).toBeInTheDocument();
    });
    
    expect(mockOnError).toHaveBeenCalledWith('No camera found on this device');
  });

  it('calls onError when camera initialization fails', async () => {
    const mockError = new Error('Camera access denied');
    mockStart.mockRejectedValue(mockError);

    render(<QrScanner onScan={mockOnScan} onError={mockOnError} />);

    await waitFor(() => {
      expect(screen.getByText('Camera Error')).toBeInTheDocument();
    });
    
    expect(mockOnError).toHaveBeenCalled();
  });

  it('renders video element', () => {
    const { container } = render(<QrScanner onScan={mockOnScan} />);

    const videoElement = container.querySelector('video');
    expect(videoElement).toBeInTheDocument();
  });

  it('displays helpful instructions', async () => {
    render(<QrScanner onScan={mockOnScan} />);

    await waitFor(() => {
      expect(mockStart).toHaveBeenCalled();
    });

    expect(
      screen.getByText(/Hold your device steady and ensure the QR code is well-lit/)
    ).toBeInTheDocument();
  });

  it('hides video when error occurs', async () => {
    mockHasCamera.mockResolvedValue(false);

    const { container } = render(<QrScanner onScan={mockOnScan} />);

    await waitFor(() => {
      expect(screen.getByText('Camera Error')).toBeInTheDocument();
    });

    const videoElement = container.querySelector('video');
    expect(videoElement).toHaveStyle({ display: 'none' });
  });

  it('cleans up scanner on unmount', async () => {
    const { unmount } = render(<QrScanner onScan={mockOnScan} />);

    await waitFor(() => {
      expect(mockStart).toHaveBeenCalled();
    });

    unmount();

    expect(mockStop).toHaveBeenCalled();
    expect(mockDestroy).toHaveBeenCalled();
  });

  it('renders with correct Tailwind classes', () => {
    const { container } = render(<QrScanner onScan={mockOnScan} />);

    // Check for modal overlay
    const overlay = container.querySelector('.fixed.inset-0');
    expect(overlay).toBeInTheDocument();
    expect(overlay).toHaveClass('bg-black', 'bg-opacity-75');

    // Check for modal content
    const modalContent = container.querySelector('.bg-white.rounded-lg');
    expect(modalContent).toBeInTheDocument();
  });
});
