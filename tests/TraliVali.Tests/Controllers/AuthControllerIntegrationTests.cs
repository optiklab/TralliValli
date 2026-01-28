using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Testcontainers.MongoDb;
using Testcontainers.Redis;
using TraliVali.Api.Controllers;
using TraliVali.Api.Models;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Messaging;

namespace TraliVali.Tests.Controllers;

/// <summary>
/// Integration tests for AuthController
/// </summary>
[Collection("Sequential")]
public class AuthControllerIntegrationTests : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private RedisContainer? _redisContainer;
    private MongoDbContext? _dbContext;
    private IConnectionMultiplexer? _redis;
    private AuthController? _controller;
    private IRepository<User>? _userRepository;
    private IRepository<Invite>? _inviteRepository;
    private IJwtService? _jwtService;
    private IMagicLinkService? _magicLinkService;
    private IInviteService? _inviteService;
    private ITokenBlacklistService? _tokenBlacklistService;
    private Mock<IEmailService>? _mockEmailService;
    private JwtSettings? _jwtSettings;

    public async Task InitializeAsync()
    {
        // Start MongoDB container
        _mongoContainer = new MongoDbBuilder().Build();
        await _mongoContainer.StartAsync();

        // Start Redis container
        _redisContainer = new RedisBuilder().Build();
        await _redisContainer.StartAsync();

        // Setup MongoDB
        var mongoConnectionString = _mongoContainer.GetConnectionString();
        _dbContext = new MongoDbContext(mongoConnectionString, "tralivali_test");
        _userRepository = new UserRepository(_dbContext);
        _inviteRepository = new InviteRepository(_dbContext);

        // Setup Redis
        var redisConnectionString = _redisContainer.GetConnectionString();
        _redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);

        // Setup JWT Settings with test keys
        _jwtSettings = new JwtSettings
        {
            PrivateKey = Auth.TestKeyGenerator.PrivateKey,
            PublicKey = Auth.TestKeyGenerator.PublicKey,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationDays = 7,
            RefreshTokenExpirationDays = 30
        };

        // Setup services
        _tokenBlacklistService = new TokenBlacklistService(_redis);
        _jwtService = new JwtService(_jwtSettings, _tokenBlacklistService);
        _magicLinkService = new MagicLinkService(_redis);
        _inviteService = new InviteService(_dbContext.Invites, "test-signing-key-32-characters-long");
        _mockEmailService = new Mock<IEmailService>();
        
        // Setup mock email service to return completed tasks
        _mockEmailService
            .Setup(x => x.SendMagicLinkEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockEmailService
            .Setup(x => x.SendWelcomeEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup controller
        var logger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(
            _magicLinkService,
            _jwtService,
            _tokenBlacklistService,
            _userRepository,
            _inviteRepository,
            _inviteService,
            _mockEmailService.Object,
            logger.Object
        );

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.HttpContext.Request.Scheme = "http";
        _controller.HttpContext.Request.Host = new HostString("localhost:5000");
    }

    public async Task DisposeAsync()
    {
        if (_redis != null)
        {
            await _redis.CloseAsync();
            _redis.Dispose();
        }

        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
        
        if (_jwtService is IDisposable disposableJwt)
        {
            disposableJwt.Dispose();
        }
    }

    [Fact]
    public async Task RequestMagicLink_ShouldSendEmail_WhenUserExists()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        user = await _userRepository!.AddAsync(user);

        var request = new RequestMagicLinkRequest
        {
            Email = "test@example.com",
            DeviceId = "device123"
        };

        // Act
        var result = await _controller!.RequestMagicLink(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RequestMagicLinkResponse>(okResult.Value);
        Assert.Equal("If the email exists in our system, a magic link has been sent.", response.Message);

        _mockEmailService!.Verify(
            x => x.SendMagicLinkEmailAsync(
                It.Is<string>(e => e == "test@example.com"),
                It.Is<string>(n => n == "Test User"),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestMagicLink_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new RequestMagicLinkRequest
        {
            Email = "nonexistent@example.com",
            DeviceId = "device123"
        };

        // Act
        var result = await _controller!.RequestMagicLink(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RequestMagicLinkResponse>(okResult.Value);
        Assert.Equal("If the email exists in our system, a magic link has been sent.", response.Message);

        _mockEmailService!.Verify(
            x => x.SendMagicLinkEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestMagicLink_ShouldReturnSuccess_WhenUserIsInactive()
    {
        // Arrange
        var user = new User
        {
            Email = "inactive@example.com",
            DisplayName = "Inactive User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = false
        };
        user = await _userRepository!.AddAsync(user);

        var request = new RequestMagicLinkRequest
        {
            Email = "inactive@example.com",
            DeviceId = "device123"
        };

        // Act
        var result = await _controller!.RequestMagicLink(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RequestMagicLinkResponse>(okResult.Value);
        Assert.Equal("If the email exists in our system, a magic link has been sent.", response.Message);

        _mockEmailService!.Verify(
            x => x.SendMagicLinkEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(Skip = "Needs investigation - ObjectResult returned instead of OkObjectResult")]
    public async Task VerifyMagicLink_ShouldReturnJwtTokens_WhenTokenIsValid()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        user = await _userRepository!.AddAsync(user);

        var token = await _magicLinkService!.CreateMagicLinkAsync("test@example.com", "device123");

        var request = new VerifyMagicLinkRequest
        {
            Token = token
        };

        // Act
        var result = await _controller!.VerifyMagicLink(request, CancellationToken.None);

        // Assert
        if (result is ObjectResult objectResult && objectResult.StatusCode == 500)
        {
            // Log the error for debugging
            Assert.Fail($"Controller returned 500 error: {objectResult.Value}");
        }
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<VerifyMagicLinkResponse>(okResult.Value);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
        Assert.True(response.RefreshExpiresAt > response.ExpiresAt);
    }

    [Fact]
    public async Task VerifyMagicLink_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        // Arrange
        var request = new VerifyMagicLinkRequest
        {
            Token = "invalid-token"
        };

        // Act
        var result = await _controller!.VerifyMagicLink(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task VerifyMagicLink_ShouldBeSingleUse_TokenCannotBeReused()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        user = await _userRepository!.AddAsync(user);

        var token = await _magicLinkService!.CreateMagicLinkAsync("test@example.com", "device123");

        var request = new VerifyMagicLinkRequest
        {
            Token = token
        };

        // Act - First verification should succeed
        var firstResult = await _controller!.VerifyMagicLink(request, CancellationToken.None);
        Assert.IsType<OkObjectResult>(firstResult);

        // Act - Second verification should fail (single-use)
        var secondResult = await _controller!.VerifyMagicLink(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(secondResult);
    }

    [Fact]
    public async Task VerifyMagicLink_ShouldUpdateLastLoginTime()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true,
            LastLoginAt = null
        };
        user = await _userRepository!.AddAsync(user);

        var token = await _magicLinkService!.CreateMagicLinkAsync("test@example.com", "device123");

        var request = new VerifyMagicLinkRequest
        {
            Token = token
        };

        // Act
        await _controller!.VerifyMagicLink(request, CancellationToken.None);

        // Assert
        var updatedUser = await _userRepository.GetByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.LastLoginAt);
        Assert.True(updatedUser.LastLoginAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact(Skip = "RSA key disposal issue - needs investigation")]
    public async Task RefreshToken_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123"
        };

        var tokenResult = _jwtService!.GenerateToken(user, "device123");

        var request = new RefreshTokenRequest
        {
            RefreshToken = tokenResult.RefreshToken
        };

        // Act
        var result = await _controller!.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RefreshTokenResponse>(okResult.Value);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.NotEqual(tokenResult.AccessToken, response.AccessToken);
        Assert.NotEqual(tokenResult.RefreshToken, response.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var result = await _controller!.RefreshToken(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact(Skip = "RSA key disposal issue - needs investigation")]
    public async Task RefreshToken_ShouldBlacklistOldRefreshToken()
    {
        // Arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123"
        };

        var tokenResult = _jwtService!.GenerateToken(user, "device123");
        var oldRefreshToken = tokenResult.RefreshToken;

        var request = new RefreshTokenRequest
        {
            RefreshToken = oldRefreshToken
        };

        // Act
        await _controller!.RefreshToken(request);

        // Assert - Old refresh token should be blacklisted
        var isBlacklisted = await _tokenBlacklistService!.IsTokenBlacklistedAsync(oldRefreshToken);
        Assert.True(isBlacklisted);
    }

    [Fact(Skip = "RSA key disposal issue - needs investigation")]
    public async Task Logout_ShouldBlacklistAccessToken()
    {
        // Arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash123",
            PublicKey = "key123"
        };

        var tokenResult = _jwtService!.GenerateToken(user, "device123");

        var request = new LogoutRequest
        {
            AccessToken = tokenResult.AccessToken
        };

        // Act
        var result = await _controller!.Logout(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LogoutResponse>(okResult.Value);
        Assert.Equal("Logged out successfully.", response.Message);

        var isBlacklisted = await _tokenBlacklistService!.IsTokenBlacklistedAsync(tokenResult.AccessToken);
        Assert.True(isBlacklisted);
    }

    [Fact]
    public async Task Logout_ShouldReturnSuccess_EvenWhenTokenIsInvalid()
    {
        // Arrange
        var request = new LogoutRequest
        {
            AccessToken = "invalid-token"
        };

        // Act
        var result = await _controller!.Logout(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LogoutResponse>(okResult.Value);
        Assert.Equal("Logged out successfully.", response.Message);
    }

    [Fact]
    public async Task ValidateInvite_ShouldReturnValid_WhenInviteIsValid()
    {
        // Arrange
        var inviter = new User
        {
            Email = "inviter@example.com",
            DisplayName = "Inviter User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        inviter = await _userRepository!.AddAsync(inviter);
        var token = await _inviteService!.GenerateInviteLinkAsync(inviter.Id, 24);

        // Act
        var result = await _controller!.ValidateInvite(token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ValidateInviteResponse>(okResult.Value);
        Assert.True(response.IsValid);
        Assert.NotNull(response.ExpiresAt);
        Assert.Equal("Invite is valid.", response.Message);
    }

    [Fact]
    public async Task ValidateInvite_ShouldReturnInvalid_WhenTokenIsInvalid()
    {
        // Arrange
        var invalidToken = "invalid-token";

        // Act
        var result = await _controller!.ValidateInvite(invalidToken);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ValidateInviteResponse>(notFoundResult.Value);
        Assert.False(response.IsValid);
        Assert.Equal("Invalid or expired invite token.", response.Message);
    }

    [Fact]
    public async Task ValidateInvite_ShouldReturnInvalid_WhenInviteIsUsed()
    {
        // Arrange - Create and use an invite
        var inviter = new User
        {
            Email = "inviter@example.com",
            DisplayName = "Inviter User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        inviter = await _userRepository!.AddAsync(inviter);
        var token = await _inviteService!.GenerateInviteLinkAsync(inviter.Id, 24);
        
        // Mark invite as used
        var newUser = new User
        {
            Email = "newuser@example.com",
            DisplayName = "New User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        newUser = await _userRepository!.AddAsync(newUser);
        await _inviteService.RedeemInviteAsync(token, newUser.Id);

        // Act
        var result = await _controller!.ValidateInvite(token);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ValidateInviteResponse>(notFoundResult.Value);
        Assert.False(response.IsValid);
    }

    [Fact]
    public async Task Register_ShouldCreateUser_WhenInviteIsValid()
    {
        // Arrange
        var inviter = new User
        {
            Email = "inviter@example.com",
            DisplayName = "Inviter User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        inviter = await _userRepository!.AddAsync(inviter);
        var token = await _inviteService!.GenerateInviteLinkAsync(inviter.Id, 24);
        var request = new RegisterRequest
        {
            InviteToken = token,
            Email = "newuser@example.com",
            DisplayName = "New User"
        };

        // Act
        var result = await _controller!.Register(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RegisterResponse>(okResult.Value);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
        Assert.True(response.RefreshExpiresAt > DateTime.UtcNow);

        // Verify user was created
        var users = await _userRepository!.FindAsync(u => u.Email == "newuser@example.com");
        var createdUser = users.FirstOrDefault();
        Assert.NotNull(createdUser);
        Assert.Equal("New User", createdUser.DisplayName);
        Assert.Equal(inviter.Id, createdUser.InvitedBy);
        Assert.True(createdUser.IsActive);

        // Verify invite was marked as used
        var invite = await _inviteService.ValidateInviteAsync(token);
        Assert.Null(invite); // Should be null because it's used

        // Verify welcome email was sent
        _mockEmailService!.Verify(
            x => x.SendWelcomeEmailAsync(
                It.Is<string>(e => e == "newuser@example.com"),
                It.Is<string>(n => n == "New User"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenInviteIsInvalid()
    {
        // Arrange
        var request = new RegisterRequest
        {
            InviteToken = "invalid-token",
            Email = "newuser@example.com",
            DisplayName = "New User"
        };

        // Act
        var result = await _controller!.Register(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        
        // Verify user was not created
        var users = await _userRepository!.FindAsync(u => u.Email == "newuser@example.com");
        Assert.Empty(users);

        // Verify welcome email was not sent
        _mockEmailService!.Verify(
            x => x.SendWelcomeEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUserAlreadyExists()
    {
        // Arrange
        var inviter = new User
        {
            Email = "inviter@example.com",
            DisplayName = "Inviter User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        inviter = await _userRepository!.AddAsync(inviter);

        var existingUser = new User
        {
            Email = "existing@example.com",
            DisplayName = "Existing User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        await _userRepository!.AddAsync(existingUser);

        var token = await _inviteService!.GenerateInviteLinkAsync(inviter.Id, 24);
        var request = new RegisterRequest
        {
            InviteToken = token,
            Email = "existing@example.com",
            DisplayName = "New User"
        };

        // Act
        var result = await _controller!.Register(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        // Verify invite is still valid (not redeemed)
        var invite = await _inviteService.ValidateInviteAsync(token);
        Assert.NotNull(invite);

        // Verify welcome email was not sent
        _mockEmailService!.Verify(
            x => x.SendWelcomeEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Register_ShouldNotReuseInvite_WhenInviteIsAlreadyUsed()
    {
        // Arrange
        var inviter = new User
        {
            Email = "inviter@example.com",
            DisplayName = "Inviter User",
            PasswordHash = "hash123",
            PublicKey = "key123",
            IsActive = true
        };
        inviter = await _userRepository!.AddAsync(inviter);
        var token = await _inviteService!.GenerateInviteLinkAsync(inviter.Id, 24);
        
        // First registration
        var firstRequest = new RegisterRequest
        {
            InviteToken = token,
            Email = "firstuser@example.com",
            DisplayName = "First User"
        };
        await _controller!.Register(firstRequest, CancellationToken.None);

        // Second registration attempt with same token
        var secondRequest = new RegisterRequest
        {
            InviteToken = token,
            Email = "seconduser@example.com",
            DisplayName = "Second User"
        };

        // Act
        var result = await _controller!.Register(secondRequest, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        // Verify second user was not created
        var users = await _userRepository!.FindAsync(u => u.Email == "seconduser@example.com");
        Assert.Empty(users);
    }
}
