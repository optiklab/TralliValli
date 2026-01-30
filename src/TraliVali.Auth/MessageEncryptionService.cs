using System.Security.Cryptography;
using System.Text;

namespace TraliVali.Auth;

/// <summary>
/// Service for encrypting and decrypting messages using AES-256-GCM
/// </summary>
public interface IMessageEncryptionService
{
    /// <summary>
    /// Decrypts an encrypted message using a conversation key
    /// </summary>
    /// <param name="encryptedContent">The encrypted message content (Base64 encoded)</param>
    /// <param name="conversationKey">The conversation encryption key</param>
    /// <param name="iv">The initialization vector (Base64 encoded)</param>
    /// <param name="tag">The authentication tag (Base64 encoded)</param>
    /// <returns>The decrypted message content</returns>
    Task<string> DecryptMessageAsync(string encryptedContent, byte[] conversationKey, string iv, string tag);

    /// <summary>
    /// Derives a master key from a password using PBKDF2
    /// </summary>
    /// <param name="password">The user's master password</param>
    /// <param name="salt">The salt for key derivation (Base64 encoded)</param>
    /// <returns>The derived master key</returns>
    Task<byte[]> DeriveMasterKeyFromPasswordAsync(string password, string salt);

    /// <summary>
    /// Decrypts a conversation key using the master key
    /// </summary>
    /// <param name="encryptedKey">The encrypted conversation key (Base64 encoded)</param>
    /// <param name="masterKey">The user's master key</param>
    /// <param name="iv">The initialization vector (Base64 encoded)</param>
    /// <param name="tag">The authentication tag (Base64 encoded)</param>
    /// <returns>The decrypted conversation key</returns>
    Task<byte[]> DecryptConversationKeyAsync(string encryptedKey, byte[] masterKey, string iv, string tag);
}

/// <summary>
/// Implementation of message encryption service using AES-256-GCM
/// </summary>
public class MessageEncryptionService : IMessageEncryptionService
{
    private const int Pbkdf2Iterations = 100000;
    private const int KeySizeBytes = 32; // 256 bits

    /// <inheritdoc/>
    public Task<string> DecryptMessageAsync(string encryptedContent, byte[] conversationKey, string iv, string tag)
    {
        if (string.IsNullOrWhiteSpace(encryptedContent))
            throw new ArgumentException("Encrypted content is required", nameof(encryptedContent));
        if (conversationKey == null || conversationKey.Length != KeySizeBytes)
            throw new ArgumentException($"Conversation key must be {KeySizeBytes} bytes", nameof(conversationKey));
        if (string.IsNullOrWhiteSpace(iv))
            throw new ArgumentException("IV is required", nameof(iv));
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag is required", nameof(tag));

        try
        {
            var ciphertext = Convert.FromBase64String(encryptedContent);
            var ivBytes = Convert.FromBase64String(iv);
            var tagBytes = Convert.FromBase64String(tag);

            using var aes = new AesGcm(conversationKey, tagBytes.Length);
            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(ivBytes, ciphertext, tagBytes, plaintext);

            return Task.FromResult(Encoding.UTF8.GetString(plaintext));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt message", ex);
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> DeriveMasterKeyFromPasswordAsync(string password, string salt)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));
        if (string.IsNullOrWhiteSpace(salt))
            throw new ArgumentException("Salt is required", nameof(salt));

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                passwordBytes,
                saltBytes,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256);

            return Task.FromResult(pbkdf2.GetBytes(KeySizeBytes));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to derive master key from password", ex);
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> DecryptConversationKeyAsync(string encryptedKey, byte[] masterKey, string iv, string tag)
    {
        if (string.IsNullOrWhiteSpace(encryptedKey))
            throw new ArgumentException("Encrypted key is required", nameof(encryptedKey));
        if (masterKey == null || masterKey.Length != KeySizeBytes)
            throw new ArgumentException($"Master key must be {KeySizeBytes} bytes", nameof(masterKey));
        if (string.IsNullOrWhiteSpace(iv))
            throw new ArgumentException("IV is required", nameof(iv));
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag is required", nameof(tag));

        try
        {
            var ciphertext = Convert.FromBase64String(encryptedKey);
            var ivBytes = Convert.FromBase64String(iv);
            var tagBytes = Convert.FromBase64String(tag);

            using var aes = new AesGcm(masterKey, tagBytes.Length);
            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(ivBytes, ciphertext, tagBytes, plaintext);

            return Task.FromResult(plaintext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt conversation key", ex);
        }
    }
}
