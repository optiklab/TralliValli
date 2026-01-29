// Export Zustand state management stores
export { useAuthStore } from './useAuthStore';
export type { AuthStore, AuthState, AuthActions, User } from './useAuthStore';

export { useConversationStore } from './useConversationStore';
export type {
  ConversationStore,
  ConversationState,
  ConversationActions,
  Conversation,
  Message,
} from './useConversationStore';

export { usePresenceStore } from './usePresenceStore';
export type {
  PresenceStore,
  PresenceState,
  PresenceActions,
  UserPresence,
} from './usePresenceStore';

export { useThemeStore } from './useThemeStore';
export type { ThemeStore, ThemeState, ThemeActions, Theme } from './useThemeStore';
