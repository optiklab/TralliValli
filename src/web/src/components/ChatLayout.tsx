/**
 * ChatLayout Component
 *
 * Main chat interface with sidebar for conversations and main area for messages.
 * Telegram-like layout with conversation list on the left and message thread on the right.
 */

import { useState } from 'react';
import { ConversationList } from './ConversationList';
import { MessageThread } from './MessageThread';
import { MessageComposer } from './MessageComposer';
import { InviteModal } from './InviteModal';
import { useConversationStore } from '@/stores/useConversationStore';
import { useAuthStore } from '@/stores/useAuthStore';

export function ChatLayout() {
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [showMobileConversations, setShowMobileConversations] = useState(true);
  
  const activeConversationId = useConversationStore((state) => state.activeConversationId);
  const conversations = useConversationStore((state) => state.conversations);
  const addMessage = useConversationStore((state) => state.addMessage);
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);

  const activeConversation = conversations.find(c => c.id === activeConversationId);

  const handleSendMessage = (content: string, encryptedContent?: string, _files?: File[], replyToId?: string) => {
    if (!activeConversationId || !user) return;

    // Create a new message
    const newMessage = {
      id: `msg-${Date.now()}`,
      conversationId: activeConversationId,
      senderId: user.id,
      content,
      encryptedContent: encryptedContent || '',
      type: 'text' as const,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      readBy: [],
      replyToId,
      isDeleted: false,
      attachments: [],
    };

    addMessage(activeConversationId, newMessage);

    // TODO: Send via SignalR/API
  };

  const handleConversationSelect = () => {
    // On mobile, hide conversation list when a conversation is selected
    setShowMobileConversations(false);
  };

  return (
    <div className="h-screen flex flex-col bg-gray-100">
      {/* Header */}
      <header className="bg-indigo-600 text-white px-4 py-3 flex items-center justify-between shadow-md">
        <div className="flex items-center space-x-3">
          {/* Mobile back button */}
          {!showMobileConversations && activeConversationId && (
            <button
              onClick={() => setShowMobileConversations(true)}
              className="md:hidden p-1 hover:bg-indigo-500 rounded"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
            </button>
          )}
          <h1 className="text-xl font-bold">TralliVali</h1>
        </div>
        
        <div className="flex items-center space-x-3">
          <button
            onClick={() => setShowInviteModal(true)}
            className="px-3 py-1.5 bg-indigo-500 hover:bg-indigo-400 rounded-md text-sm font-medium transition-colors"
          >
            Invite User
          </button>
          <div className="text-sm">
            {user?.displayName || user?.email}
          </div>
          <button
            onClick={logout}
            className="px-3 py-1.5 bg-indigo-500 hover:bg-indigo-400 rounded-md text-sm font-medium transition-colors"
          >
            Logout
          </button>
        </div>
      </header>

      {/* Main Content */}
      <div className="flex-1 flex overflow-hidden">
        {/* Conversation List Sidebar */}
        <aside 
          className={`w-full md:w-80 lg:w-96 bg-white border-r border-gray-200 flex-shrink-0 ${
            showMobileConversations ? 'block' : 'hidden md:block'
          }`}
        >
          <div className="h-full flex flex-col">
            <div className="p-4 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-800">Conversations</h2>
            </div>
            <div className="flex-1 overflow-y-auto">
              <ConversationList onConversationSelect={handleConversationSelect} />
            </div>
            {conversations.length === 0 && (
              <div className="p-4 text-center text-gray-500">
                <p className="mb-4">No conversations yet.</p>
                <p className="text-sm">Invite users to start chatting!</p>
                <button
                  onClick={() => setShowInviteModal(true)}
                  className="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 text-sm"
                >
                  Generate Invite Link
                </button>
              </div>
            )}
          </div>
        </aside>

        {/* Message Area */}
        <main 
          className={`flex-1 flex flex-col bg-gray-50 ${
            !showMobileConversations || !activeConversationId ? 'block' : 'hidden md:flex'
          }`}
        >
          {activeConversationId && activeConversation ? (
            <>
              {/* Conversation Header */}
              <div className="bg-white border-b border-gray-200 px-4 py-3">
                <h3 className="font-semibold text-gray-800">{activeConversation.name}</h3>
                <p className="text-sm text-gray-500">
                  {activeConversation.participants?.length || 0} participants
                </p>
              </div>

              {/* Messages */}
              <div className="flex-1 overflow-hidden">
                <MessageThread conversationId={activeConversationId} />
              </div>

              {/* Composer */}
              <div className="bg-white border-t border-gray-200">
                <MessageComposer
                  conversationId={activeConversationId}
                  onSendMessage={handleSendMessage}
                />
              </div>
            </>
          ) : (
            <div className="flex-1 flex items-center justify-center text-gray-500">
              <div className="text-center">
                <svg className="mx-auto h-16 w-16 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                </svg>
                <p className="text-lg">Select a conversation to start chatting</p>
                <p className="text-sm mt-2">Or invite someone to chat with you!</p>
              </div>
            </div>
          )}
        </main>
      </div>

      {/* Invite Modal */}
      {showInviteModal && (
        <InviteModal onClose={() => setShowInviteModal(false)} />
      )}
    </div>
  );
}
