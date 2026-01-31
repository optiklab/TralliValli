using System.Security.Cryptography;
using System.Text;
using TraliVali.Auth;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Tests for MessageEncryptionService
/// </summary>
public class MessageEncryptionServiceTests
{
    private readonly MessageEncryptionService _service;

    public MessageEncryptionServiceTests()
    {
        _service = new MessageEncryptionService();
    }

    #region DecryptMessageAsync Tests

    [Fact]
    public async Task DecryptMessageAsync_ShouldDecryptMessage_WhenParametersAreValid()
    {
        // Arrange
        var originalMessage = "Hello, this is a secret message!";
        var conversationKey = new byte[32]; // 256-bit key
        RandomNumberGenerator.Fill(conversationKey);

        var iv = new byte[12]; // 96-bit IV for GCM
        RandomNumberGenerator.Fill(iv);

        var plaintext = Encoding.UTF8.GetBytes(originalMessage);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16]; // 128-bit tag

        using (var aes = new AesGcm(conversationKey, tag.Length))
        {
            aes.Encrypt(iv, plaintext, ciphertext, tag);
        }

        var encryptedContent = Convert.ToBase64String(ciphertext);
        var ivBase64 = Convert.ToBase64String(iv);
        var tagBase64 = Convert.ToBase64String(tag);

        // Act
        var result = await _service.DecryptMessageAsync(encryptedContent, conversationKey, ivBase64, tagBase64);

        // Assert
        Assert.Equal(originalMessage, result);
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenEncryptedContentIsEmpty()
    {
        // Arrange
        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptMessageAsync("", conversationKey, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenEncryptedContentIsNull()
    {
        // Arrange
        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptMessageAsync(null!, conversationKey, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenConversationKeyIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptMessageAsync("validContent", null!, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenConversationKeyIsWrongSize()
    {
        // Arrange
        var conversationKey = new byte[16]; // Wrong size (should be 32)

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptMessageAsync("validContent", conversationKey, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenIvIsEmpty()
    {
        // Arrange
        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptMessageAsync("validContent", conversationKey, "", "validTag"));
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenTagIsEmpty()
    {
        // Arrange
        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptMessageAsync("validContent", conversationKey, "validIv", ""));
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenEncryptedContentIsInvalidBase64()
    {
        // Arrange
        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);
        var iv = Convert.ToBase64String(new byte[12]);
        var tag = Convert.ToBase64String(new byte[16]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DecryptMessageAsync("invalid-base64!!!", conversationKey, iv, tag));
    }

    [Fact]
    public async Task DecryptMessageAsync_ShouldThrowException_WhenTagIsIncorrect()
    {
        // Arrange
        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        var iv = new byte[12];
        RandomNumberGenerator.Fill(iv);

        var plaintext = Encoding.UTF8.GetBytes("Test message");
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(conversationKey, tag.Length))
        {
            aes.Encrypt(iv, plaintext, ciphertext, tag);
        }

        // Use a different tag to cause authentication failure
        var wrongTag = new byte[16];
        RandomNumberGenerator.Fill(wrongTag);

        var encryptedContent = Convert.ToBase64String(ciphertext);
        var ivBase64 = Convert.ToBase64String(iv);
        var wrongTagBase64 = Convert.ToBase64String(wrongTag);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DecryptMessageAsync(encryptedContent, conversationKey, ivBase64, wrongTagBase64));
    }

    #endregion

    #region DeriveMasterKeyFromPasswordAsync Tests

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldDeriveKey_WhenParametersAreValid()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        var saltBase64 = Convert.ToBase64String(salt);

        // Act
        var result = await _service.DeriveMasterKeyFromPasswordAsync(password, saltBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, result.Length); // 256 bits
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldProduceDifferentKeys_ForDifferentPasswords()
    {
        // Arrange
        var password1 = "Password1";
        var password2 = "Password2";
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        var saltBase64 = Convert.ToBase64String(salt);

        // Act
        var key1 = await _service.DeriveMasterKeyFromPasswordAsync(password1, saltBase64);
        var key2 = await _service.DeriveMasterKeyFromPasswordAsync(password2, saltBase64);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldProduceDifferentKeys_ForDifferentSalts()
    {
        // Arrange
        var password = "MyPassword";
        var salt1 = new byte[16];
        var salt2 = new byte[16];
        RandomNumberGenerator.Fill(salt1);
        RandomNumberGenerator.Fill(salt2);

        // Act
        var key1 = await _service.DeriveMasterKeyFromPasswordAsync(password, Convert.ToBase64String(salt1));
        var key2 = await _service.DeriveMasterKeyFromPasswordAsync(password, Convert.ToBase64String(salt2));

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldProduceSameKey_ForSameInputs()
    {
        // Arrange
        var password = "MyPassword";
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        var saltBase64 = Convert.ToBase64String(salt);

        // Act
        var key1 = await _service.DeriveMasterKeyFromPasswordAsync(password, saltBase64);
        var key2 = await _service.DeriveMasterKeyFromPasswordAsync(password, saltBase64);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldThrowException_WhenPasswordIsEmpty()
    {
        // Arrange
        var salt = Convert.ToBase64String(new byte[16]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeriveMasterKeyFromPasswordAsync("", salt));
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldThrowException_WhenPasswordIsNull()
    {
        // Arrange
        var salt = Convert.ToBase64String(new byte[16]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeriveMasterKeyFromPasswordAsync(null!, salt));
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldThrowException_WhenSaltIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeriveMasterKeyFromPasswordAsync("password", ""));
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldThrowException_WhenSaltIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeriveMasterKeyFromPasswordAsync("password", null!));
    }

    [Fact]
    public async Task DeriveMasterKeyFromPasswordAsync_ShouldThrowException_WhenSaltIsInvalidBase64()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeriveMasterKeyFromPasswordAsync("password", "invalid-base64!!!"));
    }

    #endregion

    #region DecryptConversationKeyAsync Tests

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldDecryptKey_WhenParametersAreValid()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);

        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        var iv = new byte[12];
        RandomNumberGenerator.Fill(iv);

        var ciphertext = new byte[conversationKey.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(masterKey, tag.Length))
        {
            aes.Encrypt(iv, conversationKey, ciphertext, tag);
        }

        var encryptedKey = Convert.ToBase64String(ciphertext);
        var ivBase64 = Convert.ToBase64String(iv);
        var tagBase64 = Convert.ToBase64String(tag);

        // Act
        var result = await _service.DecryptConversationKeyAsync(encryptedKey, masterKey, ivBase64, tagBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, result.Length);
        Assert.Equal(conversationKey, result);
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenEncryptedKeyIsEmpty()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptConversationKeyAsync("", masterKey, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenEncryptedKeyIsNull()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptConversationKeyAsync(null!, masterKey, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenMasterKeyIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptConversationKeyAsync("validKey", null!, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenMasterKeyIsWrongSize()
    {
        // Arrange
        var masterKey = new byte[16]; // Wrong size (should be 32)

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptConversationKeyAsync("validKey", masterKey, "validIv", "validTag"));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenIvIsEmpty()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptConversationKeyAsync("validKey", masterKey, "", "validTag"));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenTagIsEmpty()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecryptConversationKeyAsync("validKey", masterKey, "validIv", ""));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenEncryptedKeyIsInvalidBase64()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);
        var iv = Convert.ToBase64String(new byte[12]);
        var tag = Convert.ToBase64String(new byte[16]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DecryptConversationKeyAsync("invalid-base64!!!", masterKey, iv, tag));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldThrowException_WhenTagIsIncorrect()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);

        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        var iv = new byte[12];
        RandomNumberGenerator.Fill(iv);

        var ciphertext = new byte[conversationKey.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(masterKey, tag.Length))
        {
            aes.Encrypt(iv, conversationKey, ciphertext, tag);
        }

        // Use a different tag to cause authentication failure
        var wrongTag = new byte[16];
        RandomNumberGenerator.Fill(wrongTag);

        var encryptedKey = Convert.ToBase64String(ciphertext);
        var ivBase64 = Convert.ToBase64String(iv);
        var wrongTagBase64 = Convert.ToBase64String(wrongTag);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DecryptConversationKeyAsync(encryptedKey, masterKey, ivBase64, wrongTagBase64));
    }

    [Fact]
    public async Task DecryptConversationKeyAsync_ShouldReturnDifferentKey_WhenWrongMasterKeyUsed()
    {
        // Arrange
        var masterKey = new byte[32];
        RandomNumberGenerator.Fill(masterKey);

        var wrongMasterKey = new byte[32];
        RandomNumberGenerator.Fill(wrongMasterKey);

        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        var iv = new byte[12];
        RandomNumberGenerator.Fill(iv);

        var ciphertext = new byte[conversationKey.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(masterKey, tag.Length))
        {
            aes.Encrypt(iv, conversationKey, ciphertext, tag);
        }

        var encryptedKey = Convert.ToBase64String(ciphertext);
        var ivBase64 = Convert.ToBase64String(iv);
        var tagBase64 = Convert.ToBase64String(tag);

        // Act & Assert - Should throw because authentication will fail with wrong key
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DecryptConversationKeyAsync(encryptedKey, wrongMasterKey, ivBase64, tagBase64));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullEncryptionWorkflow_ShouldWork_EndToEnd()
    {
        // Arrange
        var password = "UserMasterPassword123!";
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        var saltBase64 = Convert.ToBase64String(salt);

        // Step 1: Derive master key from password
        var masterKey = await _service.DeriveMasterKeyFromPasswordAsync(password, saltBase64);

        // Step 2: Create a conversation key
        var conversationKey = new byte[32];
        RandomNumberGenerator.Fill(conversationKey);

        // Step 3: Encrypt conversation key with master key
        var iv1 = new byte[12];
        RandomNumberGenerator.Fill(iv1);
        var ciphertext1 = new byte[conversationKey.Length];
        var tag1 = new byte[16];

        using (var aes = new AesGcm(masterKey, tag1.Length))
        {
            aes.Encrypt(iv1, conversationKey, ciphertext1, tag1);
        }

        var encryptedConversationKey = Convert.ToBase64String(ciphertext1);
        var iv1Base64 = Convert.ToBase64String(iv1);
        var tag1Base64 = Convert.ToBase64String(tag1);

        // Step 4: Decrypt conversation key
        var decryptedConversationKey = await _service.DecryptConversationKeyAsync(
            encryptedConversationKey, masterKey, iv1Base64, tag1Base64);

        // Step 5: Encrypt a message with the conversation key
        var message = "Secret message for conversation";
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var iv2 = new byte[12];
        RandomNumberGenerator.Fill(iv2);
        var ciphertext2 = new byte[messageBytes.Length];
        var tag2 = new byte[16];

        using (var aes = new AesGcm(decryptedConversationKey, tag2.Length))
        {
            aes.Encrypt(iv2, messageBytes, ciphertext2, tag2);
        }

        var encryptedMessage = Convert.ToBase64String(ciphertext2);
        var iv2Base64 = Convert.ToBase64String(iv2);
        var tag2Base64 = Convert.ToBase64String(tag2);

        // Step 6: Decrypt the message
        var decryptedMessage = await _service.DecryptMessageAsync(
            encryptedMessage, decryptedConversationKey, iv2Base64, tag2Base64);

        // Assert
        Assert.Equal(message, decryptedMessage);
    }

    #endregion
}
