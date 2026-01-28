using System.Security.Cryptography;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Helper class for generating RSA keys for testing
/// </summary>
public static class TestKeyGenerator
{
    private static readonly Lazy<(string PrivateKey, string PublicKey)> _keys = new(() => GenerateKeys());

    /// <summary>
    /// Gets the test private key
    /// </summary>
    public static string PrivateKey => _keys.Value.PrivateKey;

    /// <summary>
    /// Gets the test public key
    /// </summary>
    public static string PublicKey => _keys.Value.PublicKey;

    private static (string PrivateKey, string PublicKey) GenerateKeys()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();
        return (privateKey, publicKey);
    }
}
