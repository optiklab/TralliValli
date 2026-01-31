/**
 * MessageComposer Component Tests
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MessageComposer } from './MessageComposer';

// Mock emoji-picker-react
vi.mock('emoji-picker-react', () => ({
  default: ({ onEmojiClick }: { onEmojiClick: (emoji: { emoji: string }) => void }) => (
    <div className="EmojiPickerReact" data-testid="emoji-picker">
      <button onClick={() => onEmojiClick({ emoji: '😀' })}>😀</button>
    </div>
  ),
}));

describe('MessageComposer', () => {
  const mockOnSendMessage = vi.fn();
  const mockOnTyping = vi.fn();
  const mockOnCancelReply = vi.fn();

  const defaultProps = {
    conversationId: 'conv-1',
    onSendMessage: mockOnSendMessage,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('Rendering', () => {
    it('renders the message composer', () => {
      render(<MessageComposer {...defaultProps} />);

      expect(screen.getByPlaceholderText('Type a message...')).toBeInTheDocument();
      expect(screen.getByLabelText('Attach file')).toBeInTheDocument();
      expect(screen.getByLabelText('Open emoji picker')).toBeInTheDocument();
      expect(screen.getByLabelText('Send message')).toBeInTheDocument();
    });

    it('shows custom placeholder when provided', () => {
      render(<MessageComposer {...defaultProps} placeholder="Write something..." />);

      expect(screen.getByPlaceholderText('Write something...')).toBeInTheDocument();
    });

    it('shows disconnected placeholder when disabled', () => {
      render(<MessageComposer {...defaultProps} disabled={true} />);

      expect(screen.getByPlaceholderText('Disconnected...')).toBeInTheDocument();
    });

    it('shows helper text', () => {
      render(<MessageComposer {...defaultProps} />);

      expect(screen.getByText('Press Enter to send, Shift+Enter for new line')).toBeInTheDocument();
    });
  });

  describe('Text Input', () => {
    it('allows typing in the textarea', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Hello world');

      expect(textarea).toHaveValue('Hello world');
    });

    it('sends message on Enter key', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Test message');
      await user.keyboard('{Enter}');

      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Test message',
        undefined,
        undefined,
        undefined
      );
    });

    it('adds newline on Shift+Enter', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Line 1{Shift>}{Enter}{/Shift}Line 2');

      expect(textarea).toHaveValue('Line 1\nLine 2');
      expect(mockOnSendMessage).not.toHaveBeenCalled();
    });

    it('clears input after sending', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Test message{Enter}');

      expect(textarea).toHaveValue('');
    });

    it('does not send empty messages', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, '   {Enter}');

      expect(mockOnSendMessage).not.toHaveBeenCalled();
    });

    it('trims whitespace from messages', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, '  Test message  {Enter}');

      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Test message',
        undefined,
        undefined,
        undefined
      );
    });
  });

  describe('Send Button', () => {
    it('sends message when clicking send button', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Test message');

      const sendButton = screen.getByLabelText('Send message');
      await user.click(sendButton);

      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Test message',
        undefined,
        undefined,
        undefined
      );
    });

    it('is disabled when no message and no files', () => {
      render(<MessageComposer {...defaultProps} />);

      const sendButton = screen.getByLabelText('Send message');
      expect(sendButton).toBeDisabled();
    });

    it('is enabled when message is entered', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      const sendButton = screen.getByLabelText('Send message');

      expect(sendButton).toBeDisabled();

      await user.type(textarea, 'Test');

      expect(sendButton).not.toBeDisabled();
    });

    it('is disabled when component is disabled', () => {
      render(<MessageComposer {...defaultProps} disabled={true} />);

      const sendButton = screen.getByLabelText('Send message');
      expect(sendButton).toBeDisabled();
    });
  });

  describe('File Attachment', () => {
    it('shows file input button', () => {
      render(<MessageComposer {...defaultProps} />);

      expect(screen.getByLabelText('Attach file')).toBeInTheDocument();
    });

    it('allows selecting files', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const file = new File(['test content'], 'test.txt', { type: 'text/plain' });
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      await user.upload(fileInput, file);

      await waitFor(() => {
        expect(screen.getByText('test.txt')).toBeInTheDocument();
      });
    });

    it('shows multiple attached files', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const file1 = new File(['content1'], 'file1.txt', { type: 'text/plain' });
      const file2 = new File(['content2'], 'file2.txt', { type: 'text/plain' });
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      await user.upload(fileInput, [file1, file2]);

      await waitFor(() => {
        expect(screen.getByText('file1.txt')).toBeInTheDocument();
        expect(screen.getByText('file2.txt')).toBeInTheDocument();
      });
    });

    it('displays file size', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const file = new File(['x'.repeat(1024)], 'test.txt', { type: 'text/plain' });
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      await user.upload(fileInput, file);

      await waitFor(() => {
        expect(screen.getByText('1.0 KB')).toBeInTheDocument();
      });
    });

    it('allows removing attached files', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      await user.upload(fileInput, file);

      await waitFor(() => {
        expect(screen.getByText('test.txt')).toBeInTheDocument();
      });

      const removeButton = screen.getByLabelText('Remove test.txt');
      await user.click(removeButton);

      await waitFor(() => {
        expect(screen.queryByText('test.txt')).not.toBeInTheDocument();
      });
    });

    it('sends files with message', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      await user.upload(fileInput, file);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Message with file{Enter}');

      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Message with file',
        undefined,
        expect.arrayContaining([expect.objectContaining({ name: 'test.txt' })]),
        undefined
      );
    });

    it('can send files without text', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      await user.upload(fileInput, file);

      await waitFor(() => {
        expect(screen.getByText('test.txt')).toBeInTheDocument();
      });

      const sendButton = screen.getByLabelText('Send message');
      await user.click(sendButton);

      expect(mockOnSendMessage).toHaveBeenCalledWith(
        '',
        undefined,
        expect.arrayContaining([expect.objectContaining({ name: 'test.txt' })]),
        undefined
      );
    });

    it('clears files after sending', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      await user.upload(fileInput, file);

      await waitFor(() => {
        expect(screen.getByText('test.txt')).toBeInTheDocument();
      });

      const sendButton = screen.getByLabelText('Send message');
      await user.click(sendButton);

      await waitFor(() => {
        expect(screen.queryByText('test.txt')).not.toBeInTheDocument();
      });
    });

    it('disables file button when disabled', () => {
      render(<MessageComposer {...defaultProps} disabled={true} />);

      const fileButton = screen.getByLabelText('Attach file');
      expect(fileButton).toBeDisabled();
    });
  });

  describe('Emoji Picker', () => {
    it('shows emoji picker button', () => {
      render(<MessageComposer {...defaultProps} />);

      expect(screen.getByLabelText('Open emoji picker')).toBeInTheDocument();
    });

    it('toggles emoji picker on button click', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const emojiButton = screen.getByLabelText('Open emoji picker');

      // Emoji picker should not be visible initially
      expect(screen.queryByTestId('emoji-picker')).not.toBeInTheDocument();

      await user.click(emojiButton);

      // Should show emoji picker
      await waitFor(() => {
        expect(screen.getByTestId('emoji-picker')).toBeInTheDocument();
      });

      await user.click(emojiButton);

      // Should hide emoji picker
      await waitFor(() => {
        expect(screen.queryByTestId('emoji-picker')).not.toBeInTheDocument();
      });
    });

    it('disables emoji button when disabled', () => {
      render(<MessageComposer {...defaultProps} disabled={true} />);

      const emojiButton = screen.getByLabelText('Open emoji picker');
      expect(emojiButton).toBeDisabled();
    });

    it('does not show emoji picker when disabled', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} disabled={true} />);

      const emojiButton = screen.getByLabelText('Open emoji picker');
      await user.click(emojiButton);

      expect(screen.queryByTestId('emoji-picker')).not.toBeInTheDocument();
    });
  });

  describe('Reply Preview', () => {
    const replyTo = {
      messageId: 'msg-1',
      content: 'Original message',
      senderName: 'John Doe',
    };

    it('shows reply preview when replying', () => {
      render(
        <MessageComposer {...defaultProps} replyTo={replyTo} onCancelReply={mockOnCancelReply} />
      );

      expect(screen.getByText('Replying to John Doe')).toBeInTheDocument();
      expect(screen.getByText('Original message')).toBeInTheDocument();
    });

    it('calls onCancelReply when clicking cancel button', async () => {
      const user = userEvent.setup();
      render(
        <MessageComposer {...defaultProps} replyTo={replyTo} onCancelReply={mockOnCancelReply} />
      );

      const cancelButton = screen.getByLabelText('Cancel reply');
      await user.click(cancelButton);

      expect(mockOnCancelReply).toHaveBeenCalled();
    });

    it('sends message with replyTo id', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} replyTo={replyTo} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Reply message{Enter}');

      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Reply message',
        undefined,
        undefined,
        'msg-1'
      );
    });

    it('does not show reply preview when not replying', () => {
      render(<MessageComposer {...defaultProps} />);

      expect(screen.queryByText(/Replying to/)).not.toBeInTheDocument();
    });
  });

  describe('Typing Indicator', () => {
    it('triggers typing indicator when typing', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} onTyping={mockOnTyping} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Test');

      expect(mockOnTyping).toHaveBeenCalledWith(true);
    });

    it('stops typing indicator after inactivity', async () => {
      vi.useFakeTimers();
      render(<MessageComposer {...defaultProps} onTyping={mockOnTyping} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      fireEvent.change(textarea, { target: { value: 'Test' } });

      expect(mockOnTyping).toHaveBeenCalledWith(true);

      // Fast-forward time by 3 seconds
      vi.advanceTimersByTime(3000);

      expect(mockOnTyping).toHaveBeenCalledWith(false);
      vi.useRealTimers();
    });

    it('does not trigger typing on empty input', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} onTyping={mockOnTyping} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Test');

      mockOnTyping.mockClear();

      // Clear the text
      await user.clear(textarea);

      expect(mockOnTyping).toHaveBeenCalledWith(false);
    });

    it('stops typing indicator on send', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} onTyping={mockOnTyping} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Test message{Enter}');

      expect(mockOnTyping).toHaveBeenCalledWith(false);
    });
  });

  describe('Paste Images', () => {
    it('supports pasting images from clipboard', async () => {
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      const file = new File(['fake image'], 'image.png', { type: 'image/png' });

      const clipboardData = {
        items: [
          {
            type: 'image/png',
            getAsFile: () => file,
          },
        ],
      } as unknown as DataTransfer;

      fireEvent.paste(textarea, { clipboardData });

      await waitFor(() => {
        expect(screen.getByText('image.png')).toBeInTheDocument();
      });
    });

    it('ignores non-image paste', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');

      // Regular text paste should work normally
      await user.click(textarea);
      await user.paste('pasted text');

      expect(textarea).toHaveValue('pasted text');
    });
  });

  describe('Disabled State', () => {
    it('disables all controls when disabled', () => {
      render(<MessageComposer {...defaultProps} disabled={true} />);

      expect(screen.getByPlaceholderText('Disconnected...')).toBeDisabled();
      expect(screen.getByLabelText('Attach file')).toBeDisabled();
      expect(screen.getByLabelText('Open emoji picker')).toBeDisabled();
      expect(screen.getByLabelText('Send message')).toBeDisabled();
    });

    it('does not send message when disabled', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} disabled={true} />);

      const textarea = screen.getByPlaceholderText('Disconnected...');

      // Try to type (should not work)
      await user.type(textarea, 'Test{Enter}');

      expect(mockOnSendMessage).not.toHaveBeenCalled();
    });
  });

  describe('Conversation Change', () => {
    it('clears state when conversation changes', () => {
      const { rerender } = render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...') as HTMLTextAreaElement;
      fireEvent.change(textarea, { target: { value: 'Test message' } });

      expect(textarea.value).toBe('Test message');

      // Change conversation
      rerender(<MessageComposer {...defaultProps} conversationId="conv-2" />);

      expect(textarea.value).toBe('');
    });
  });

  describe('Encryption Integration', () => {
    it('encrypts message when encryption service is provided', async () => {
      const mockEncryptionService = {
        encryptMessage: vi.fn().mockResolvedValue({
          success: true,
          encryptedContent: '{"iv":"abc","ciphertext":"xyz","tag":"123"}',
        }),
      };

      const user = userEvent.setup();
      render(
        <MessageComposer {...defaultProps} encryptionService={mockEncryptionService as any} />
      );

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Secret message{Enter}');

      // Verify encryption was called
      expect(mockEncryptionService.encryptMessage).toHaveBeenCalledWith('conv-1', 'Secret message');

      // Verify onSendMessage was called with both plaintext and encrypted content
      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Secret message',
        '{"iv":"abc","ciphertext":"xyz","tag":"123"}',
        undefined,
        undefined
      );
    });

    it('sends plaintext when encryption service is not provided', async () => {
      const user = userEvent.setup();
      render(<MessageComposer {...defaultProps} />);

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Regular message{Enter}');

      // Verify onSendMessage was called with plaintext and undefined encrypted content
      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Regular message',
        undefined,
        undefined,
        undefined
      );
    });

    it('sends plaintext when encryption fails', async () => {
      const mockEncryptionService = {
        encryptMessage: vi.fn().mockResolvedValue({
          success: false,
          error: 'Encryption failed',
          encryptedContent: '',
        }),
      };

      const user = userEvent.setup();
      render(
        <MessageComposer {...defaultProps} encryptionService={mockEncryptionService as any} />
      );

      const textarea = screen.getByPlaceholderText('Type a message...');
      await user.type(textarea, 'Failed encryption{Enter}');

      // Verify encryption was attempted
      expect(mockEncryptionService.encryptMessage).toHaveBeenCalled();

      // Verify message was sent with undefined encrypted content (fallback)
      expect(mockOnSendMessage).toHaveBeenCalledWith(
        'Failed encryption',
        undefined,
        undefined,
        undefined
      );
    });
  });

  describe('Drag and Drop', () => {
    it('shows drag overlay when dragging files over the component', () => {
      render(<MessageComposer {...defaultProps} />);

      const composer = screen
        .getByPlaceholderText('Type a message...')
        .closest('div')?.parentElement;
      expect(composer).toBeInTheDocument();

      // Simulate drag enter with proper dataTransfer
      fireEvent.dragEnter(composer!, {
        dataTransfer: {
          items: [{ kind: 'file', type: 'image/png' }],
        },
      });

      // Drag overlay should appear
      expect(screen.getByText('Drop files here to attach')).toBeInTheDocument();
    });

    it('hides drag overlay when drag leaves', () => {
      render(<MessageComposer {...defaultProps} />);

      const composer = screen
        .getByPlaceholderText('Type a message...')
        .closest('div')?.parentElement;

      // Simulate drag enter
      fireEvent.dragEnter(composer!, {
        dataTransfer: {
          items: [{ kind: 'file', type: 'image/png' }],
        },
      });

      // Overlay should be visible
      expect(screen.getByText('Drop files here to attach')).toBeInTheDocument();

      // Simulate drag leave
      fireEvent.dragLeave(composer!, {
        dataTransfer: {
          items: [{ kind: 'file', type: 'image/png' }],
        },
      });

      // Overlay should be hidden
      expect(screen.queryByText('Drop files here to attach')).not.toBeInTheDocument();
    });

    it('attaches files when dropped', async () => {
      render(<MessageComposer {...defaultProps} />);

      const composer = screen
        .getByPlaceholderText('Type a message...')
        .closest('div')?.parentElement;

      const file = new File(['test'], 'test.png', { type: 'image/png' });

      // Simulate drop event with files
      fireEvent.drop(composer!, {
        dataTransfer: {
          files: [file],
        },
      });

      // File should be attached
      await waitFor(() => {
        expect(screen.getByText('test.png')).toBeInTheDocument();
      });
    });

    it('attaches multiple files when dropped', async () => {
      render(<MessageComposer {...defaultProps} />);

      const composer = screen
        .getByPlaceholderText('Type a message...')
        .closest('div')?.parentElement;

      const file1 = new File(['test1'], 'test1.png', { type: 'image/png' });
      const file2 = new File(['test2'], 'test2.jpg', { type: 'image/jpeg' });

      // Simulate drop event with multiple files
      fireEvent.drop(composer!, {
        dataTransfer: {
          files: [file1, file2],
        },
      });

      // Both files should be attached
      await waitFor(() => {
        expect(screen.getByText('test1.png')).toBeInTheDocument();
        expect(screen.getByText('test2.jpg')).toBeInTheDocument();
      });
    });
  });
});
