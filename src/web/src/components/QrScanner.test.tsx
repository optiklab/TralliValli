/**
 * QrScanner Component Tests
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import QrScannerLib from 'qr-scanner';
import { QrScanner } from './QrScanner';

// Define types for our mock QR scanner
interface MockQrScanner {
  start: ReturnType<typeof vi.fn>;
  stop: ReturnType<typeof vi.fn>;
  destroy: ReturnType<typeof vi.fn>;
  _mockStart?: ReturnType<typeof vi.fn>;
  _mockStop?: ReturnType<typeof vi.fn>;
  _mockDestroy?: ReturnType<typeof vi.fn>;
  _mockHasCamera?: ReturnType<typeof vi.fn>;
}

// Mock the qr-scanner library
vi.mock('qr-scanner', () => {
  const mockStart = vi.fn().mockResolvedValue(undefined);
  const mockStop = vi.fn();
  const mockDestroy = vi.fn();
  const mockHasCamera = vi.fn().mockResolvedValue(true);

  const MockQrScannerClass = vi.fn(function (this: MockQrScanner) {
    this.start = mockStart;
    this.stop = mockStop;
    this.destroy = mockDestroy;
    return this;
  });

  MockQrScannerClass.hasCamera = mockHasCamera;

  // Export mocks for access in tests
  (MockQrScannerClass as MockQrScanner)._mockStart = mockStart;
  (MockQrScannerClass as MockQrScanner)._mockStop = mockStop;
  (MockQrScannerClass as MockQrScanner)._mockDestroy = mockDestroy;
  (MockQrScannerClass as MockQrScanner)._mockHasCamera = mockHasCamera;

  return {
    default: MockQrScannerClass,
  };
});

describe('QrScanner', () => {
  const mockOnScan = vi.fn();
  const mockOnError = vi.fn();
  const mockOnClose = vi.fn();

  // Get the mock functions
  const mockStart = (QrScannerLib as unknown as MockQrScanner)._mockStart!;
  const mockStop = (QrScannerLib as unknown as MockQrScanner)._mockStop!;
  const mockDestroy = (QrScannerLib as unknown as MockQrScanner)._mockDestroy!;
  const mockHasCamera = (QrScannerLib as unknown as MockQrScanner)._mockHasCamera!;

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
