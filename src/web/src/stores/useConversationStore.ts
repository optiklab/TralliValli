/**
 * Conversation Store using Zustand
 *
 * Manages conversation and message state.
 * Syncs with SignalR events for real-time updates.
 */

import { create } from 'zustand';
import type { ConversationResponse, MessageResponse } from '@/types/api';

// Type alias for Message - currently same as MessageResponse
// but allows for future client-side extensions
export type Message = MessageResponse;

// Type alias for Conversation - currently same as ConversationResponse
// but allows for future client-side extensions
export type Conversation = ConversationResponse;

export interface ConversationState {
  conversations: Conversation[];
  activeConversationId: string | null;
  messages: Record<string, Message[]>; // conversationId -> messages
  loading: boolean;
  error: string | null;
}

export interface ConversationActions {
  loadConversations: (conversations: Conversation[]) => void;
  loadMessages: (conversationId: string, messages: Message[]) => void;
  addMessage: (conversationId: string, message: Message) => void;
  setActiveConversation: (conversationId: string | null) => void;
  updateConversation: (conversation: Conversation) => void;
  removeConversation: (conversationId: string) => void;
  updateMessage: (conversationId: string, messageId: string, updates: Partial<Message>) => void;
  deleteMessage: (conversationId: string, messageId: string) => void;
  markMessageAsRead: (
    conversationId: string,
    messageId: string,
    userId: string,
    readAt: Date
  ) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  clearMessages: (conversationId: string) => void;
  reset: () => void;
}

export type ConversationStore = ConversationState & ConversationActions;

const initialState: ConversationState = {
  conversations: [],
  activeConversationId: null,
  messages: {},
  loading: false,
  error: null,
};

export const useConversationStore = create<ConversationStore>((set) => ({
  ...initialState,

  loadConversations: (conversations) => {
    set({
      conversations,
      error: null,
    });
  },

  loadMessages: (conversationId, messages) => {
    set((state) => ({
      messages: {
        ...state.messages,
        [conversationId]: messages,
      },
      error: null,
    }));
  },

  addMessage: (conversationId, message) => {
    set((state) => {
      const existingMessages = state.messages[conversationId] || [];

      // Check if message already exists to avoid duplicates
      const messageExists = existingMessages.some((m) => m.id === message.id);
      if (messageExists) {
        return state;
      }

      const updatedMessages = [...existingMessages, message];

      // Update the conversation's lastMessageAt
      const updatedConversations = state.conversations.map((conv) => {
        if (conv.id === conversationId) {
          return {
            ...conv,
            lastMessageAt: message.createdAt,
          };
        }
        return conv;
      });

      return {
        messages: {
          ...state.messages,
          [conversationId]: updatedMessages,
        },
        conversations: updatedConversations,
      };
    });
  },

  setActiveConversation: (conversationId) => {
    set({
      activeConversationId: conversationId,
    });
  },

  updateConversation: (conversation) => {
    set((state) => {
      const existingIndex = state.conversations.findIndex((c) => c.id === conversation.id);

      if (existingIndex >= 0) {
        const updatedConversations = [...state.conversations];
        updatedConversations[existingIndex] = conversation;
        return { conversations: updatedConversations };
      }

      return { conversations: [...state.conversations, conversation] };
    });
  },

  removeConversation: (conversationId) => {
    set((state) => {
      const updatedMessages = { ...state.messages };
      delete updatedMessages[conversationId];

      return {
        conversations: state.conversations.filter((c) => c.id !== conversationId),
        messages: updatedMessages,
        activeConversationId:
          state.activeConversationId === conversationId ? null : state.activeConversationId,
      };
    });
  },

  updateMessage: (conversationId, messageId, updates) => {
    set((state) => {
      const conversationMessages = state.messages[conversationId];
      if (!conversationMessages) return state;

      const updatedMessages = conversationMessages.map((msg) =>
        msg.id === messageId ? { ...msg, ...updates } : msg
      );

      return {
        messages: {
          ...state.messages,
          [conversationId]: updatedMessages,
        },
      };
    });
  },

  deleteMessage: (conversationId, messageId) => {
    set((state) => {
      const conversationMessages = state.messages[conversationId];
      if (!conversationMessages) return state;

      const updatedMessages = conversationMessages.map((msg) =>
        msg.id === messageId ? { ...msg, isDeleted: true } : msg
      );

      return {
        messages: {
          ...state.messages,
          [conversationId]: updatedMessages,
        },
      };
    });
  },

  markMessageAsRead: (conversationId, messageId, userId, readAt) => {
    set((state) => {
      const conversationMessages = state.messages[conversationId];
      if (!conversationMessages) return state;

      const updatedMessages = conversationMessages.map((msg) => {
        if (msg.id === messageId) {
          // Check if user already marked as read
          const alreadyRead = msg.readBy.some((r) => r.userId === userId);
          if (alreadyRead) return msg;

          return {
            ...msg,
            readBy: [...msg.readBy, { userId, readAt: readAt.toISOString() }],
          };
        }
        return msg;
      });

      return {
        messages: {
          ...state.messages,
          [conversationId]: updatedMessages,
        },
      };
    });
  },

  setLoading: (loading) => {
    set({ loading });
  },

  setError: (error) => {
    set({ error });
  },

  clearMessages: (conversationId) => {
    set((state) => {
      const updatedMessages = { ...state.messages };
      delete updatedMessages[conversationId];
      return { messages: updatedMessages };
    });
  },

  reset: () => {
    set(initialState);
  },
}));
