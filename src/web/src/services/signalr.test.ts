import { describe, it, expect, vi, beforeEach } from 'vitest';
import { SignalRService, ConnectionState, type ChatClientHandlers } from './signalr';

// Create mock functions at module level
const mockStart = vi.fn();
const mockStop = vi.fn();
const mockInvoke = vi.fn();
const mockOn = vi.fn();
const mockOnclose = vi.fn();
const mockOnreconnecting = vi.fn();
const mockOnreconnected = vi.fn();

const mockConnection = {
  start: mockStart,
  stop: mockStop,
  invoke: mockInvoke,
  on: mockOn,
  onclose: mockOnclose,
  onreconnecting: mockOnreconnecting,
  onreconnected: mockOnreconnected,
  state: 'Disconnected',
};

const mockBuilder = {
  withUrl: vi.fn().mockReturnThis(),
  withAutomaticReconnect: vi.fn().mockReturnThis(),
  configureLogging: vi.fn().mockReturnThis(),
  build: vi.fn(() => mockConnection),
};

// Mock the @microsoft/signalr module
vi.mock('@microsoft/signalr', () => {
  return {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    HubConnectionBuilder: vi.fn(function (this: any) {
      return mockBuilder;
    }),
    HubConnectionState: {
      Disconnected: 'Disconnected',
      Connecting: 'Connecting',
      Connected: 'Connected',
      Disconnecting: 'Disconnecting',
      Reconnecting: 'Reconnecting',
    },
    LogLevel: {
      Trace: 0,
      Debug: 1,
      Information: 2,
      Warning: 3,
      Error: 4,
      Critical: 5,
      None: 6,
    },
  };
});

describe('SignalRService', () => {
  let service: SignalRService;

  beforeEach(() => {
    // Reset all mocks before each test
    vi.clearAllMocks();
    mockConnection.state = 'Disconnected';

    service = new SignalRService({
      url: 'http://localhost:5000/hubs/chat',
      accessTokenFactory: () => 'test-token',
    });
  });

  describe('Constructor', () => {
    it('should create service with default options', () => {
      const service = new SignalRService({
        url: 'http://localhost:5000/hubs/chat',
      });

      expect(service).toBeDefined();
      expect(service.getState()).toBe(ConnectionState.Disconnected);
    });

    it('should create service with custom options', () => {
      const service = new SignalRService({
        url: 'http://localhost:5000/hubs/chat',
        accessTokenFactory: () => 'custom-token',
        automaticReconnect: false,
        maxReconnectAttempts: 5,
        initialRetryDelayMs: 2000,
        maxRetryDelayMs: 60000,
      });

      expect(service).toBeDefined();
    });
  });

  describe('Connection Management', () => {
    it('should start connection successfully', async () => {
      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      expect(mockBuilder.withUrl).toHaveBeenCalledWith(
        'http://localhost:5000/hubs/chat',
        expect.objectContaining({
          accessTokenFactory: expect.any(Function),
        })
      );
      expect(mockBuilder.withAutomaticReconnect).toHaveBeenCalled();
      expect(mockBuilder.configureLogging).toHaveBeenCalled();
      expect(mockStart).toHaveBeenCalled();
      expect(service.getState()).toBe(ConnectionState.Connected);
    });

    it('should handle start connection failure', async () => {
      mockStart.mockRejectedValue(new Error('Connection failed'));

      await expect(service.start()).rejects.toThrow('Connection failed');
      expect(service.getState()).toBe(ConnectionState.Disconnected);
    });

    it('should stop connection successfully', async () => {
      mockStart.mockResolvedValue(undefined);
      mockStop.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();
      await service.stop();

      expect(mockStop).toHaveBeenCalled();
      expect(service.getState()).toBe(ConnectionState.Disconnected);
    });

    it('should not start if already connected', async () => {
      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();
      mockStart.mockClear();

      // Try to start again - should return immediately without calling start
      mockConnection.state = 'Connected';
      await service.start();

      expect(mockStart).not.toHaveBeenCalled();
    });

    it('should check connection state correctly', async () => {
      expect(service.isConnected()).toBe(false);

      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      expect(service.isConnected()).toBe(true);
    });
  });

  describe('Message Queuing', () => {
    it('should queue messages when disconnected', async () => {
      await service.sendMessage('conv1', 'msg1', 'Hello');

      expect(mockInvoke).not.toHaveBeenCalled();
    });

    it('should process queued messages after connection', async () => {
      // Queue messages while disconnected
      await service.sendMessage('conv1', 'msg1', 'Hello');
      await service.joinConversation('conv1');

      mockStart.mockResolvedValue(undefined);
      mockInvoke.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Connect
      await service.start();

      // Give time for queue processing
      await new Promise((resolve) => setTimeout(resolve, 10));

      // Messages should have been processed
      expect(mockInvoke).toHaveBeenCalledWith('SendMessage', 'conv1', 'msg1', 'Hello');
      expect(mockInvoke).toHaveBeenCalledWith('JoinConversation', 'conv1');
    });

    it('should send messages immediately when connected', async () => {
      mockStart.mockResolvedValue(undefined);
      mockInvoke.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      await service.sendMessage('conv1', 'msg1', 'Hello');

      expect(mockInvoke).toHaveBeenCalledWith('SendMessage', 'conv1', 'msg1', 'Hello');
    });
  });

  describe('Typed Methods', () => {
    beforeEach(async () => {
      mockStart.mockResolvedValue(undefined);
      mockInvoke.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';
      await service.start();
    });

    it('should call sendMessage', async () => {
      await service.sendMessage('conv1', 'msg1', 'Hello World');

      expect(mockInvoke).toHaveBeenCalledWith('SendMessage', 'conv1', 'msg1', 'Hello World');
    });

    it('should call joinConversation', async () => {
      await service.joinConversation('conv1');

      expect(mockInvoke).toHaveBeenCalledWith('JoinConversation', 'conv1');
    });

    it('should call leaveConversation', async () => {
      await service.leaveConversation('conv1');

      expect(mockInvoke).toHaveBeenCalledWith('LeaveConversation', 'conv1');
    });

    it('should call startTyping', async () => {
      await service.startTyping('conv1');

      expect(mockInvoke).toHaveBeenCalledWith('StartTyping', 'conv1');
    });

    it('should call stopTyping', async () => {
      await service.stopTyping('conv1');

      expect(mockInvoke).toHaveBeenCalledWith('StopTyping', 'conv1');
    });

    it('should call markAsRead', async () => {
      await service.markAsRead('conv1', 'msg1');

      expect(mockInvoke).toHaveBeenCalledWith('MarkAsRead', 'conv1', 'msg1');
    });
  });

  describe('Event Handlers', () => {
    let handlers: ChatClientHandlers;

    beforeEach(async () => {
      handlers = {
        onReceiveMessage: vi.fn(),
        onUserJoined: vi.fn(),
        onUserLeft: vi.fn(),
        onTypingIndicator: vi.fn(),
        onMessageRead: vi.fn(),
        onPresenceUpdate: vi.fn(),
      };

      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      service.on(handlers);
      await service.start();
    });

    it('should register onReceiveMessage handler', () => {
      const onCallback = mockOn.mock.calls.find((call) => call[0] === 'ReceiveMessage');

      expect(onCallback).toBeDefined();

      // Simulate receiving a message
      const handler = onCallback[1];
      handler('conv1', 'msg1', 'user1', 'John Doe', 'Hello', new Date().toISOString());

      expect(handlers.onReceiveMessage).toHaveBeenCalledWith(
        'conv1',
        'msg1',
        'user1',
        'John Doe',
        'Hello',
        expect.any(Date)
      );
    });

    it('should register onUserJoined handler', () => {
      const onCallback = mockOn.mock.calls.find((call) => call[0] === 'UserJoined');

      expect(onCallback).toBeDefined();

      // Simulate user joined
      const handler = onCallback[1];
      handler('conv1', 'user1', 'John Doe');

      expect(handlers.onUserJoined).toHaveBeenCalledWith('conv1', 'user1', 'John Doe');
    });

    it('should register onTypingIndicator handler', () => {
      const onCallback = mockOn.mock.calls.find((call) => call[0] === 'TypingIndicator');

      expect(onCallback).toBeDefined();

      // Simulate typing indicator
      const handler = onCallback[1];
      handler('conv1', 'user1', 'John Doe', true);

      expect(handlers.onTypingIndicator).toHaveBeenCalledWith('conv1', 'user1', 'John Doe', true);
    });

    it('should register onPresenceUpdate handler', () => {
      const onCallback = mockOn.mock.calls.find((call) => call[0] === 'PresenceUpdate');

      expect(onCallback).toBeDefined();

      // Simulate presence update
      const handler = onCallback[1];
      const lastSeen = new Date().toISOString();
      handler('user1', false, lastSeen);

      expect(handlers.onPresenceUpdate).toHaveBeenCalledWith('user1', false, expect.any(Date));
    });

    it('should handle presence update with null lastSeen', () => {
      const onCallback = mockOn.mock.calls.find((call) => call[0] === 'PresenceUpdate');

      // Simulate presence update with null lastSeen
      const handler = onCallback[1];
      handler('user1', true, null);

      expect(handlers.onPresenceUpdate).toHaveBeenCalledWith('user1', true, null);
    });
  });

  describe('Connection State Events', () => {
    it('should notify state change handlers', async () => {
      const stateHandler = vi.fn();
      service.onStateChange(stateHandler);

      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      expect(stateHandler).toHaveBeenCalledWith(ConnectionState.Connecting);
      expect(stateHandler).toHaveBeenCalledWith(ConnectionState.Connected);
    });

    it('should unsubscribe state change handler', async () => {
      const stateHandler = vi.fn();
      const unsubscribe = service.onStateChange(stateHandler);

      unsubscribe();

      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      expect(stateHandler).not.toHaveBeenCalled();
    });

    it('should handle onclose event', async () => {
      const stateHandler = vi.fn();
      service.onStateChange(stateHandler);

      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      // Get the onclose callback
      const oncloseCallback = mockOnclose.mock.calls[0][0];

      // Simulate connection close
      oncloseCallback();

      expect(stateHandler).toHaveBeenCalledWith(ConnectionState.Disconnected);
    });

    it('should handle onreconnecting event', async () => {
      const stateHandler = vi.fn();
      service.onStateChange(stateHandler);

      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      // Get the onreconnecting callback
      const onreconnectingCallback = mockOnreconnecting.mock.calls[0][0];

      // Simulate reconnecting
      onreconnectingCallback();

      expect(stateHandler).toHaveBeenCalledWith(ConnectionState.Reconnecting);
    });

    it('should handle onreconnected event', async () => {
      mockStart.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      await service.start();

      const stateHandler = vi.fn();
      service.onStateChange(stateHandler);

      // Get the onreconnecting callback to set state to Reconnecting first
      const onreconnectingCallback = mockOnreconnecting.mock.calls[0][0];
      onreconnectingCallback();

      // Clear the reconnecting state call
      stateHandler.mockClear();

      // Get the onreconnected callback
      const onreconnectedCallback = mockOnreconnected.mock.calls[0][0];

      // Simulate reconnected
      onreconnectedCallback();

      expect(stateHandler).toHaveBeenCalledWith(ConnectionState.Connected);
    });
  });

  describe('Auto-reconnection', () => {
    it('should not reconnect when automaticReconnect is false', async () => {
      const service = new SignalRService({
        url: 'http://localhost:5000/hubs/chat',
        automaticReconnect: false,
      });

      mockStart.mockRejectedValue(new Error('Connection failed'));

      await expect(service.start()).rejects.toThrow('Connection failed');

      // Service should remain disconnected with no reconnection attempts
      expect(service.getState()).toBe(ConnectionState.Disconnected);
    });
  });
});
