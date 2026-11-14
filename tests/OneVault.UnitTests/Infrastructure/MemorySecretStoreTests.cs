using OneVault.App.Models;
using OneVault.Infrastructure;
using OneVault.UnitTests.Shared;

namespace OneVault.UnitTests.Infrastructure;

public class MemorySecretStoreTests
{
    [Theory]
    [AutoMoqData]
    public void RejectsCapacityAndReclaimsExpiredEntries(ManualTimeProvider time, Guid firstId,
        Guid secondId)
    {
        // This payload and capacity deliberately leave room for exactly one entry.
        using var sut = new MemorySecretStore(time, 300);
        var firstToken = firstId.ToString("N").PadRight(43, 'a');
        var secondToken = secondId.ToString("N").PadRight(43, 'a');
        var first = new StoredSecret(new EncryptedPayload(new byte[20], new byte[12], new byte[16]),
            time.Now.AddSeconds(1));
        var second = first with {ExpiresAt = time.Now.AddSeconds(60)};

        sut.TryAdd(firstToken, first).Should().BeTrue();
        sut.TryAdd(secondToken, second).Should().BeFalse();
        sut.Get(firstToken).Should().BeSameAs(first);
        time.Now = first.ExpiresAt;
        sut.Get(firstToken).Should().BeNull();
        sut.TryAdd(secondToken, second).Should().BeTrue();
    }

    [Theory]
    [AutoMoqData]
    public void DuplicateTokensDoNotOverwriteSecrets(ManualTimeProvider time,
        string token, EncryptedPayload firstPayload, EncryptedPayload secondPayload)
    {
        using var sut = new MemorySecretStore(time);
        var secret = new StoredSecret(firstPayload, time.Now.AddMinutes(1));

        sut.TryAdd(token, secret).Should().BeTrue();
        sut.TryAdd(token, secret with {Payload = secondPayload}).Should().BeFalse();
        sut.Get(token).Should().BeSameAs(secret);
    }

    [Theory]
    [AutoMoqData]
    public void ParallelWritesRespectCapacity(ManualTimeProvider time)
    {
        using var sut = new MemorySecretStore(time, 300);
        var secret = new StoredSecret(
            new EncryptedPayload(new byte[20], new byte[12], new byte[16]),
            time.Now.AddMinutes(1));
        var successes = 0;

        Parallel.For(0, 100, i =>
        {
            if (sut.TryAdd(i.ToString("D43"), secret))
            {
                Interlocked.Increment(ref successes);
            }
        });

        successes.Should().Be(1);
    }
}