/**
 * MessageComposer Component
 *
 * Message input component with:
 * - Text input with Enter to send (Shift+Enter for newline)
 * - File attachment button
 * - Emoji picker (emoji-picker-react)
 * - Reply preview when replying
 * - Typing indicator trigger (debounced)
 * - Support paste images
 * - Disabled when disconnected
 */

import {
  useState,
  useRef,
  useCallback,
  useEffect,
  type FormEvent,
  type KeyboardEvent,
  type ClipboardEvent,
  type ChangeEvent,
} from 'react';
import EmojiPicker, { type EmojiClickData } from 'emoji-picker-react';

export interface MessageComposerProps {
  conversationId: string;
  onSendMessage: (content: string, files?: File[], replyToId?: string) => void;
  onTyping?: (isTyping: boolean) => void;
  replyTo?: {
    messageId: string;
    content: string;
    senderName: string;
  };
  onCancelReply?: () => void;
  disabled?: boolean;
  placeholder?: string;
}

const TYPING_INDICATOR_TIMEOUT = 3000; // Stop typing after 3 seconds of inactivity
const MAX_TEXTAREA_HEIGHT = 150; // Maximum height in pixels
const BYTES_PER_KB = 1024;
const BYTES_PER_MB = 1024 * 1024;

export function MessageComposer({
  conversationId,
  onSendMessage,
  onTyping,
  replyTo,
  onCancelReply,
  disabled = false,
  placeholder = 'Type a message...',
}: MessageComposerProps) {
  const [message, setMessage] = useState('');
  const [attachedFiles, setAttachedFiles] = useState<File[]>([]);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isTypingRef = useRef(false);

  // Reset state when conversation changes
  // Note: Setting state in useEffect is intentional here - we want to reset
  // the form state when switching conversations, which is a valid use case
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setMessage('');

    setAttachedFiles([]);

    setShowEmojiPicker(false);
  }, [conversationId]);

  // Debounced typing indicator
  const handleTypingIndicator = useCallback(
    (typing: boolean) => {
      if (!onTyping) return;

      // Clear existing timeout
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }

      if (typing) {
        // Send typing start if not already typing
        if (!isTypingRef.current) {
          onTyping(true);
          isTypingRef.current = true;
        }

        // Set timeout to stop typing indicator
        typingTimeoutRef.current = setTimeout(() => {
          onTyping(false);
          isTypingRef.current = false;
        }, TYPING_INDICATOR_TIMEOUT);
      } else {
        // Send typing stop
        if (isTypingRef.current) {
          onTyping(false);
          isTypingRef.current = false;
        }
      }
    },
    [onTyping]
  );

  // Cleanup typing indicator on unmount
  useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
      if (isTypingRef.current && onTyping) {
        onTyping(false);
      }
    };
  }, [onTyping]);

  const handleMessageChange = (e: ChangeEvent<HTMLTextAreaElement>) => {
    const newValue = e.target.value;
    setMessage(newValue);

    // Trigger typing indicator if there's content
    if (newValue.trim()) {
      handleTypingIndicator(true);
    } else {
      handleTypingIndicator(false);
    }

    // Auto-resize textarea
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, MAX_TEXTAREA_HEIGHT)}px`;
    }
  };

  const handleSend = useCallback(() => {
    if (disabled) return;

    const trimmedMessage = message.trim();

    // Must have either message content or files
    if (!trimmedMessage && attachedFiles.length === 0) {
      return;
    }

    // Send message
    onSendMessage(
      trimmedMessage,
      attachedFiles.length > 0 ? attachedFiles : undefined,
      replyTo?.messageId
    );

    // Clear state
    setMessage('');
    setAttachedFiles([]);
    setShowEmojiPicker(false);
    handleTypingIndicator(false);

    // Reset textarea height
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }

    // Focus back on textarea
    textareaRef.current?.focus();
  }, [disabled, message, attachedFiles, onSendMessage, replyTo, handleTypingIndicator]);

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    // Enter to send (without Shift)
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    handleSend();
  };

  const handleFileSelect = (e: ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const newFiles = Array.from(e.target.files);
      setAttachedFiles((prev) => [...prev, ...newFiles]);
    }
    // Reset input so same file can be selected again
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleFileButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleRemoveFile = (index: number) => {
    setAttachedFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const handleEmojiClick = (emojiData: EmojiClickData) => {
    const emoji = emojiData.emoji;
    setMessage((prev) => prev + emoji);

    // Focus back on textarea
    textareaRef.current?.focus();
  };

  const handlePaste = (e: ClipboardEvent<HTMLTextAreaElement>) => {
    // Check if clipboard has files (images)
    const items = e.clipboardData.items;
    const imageFiles: File[] = [];

    for (let i = 0; i < items.length; i++) {
      const item = items[i];
      if (item.type.startsWith('image/')) {
        const file = item.getAsFile();
        if (file) {
          imageFiles.push(file);
        }
      }
    }

    if (imageFiles.length > 0) {
      e.preventDefault(); // Prevent default paste behavior
      setAttachedFiles((prev) => [...prev, ...imageFiles]);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < BYTES_PER_KB) return `${bytes} B`;
    if (bytes < BYTES_PER_MB) return `${(bytes / BYTES_PER_KB).toFixed(1)} KB`;
    return `${(bytes / BYTES_PER_MB).toFixed(1)} MB`;
  };

  return (
    <div className="border-t border-gray-200 bg-white p-4">
      <form onSubmit={handleSubmit}>
        {/* Reply Preview */}
        {replyTo && (
          <div className="mb-2 flex items-center justify-between bg-gray-50 border-l-4 border-indigo-600 px-3 py-2 rounded">
            <div className="flex-1 min-w-0">
              <div className="text-xs font-semibold text-gray-700">
                Replying to {replyTo.senderName}
              </div>
              <div className="text-sm text-gray-600 truncate">{replyTo.content}</div>
            </div>
            <button
              type="button"
              onClick={onCancelReply}
              className="ml-2 text-gray-400 hover:text-gray-600"
              aria-label="Cancel reply"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>
        )}

        {/* Attached Files Preview */}
        {attachedFiles.length > 0 && (
          <div className="mb-2 space-y-1">
            {attachedFiles.map((file, index) => (
              <div
                key={`${file.name}-${index}`}
                className="flex items-center justify-between bg-gray-50 px-3 py-2 rounded border border-gray-200"
              >
                <div className="flex items-center space-x-2 min-w-0 flex-1">
                  <svg
                    className="w-5 h-5 text-gray-500 flex-shrink-0"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13"
                    />
                  </svg>
                  <div className="min-w-0 flex-1">
                    <div className="text-sm text-gray-700 truncate" title={file.name}>
                      {file.name}
                    </div>
                    <div className="text-xs text-gray-500">{formatFileSize(file.size)}</div>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => handleRemoveFile(index)}
                  className="ml-2 text-gray-400 hover:text-red-600 flex-shrink-0"
                  aria-label={`Remove ${file.name}`}
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </div>
            ))}
          </div>
        )}

        {/* Input Container */}
        <div className="flex items-end space-x-2">
          {/* File Attachment Button */}
          <button
            type="button"
            onClick={handleFileButtonClick}
            disabled={disabled}
            className="flex-shrink-0 p-2 text-gray-500 hover:text-indigo-600 disabled:opacity-50 disabled:cursor-not-allowed"
            aria-label="Attach file"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13"
              />
            </svg>
          </button>
          <input
            ref={fileInputRef}
            type="file"
            multiple
            onChange={handleFileSelect}
            className="hidden"
            aria-label="File input"
          />

          {/* Emoji Picker Button */}
          <div className="relative flex-shrink-0">
            <button
              type="button"
              onClick={() => setShowEmojiPicker(!showEmojiPicker)}
              disabled={disabled}
              className="p-2 text-gray-500 hover:text-indigo-600 disabled:opacity-50 disabled:cursor-not-allowed"
              aria-label="Open emoji picker"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </button>

            {/* Emoji Picker Popup */}
            {showEmojiPicker && !disabled && (
              <div className="absolute bottom-full mb-2 left-0 z-50">
                <EmojiPicker
                  onEmojiClick={handleEmojiClick}
                  autoFocusSearch={false}
                  width={320}
                  height={400}
                />
              </div>
            )}
          </div>

          {/* Text Input */}
          <textarea
            ref={textareaRef}
            value={message}
            onChange={handleMessageChange}
            onKeyDown={handleKeyDown}
            onPaste={handlePaste}
            placeholder={disabled ? 'Disconnected...' : placeholder}
            disabled={disabled}
            rows={1}
            className="flex-1 resize-none border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
            style={{ minHeight: '42px', maxHeight: `${MAX_TEXTAREA_HEIGHT}px` }}
            aria-label="Message input"
          />

          {/* Send Button */}
          <button
            type="submit"
            disabled={disabled || (!message.trim() && attachedFiles.length === 0)}
            className="flex-shrink-0 bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            aria-label="Send message"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"
              />
            </svg>
          </button>
        </div>

        {/* Helper Text */}
        <div className="mt-2 text-xs text-gray-500">
          Press Enter to send, Shift+Enter for new line
        </div>
      </form>
    </div>
  );
}
