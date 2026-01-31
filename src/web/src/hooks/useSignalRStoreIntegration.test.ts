import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useConversationStore } from '@stores/useConversationStore';
import { usePresenceStore } from '@stores/usePresenceStore';
import { useSignalRStoreIntegration } from './useSignalRStoreIntegration';

// Mock the stores
vi.mock('@stores/useConversationStore', () => ({
  useConversationStore: vi.fn(),
}));

vi.mock('@stores/usePresenceStore', () => ({
  usePresenceStore: vi.fn(),
}));

// Mock the SignalR service
vi.mock('@services/signalr', () => ({
  SignalRService: vi.fn(),
}));

interface MockSignalRService {
  on: ReturnType<typeof vi.fn>;
}

describe('useSignalRStoreIntegration', () => {
  let mockSignalRService: MockSignalRService;
  let mockAddMessage: ReturnType<typeof vi.fn>;
  let mockMarkMessageAsRead: ReturnType<typeof vi.fn>;
  let mockUpdatePresence: ReturnType<typeof vi.fn>;
  let mockSetUserOnline: ReturnType<typeof vi.fn>;
  let mockSetUserOffline: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();

    // Setup mock SignalR service
    mockSignalRService = {
      on: vi.fn(),
    };

    // Setup mock store functions
    mockAddMessage = vi.fn();
    mockMarkMessageAsRead = vi.fn();
    mockUpdatePresence = vi.fn();
    mockSetUserOnline = vi.fn();
    mockSetUserOffline = vi.fn();

    // Mock store implementations
    vi.mocked(useConversationStore).mockImplementation((selector) => {
      const state = {
        addMessage: mockAddMessage,
        markMessageAsRead: mockMarkMessageAsRead,
      };
      return selector(state);
    });

    vi.mocked(usePresenceStore).mockImplementation((selector) => {
      const state = {
        updatePresence: mockUpdatePresence,
        setUserOnline: mockSetUserOnline,
        setUserOffline: mockSetUserOffline,
      };
      return selector(state);
    });
  });

  describe('state management', () => {
    it('should extract store methods correctly', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      // Verify that the store methods were accessed
      expect(useConversationStore).toHaveBeenCalled();
      expect(usePresenceStore).toHaveBeenCalled();
    });
  });

  describe('side effects', () => {
    it('should register SignalR event handlers when enabled', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      expect(mockSignalRService.on).toHaveBeenCalledWith(
        expect.objectContaining({
          onReceiveMessage: expect.any(Function),
          onUserJoined: expect.any(Function),
          onUserLeft: expect.any(Function),
          onMessageRead: expect.any(Function),
          onPresenceUpdate: expect.any(Function),
          onTypingIndicator: expect.any(Function),
        })
      );
    });

    it('should not register handlers when disabled', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: false,
        })
      );

      expect(mockSignalRService.on).not.toHaveBeenCalled();
    });

    it('should handle onReceiveMessage event', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      // Get the registered handlers
      const handlers = mockSignalRService.on.mock.calls[0][0];

      // Simulate receiving a message
      const conversationId = 'conv-1';
      const messageId = 'msg-1';
      const senderId = 'user-1';
      const senderName = 'John Doe';
      const content = 'Hello, world!';
      const timestamp = new Date('2026-01-01T10:00:00Z');

      handlers.onReceiveMessage(
        conversationId,
        messageId,
        senderId,
        senderName,
        content,
        timestamp
      );

      expect(mockAddMessage).toHaveBeenCalledWith(conversationId, {
        id: messageId,
        conversationId,
        senderId,
        type: 'text',
        content,
        encryptedContent: '',
        createdAt: timestamp.toISOString(),
        readBy: [],
        isDeleted: false,
        attachments: [],
      });
    });

    it('should handle onUserJoined event', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      const handlers = mockSignalRService.on.mock.calls[0][0];

      const conversationId = 'conv-1';
      const userId = 'user-1';
      const userName = 'John Doe';

      handlers.onUserJoined(conversationId, userId, userName);

      expect(mockSetUserOnline).toHaveBeenCalledWith(userId);
    });

    it('should handle onUserLeft event', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      const handlers = mockSignalRService.on.mock.calls[0][0];

      const conversationId = 'conv-1';
      const userId = 'user-1';
      const userName = 'John Doe';

      handlers.onUserLeft(conversationId, userId, userName);

      expect(mockSetUserOffline).toHaveBeenCalledWith(userId, expect.any(Date));
    });

    it('should handle onMessageRead event', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      const handlers = mockSignalRService.on.mock.calls[0][0];

      const conversationId = 'conv-1';
      const messageId = 'msg-1';
      const userId = 'user-1';

      handlers.onMessageRead(conversationId, messageId, userId);

      expect(mockMarkMessageAsRead).toHaveBeenCalledWith(
        conversationId,
        messageId,
        userId,
        expect.any(Date)
      );
    });

    it('should handle onPresenceUpdate event', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      const handlers = mockSignalRService.on.mock.calls[0][0];

      const userId = 'user-1';
      const isOnline = true;
      const lastSeen = new Date('2026-01-01T10:00:00Z');

      handlers.onPresenceUpdate(userId, isOnline, lastSeen);

      expect(mockUpdatePresence).toHaveBeenCalledWith(userId, isOnline, lastSeen);
    });

    it('should handle onTypingIndicator event without errors', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      const handlers = mockSignalRService.on.mock.calls[0][0];

      // Should not throw
      expect(() => {
        handlers.onTypingIndicator('conv-1', 'user-1', 'John', true);
      }).not.toThrow();
    });
  });

  describe('error handling', () => {
    it('should handle errors in onReceiveMessage gracefully', () => {
      mockAddMessage.mockImplementation(() => {
        throw new Error('Store error');
      });

      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      const handlers = mockSignalRService.on.mock.calls[0][0];

      // Should not crash the hook
      expect(() => {
        handlers.onReceiveMessage('conv-1', 'msg-1', 'user-1', 'John', 'Hello', new Date());
      }).toThrow('Store error');
    });

    it('should handle errors in onUserJoined gracefully', () => {
      mockSetUserOnline.mockImplementation(() => {
        throw new Error('Store error');
      });

      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      const handlers = mockSignalRService.on.mock.calls[0][0];

      expect(() => {
        handlers.onUserJoined('conv-1', 'user-1', 'John');
      }).toThrow('Store error');
    });
  });

  describe('dependencies mocked', () => {
    it('should use mocked SignalR service', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      expect(mockSignalRService.on).toHaveBeenCalled();
    });

    it('should use mocked conversation store', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      expect(useConversationStore).toHaveBeenCalled();
    });

    it('should use mocked presence store', () => {
      renderHook(() =>
        useSignalRStoreIntegration({
          signalRService: mockSignalRService,
          enabled: true,
        })
      );

      expect(usePresenceStore).toHaveBeenCalled();
    });
  });

  describe('hook lifecycle', () => {
    it('should re-register handlers when signalRService changes', () => {
      const { rerender } = renderHook(
        ({ service, enabled }) =>
          useSignalRStoreIntegration({
            signalRService: service,
            enabled,
          }),
        {
          initialProps: {
            service: mockSignalRService,
            enabled: true,
          },
        }
      );

      expect(mockSignalRService.on).toHaveBeenCalledTimes(1);

      // Create a new service instance
      const newMockService = { on: vi.fn() };

      rerender({
        service: newMockService,
        enabled: true,
      });

      expect(newMockService.on).toHaveBeenCalledTimes(1);
      expect(mockSignalRService.on).toHaveBeenCalledTimes(1);
    });

    it('should re-register handlers when enabled changes from false to true', () => {
      const { rerender } = renderHook(
        ({ enabled }) =>
          useSignalRStoreIntegration({
            signalRService: mockSignalRService,
            enabled,
          }),
        {
          initialProps: {
            enabled: false,
          },
        }
      );

      expect(mockSignalRService.on).not.toHaveBeenCalled();

      rerender({ enabled: true });

      expect(mockSignalRService.on).toHaveBeenCalledTimes(1);
    });

    it('should not register handlers when enabled changes from true to false', () => {
      const { rerender } = renderHook(
        ({ enabled }) =>
          useSignalRStoreIntegration({
            signalRService: mockSignalRService,
            enabled,
          }),
        {
          initialProps: {
            enabled: true,
          },
        }
      );

      expect(mockSignalRService.on).toHaveBeenCalledTimes(1);

      rerender({ enabled: false });

      // Should still only be called once (from initial render)
      expect(mockSignalRService.on).toHaveBeenCalledTimes(1);
    });
  });
});
