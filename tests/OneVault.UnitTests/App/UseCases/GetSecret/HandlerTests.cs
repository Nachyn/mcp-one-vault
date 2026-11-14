using AutoFixture.Xunit2;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OneVault.App.Errors;
using OneVault.App.Interfaces;
using OneVault.App.Models;
using OneVault.UnitTests.Shared;
using Feature = OneVault.App.UseCases.GetSecret.GetSecret;

namespace OneVault.UnitTests.App.UseCases.GetSecret;

public class HandlerTests
{
    [Theory]
    [AutoMoqData]
    public async Task DecryptsExistingSecret(
        [Frozen] Mock<IValidator<Feature.Query>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] ManualTimeProvider time,
        Feature.Query query, EncryptedPayload payload, string text, Feature.Handler sut)
    {
        validator.Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        var deadline = time.Now.AddMinutes(1);
        store.Setup(x => x.Get(query.Token)).Returns(new StoredSecret(payload, deadline));
        encryption.Setup(x => x.Decrypt(payload)).Returns(text);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new Feature.Secret(text, deadline));
        encryption.Verify(x => x.Decrypt(payload), Times.Once);
    }

    [Theory]
    [AutoMoqData]
    public async Task MissingSecretIsNotDecrypted(
        [Frozen] Mock<IValidator<Feature.Query>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        Feature.Query query, Feature.Handler sut)
    {
        validator.Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        store.Setup(x => x.Get(query.Token)).Returns((StoredSecret?) null);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<SecretFailure>()
            .Which.Code.Should().Be(SecretError.NotFound);
        encryption.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineAutoMoqData(-1)]
    [InlineAutoMoqData(0)]
    public async Task ExpiredSecretIsNotDecrypted(int secondsRemaining,
        [Frozen] Mock<IValidator<Feature.Query>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] ManualTimeProvider time,
        Feature.Query query, EncryptedPayload payload, Feature.Handler sut)
    {
        validator.Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        store.Setup(x => x.Get(query.Token))
            .Returns(new StoredSecret(payload, time.Now.AddSeconds(secondsRemaining)));

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<SecretFailure>()
            .Which.Code.Should().Be(SecretError.NotFound);
        encryption.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task InvalidQueryDoesNotTouchDependencies(
        [Frozen] Mock<IValidator<Feature.Query>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        Feature.Handler sut)
    {
        var query = new Feature.Query("bad token");
        validator.Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([
                new ValidationFailure(nameof(Feature.Query.Token), "Invalid token")
            ]));

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty().And.AllSatisfy(error =>
            error.Should().BeOfType<SecretFailure>().Which.Code.Should()
                .Be(SecretError.InvalidInput));
        store.VerifyNoOtherCalls();
        encryption.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task CancelledQueryDoesNotTouchDependencies(
        [Frozen] Mock<IValidator<Feature.Query>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        Feature.Query query, Feature.Handler sut)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => sut.Handle(query, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        validator.VerifyNoOtherCalls();
        store.VerifyNoOtherCalls();
        encryption.VerifyNoOtherCalls();
    }
}