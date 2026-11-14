using OneVault.Infrastructure;
using OneVault.UnitTests.Shared;

namespace OneVault.UnitTests.Infrastructure;

public class CryptoTokenGeneratorTests
{
    [Theory]
    [AutoMoqData]
    public void TokensAreUrlSafeAndUnique(CryptoTokenGenerator sut)
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => sut.Generate()).ToArray();

        tokens.Should().OnlyHaveUniqueItems();
        tokens.Should().AllSatisfy(token => token.Should().MatchRegex("^[A-Za-z0-9_-]{43}$"));
    }
}