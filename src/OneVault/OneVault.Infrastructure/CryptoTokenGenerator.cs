using System.Security.Cryptography;
using OneVault.App.Interfaces;

namespace OneVault.Infrastructure;

public sealed class CryptoTokenGenerator : ITokenGenerator
{
    public string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}