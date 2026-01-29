/**
 * Presence Store using Zustand
 *
 * Manages user presence/online status.
 * Syncs with SignalR events for real-time presence updates.
 */

import { create } from 'zustand';

export interface UserPresence {
  userId: string;
  isOnline: boolean;
  lastSeen: Date | null;
}

export interface PresenceState {
  onlineUsers: Record<string, UserPresence>; // userId -> presence
}

export interface PresenceActions {
  updatePresence: (userId: string, isOnline: boolean, lastSeen: Date | null) => void;
  setUserOnline: (userId: string) => void;
  setUserOffline: (userId: string, lastSeen: Date) => void;
  removeUser: (userId: string) => void;
  bulkUpdatePresence: (updates: UserPresence[]) => void;
  reset: () => void;
}

export type PresenceStore = PresenceState & PresenceActions;

const initialState: PresenceState = {
  onlineUsers: {},
};

export const usePresenceStore = create<PresenceStore>((set) => ({
  ...initialState,

  updatePresence: (userId, isOnline, lastSeen) => {
    set((state) => ({
      onlineUsers: {
        ...state.onlineUsers,
        [userId]: {
          userId,
          isOnline,
          lastSeen,
        },
      },
    }));
  },

  setUserOnline: (userId) => {
    set((state) => ({
      onlineUsers: {
        ...state.onlineUsers,
        [userId]: {
          userId,
          isOnline: true,
          lastSeen: null,
        },
      },
    }));
  },

  setUserOffline: (userId, lastSeen) => {
    set((state) => ({
      onlineUsers: {
        ...state.onlineUsers,
        [userId]: {
          userId,
          isOnline: false,
          lastSeen,
        },
      },
    }));
  },

  removeUser: (userId) => {
    set((state) => {
      const updatedUsers = { ...state.onlineUsers };
      delete updatedUsers[userId];
      return { onlineUsers: updatedUsers };
    });
  },

  bulkUpdatePresence: (updates) => {
    set((state) => {
      const updatedUsers = { ...state.onlineUsers };
      updates.forEach((update) => {
        updatedUsers[update.userId] = update;
      });
      return { onlineUsers: updatedUsers };
    });
  },

  reset: () => {
    set(initialState);
  },
}));
