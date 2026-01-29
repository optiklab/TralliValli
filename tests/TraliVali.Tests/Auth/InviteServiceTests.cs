using MongoDB.Driver;
using Testcontainers.MongoDb;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Tests for InviteService
/// </summary>
public class InviteServiceTests : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private MongoDbContext? _mongoContext;
    private InviteService? _inviteService;
    private const string TestSigningKey = "test-signing-key-for-hmac-sha256-minimum-32-chars";

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:6.0")
            .Build();
        await _mongoContainer.StartAsync();

        var connectionString = _mongoContainer.GetConnectionString();
        _mongoContext = new MongoDbContext(connectionString, "test_tralivali");
        await _mongoContext.CreateIndexesAsync();

        _inviteService = new InviteService(_mongoContext.Invites, TestSigningKey);
    }

    public async Task DisposeAsync()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_ShouldCreateValidSignedToken()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var expiryHours = 24;

        // Act
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Assert
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // Should have token.signature format
        var parts = token.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.True(parts[0].Length > 20); // Token part should be sufficiently long
        Assert.True(parts[1].Length > 20); // Signature part should be sufficiently long
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_ShouldThrowException_WhenInviterIdIsEmpty()
    {
        // Arrange
        var expiryHours = 24;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _inviteService!.GenerateInviteLinkAsync("", expiryHours));
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_ShouldThrowException_WhenExpiryHoursIsZero()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _inviteService!.GenerateInviteLinkAsync(inviterId, 0));
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_ShouldThrowException_WhenExpiryHoursIsNegative()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _inviteService!.GenerateInviteLinkAsync(inviterId, -1));
    }

    [Fact]
    public async Task GenerateInviteQrCode_ShouldReturnBase64Image()
    {
        // Arrange
        var inviteLink = "https://example.com/invite/test-token";

        // Act
        var qrCode = _inviteService!.GenerateInviteQrCode(inviteLink);

        // Assert
        Assert.NotEmpty(qrCode);
        
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(qrCode);
        Assert.NotEmpty(bytes);
        
        // Verify it's a PNG image (PNG starts with specific bytes)
        Assert.Equal(0x89, bytes[0]); // PNG signature first byte
        Assert.Equal(0x50, bytes[1]); // PNG signature second byte
        Assert.Equal(0x4E, bytes[2]); // PNG signature third byte
        Assert.Equal(0x47, bytes[3]); // PNG signature fourth byte
    }

    [Fact]
    public void GenerateInviteQrCode_ShouldThrowException_WhenInviteLinkIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _inviteService!.GenerateInviteQrCode(""));
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnValidResult_WhenTokenIsValid()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var expiryHours = 24;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Act
        var result = await _inviteService.ValidateInviteAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token, result.Token);
        Assert.Equal(inviterId, result.InviterId);
        Assert.False(result.IsUsed);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnNull_WhenTokenIsInvalid()
    {
        // Act
        var result = await _inviteService!.ValidateInviteAsync("invalid-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnNull_WhenTokenIsEmpty()
    {
        // Act
        var result = await _inviteService!.ValidateInviteAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnNull_WhenTokenIsExpired()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var expiryHours = 1;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Manually set expiry to past in database
        var filter = Builders<Invite>.Filter.Eq(i => i.Token, token);
        var update = Builders<Invite>.Update.Set(i => i.ExpiresAt, DateTime.UtcNow.AddHours(-1));
        await _mongoContext!.Invites.UpdateOneAsync(filter, update);

        // Act
        var result = await _inviteService.ValidateInviteAsync(token);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnNull_WhenTokenIsAlreadyUsed()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var userId = "507f1f77bcf86cd799439012";
        var expiryHours = 24;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Redeem the invite first
        await _inviteService.RedeemInviteAsync(token, userId);

        // Act
        var result = await _inviteService.ValidateInviteAsync(token);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RedeemInviteAsync_ShouldReturnTrue_WhenTokenIsValid()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var userId = "507f1f77bcf86cd799439012";
        var expiryHours = 24;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Act
        var result = await _inviteService.RedeemInviteAsync(token, userId);

        // Assert
        Assert.True(result);

        // Verify invite is marked as used in database
        var filter = Builders<Invite>.Filter.Eq(i => i.Token, token);
        var invite = await _mongoContext!.Invites.Find(filter).FirstOrDefaultAsync();
        Assert.NotNull(invite);
        Assert.True(invite.IsUsed);
        Assert.Equal(userId, invite.UsedBy);
        Assert.NotNull(invite.UsedAt);
    }

    [Fact]
    public async Task RedeemInviteAsync_ShouldReturnFalse_WhenTokenIsInvalid()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439012";

        // Act
        var result = await _inviteService!.RedeemInviteAsync("invalid-token", userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RedeemInviteAsync_ShouldReturnFalse_WhenTokenIsEmpty()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439012";

        // Act
        var result = await _inviteService!.RedeemInviteAsync("", userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RedeemInviteAsync_ShouldReturnFalse_WhenUserIdIsEmpty()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var expiryHours = 24;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Act
        var result = await _inviteService.RedeemInviteAsync(token, "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RedeemInviteAsync_ShouldBeSingleUse()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var userId1 = "507f1f77bcf86cd799439012";
        var userId2 = "507f1f77bcf86cd799439013";
        var expiryHours = 24;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Act - First redemption should succeed
        var firstResult = await _inviteService.RedeemInviteAsync(token, userId1);
        Assert.True(firstResult);

        // Act - Second redemption should fail (single-use)
        var secondResult = await _inviteService.RedeemInviteAsync(token, userId2);

        // Assert
        Assert.False(secondResult);

        // Verify only the first user redeemed it
        var filter = Builders<Invite>.Filter.Eq(i => i.Token, token);
        var invite = await _mongoContext!.Invites.Find(filter).FirstOrDefaultAsync();
        Assert.NotNull(invite);
        Assert.Equal(userId1, invite.UsedBy);
    }

    [Fact]
    public async Task RedeemInviteAsync_ShouldReturnFalse_WhenTokenIsExpired()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var userId = "507f1f77bcf86cd799439012";
        var expiryHours = 1;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Manually set expiry to past in database
        var filter = Builders<Invite>.Filter.Eq(i => i.Token, token);
        var update = Builders<Invite>.Update.Set(i => i.ExpiresAt, DateTime.UtcNow.AddHours(-1));
        await _mongoContext!.Invites.UpdateOneAsync(filter, update);

        // Act
        var result = await _inviteService.RedeemInviteAsync(token, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Invite_ShouldExpireAtConfiguredTime()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var expiryHours = 48;
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Get the invite to check expiry
        var result = await _inviteService.ValidateInviteAsync(token);
        Assert.NotNull(result);

        // Assert - Expiry should be approximately expiryHours from now
        var expectedExpiry = DateTime.UtcNow.AddHours(expiryHours);
        var timeDifference = Math.Abs((result.ExpiresAt - expectedExpiry).TotalSeconds);
        Assert.True(timeDifference < 5, $"Expiry time difference was {timeDifference} seconds, expected less than 5 seconds");
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var expiryHours = 24;

        // Act - Create multiple invite links
        var token1 = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);
        var token2 = await _inviteService.GenerateInviteLinkAsync(inviterId, expiryHours);
        var token3 = await _inviteService.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Assert - All tokens should be unique
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token2, token3);
        Assert.NotEqual(token1, token3);
    }

    [Fact]
    public async Task Invite_ShouldBeStoredInMongoDb()
    {
        // Arrange
        var inviterId = "507f1f77bcf86cd799439011";
        var expiryHours = 24;

        // Act
        var token = await _inviteService!.GenerateInviteLinkAsync(inviterId, expiryHours);

        // Assert - Verify invite exists in MongoDB
        var filter = Builders<Invite>.Filter.Eq(i => i.Token, token);
        var invite = await _mongoContext!.Invites.Find(filter).FirstOrDefaultAsync();
        
        Assert.NotNull(invite);
        Assert.Equal(token, invite.Token);
        Assert.Equal(inviterId, invite.InviterId);
        Assert.False(invite.IsUsed);
        Assert.Null(invite.UsedBy);
        Assert.Null(invite.UsedAt);
    }
}
