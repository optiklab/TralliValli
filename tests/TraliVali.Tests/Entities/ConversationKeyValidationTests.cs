using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for ConversationKey entity validation following Given-When-Then pattern
/// </summary>
public class ConversationKeyValidationTests
{
    [Fact]
    public void GivenValidConversationKey_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = 1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyConversationId_WhenValidating_ThenReturnsConversationIdRequiredError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = 1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("ConversationId is required", errors);
    }

    [Fact]
    public void GivenEmptyEncryptedKey_WhenValidating_ThenReturnsEncryptedKeyRequiredError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = 1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("EncryptedKey is required", errors);
    }

    [Fact]
    public void GivenEmptyIv_WhenValidating_ThenReturnsIvRequiredError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = 1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("Iv is required", errors);
    }

    [Fact]
    public void GivenEmptySalt_WhenValidating_ThenReturnsSaltRequiredError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "",
            Tag = "base64encodedtag==",
            Version = 1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("Salt is required", errors);
    }

    [Fact]
    public void GivenEmptyTag_WhenValidating_ThenReturnsTagRequiredError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "",
            Version = 1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("Tag is required", errors);
    }

    [Fact]
    public void GivenZeroVersion_WhenValidating_ThenReturnsVersionMustBePositiveError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = 0
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("Version must be positive", errors);
    }

    [Fact]
    public void GivenNegativeVersion_WhenValidating_ThenReturnsVersionMustBePositiveError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = -1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("Version must be positive", errors);
    }

    [Fact]
    public void GivenMultipleInvalidFields_WhenValidating_ThenReturnsMultipleErrors()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "",
            EncryptedKey = "",
            Iv = "",
            Salt = "",
            Tag = "",
            Version = 0
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Equal(6, errors.Count);
        Assert.Contains("ConversationId is required", errors);
        Assert.Contains("EncryptedKey is required", errors);
        Assert.Contains("Iv is required", errors);
        Assert.Contains("Salt is required", errors);
        Assert.Contains("Tag is required", errors);
        Assert.Contains("Version must be positive", errors);
    }

    [Fact]
    public void GivenWhitespaceConversationId_WhenValidating_ThenReturnsConversationIdRequiredError()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "   ",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = 1
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Contains("ConversationId is required", errors);
    }

    [Fact]
    public void GivenVersionGreaterThanOne_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var conversationKey = new ConversationKey
        {
            ConversationId = "507f1f77bcf86cd799439011",
            EncryptedKey = "base64encodedencryptedkey==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt==",
            Tag = "base64encodedtag==",
            Version = 5
        };

        // Act
        var errors = conversationKey.Validate();

        // Assert
        Assert.Empty(errors);
    }
}
