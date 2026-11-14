namespace OneVault.App.Models;

public sealed record StoredSecret(EncryptedPayload Payload, DateTimeOffset ExpiresAt)
{
    public bool IsExpired(DateTimeOffset now)
    {
        return now >= ExpiresAt;
    }
}