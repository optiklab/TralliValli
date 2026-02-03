using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Api.Controllers;
using TraliVali.Api.Models;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Messaging;

namespace TraliVali.Tests.Controllers;

/// <summary>
/// Tests for AuthController system status and bootstrap registration
/// </summary>
public class AuthControllerBootstrapTests
{
    private readonly Mock<IMagicLinkService> _mockMagicLinkService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<ITokenBlacklistService> _mockTokenBlacklistService;
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<IRepository<Invite>> _mockInviteRepository;
    private readonly Mock<IInviteService> _mockInviteService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerBootstrapTests()
    {
        _mockMagicLinkService = new Mock<IMagicLinkService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockTokenBlacklistService = new Mock<ITokenBlacklistService>();
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockInviteRepository = new Mock<IRepository<Invite>>();
        _mockInviteService = new Mock<IInviteService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _mockMagicLinkService.Object,
            _mockJwtService.Object,
            _mockTokenBlacklistService.Object,
            _mockUserRepository.Object,
            _mockInviteRepository.Object,
            _mockInviteService.Object,
            _mockEmailService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetSystemStatus_ReturnsNotBootstrapped_WhenNoUsersExist()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _controller.GetSystemStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SystemStatusResponse>(okResult.Value);
        Assert.False(response.IsBootstrapped);
        Assert.False(response.RequiresInvite);
    }

    [Fact]
    public async Task GetSystemStatus_ReturnsBootstrapped_WhenUsersExist()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = "507f1f77bcf86cd799439011",
                Email = "admin@example.com",
                DisplayName = "Admin",
                Role = "admin"
            }
        };

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetSystemStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SystemStatusResponse>(okResult.Value);
        Assert.True(response.IsBootstrapped);
        Assert.True(response.RequiresInvite);
    }

    [Fact]
    public async Task Register_AllowsWithoutInvite_WhenNotBootstrapped()
    {
        // Arrange
        var request = new RegisterRequest
        {
            InviteToken = null,
            Email = "admin@example.com",
            DisplayName = "Admin User"
        };

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>()); // No users exist

        var createdUser = new User
        {
            Id = "507f1f77bcf86cd799439011",
            Email = request.Email.ToLowerInvariant(),
            DisplayName = request.DisplayName,
            Role = "admin"
        };

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        _mockJwtService
            .Setup(s => s.GenerateToken(It.IsAny<User>(), It.IsAny<string>()))
            .Returns(new TokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(7)
            });

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RegisterResponse>(okResult.Value);
        Assert.NotNull(response.AccessToken);
        Assert.NotNull(response.RefreshToken);

        // Verify user was created with admin role
        _mockUserRepository.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.Email == request.Email.ToLowerInvariant() && u.Role == "admin"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        // Verify invite service was not called
        _mockInviteService.Verify(
            s => s.ValidateInviteAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Register_RequiresInvite_WhenBootstrapped()
    {
        // Arrange
        var existingUsers = new List<User>
        {
            new User
            {
                Id = "507f1f77bcf86cd799439011",
                Email = "existing@example.com",
                DisplayName = "Existing User",
                Role = "admin"
            }
        };

        var request = new RegisterRequest
        {
            InviteToken = null, // No invite provided
            Email = "newuser@example.com",
            DisplayName = "New User"
        };

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUsers); // Users exist - bootstrapped

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_AssignsAdminRole_ToFirstUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            InviteToken = null,
            Email = "admin@example.com",
            DisplayName = "Admin User"
        };

        _mockUserRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>()); // No users exist

        var createdUser = new User
        {
            Id = "507f1f77bcf86cd799439011",
            Email = request.Email.ToLowerInvariant(),
            DisplayName = request.DisplayName,
            Role = "admin"
        };

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        _mockJwtService
            .Setup(s => s.GenerateToken(It.IsAny<User>(), It.IsAny<string>()))
            .Returns(new TokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(7)
            });

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify admin role was assigned
        _mockUserRepository.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.Role == "admin"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Register_AssignsUserRole_ToSubsequentUsers()
    {
        // Arrange
        var existingUsers = new List<User>
        {
            new User
            {
                Id = "507f1f77bcf86cd799439011",
                Email = "admin@example.com",
                DisplayName = "Admin",
                Role = "admin"
            }
        };

        var request = new RegisterRequest
        {
            InviteToken = "valid-invite-token",
            Email = "user@example.com",
            DisplayName = "Regular User"
        };

        _mockUserRepository
            .SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUsers) // First call: check if bootstrapped
            .ReturnsAsync(new List<User>()); // Second call: check if user exists

        _mockInviteService
            .Setup(s => s.ValidateInviteAsync(request.InviteToken))
            .ReturnsAsync(new InviteValidationResult
            {
                Token = request.InviteToken,
                InviterId = "507f1f77bcf86cd799439011",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false
            });

        var createdUser = new User
        {
            Id = "507f1f77bcf86cd799439012",
            Email = request.Email.ToLowerInvariant(),
            DisplayName = request.DisplayName,
            Role = "user"
        };

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        _mockInviteService
            .Setup(s => s.RedeemInviteAsync(request.InviteToken, It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockJwtService
            .Setup(s => s.GenerateToken(It.IsAny<User>(), It.IsAny<string>()))
            .Returns(new TokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(7)
            });

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify user role was assigned (not admin)
        _mockUserRepository.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.Role == "user"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
