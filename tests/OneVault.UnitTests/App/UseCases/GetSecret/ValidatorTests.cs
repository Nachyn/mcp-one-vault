using OneVault.UnitTests.Shared;
using Feature = OneVault.App.UseCases.GetSecret.GetSecret;

namespace OneVault.UnitTests.App.UseCases.GetSecret;

public class ValidatorTests
{
    [Theory]
    [InlineAutoMoqData(null)]
    [InlineAutoMoqData("")]
    [InlineAutoMoqData("short")]
    [InlineAutoMoqData("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!")]
    public void RejectsInvalidTokens(string? token, Feature.Validator sut)
    {
        sut.Validate(new Feature.Query(token!)).IsValid.Should().BeFalse();
    }

    [Theory]
    [AutoMoqData]
    public void AcceptsUrlSafeToken(Guid id, Feature.Validator sut)
    {
        var token = id.ToString("N").PadRight(43, '_');
        sut.Validate(new Feature.Query(token)).IsValid.Should().BeTrue();
    }
}