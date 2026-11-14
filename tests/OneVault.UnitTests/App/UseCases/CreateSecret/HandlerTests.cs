using AutoFixture.Xunit2;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OneVault.App.Errors;
using OneVault.App.Interfaces;
using OneVault.App.Models;
using OneVault.UnitTests.Shared;
using Feature = OneVault.App.UseCases.CreateSecret.CreateSecret;

namespace OneVault.UnitTests.App.UseCases.CreateSecret;

public class HandlerTests
{
    [Theory]
    [AutoMoqData]
    public async Task StoresEncryptedPayloadAndCorrectDeadline(
        [Frozen] Mock<IValidator<Feature.Command>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] Mock<ITokenGenerator> tokens,
        [Frozen] ManualTimeProvider time,
        Feature.Command command, EncryptedPayload payload, string token, Feature.Handler sut)
    {
        validator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        tokens.Setup(x => x.Generate()).Returns(token);
        encryption.Setup(x => x.Encrypt(command.Text)).Returns(payload);
        store.Setup(x => x.TryAdd(token, It.IsAny<StoredSecret>())).Returns(true);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should()
            .Be(new Feature.Secret(token, time.Now.AddSeconds(command.TtlSeconds)));
        store.Verify(x => x.TryAdd(token, It.Is<StoredSecret>(secret =>
            ReferenceEquals(secret.Payload, payload) &&
            secret.ExpiresAt == result.Value.ExpiresAt)), Times.Once);
        encryption.Verify(x => x.Encrypt(command.Text), Times.Once);
    }

    [Theory]
    [AutoMoqData]
    public async Task ReturnsCapacityFailure(
        [Frozen] Mock<IValidator<Feature.Command>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] Mock<ITokenGenerator> tokens,
        Feature.Command command, EncryptedPayload payload, string token, Feature.Handler sut)
    {
        validator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        tokens.Setup(x => x.Generate()).Returns(token);
        encryption.Setup(x => x.Encrypt(command.Text)).Returns(payload);
        store.Setup(x => x.TryAdd(token, It.IsAny<StoredSecret>())).Returns(false);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<SecretFailure>()
            .Which.Code.Should().Be(SecretError.CapacityExceeded);
    }

    [Theory]
    [InlineAutoMoqData("", 60)]
    [InlineAutoMoqData("secret", 0)]
    public async Task InvalidCommandDoesNotTouchDependencies(string text, int ttl,
        [Frozen] Mock<IValidator<Feature.Command>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] Mock<ITokenGenerator> tokens,
        Feature.Handler sut)
    {
        var command = new Feature.Command(text, ttl);
        validator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ValidationResult([
                    new ValidationFailure(nameof(Feature.Command.Text), "Invalid command")
                ]));

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty().And.AllSatisfy(error =>
            error.Should().BeOfType<SecretFailure>().Which.Code.Should()
                .Be(SecretError.InvalidInput));
        store.VerifyNoOtherCalls();
        encryption.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task CancelledCommandDoesNotTouchDependencies(
        [Frozen] Mock<IValidator<Feature.Command>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] Mock<ITokenGenerator> tokens,
        Feature.Command command, Feature.Handler sut)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => sut.Handle(command, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        validator.VerifyNoOtherCalls();
        store.VerifyNoOtherCalls();
        encryption.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
    }
}