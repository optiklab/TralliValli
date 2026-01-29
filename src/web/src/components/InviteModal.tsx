/**
 * InviteModal Component
 *
 * Modal for generating and displaying invite links with QR code.
 * Allows users to generate invite links, view QR code, copy link, and set expiry.
 */

import { useState, useEffect, useRef } from 'react';
import QRCode from 'qrcode';

export interface InviteModalProps {
  onClose?: () => void;
  onGenerate?: (expiryHours: number) => void;
}

export function InviteModal({ onClose, onGenerate }: InviteModalProps) {
  const [expiryHours, setExpiryHours] = useState(24);
  const [inviteLink, setInviteLink] = useState('');
  const [qrCodeUrl, setQrCodeUrl] = useState('');
  const [copied, setCopied] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // Generate invite link
  const generateInviteLink = () => {
    setIsGenerating(true);
    
    // Generate a random invite token
    const token = crypto.randomUUID();
    const baseUrl = window.location.origin;
    const link = `${baseUrl}/invite/${token}`;
    
    setInviteLink(link);
    onGenerate?.(expiryHours);
    setIsGenerating(false);
  };

  // Generate QR code when invite link changes
  useEffect(() => {
    if (inviteLink && canvasRef.current) {
      QRCode.toCanvas(
        canvasRef.current,
        inviteLink,
        {
          width: 256,
          margin: 2,
          color: {
            dark: '#000000',
            light: '#ffffff',
          },
        },
        (error) => {
          if (error) {
            console.error('QR code generation error:', error);
          } else {
            // Also generate data URL for display
            QRCode.toDataURL(inviteLink, { width: 256, margin: 2 })
              .then((url) => setQrCodeUrl(url))
              .catch((err) => console.error('QR code data URL error:', err));
          }
        }
      );
    }
  }, [inviteLink]);

  // Copy invite link to clipboard
  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(inviteLink);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 p-6">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-900">Invite Friends</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 focus:outline-none"
            aria-label="Close modal"
          >
            <svg
              className="h-6 w-6"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
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
                <canvas ref={canvasRef} style={{ display: 'block' }} />
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
              This link will expire in <span className="font-medium">{expiryHours} hour{expiryHours !== 1 ? 's' : ''}</span>
            </div>

            {/* Generate New Link Button */}
            <button
              onClick={() => {
                setInviteLink('');
                setQrCodeUrl('');
                setCopied(false);
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
