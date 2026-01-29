/**
 * ConversationList Component
 *
 * Displays a list of user's conversations with search/filter functionality.
 * Shows conversations sorted by lastMessageAt, with unread counts and online indicators.
 */

import { useState, useMemo } from 'react';
import { useConversationStore } from '@/stores/useConversationStore';
import { usePresenceStore } from '@/stores/usePresenceStore';
import { useAuthStore } from '@/stores/useAuthStore';
import type { Conversation } from '@/stores/useConversationStore';

export interface ConversationListProps {
  onConversationSelect?: (conversationId: string) => void;
}

export function ConversationList({ onConversationSelect }: ConversationListProps) {
  const [searchQuery, setSearchQuery] = useState('');

  const conversations = useConversationStore((state) => state.conversations);
  const activeConversationId = useConversationStore((state) => state.activeConversationId);
  const setActiveConversation = useConversationStore((state) => state.setActiveConversation);
  const messages = useConversationStore((state) => state.messages);
  const onlineUsers = usePresenceStore((state) => state.onlineUsers);
  const currentUser = useAuthStore((state) => state.user);

  // Sort conversations by lastMessageAt (most recent first)
  const sortedConversations = useMemo(() => {
    return [...conversations].sort((a, b) => {
      const aTime = a.lastMessageAt ? new Date(a.lastMessageAt).getTime() : 0;
      const bTime = b.lastMessageAt ? new Date(b.lastMessageAt).getTime() : 0;
      return bTime - aTime;
    });
  }, [conversations]);

  // Filter conversations based on search query
  const filteredConversations = useMemo(() => {
    if (!searchQuery.trim()) {
      return sortedConversations;
    }

    const query = searchQuery.toLowerCase();
    return sortedConversations.filter((conversation) => {
      return conversation.name.toLowerCase().includes(query);
    });
  }, [sortedConversations, searchQuery]);

  const handleConversationClick = (conversationId: string) => {
    setActiveConversation(conversationId);
    onConversationSelect?.(conversationId);
  };

  const getUnreadCount = (conversation: Conversation): number => {
    if (!currentUser) return 0;

    const conversationMessages = messages[conversation.id] || [];

    // Count messages that are:
    // 1. Not sent by current user
    // 2. Not read by current user (not in readBy array)
    return conversationMessages.filter((msg) => {
      if (msg.senderId === currentUser.id) return false;
      const isReadByCurrentUser = msg.readBy.some((r) => r.userId === currentUser.id);
      return !isReadByCurrentUser;
    }).length;
  };

  const getLastMessagePreview = (conversation: Conversation): string => {
    const conversationMessages = messages[conversation.id] || [];
    if (conversationMessages.length === 0) {
      return 'No messages yet';
    }

    const lastMessage = conversationMessages[conversationMessages.length - 1];
    if (lastMessage.isDeleted) {
      return 'Message deleted';
    }

    // Truncate long messages
    const preview = lastMessage.content || lastMessage.encryptedContent || 'Message';
    return preview.length > 50 ? preview.substring(0, 50) + '...' : preview;
  };

  const getOtherUserId = (conversation: Conversation): string | null => {
    if (conversation.isGroup || !currentUser) return null;

    const otherParticipant = conversation.participants.find((p) => p.userId !== currentUser.id);
    return otherParticipant?.userId || null;
  };

  const isUserOnline = (userId: string | null): boolean => {
    if (!userId) return false;
    const presence = onlineUsers[userId];
    return presence?.isOnline || false;
  };

  const getConversationAvatar = (conversation: Conversation): string => {
    // Return first letter of conversation name
    return conversation.name.charAt(0).toUpperCase();
  };

  return (
    <div className="flex flex-col h-full bg-white">
      {/* Search input */}
      <div className="p-4 border-b border-gray-200">
        <input
          type="text"
          placeholder="Search conversations..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
        />
      </div>

      {/* Conversation list */}
      <div className="flex-1 overflow-y-auto">
        {filteredConversations.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full p-8 text-center">
            <div className="text-gray-400 mb-2">
              <svg
                className="w-16 h-16 mx-auto"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
                />
              </svg>
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-1">
              {searchQuery ? 'No conversations found' : 'No conversations yet'}
            </h3>
            <p className="text-sm text-gray-500">
              {searchQuery ? 'Try a different search term' : 'Start a conversation to get started'}
            </p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {filteredConversations.map((conversation) => {
              const otherUserId = getOtherUserId(conversation);
              const isOnline = !conversation.isGroup && isUserOnline(otherUserId);
              const unreadCount = getUnreadCount(conversation);
              const lastMessagePreview = getLastMessagePreview(conversation);
              const avatar = getConversationAvatar(conversation);
              const isActive = conversation.id === activeConversationId;

              return (
                <button
                  key={conversation.id}
                  onClick={() => handleConversationClick(conversation.id)}
                  className={`w-full text-left p-4 hover:bg-gray-50 transition-colors ${
                    isActive ? 'bg-indigo-50' : ''
                  }`}
                >
                  <div className="flex items-start space-x-3">
                    {/* Avatar */}
                    <div className="relative flex-shrink-0">
                      <div
                        className={`w-12 h-12 rounded-full flex items-center justify-center text-white font-semibold ${
                          isActive ? 'bg-indigo-600' : 'bg-gray-400'
                        }`}
                      >
                        {avatar}
                      </div>
                      {/* Online indicator for direct chats */}
                      {isOnline && (
                        <span className="absolute bottom-0 right-0 block h-3 w-3 rounded-full bg-green-400 ring-2 ring-white" />
                      )}
                    </div>

                    {/* Conversation info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between">
                        <h3
                          className={`text-sm font-medium truncate ${
                            isActive ? 'text-indigo-900' : 'text-gray-900'
                          }`}
                        >
                          {conversation.name}
                        </h3>
                        {unreadCount > 0 && (
                          <span className="ml-2 inline-flex items-center justify-center px-2 py-1 text-xs font-bold leading-none text-white bg-indigo-600 rounded-full">
                            {unreadCount}
                          </span>
                        )}
                      </div>
                      <p className="text-sm text-gray-500 truncate mt-1">{lastMessagePreview}</p>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
