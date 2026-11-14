using AutoFixture.Xunit2;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using OneVault.App.Interfaces;
using OneVault.App.Models;
using OneVault.UnitTests.Shared;
using Feature = OneVault.App.UseCases.CreateSecret.CreateSecret;

namespace OneVault.UnitTests.App.UseCases.CreateSecret;

public class LoggerTests
{
    [Theory]
    [AutoMoqData]
    public async Task LogsDoNotExposeTextOrToken(
        [Frozen] Mock<IValidator<Feature.Command>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] Mock<ITokenGenerator> tokens,
        [Frozen] Mock<ILogger<Feature.Handler>> logger,
        Feature.Command command, EncryptedPayload payload, string token, Feature.Handler sut)
    {
        validator.SetupSequence(x =>
                x.ValidateAsync(It.IsAny<Feature.Command>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult())
            .ReturnsAsync(new ValidationResult())
            .ReturnsAsync(new ValidationResult([
                new ValidationFailure(nameof(Feature.Command.TtlSeconds), "Invalid TTL")
            ]));
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        tokens.Setup(x => x.Generate()).Returns(token);
        encryption.Setup(x => x.Encrypt(command.Text)).Returns(payload);
        store.SetupSequence(x => x.TryAdd(token, It.IsAny<StoredSecret>())).Returns(true)
            .Returns(false);

        (await sut.Handle(command, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await sut.Handle(command, CancellationToken.None)).IsFailed.Should().BeTrue();
        (await sut.Handle(command with {TtlSeconds = 0}, CancellationToken.None)).IsFailed.Should()
            .BeTrue();

        var states = logger.Invocations.Where(call => call.Method.Name == nameof(ILogger.Log))
            .Select(call => call.Arguments[2]).ToArray();
        states.Should().HaveCount(3);
        foreach (var state in states)
        {
            state.ToString().Should().NotContain(command.Text).And.NotContain(token);
            ((IEnumerable<KeyValuePair<string, object?>>) state).Should().AllSatisfy(property =>
                (property.Value?.ToString() ?? "").Should().NotContain(command.Text).And
                .NotContain(token));
        }
    }
}