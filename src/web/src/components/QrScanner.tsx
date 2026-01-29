/**
 * QrScanner Component
 *
 * Camera-based QR code scanning component using the qr-scanner library.
 * Allows users to scan QR codes for invite links or other purposes.
 */

import { useEffect, useRef, useState } from 'react';
import QrScannerLib from 'qr-scanner';

export interface QrScannerProps {
  onScan: (result: string) => void;
  onError?: (error: string) => void;
  onClose?: () => void;
}

export function QrScanner({ onScan, onError, onClose }: QrScannerProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const scannerRef = useRef<QrScannerLib | null>(null);
  const [hasCamera, setHasCamera] = useState(true);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const initScanner = async () => {
      if (!videoRef.current) return;

      try {
        // Check if camera is available
        const hasCameraDevice = await QrScannerLib.hasCamera();
        if (!hasCameraDevice) {
          const errorMsg = 'No camera found on this device';
          setHasCamera(false);
          setError(errorMsg);
          setIsLoading(false);
          onError?.(errorMsg);
          return;
        }

        // Initialize QR scanner
        const scanner = new QrScannerLib(
          videoRef.current,
          (result) => {
            // Stop scanning after successful scan
            scanner.stop();
            onScan(result.data);
          },
          {
            returnDetailedScanResult: true,
            highlightScanRegion: true,
            highlightCodeOutline: true,
          }
        );

        scannerRef.current = scanner;

        // Start scanning
        await scanner.start();
        setIsLoading(false);
      } catch (err) {
        const errorMsg =
          err instanceof Error
            ? err.message
            : 'Failed to access camera. Please check permissions.';
        setError(errorMsg);
        setIsLoading(false);
        onError?.(errorMsg);
      }
    };

    initScanner();

    // Cleanup
    return () => {
      if (scannerRef.current) {
        scannerRef.current.stop();
        scannerRef.current.destroy();
      }
    };
  }, [onScan, onError]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-75">
      <div className="relative w-full max-w-lg mx-4">
        {/* Close Button */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 z-10 p-2 bg-white rounded-full shadow-lg hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          aria-label="Close scanner"
        >
          <svg
            className="w-6 h-6 text-gray-600"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>

        <div className="bg-white rounded-lg overflow-hidden shadow-xl">
          {/* Header */}
          <div className="px-6 py-4 bg-indigo-600">
            <h3 className="text-xl font-semibold text-white">Scan QR Code</h3>
            <p className="text-sm text-indigo-100 mt-1">
              Position the QR code within the frame
            </p>
          </div>

          {/* Video Container */}
          <div className="relative bg-black">
            {isLoading && (
              <div className="absolute inset-0 flex items-center justify-center bg-gray-900">
                <div className="text-center">
                  <svg
                    className="animate-spin h-12 w-12 text-white mx-auto"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    ></circle>
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    ></path>
                  </svg>
                  <p className="text-white mt-4">Initializing camera...</p>
                </div>
              </div>
            )}

            {error && (
              <div className="absolute inset-0 flex items-center justify-center bg-gray-900">
                <div className="text-center px-4">
                  <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100 mb-4">
                    <svg
                      className="h-6 w-6 text-red-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M6 18L18 6M6 6l12 12"
                      />
                    </svg>
                  </div>
                  <p className="text-white text-lg font-semibold mb-2">Camera Error</p>
                  <p className="text-gray-300 text-sm">{error}</p>
                </div>
              </div>
            )}

            <video
              ref={videoRef}
              className="w-full aspect-square object-cover"
              style={{ display: error ? 'none' : 'block' }}
            />
          </div>

          {/* Instructions */}
          {!error && hasCamera && (
            <div className="px-6 py-4 bg-gray-50">
              <div className="flex items-start">
                <div className="flex-shrink-0">
                  <svg
                    className="h-5 w-5 text-blue-400"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      fillRule="evenodd"
                      d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
                <div className="ml-3">
                  <p className="text-sm text-gray-700">
                    Hold your device steady and ensure the QR code is well-lit and within the
                    scanning area.
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
