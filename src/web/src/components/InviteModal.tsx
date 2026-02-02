/**
 * InviteModal Component
 *
 * Modal for generating and displaying invite links with QR code.
 * Allows users to generate invite links, view QR code, copy link, and set expiry.
 */

import { useState, useEffect, useRef } from 'react';
import { api } from '@services/index';

export interface InviteModalProps {
  onClose?: () => void;
  onGenerate?: (expiryHours: number) => void;
}

export function InviteModal({ onClose, onGenerate }: InviteModalProps) {
  const [expiryHours, setExpiryHours] = useState(24);
  const [inviteLink, setInviteLink] = useState('');
  const [qrCodeDataUrl, setQrCodeDataUrl] = useState('');
  const [expiresAt, setExpiresAt] = useState<Date | null>(null);
  const [copied, setCopied] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // Generate invite link via API
  const generateInviteLink = async () => {
    setIsGenerating(true);
    setError(null);

    try {
      const response = await api.generateInvite({ expiryHours });

      setInviteLink(response.inviteLink);
      setQrCodeDataUrl(response.qrCodeDataUrl);
      setExpiresAt(new Date(response.expiresAt));

      onGenerate?.(expiryHours);
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to generate invite link';
      setError(errorMsg);
      console.error('Failed to generate invite:', err);
    } finally {
      setIsGenerating(false);
    }
  };

  // Render QR code to canvas when QR code data URL changes
  useEffect(() => {
    if (qrCodeDataUrl && canvasRef.current) {
      const img = new Image();
      img.onload = () => {
        const canvas = canvasRef.current;
        if (canvas) {
          const ctx = canvas.getContext('2d');
          if (ctx) {
            canvas.width = img.width;
            canvas.height = img.height;
            ctx.drawImage(img, 0, 0);
          }
        }
      };
      img.src = qrCodeDataUrl;
    }
  }, [qrCodeDataUrl]);

  // Copy invite link to clipboard
  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(inviteLink);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
      setError('Failed to copy to clipboard');
    }
  };

  // Format expiry time remaining
  const formatTimeRemaining = () => {
    if (!expiresAt) return '';

    const now = new Date();
    const diffMs = expiresAt.getTime() - now.getTime();

    // If expired, return "Expired"
    if (diffMs <= 0) {
      return 'Expired';
    }

    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

    if (diffHours > 0) {
      return `${diffHours} hour${diffHours !== 1 ? 's' : ''}`;
    }
    return `${diffMinutes} minute${diffMinutes !== 1 ? 's' : ''}`;
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="invite-modal-title"
        className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 p-6"
      >
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <h2 id="invite-modal-title" className="text-2xl font-bold text-gray-900">
            Invite Friends
          </h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 focus:outline-none"
            aria-label="Close modal"
          >
            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        {/* Expiry Selection */}
        <div className="mb-6">
          <label htmlFor="expiry" className="block text-sm font-medium text-gray-700 mb-2">
            Link Expiry
          </label>
          <select
            id="expiry"
            value={expiryHours}
            onChange={(e) => setExpiryHours(Number(e.target.value))}
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
            disabled={!!inviteLink}
          >
            <option value={1}>1 hour</option>
            <option value={6}>6 hours</option>
            <option value={24}>24 hours</option>
            <option value={72}>3 days</option>
            <option value={168}>1 week</option>
          </select>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-4 rounded-md bg-red-50 p-4">
            <div className="flex">
              <div className="ml-3">
                <h3 className="text-sm font-medium text-red-800">{error}</h3>
              </div>
            </div>
          </div>
        )}

        {/* Generate Button */}
        {!inviteLink && (
          <button
            onClick={generateInviteLink}
            disabled={isGenerating}
            className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed mb-6"
          >
            {isGenerating ? 'Generating...' : 'Generate Invite Link'}
          </button>
        )}

        {/* Invite Link and QR Code */}
        {inviteLink && (
          <div className="space-y-4">
            {/* QR Code */}
            <div className="flex justify-center">
              <div className="bg-white p-4 rounded-lg border-2 border-gray-200">
                <canvas ref={canvasRef} style={{ display: 'block', maxWidth: '256px' }} />
              </div>
            </div>

            {/* Invite Link */}
            <div>
              <label htmlFor="invite-link" className="block text-sm font-medium text-gray-700 mb-2">
                Invite Link
              </label>
              <div className="flex space-x-2">
                <input
                  id="invite-link"
                  type="text"
                  value={inviteLink}
                  readOnly
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm bg-gray-50"
                />
                <button
                  onClick={copyToClipboard}
                  className="px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                >
                  {copied ? 'Copied!' : 'Copy'}
                </button>
              </div>
            </div>

            {/* Expiry Info */}
            <div className="text-sm text-gray-600">
              This link will expire in{' '}
              <span className="font-medium">
                {formatTimeRemaining() || `${expiryHours} hour${expiryHours !== 1 ? 's' : ''}`}
              </span>
            </div>

            {/* Generate New Link Button */}
            <button
              onClick={() => {
                setInviteLink('');
                setQrCodeDataUrl('');
                setExpiresAt(null);
                setCopied(false);
                setError(null);
              }}
              className="w-full flex justify-center py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
              Generate New Link
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
