namespace OneVault.App.Models;

public sealed record EncryptedPayload(byte[] Ciphertext, byte[] Nonce, byte[] Tag);