using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OneVault.App.Interfaces;
using OneVault.App.Models;

namespace OneVault.Infrastructure;

public sealed class MemorySecretStore : ISecretStore, IDisposable
{
    private readonly MemoryCache cache;
    private readonly object gate = new();
    private readonly TimeProvider time;

    public MemorySecretStore(TimeProvider time, long capacityBytes = 16 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacityBytes);
        this.time = time;
#pragma warning disable CS0618 // MemoryCache's clock hook is required for deterministic expiration.
        cache = new MemoryCache(Options.Create(new MemoryCacheOptions
        {
            SizeLimit = capacityBytes, Clock = new CacheClock(time)
        }));
#pragma warning restore CS0618
    }

    public void Dispose()
    {
        cache.Dispose();
    }

    public bool TryAdd(string token, StoredSecret secret)
    {
        lock (gate)
        {
            if (secret.IsExpired(time.GetUtcNow()) || cache.TryGetValue(token, out _))
            {
                return false;
            }

            // Reclaim expired entries synchronously before deciding whether there is capacity.
            cache.Compact(0);
            var size = secret.Payload.Ciphertext.LongLength + secret.Payload.Nonce.Length +
                       secret.Payload.Tag.Length + token.Length * 2L + 128;
            cache.Set(token, secret, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = secret.ExpiresAt,
                Size = size,
                Priority = CacheItemPriority.NeverRemove
            });
            return cache.TryGetValue(token, out StoredSecret? stored) &&
                   ReferenceEquals(stored, secret);
        }
    }

    public StoredSecret? Get(string token)
    {
        return cache.Get<StoredSecret>(token);
    }
}