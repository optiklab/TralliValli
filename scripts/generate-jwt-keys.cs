using System;
using System.Security.Cryptography;

/// <summary>
/// Script to generate RSA key pair for JWT authentication
/// Usage: dotnet run scripts/generate-jwt-keys.cs
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("Generating RSA key pair for JWT authentication...");
        Console.WriteLine();
        
        using var rsa = RSA.Create(2048);
        
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        
        Console.WriteLine("JWT Private Key:");
        Console.WriteLine(privateKeyPem);
        Console.WriteLine();
        Console.WriteLine("JWT Public Key:");
        Console.WriteLine(publicKeyPem);
        Console.WriteLine();
        Console.WriteLine("Instructions:");
        Console.WriteLine("1. Copy the private key to Jwt:PrivateKey in appsettings.Development.json");
        Console.WriteLine("2. Copy the public key to Jwt:PublicKey in appsettings.Development.json");
        Console.WriteLine("3. For production, use environment variables JWT_PRIVATE_KEY and JWT_PUBLIC_KEY");
        Console.WriteLine("4. Keep these keys secure and never commit them to source control");
    }
}