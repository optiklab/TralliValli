import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

/**
 * Connection state for the SignalR service
 */
export const ConnectionState = {
  Disconnected: 'Disconnected',
  Connecting: 'Connecting',
  Connected: 'Connected',
  Reconnecting: 'Reconnecting',
  Disconnecting: 'Disconnecting',
} as const;

export type ConnectionState = (typeof ConnectionState)[keyof typeof ConnectionState];

/**
 * Message payload for queuing during disconnection
 */
interface QueuedMessage {
  method: string;
  args: unknown[];
  timestamp: number;
}

/**
 * Typed event handlers matching IChatClient interface
 */
export interface ChatClientHandlers {
  onReceiveMessage?: (
    conversationId: string,
    messageId: string,
    senderId: string,
    senderName: string,
    content: string,
    timestamp: Date
  ) => void;
  onUserJoined?: (conversationId: string, userId: string, userName: string) => void;
  onUserLeft?: (conversationId: string, userId: string, userName: string) => void;
  onTypingIndicator?: (
    conversationId: string,
    userId: string,
    userName: string,
    isTyping: boolean
  ) => void;
  onMessageRead?: (conversationId: string, messageId: string, userId: string) => void;
  onPresenceUpdate?: (userId: string, isOnline: boolean, lastSeen: Date | null) => void;
}

/**
 * Connection state event handler
 */
export type ConnectionStateHandler = (state: ConnectionState) => void;

/**
 * SignalR service configuration options
 */
export interface SignalRServiceOptions {
  url: string;
  accessTokenFactory?: () => string | Promise<string>;
  automaticReconnect?: boolean;
  maxReconnectAttempts?: number;
  initialRetryDelayMs?: number;
  maxRetryDelayMs?: number;
  logLevel?: LogLevel;
}

/**
 * SignalR client service managing WebSocket connection with auto-reconnection,
 * message queuing, and typed methods matching IChatClient interface
 */
export class SignalRService {
  private connection: HubConnection | null = null;
  private state: ConnectionState = ConnectionState.Disconnected;
  private messageQueue: QueuedMessage[] = [];
  private handlers: ChatClientHandlers = {};
  private stateHandlers: ConnectionStateHandler[] = [];
  private reconnectAttempts = 0;
  private reconnectTimer: number | null = null;

  private readonly options: Required<SignalRServiceOptions>;

  constructor(options: SignalRServiceOptions) {
    this.options = {
      url: options.url,
      accessTokenFactory: options.accessTokenFactory || (() => ''),
      automaticReconnect: options.automaticReconnect ?? true,
      maxReconnectAttempts: options.maxReconnectAttempts ?? 10,
      initialRetryDelayMs: options.initialRetryDelayMs ?? 1000,
      maxRetryDelayMs: options.maxRetryDelayMs ?? 30000,
      logLevel: options.logLevel ?? LogLevel.Information,
    };
  }

  /**
   * Start the SignalR connection
   */
  async start(): Promise<void> {
    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.setState(ConnectionState.Connecting);

    try {
      this.connection = new HubConnectionBuilder()
        .withUrl(this.options.url, {
          accessTokenFactory: this.options.accessTokenFactory,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: () => this.getNextRetryDelay(),
        })
        .configureLogging(this.options.logLevel)
        .build();

      this.setupEventHandlers();
      this.setupConnectionHandlers();

      await this.connection.start();
      this.setState(ConnectionState.Connected);
      this.reconnectAttempts = 0;

      // Process queued messages after successful connection
      await this.processMessageQueue();
    } catch (error) {
      this.setState(ConnectionState.Disconnected);
      if (this.options.automaticReconnect) {
        this.scheduleReconnect();
      }
      throw error;
    }
  }

  /**
   * Stop the SignalR connection
   */
  async stop(): Promise<void> {
    if (!this.connection) {
      return;
    }

    this.setState(ConnectionState.Disconnecting);

    if (this.reconnectTimer !== null) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    try {
      await this.connection.stop();
    } finally {
      this.setState(ConnectionState.Disconnected);
      this.connection = null;
    }
  }

  /**
   * Get the current connection state
   */
  getState(): ConnectionState {
    return this.state;
  }

  /**
   * Check if the connection is active
   */
  isConnected(): boolean {
    return this.connection !== null && this.connection.state === HubConnectionState.Connected;
  }

  /**
   * Register event handlers for chat client events
   */
  on(handlers: ChatClientHandlers): void {
    this.handlers = { ...this.handlers, ...handlers };
  }

  /**
   * Register connection state change handler
   */
  onStateChange(handler: ConnectionStateHandler): () => void {
    this.stateHandlers.push(handler);
    // Return unsubscribe function
    return () => {
      const index = this.stateHandlers.indexOf(handler);
      if (index > -1) {
        this.stateHandlers.splice(index, 1);
      }
    };
  }

  /**
   * Send a message to a conversation
   */
  async sendMessage(conversationId: string, messageId: string, content: string): Promise<void> {
    await this.invoke('SendMessage', conversationId, messageId, content);
  }

  /**
   * Join a conversation group
   */
  async joinConversation(conversationId: string): Promise<void> {
    await this.invoke('JoinConversation', conversationId);
  }

  /**
   * Leave a conversation group
   */
  async leaveConversation(conversationId: string): Promise<void> {
    await this.invoke('LeaveConversation', conversationId);
  }

  /**
   * Notify others that the current user is typing
   */
  async startTyping(conversationId: string): Promise<void> {
    await this.invoke('StartTyping', conversationId);
  }

  /**
   * Notify others that the current user stopped typing
   */
  async stopTyping(conversationId: string): Promise<void> {
    await this.invoke('StopTyping', conversationId);
  }

  /**
   * Mark a message as read
   */
  async markAsRead(conversationId: string, messageId: string): Promise<void> {
    await this.invoke('MarkAsRead', conversationId, messageId);
  }

  /**
   * Invoke a hub method with automatic queuing during disconnection
   */
  private async invoke(method: string, ...args: unknown[]): Promise<void> {
    if (this.isConnected() && this.connection) {
      await this.connection.invoke(method, ...args);
    } else {
      // Queue the message if disconnected
      this.messageQueue.push({
        method,
        args,
        timestamp: Date.now(),
      });
    }
  }

  /**
   * Setup event handlers for incoming messages from server
   */
  private setupEventHandlers(): void {
    if (!this.connection) return;

    // ReceiveMessage - maps to onReceiveMessage
    this.connection.on(
      'ReceiveMessage',
      (
        conversationId: string,
        messageId: string,
        senderId: string,
        senderName: string,
        content: string,
        timestamp: string
      ) => {
        this.handlers.onReceiveMessage?.(
          conversationId,
          messageId,
          senderId,
          senderName,
          content,
          new Date(timestamp)
        );
      }
    );

    // UserJoined - maps to onPresenceUpdate (user joined)
    this.connection.on('UserJoined', (conversationId: string, userId: string, userName: string) => {
      this.handlers.onUserJoined?.(conversationId, userId, userName);
    });

    // UserLeft - maps to onPresenceUpdate (user left)
    this.connection.on('UserLeft', (conversationId: string, userId: string, userName: string) => {
      this.handlers.onUserLeft?.(conversationId, userId, userName);
    });

    // TypingIndicator - maps to onTypingIndicator
    this.connection.on(
      'TypingIndicator',
      (conversationId: string, userId: string, userName: string, isTyping: boolean) => {
        this.handlers.onTypingIndicator?.(conversationId, userId, userName, isTyping);
      }
    );

    // MessageRead
    this.connection.on(
      'MessageRead',
      (conversationId: string, messageId: string, userId: string) => {
        this.handlers.onMessageRead?.(conversationId, messageId, userId);
      }
    );

    // PresenceUpdate - maps to onPresenceUpdate
    this.connection.on(
      'PresenceUpdate',
      (userId: string, isOnline: boolean, lastSeen: string | null) => {
        this.handlers.onPresenceUpdate?.(userId, isOnline, lastSeen ? new Date(lastSeen) : null);
      }
    );
  }

  /**
   * Setup connection lifecycle handlers
   */
  private setupConnectionHandlers(): void {
    if (!this.connection) return;

    this.connection.onclose(() => {
      this.setState(ConnectionState.Disconnected);
      if (this.options.automaticReconnect) {
        this.scheduleReconnect();
      }
    });

    this.connection.onreconnecting(() => {
      this.setState(ConnectionState.Reconnecting);
    });

    this.connection.onreconnected(() => {
      this.setState(ConnectionState.Connected);
      this.reconnectAttempts = 0;
      // Process queued messages after successful reconnection
      this.processMessageQueue();
    });
  }

  /**
   * Calculate next retry delay with exponential backoff
   */
  private getNextRetryDelay(): number {
    const delay = Math.min(
      this.options.initialRetryDelayMs * Math.pow(2, this.reconnectAttempts),
      this.options.maxRetryDelayMs
    );
    this.reconnectAttempts++;
    return delay;
  }

  /**
   * Schedule a reconnection attempt
   */
  private scheduleReconnect(): void {
    if (this.reconnectAttempts >= this.options.maxReconnectAttempts) {
      return;
    }

    if (this.reconnectTimer !== null) {
      return;
    }

    const delay = this.getNextRetryDelay();
    this.reconnectTimer = window.setTimeout(() => {
      this.reconnectTimer = null;
      this.start().catch(() => {
        // Error is handled in start method
      });
    }, delay);
  }

  /**
   * Process queued messages after connection is established
   */
  private async processMessageQueue(): Promise<void> {
    if (!this.isConnected() || !this.connection) {
      return;
    }

    while (this.messageQueue.length > 0) {
      const message = this.messageQueue.shift();
      if (message) {
        try {
          await this.connection.invoke(message.method, ...message.args);
        } catch {
          // If invoke fails, re-queue the message
          this.messageQueue.unshift(message);
          break;
        }
      }
    }
  }

  /**
   * Update connection state and notify handlers
   */
  private setState(newState: ConnectionState): void {
    if (this.state !== newState) {
      this.state = newState;
      this.stateHandlers.forEach((handler) => handler(newState));
    }
  }
}
