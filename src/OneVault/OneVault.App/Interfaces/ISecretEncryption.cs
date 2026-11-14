using OneVault.App.Models;

namespace OneVault.App.Interfaces;

public interface ISecretEncryption
{
    EncryptedPayload Encrypt(string text);
    string Decrypt(EncryptedPayload payload);
}