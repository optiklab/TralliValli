using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.MongoDb;
using Testcontainers.Redis;
using TraliVali.Api.Hubs;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Tests.Hubs;

/// <summary>
/// Integration tests for ChatHub using SignalR client
/// </summary>
[Collection("Sequential")]
public class ChatHubIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private MongoDbContainer? _mongoContainer;
    private RedisContainer? _redisContainer;
    private string? _testToken;
    private string? _unauthorizedToken;

    public async Task InitializeAsync()
    {
        // Start MongoDB container
        _mongoContainer = new MongoDbBuilder("mongo:6.0").Build();
        await _mongoContainer.StartAsync();

        // Start Redis container
        _redisContainer = new RedisBuilder("redis:7-alpine").Build();
        await _redisContainer.StartAsync();

        // Setup JWT Settings with test keys
        var jwtSettings = new JwtSettings
        {
            PrivateKey = Auth.TestKeyGenerator.PrivateKey,
            PublicKey = Auth.TestKeyGenerator.PublicKey,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationDays = 7,
            RefreshTokenExpirationDays = 30
        };

        var mongoConnectionString = _mongoContainer.GetConnectionString();
        var redisConnectionString = _redisContainer.GetConnectionString();

        // Create factory with test configuration
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Set environment variables for JWT configuration
                Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY", jwtSettings.PrivateKey);
                Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY", jwtSettings.PublicKey);
                Environment.SetEnvironmentVariable("REDIS_CONNECTION_STRING", redisConnectionString);
                Environment.SetEnvironmentVariable("MONGODB_CONNECTION_STRING", mongoConnectionString);

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Provide additional configuration for the test
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] = jwtSettings.Issuer,
                        ["Jwt:Audience"] = jwtSettings.Audience,
                        ["Jwt:ExpirationDays"] = jwtSettings.ExpirationDays.ToString(),
                        ["Jwt:RefreshTokenExpirationDays"] = jwtSettings.RefreshTokenExpirationDays.ToString(),
                        ["MongoDb:DatabaseName"] = "tralivali_test"
                    }!);
                });

                builder.ConfigureTestServices(services =>
                {
                    // Replace MongoDB with test container
                    services.AddSingleton<MongoDbContext>(sp =>
                        new MongoDbContext(mongoConnectionString, "tralivali_test"));

                    // Replace Redis with test container  
                    services.AddSingleton<IConnectionMultiplexer>(sp =>
                        ConnectionMultiplexer.Connect(redisConnectionString));
                });

                builder.UseEnvironment("Test");
            });

        // Generate test tokens
        var redisConnection = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
        var tokenBlacklistService = new TokenBlacklistService(redisConnection);
        var jwtService = new JwtService(jwtSettings, tokenBlacklistService);
        
        var testUser = new User
        {
            Id = "testUser123",
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123"
        };
        var tokenResult = jwtService.GenerateToken(testUser, "testDevice");
        _testToken = tokenResult.AccessToken;
        _unauthorizedToken = "invalid.token.value";
    }

    public async Task DisposeAsync()
    {
        // Clean up environment variables
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY", null);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY", null);
        Environment.SetEnvironmentVariable("REDIS_CONNECTION_STRING", null);
        Environment.SetEnvironmentVariable("MONGODB_CONNECTION_STRING", null);

        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }

        if (_mongoContainer != null)
        {
            await _mongoContainer.DisposeAsync();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Test: Multi-client message delivery
    /// </summary>
    [Fact]
    public async Task SendMessage_ShouldBeReceivedByMultipleClients_InSameConversation()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "msg123";
        var content = "Hello from integration test!";
        var receivedMessages = new List<string>();
        var receivedByClient1 = new TaskCompletionSource<bool>();
        var receivedByClient2 = new TaskCompletionSource<bool>();

        var client1 = await CreateConnectedHubConnectionAsync();
        var client2 = await CreateConnectedHubConnectionAsync();

        client1.On<string, string, string, string, string, DateTime>("ReceiveMessage",
            (convId, msgId, userId, userName, msg, timestamp) =>
            {
                if (convId == conversationId && msgId == messageId)
                {
                    receivedMessages.Add($"Client1:{msg}");
                    receivedByClient1.TrySetResult(true);
                }
            });

        client2.On<string, string, string, string, string, DateTime>("ReceiveMessage",
            (convId, msgId, userId, userName, msg, timestamp) =>
            {
                if (convId == conversationId && msgId == messageId)
                {
                    receivedMessages.Add($"Client2:{msg}");
                    receivedByClient2.TrySetResult(true);
                }
            });

        // Both clients join the same conversation
        await client1.InvokeAsync("JoinConversation", conversationId);
        await client2.InvokeAsync("JoinConversation", conversationId);

        // Act - Client 1 sends a message
        await client1.InvokeAsync("SendMessage", conversationId, messageId, content);

        // Assert - Both clients should receive the message
        await Task.WhenAll(
            receivedByClient1.Task.WaitAsync(TimeSpan.FromSeconds(5)),
            receivedByClient2.Task.WaitAsync(TimeSpan.FromSeconds(5))
        );

        Assert.Equal(2, receivedMessages.Count);
        Assert.Contains($"Client1:{content}", receivedMessages);
        Assert.Contains($"Client2:{content}", receivedMessages);

        await client1.StopAsync();
        await client2.StopAsync();
    }

    /// <summary>
    /// Test: Message isolation between conversations
    /// </summary>
    [Fact]
    public async Task SendMessage_ShouldNotBeReceivedByClientsInDifferentConversation()
    {
        // Arrange
        var conversationId1 = "conv123";
        var conversationId2 = "conv456";
        var messageId = "msg123";
        var content = "Hello from conversation 1";
        var receivedByClient2 = false;
        var receivedByClient1 = new TaskCompletionSource<bool>();

        var client1 = await CreateConnectedHubConnectionAsync();
        var client2 = await CreateConnectedHubConnectionAsync();

        client1.On<string, string, string, string, string, DateTime>("ReceiveMessage",
            (convId, msgId, userId, userName, msg, timestamp) =>
            {
                if (convId == conversationId1)
                {
                    receivedByClient1.TrySetResult(true);
                }
            });

        client2.On<string, string, string, string, string, DateTime>("ReceiveMessage",
            (convId, msgId, userId, userName, msg, timestamp) =>
            {
                if (convId == conversationId1)
                {
                    receivedByClient2 = true;
                }
            });

        // Clients join different conversations
        await client1.InvokeAsync("JoinConversation", conversationId1);
        await client2.InvokeAsync("JoinConversation", conversationId2);

        // Act - Client 1 sends message to conversation 1
        await client1.InvokeAsync("SendMessage", conversationId1, messageId, content);

        // Assert - Only client 1 should receive the message
        await receivedByClient1.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(1000); // Wait to ensure client2 doesn't receive it

        Assert.False(receivedByClient2);

        await client1.StopAsync();
        await client2.StopAsync();
    }

    /// <summary>
    /// Test: Presence updates on connection
    /// </summary>
    [Fact]
    public async Task OnConnectedAsync_ShouldBroadcastPresenceUpdate_ToAllClients()
    {
        // Arrange
        var presenceReceived = new TaskCompletionSource<(string userId, bool isOnline)>();
        var observerClient = await CreateConnectedHubConnectionAsync();

        observerClient.On<string, bool, DateTime?>("PresenceUpdate",
            (userId, isOnline, lastSeen) =>
            {
                if (userId == "testUser123")
                {
                    presenceReceived.TrySetResult((userId, isOnline));
                }
            });

        // Act - Create a new connection (this triggers OnConnectedAsync)
        var newClient = await CreateConnectedHubConnectionAsync();

        // Assert
        var (receivedUserId, receivedIsOnline) = await presenceReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("testUser123", receivedUserId);
        Assert.True(receivedIsOnline);

        await observerClient.StopAsync();
        await newClient.StopAsync();
    }

    /// <summary>
    /// Test: Presence updates on disconnection
    /// </summary>
    [Fact]
    public async Task OnDisconnectedAsync_ShouldBroadcastPresenceUpdate_WhenUserGoesOffline()
    {
        // Arrange
        var presenceOfflineReceived = new TaskCompletionSource<(string userId, bool isOnline)>();
        var observerClient = await CreateConnectedHubConnectionAsync();
        var disconnectingClient = await CreateConnectedHubConnectionAsync();

        observerClient.On<string, bool, DateTime?>("PresenceUpdate",
            (userId, isOnline, lastSeen) =>
            {
                if (userId == "testUser123" && !isOnline)
                {
                    presenceOfflineReceived.TrySetResult((userId, isOnline));
                }
            });

        // Act - Disconnect the client
        await disconnectingClient.StopAsync();

        // Assert
        var (receivedUserId, receivedIsOnline) = await presenceOfflineReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("testUser123", receivedUserId);
        Assert.False(receivedIsOnline);

        await observerClient.StopAsync();
    }

    /// <summary>
    /// Test: User join/leave notifications
    /// </summary>
    [Fact]
    public async Task JoinConversation_ShouldNotifyOtherClients()
    {
        // Arrange
        var conversationId = "conv123";
        var userJoinedReceived = new TaskCompletionSource<(string userId, string userName)>();
        
        var client1 = await CreateConnectedHubConnectionAsync();
        var client2 = await CreateConnectedHubConnectionAsync();

        // Client 1 joins first
        await client1.InvokeAsync("JoinConversation", conversationId);

        // Client 1 listens for new users
        client1.On<string, string, string>("UserJoined",
            (convId, userId, userName) =>
            {
                if (convId == conversationId)
                {
                    userJoinedReceived.TrySetResult((userId, userName));
                }
            });

        // Act - Client 2 joins the conversation
        await client2.InvokeAsync("JoinConversation", conversationId);

        // Assert
        var (joinedUserId, joinedUserName) = await userJoinedReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("testUser123", joinedUserId);
        Assert.Equal("Test User", joinedUserName);

        await client1.StopAsync();
        await client2.StopAsync();
    }

    /// <summary>
    /// Test: Reconnection scenario
    /// </summary>
    [Fact]
    public async Task Reconnection_ShouldAllowClientToReconnectAndReceiveMessages()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "msg123";
        var content = "Hello after reconnection!";
        var messageReceived = new TaskCompletionSource<string>();

        var client = await CreateConnectedHubConnectionAsync();
        await client.InvokeAsync("JoinConversation", conversationId);

        // Simulate disconnection
        await client.StopAsync();

        // Act - Reconnect
        client = await CreateConnectedHubConnectionAsync();
        await client.InvokeAsync("JoinConversation", conversationId);

        client.On<string, string, string, string, string, DateTime>("ReceiveMessage",
            (convId, msgId, userId, userName, msg, timestamp) =>
            {
                if (convId == conversationId)
                {
                    messageReceived.TrySetResult(msg);
                }
            });

        // Send a message to verify the reconnected client can receive it
        await client.InvokeAsync("SendMessage", conversationId, messageId, content);

        // Assert
        var received = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(content, received);

        await client.StopAsync();
    }

    /// <summary>
    /// Test: Authorization - valid token allows connection
    /// </summary>
    [Fact]
    public async Task Connection_WithValidToken_ShouldSucceed()
    {
        // Arrange & Act
        var client = await CreateConnectedHubConnectionAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, client.State);

        await client.StopAsync();
    }

    /// <summary>
    /// Test: Authorization - invalid token prevents connection
    /// </summary>
    [Fact]
    public async Task Connection_WithInvalidToken_ShouldFail()
    {
        // Arrange
        var hubUrl = $"{_factory!.Server.BaseAddress}hubs/chat";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(_unauthorizedToken);
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await connection.StartAsync();
        });
    }

    /// <summary>
    /// Test: Authorization - no token prevents connection
    /// </summary>
    [Fact]
    public async Task Connection_WithoutToken_ShouldFail()
    {
        // Arrange
        var hubUrl = $"{_factory!.Server.BaseAddress}hubs/chat";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                // No token provider set
            })
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await connection.StartAsync();
        });
    }

    /// <summary>
    /// Test: Typing indicators
    /// </summary>
    [Fact]
    public async Task StartTyping_ShouldNotifyOtherClientsInConversation()
    {
        // Arrange
        var conversationId = "conv123";
        var typingReceived = new TaskCompletionSource<(string userId, bool isTyping)>();

        var client1 = await CreateConnectedHubConnectionAsync();
        var client2 = await CreateConnectedHubConnectionAsync();

        await client1.InvokeAsync("JoinConversation", conversationId);
        await client2.InvokeAsync("JoinConversation", conversationId);

        client2.On<string, string, string, bool>("TypingIndicator",
            (convId, userId, userName, isTyping) =>
            {
                if (convId == conversationId)
                {
                    typingReceived.TrySetResult((userId, isTyping));
                }
            });

        // Act
        await client1.InvokeAsync("StartTyping", conversationId);

        // Assert
        var (typingUserId, isTyping) = await typingReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("testUser123", typingUserId);
        Assert.True(isTyping);

        await client1.StopAsync();
        await client2.StopAsync();
    }

    /// <summary>
    /// Test: Message read receipts
    /// </summary>
    [Fact]
    public async Task MarkAsRead_ShouldNotifyClientsInConversation()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "msg123";
        var readReceived = new TaskCompletionSource<(string msgId, string userId)>();

        var client1 = await CreateConnectedHubConnectionAsync();
        var client2 = await CreateConnectedHubConnectionAsync();

        await client1.InvokeAsync("JoinConversation", conversationId);
        await client2.InvokeAsync("JoinConversation", conversationId);

        client2.On<string, string, string>("MessageRead",
            (convId, msgId, userId) =>
            {
                if (convId == conversationId)
                {
                    readReceived.TrySetResult((msgId, userId));
                }
            });

        // Act
        await client1.InvokeAsync("MarkAsRead", conversationId, messageId);

        // Assert
        var (readMessageId, readUserId) = await readReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(messageId, readMessageId);
        Assert.Equal("testUser123", readUserId);

        await client1.StopAsync();
        await client2.StopAsync();
    }

    /// <summary>
    /// Helper method to create and connect a SignalR hub connection
    /// </summary>
    private async Task<HubConnection> CreateConnectedHubConnectionAsync()
    {
        var hubUrl = $"{_factory!.Server.BaseAddress}hubs/chat";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Pass token via AccessTokenProvider (it will add to query string and headers as needed)
                options.AccessTokenProvider = () => Task.FromResult<string?>(_testToken);
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                // Force long polling initially to avoid WebSocket issues in tests
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .Build();

        await connection.StartAsync();
        return connection;
    }
}
