using OneVault.App.Models;
using OneVault.UnitTests.Shared;

namespace OneVault.UnitTests.App.Models;

public class StoredSecretTests
{
    [Theory]
    [InlineAutoMoqData(-1, false)]
    [InlineAutoMoqData(0, true)]
    [InlineAutoMoqData(1, true)]
    public void ExpirationIncludesExactDeadline(int delta, bool expired,
        EncryptedPayload payload, DateTimeOffset deadline)
    {
        var secret = new StoredSecret(payload, deadline);
        secret.IsExpired(deadline.AddTicks(delta)).Should().Be(expired);
    }
}