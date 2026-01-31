using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for UserKeyBackup entity validation following Given-When-Then pattern
/// </summary>
public class UserKeyBackupValidationTests
{
    [Fact]
    public void GivenValidUserKeyBackup_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyUserId_WhenValidating_ThenReturnsUserIdRequiredError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "",
            Version = 1,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("UserId is required", errors);
    }

    [Fact]
    public void GivenWhitespaceUserId_WhenValidating_ThenReturnsUserIdRequiredError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "   ",
            Version = 1,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("UserId is required", errors);
    }

    [Fact]
    public void GivenZeroVersion_WhenValidating_ThenReturnsVersionMustBePositiveError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 0,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("Version must be positive", errors);
    }

    [Fact]
    public void GivenNegativeVersion_WhenValidating_ThenReturnsVersionMustBePositiveError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = -1,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("Version must be positive", errors);
    }

    [Fact]
    public void GivenEmptyEncryptedData_WhenValidating_ThenReturnsEncryptedDataRequiredError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("EncryptedData is required", errors);
    }

    [Fact]
    public void GivenEmptyIv_WhenValidating_ThenReturnsIvRequiredError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("Iv is required", errors);
    }

    [Fact]
    public void GivenEmptySalt_WhenValidating_ThenReturnsSaltRequiredError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "base64encodediv==",
            Salt = ""
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("Salt is required", errors);
    }

    [Fact]
    public void GivenMultipleInvalidFields_WhenValidating_ThenReturnsMultipleErrors()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "",
            Version = 0,
            EncryptedData = "",
            Iv = "",
            Salt = ""
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Equal(5, errors.Count);
        Assert.Contains("UserId is required", errors);
        Assert.Contains("Version must be positive", errors);
        Assert.Contains("EncryptedData is required", errors);
        Assert.Contains("Iv is required", errors);
        Assert.Contains("Salt is required", errors);
    }

    [Fact]
    public void GivenWhitespaceEncryptedData_WhenValidating_ThenReturnsEncryptedDataRequiredError()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "   ",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Contains("EncryptedData is required", errors);
    }

    [Fact]
    public void GivenVersionGreaterThanOne_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var userKeyBackup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 5,
            EncryptedData = "base64encodedencrypteddata==",
            Iv = "base64encodediv==",
            Salt = "base64encodedsalt=="
        };

        // Act
        var errors = userKeyBackup.Validate();

        // Assert
        Assert.Empty(errors);
    }
}
