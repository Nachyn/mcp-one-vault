using System.Security.Cryptography;
using System.Text;
using OneVault.App.Interfaces;
using OneVault.App.Models;

namespace OneVault.Infrastructure;

// A process-local key matches the lifetime of the volatile store. Never log or persist it.
public sealed class AesGcmEncryption : ISecretEncryption, IDisposable
{
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_key);
        _disposed = true;
    }

    public EncryptedPayload Encrypt(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var plaintext = Encoding.UTF8.GetBytes(text);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(_key, tag.Length);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
            return new EncryptedPayload(ciphertext, nonce, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public string Decrypt(EncryptedPayload payload)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var plaintext = new byte[payload.Ciphertext.Length];
        try
        {
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}