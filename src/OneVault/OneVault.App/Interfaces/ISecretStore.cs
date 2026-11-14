using OneVault.App.Models;

namespace OneVault.App.Interfaces;

public interface ISecretStore
{
    bool TryAdd(string token, StoredSecret secret);
    StoredSecret? Get(string token);
}