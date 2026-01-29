/**
 * MessageThread Component
 *
 * Displays a thread of messages with:
 * - Sender avatar/name
 * - Message content
 * - Timestamp
 * - Read receipts
 * - Reply threading
 * - Support for text and file messages
 * - Infinite scroll for loading older messages
 * - Typing indicators
 * - Auto-scroll to new messages
 */

import { useEffect, useRef, useState, useCallback, useMemo } from 'react';
import { useConversationStore } from '@/stores/useConversationStore';
import { useAuthStore } from '@/stores/useAuthStore';
import type { Message } from '@/stores/useConversationStore';

export interface MessageThreadProps {
  conversationId: string;
  onLoadMore?: (cursor?: string) => Promise<void>;
  hasMore?: boolean;
  typingUsers?: Array<{ userId: string; userName: string }>;
}

interface MessageItemProps {
  message: Message;
  isOwnMessage: boolean;
  senderName?: string;
  showAvatar?: boolean;
  onReply?: (messageId: string) => void;
}

function MessageItem({ message, isOwnMessage, senderName, showAvatar, onReply }: MessageItemProps) {
  const getReadByCount = () => {
    // Don't count the sender in read receipts
    return message.readBy.filter((r) => r.userId !== message.senderId).length;
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    
    return date.toLocaleDateString();
  };

  const renderMessageContent = () => {
    if (message.isDeleted) {
      return <span className="italic text-gray-400">Message deleted</span>;
    }

    const content = message.content || message.encryptedContent;
    
    // Check if message has attachments (file message)
    if (message.attachments && message.attachments.length > 0) {
      return (
        <div className="space-y-2">
          {content && <p className="whitespace-pre-wrap break-words">{content}</p>}
          <div className="space-y-1">
            {message.attachments.map((attachment, index) => (
              <div
                key={index}
                className="flex items-center space-x-2 p-2 bg-gray-100 rounded border border-gray-200"
              >
                <svg
                  className="w-5 h-5 text-gray-500"
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
                <span className="text-sm text-gray-700 truncate">{attachment}</span>
              </div>
            ))}
          </div>
        </div>
      );
    }

    return <p className="whitespace-pre-wrap break-words">{content}</p>;
  };

  return (
    <div className={`flex ${isOwnMessage ? 'justify-end' : 'justify-start'} mb-4`}>
      <div className={`flex ${isOwnMessage ? 'flex-row-reverse' : 'flex-row'} max-w-[70%]`}>
        {/* Avatar */}
        {showAvatar && !isOwnMessage && (
          <div className="flex-shrink-0 mr-2">
            <div className="w-8 h-8 rounded-full bg-gray-400 flex items-center justify-center text-white text-sm font-semibold">
              {senderName?.charAt(0).toUpperCase() || '?'}
            </div>
          </div>
        )}
        {showAvatar && isOwnMessage && <div className="w-8 ml-2" />}

        {/* Message bubble */}
        <div className="flex flex-col">
          {/* Sender name (only for other users' messages) */}
          {!isOwnMessage && senderName && (
            <span className="text-xs text-gray-600 mb-1 ml-2">{senderName}</span>
          )}

          {/* Reply indicator */}
          {message.replyTo && (
            <div className="text-xs text-gray-500 mb-1 ml-2 italic">
              Replying to previous message
            </div>
          )}

          {/* Message content */}
          <div
            className={`rounded-lg px-4 py-2 ${
              isOwnMessage
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-100 text-gray-900'
            }`}
          >
            {renderMessageContent()}
          </div>

          {/* Timestamp and read receipts */}
          <div className={`flex items-center mt-1 space-x-2 ${isOwnMessage ? 'justify-end' : 'justify-start'}`}>
            <span className="text-xs text-gray-500">{formatTimestamp(message.createdAt)}</span>
            {message.editedAt && (
              <span className="text-xs text-gray-500 italic">(edited)</span>
            )}
            {isOwnMessage && getReadByCount() > 0 && (
              <span className="text-xs text-gray-500">
                ✓✓ {getReadByCount()}
              </span>
            )}
          </div>

          {/* Reply button */}
          {!message.isDeleted && onReply && (
            <button
              onClick={() => onReply(message.id)}
              className="text-xs text-gray-500 hover:text-indigo-600 mt-1 self-start"
            >
              Reply
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function TypingIndicator({ users }: { users: Array<{ userId: string; userName: string }> }) {
  if (users.length === 0) return null;

  const displayText =
    users.length === 1
      ? `${users[0].userName} is typing...`
      : users.length === 2
      ? `${users[0].userName} and ${users[1].userName} are typing...`
      : `${users[0].userName} and ${users.length - 1} others are typing...`;

  return (
    <div className="flex items-center space-x-2 px-4 py-2 text-sm text-gray-500">
      <div className="flex space-x-1">
        <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
        <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
        <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
      </div>
      <span>{displayText}</span>
    </div>
  );
}

export function MessageThread({
  conversationId,
  onLoadMore,
  hasMore = false,
  typingUsers = [],
}: MessageThreadProps) {
  const messagesFromStore = useConversationStore((state) => state.messages[conversationId]);
  const messages = useMemo(() => messagesFromStore || [], [messagesFromStore]);
  const currentUser = useAuthStore((state) => state.user);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [shouldAutoScroll, setShouldAutoScroll] = useState(true);
  const previousMessageCountRef = useRef(messages.length);

  // Sort messages by createdAt
  const sortedMessages = useMemo(() => {
    return [...messages].sort((a, b) => {
      return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
    });
  }, [messages]);

  // Auto-scroll to bottom on new messages
  useEffect(() => {
    const hasNewMessages = messages.length > previousMessageCountRef.current;
    
    if (hasNewMessages && shouldAutoScroll) {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
    
    previousMessageCountRef.current = messages.length;
  }, [messages, shouldAutoScroll]);

  // Check if user is near bottom of scroll container
  const handleScroll = useCallback(() => {
    if (!scrollContainerRef.current) return;

    const { scrollTop, scrollHeight, clientHeight } = scrollContainerRef.current;
    const distanceFromBottom = scrollHeight - scrollTop - clientHeight;
    
    // Enable auto-scroll if within 100px of bottom
    setShouldAutoScroll(distanceFromBottom < 100);

    // Load more messages when scrolling to top
    if (scrollTop === 0 && hasMore && !isLoadingMore && onLoadMore) {
      setIsLoadingMore(true);
      const oldScrollHeight = scrollHeight;
      
      onLoadMore().then(() => {
        setIsLoadingMore(false);
        // Maintain scroll position after loading
        if (scrollContainerRef.current) {
          const newScrollHeight = scrollContainerRef.current.scrollHeight;
          scrollContainerRef.current.scrollTop = newScrollHeight - oldScrollHeight;
        }
      }).catch(() => {
        setIsLoadingMore(false);
      });
    }
  }, [hasMore, isLoadingMore, onLoadMore]);

  // Get sender name from conversation participants
  const getSenderName = (senderId: string): string => {
    // In a real app, you'd look this up from participants or a user store
    // For now, return a placeholder
    return senderId === currentUser?.id ? 'You' : 'User';
  };

  // Determine if avatar should be shown (first message in group from same sender)
  const shouldShowAvatar = (index: number): boolean => {
    if (index === 0) return true;
    const currentMessage = sortedMessages[index];
    const previousMessage = sortedMessages[index - 1];
    return currentMessage.senderId !== previousMessage.senderId;
  };

  const handleReply = (messageId: string) => {
    // This would typically trigger a reply UI
    // For now, just a placeholder
    console.log('Reply to message:', messageId);
  };

  if (!currentUser) {
    return (
      <div className="flex items-center justify-center h-full text-gray-500">
        Please log in to view messages
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full bg-white">
      {/* Messages container */}
      <div
        ref={scrollContainerRef}
        onScroll={handleScroll}
        className="flex-1 overflow-y-auto p-4"
      >
        {/* Load more indicator */}
        {isLoadingMore && (
          <div className="flex justify-center py-4">
            <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600" />
          </div>
        )}

        {hasMore && !isLoadingMore && (
          <div className="flex justify-center py-4">
            <span className="text-sm text-gray-500">Scroll up to load more</span>
          </div>
        )}

        {/* Empty state */}
        {sortedMessages.length === 0 && (
          <div className="flex flex-col items-center justify-center h-full text-center">
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
            <h3 className="text-lg font-medium text-gray-900 mb-1">No messages yet</h3>
            <p className="text-sm text-gray-500">Start the conversation!</p>
          </div>
        )}

        {/* Messages */}
        {sortedMessages.map((message, index) => (
          <MessageItem
            key={message.id}
            message={message}
            isOwnMessage={message.senderId === currentUser.id}
            senderName={getSenderName(message.senderId)}
            showAvatar={shouldShowAvatar(index)}
            onReply={handleReply}
          />
        ))}

        {/* Typing indicator */}
        <TypingIndicator users={typingUsers} />

        {/* Scroll anchor */}
        <div ref={messagesEndRef} />
      </div>
    </div>
  );
}
