using OneVault.UnitTests.Shared;
using Feature = OneVault.App.UseCases.CreateSecret.CreateSecret;

namespace OneVault.UnitTests.App.UseCases.CreateSecret;

public class ValidatorTests
{
    [Theory]
    [InlineAutoMoqData(null)]
    [InlineAutoMoqData("")]
    [InlineAutoMoqData(" \r\n\t")]
    public void RejectsEmptyText(string? text, Feature.Validator sut)
    {
        sut.Validate(new Feature.Command(text!, 60)).IsValid.Should().BeFalse();
    }

    [Theory]
    [AutoMoqData]
    public void RejectsUnpairedSurrogate(Feature.Validator sut)
    {
        sut.Validate(new Feature.Command(new string((char) 0xD800, 1), 60)).IsValid.Should()
            .BeFalse();
    }

    [Theory]
    [InlineAutoMoqData(0, false)]
    [InlineAutoMoqData(-1, false)]
    [InlineAutoMoqData(1, true)]
    [InlineAutoMoqData(86400, true)]
    [InlineAutoMoqData(86401, false)]
    public void ChecksTtlBoundaries(int ttl, bool valid, string text, Feature.Validator sut)
    {
        sut.Validate(new Feature.Command(text, ttl)).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineAutoMoqData("a", 65536, true)]
    [InlineAutoMoqData("a", 65537, false)]
    [InlineAutoMoqData("я", 32768, true)]
    [InlineAutoMoqData("я", 32769, false)]
    [InlineAutoMoqData("🔑", 16384, true)]
    [InlineAutoMoqData("🔑", 16385, false)]
    public void MeasuresUtf8Bytes(string character, int count, bool valid, Feature.Validator sut)
    {
        var text = string.Concat(Enumerable.Repeat(character, count));
        sut.Validate(new Feature.Command(text, 60)).IsValid.Should().Be(valid);
    }
}