/**
 * SignalR Store Integration Hook
 *
 * Connects Zustand stores with SignalR events for real-time synchronization.
 * Handles incoming SignalR events and updates the appropriate stores.
 */

import { useEffect } from 'react';
import { SignalRService } from '@services/signalr';
import { useConversationStore } from '@stores/useConversationStore';
import { usePresenceStore } from '@stores/usePresenceStore';
import type { Message } from '@stores/useConversationStore';

export interface UseSignalRStoreIntegrationOptions {
  signalRService: SignalRService;
  enabled?: boolean;
}

/**
 * Hook to integrate SignalR events with Zustand stores
 *
 * @param options Configuration options including the SignalR service instance
 */
export function useSignalRStoreIntegration({
  signalRService,
  enabled = true,
}: UseSignalRStoreIntegrationOptions): void {
  const addMessage = useConversationStore((state) => state.addMessage);
  const markMessageAsRead = useConversationStore((state) => state.markMessageAsRead);
  const updatePresence = usePresenceStore((state) => state.updatePresence);
  const setUserOnline = usePresenceStore((state) => state.setUserOnline);
  const setUserOffline = usePresenceStore((state) => state.setUserOffline);

  useEffect(() => {
    if (!enabled) return;

    // Register SignalR event handlers to sync with stores
    signalRService.on({
      onReceiveMessage: (
        conversationId: string,
        messageId: string,
        senderId: string,
        _senderName: string,
        content: string,
        timestamp: Date
      ) => {
        const message: Message = {
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
        };
        addMessage(conversationId, message);
      },

      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      onUserJoined: (_conversationId: string, userId: string, _userName: string) => {
        // When a user joins a conversation, mark them as online
        setUserOnline(userId);
      },

      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      onUserLeft: (_conversationId: string, userId: string, _userName: string) => {
        // When a user leaves a conversation, mark them as offline
        setUserOffline(userId, new Date());
      },

      onMessageRead: (conversationId: string, messageId: string, userId: string) => {
        markMessageAsRead(conversationId, messageId, userId, new Date());
      },

      onPresenceUpdate: (userId: string, isOnline: boolean, lastSeen: Date | null) => {
        updatePresence(userId, isOnline, lastSeen);
      },

      onTypingIndicator: () => {
        // Typing indicators could be added to conversation store if needed
        // For now, this is just a placeholder for the event handler
        // Parameters available: conversationId, userId, userName, isTyping
      },
    });

    // No cleanup needed as SignalR service manages its own handlers
  }, [
    signalRService,
    enabled,
    addMessage,
    markMessageAsRead,
    updatePresence,
    setUserOnline,
    setUserOffline,
  ]);
}
