import { describe, it, expect, beforeEach } from 'vitest';
import { usePresenceStore } from './usePresenceStore';
import type { UserPresence } from './usePresenceStore';

describe('usePresenceStore', () => {
  beforeEach(() => {
    // Reset the store before each test
    usePresenceStore.getState().reset();
  });

  describe('initial state', () => {
    it('should have empty initial state', () => {
      const state = usePresenceStore.getState();
      expect(state.onlineUsers).toEqual({});
    });
  });

  describe('updatePresence', () => {
    it('should update user presence with online status', () => {
      const userId = 'user-1';
      const isOnline = true;
      const lastSeen = null;

      usePresenceStore.getState().updatePresence(userId, isOnline, lastSeen);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual({
        userId,
        isOnline,
        lastSeen,
      });
    });

    it('should update user presence with offline status and lastSeen', () => {
      const userId = 'user-2';
      const isOnline = false;
      const lastSeen = new Date('2026-01-01T10:00:00Z');

      usePresenceStore.getState().updatePresence(userId, isOnline, lastSeen);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual({
        userId,
        isOnline,
        lastSeen,
      });
    });

    it('should update existing user presence', () => {
      const userId = 'user-3';

      // First set user as online
      usePresenceStore.getState().updatePresence(userId, true, null);

      // Then set user as offline
      const lastSeen = new Date('2026-01-01T10:00:00Z');
      usePresenceStore.getState().updatePresence(userId, false, lastSeen);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual({
        userId,
        isOnline: false,
        lastSeen,
      });
    });
  });

  describe('setUserOnline', () => {
    it('should set user as online', () => {
      const userId = 'user-4';

      usePresenceStore.getState().setUserOnline(userId);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual({
        userId,
        isOnline: true,
        lastSeen: null,
      });
    });

    it('should update offline user to online', () => {
      const userId = 'user-5';
      const lastSeen = new Date('2026-01-01T10:00:00Z');

      // First set user as offline
      usePresenceStore.getState().updatePresence(userId, false, lastSeen);

      // Then set user as online
      usePresenceStore.getState().setUserOnline(userId);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual({
        userId,
        isOnline: true,
        lastSeen: null,
      });
    });
  });

  describe('setUserOffline', () => {
    it('should set user as offline with lastSeen', () => {
      const userId = 'user-6';
      const lastSeen = new Date('2026-01-01T10:00:00Z');

      usePresenceStore.getState().setUserOffline(userId, lastSeen);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual({
        userId,
        isOnline: false,
        lastSeen,
      });
    });

    it('should update online user to offline', () => {
      const userId = 'user-7';

      // First set user as online
      usePresenceStore.getState().setUserOnline(userId);

      // Then set user as offline
      const lastSeen = new Date('2026-01-01T10:00:00Z');
      usePresenceStore.getState().setUserOffline(userId, lastSeen);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual({
        userId,
        isOnline: false,
        lastSeen,
      });
    });
  });

  describe('removeUser', () => {
    it('should remove user from presence tracking', () => {
      const userId = 'user-8';

      // First add user
      usePresenceStore.getState().setUserOnline(userId);
      expect(usePresenceStore.getState().onlineUsers[userId]).toBeDefined();

      // Then remove user
      usePresenceStore.getState().removeUser(userId);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toBeUndefined();
    });

    it('should not error when removing non-existent user', () => {
      const userId = 'non-existent-user';

      expect(() => {
        usePresenceStore.getState().removeUser(userId);
      }).not.toThrow();

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toBeUndefined();
    });
  });

  describe('bulkUpdatePresence', () => {
    it('should update multiple users at once', () => {
      const updates: UserPresence[] = [
        {
          userId: 'user-9',
          isOnline: true,
          lastSeen: null,
        },
        {
          userId: 'user-10',
          isOnline: false,
          lastSeen: new Date('2026-01-01T10:00:00Z'),
        },
        {
          userId: 'user-11',
          isOnline: true,
          lastSeen: null,
        },
      ];

      usePresenceStore.getState().bulkUpdatePresence(updates);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers['user-9']).toEqual(updates[0]);
      expect(state.onlineUsers['user-10']).toEqual(updates[1]);
      expect(state.onlineUsers['user-11']).toEqual(updates[2]);
    });

    it('should update existing users in bulk update', () => {
      const userId = 'user-12';

      // First set user as online
      usePresenceStore.getState().setUserOnline(userId);

      // Then bulk update to set as offline
      const updates: UserPresence[] = [
        {
          userId,
          isOnline: false,
          lastSeen: new Date('2026-01-01T10:00:00Z'),
        },
      ];

      usePresenceStore.getState().bulkUpdatePresence(updates);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers[userId]).toEqual(updates[0]);
    });

    it('should handle empty bulk update', () => {
      usePresenceStore.getState().bulkUpdatePresence([]);

      const state = usePresenceStore.getState();
      expect(state.onlineUsers).toEqual({});
    });
  });

  describe('reset', () => {
    it('should reset store to initial state', () => {
      // Add some users
      usePresenceStore.getState().setUserOnline('user-13');
      usePresenceStore.getState().setUserOnline('user-14');
      usePresenceStore.getState().setUserOffline('user-15', new Date());

      expect(Object.keys(usePresenceStore.getState().onlineUsers)).toHaveLength(3);

      // Reset
      usePresenceStore.getState().reset();

      const state = usePresenceStore.getState();
      expect(state.onlineUsers).toEqual({});
    });
  });

  describe('multiple user presence tracking', () => {
    it('should track presence of multiple users independently', () => {
      const user1 = 'user-16';
      const user2 = 'user-17';
      const user3 = 'user-18';

      usePresenceStore.getState().setUserOnline(user1);
      usePresenceStore.getState().setUserOffline(user2, new Date('2026-01-01T10:00:00Z'));
      usePresenceStore.getState().setUserOnline(user3);

      const state = usePresenceStore.getState();

      expect(state.onlineUsers[user1].isOnline).toBe(true);
      expect(state.onlineUsers[user2].isOnline).toBe(false);
      expect(state.onlineUsers[user3].isOnline).toBe(true);

      expect(Object.keys(state.onlineUsers)).toHaveLength(3);
    });

    it('should maintain other users when updating one user', () => {
      const user1 = 'user-19';
      const user2 = 'user-20';

      usePresenceStore.getState().setUserOnline(user1);
      usePresenceStore.getState().setUserOnline(user2);

      // Update user1
      const lastSeen = new Date('2026-01-01T10:00:00Z');
      usePresenceStore.getState().setUserOffline(user1, lastSeen);

      const state = usePresenceStore.getState();

      // user1 should be updated
      expect(state.onlineUsers[user1].isOnline).toBe(false);
      expect(state.onlineUsers[user1].lastSeen).toEqual(lastSeen);

      // user2 should remain unchanged
      expect(state.onlineUsers[user2].isOnline).toBe(true);
      expect(state.onlineUsers[user2].lastSeen).toBeNull();
    });
  });
});
