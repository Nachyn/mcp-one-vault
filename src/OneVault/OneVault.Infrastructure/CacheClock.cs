using Microsoft.Extensions.Internal;

namespace OneVault.Infrastructure;

#pragma warning disable CS0618 // MemoryCache still exposes ISystemClock as its deterministic time hook.
internal sealed class CacheClock(TimeProvider time) : ISystemClock
{
    public DateTimeOffset UtcNow => time.GetUtcNow();
}
#pragma warning restore CS0618