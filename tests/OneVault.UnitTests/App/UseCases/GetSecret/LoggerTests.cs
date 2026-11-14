using AutoFixture.Xunit2;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using OneVault.App.Interfaces;
using OneVault.App.Models;
using OneVault.UnitTests.Shared;
using Feature = OneVault.App.UseCases.GetSecret.GetSecret;

namespace OneVault.UnitTests.App.UseCases.GetSecret;

public class LoggerTests
{
    [Theory]
    [AutoMoqData]
    public async Task LogsDoNotExposeTextOrToken(
        [Frozen] Mock<IValidator<Feature.Query>> validator,
        [Frozen] Mock<ISecretStore> store,
        [Frozen] Mock<ISecretEncryption> encryption,
        [Frozen] Mock<ILogger<Feature.Handler>> logger,
        [Frozen] ManualTimeProvider time,
        Feature.Query query, EncryptedPayload payload, string text, Feature.Handler sut)
    {
        validator.SetupSequence(x =>
                x.ValidateAsync(It.IsAny<Feature.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult())
            .ReturnsAsync(new ValidationResult())
            .ReturnsAsync(new ValidationResult([
                new ValidationFailure(nameof(Feature.Query.Token), "Invalid token")
            ]));
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var secret = new StoredSecret(payload, time.Now.AddMinutes(1));
        store.SetupSequence(x => x.Get(query.Token)).Returns(secret).Returns((StoredSecret?) null);
        encryption.Setup(x => x.Decrypt(payload)).Returns(text);

        (await sut.Handle(query, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await sut.Handle(query, CancellationToken.None)).IsFailed.Should().BeTrue();
        (await sut.Handle(query with {Token = query.Token + "!"}, CancellationToken.None)).IsFailed
            .Should().BeTrue();

        var states = logger.Invocations.Where(call => call.Method.Name == nameof(ILogger.Log))
            .Select(call => call.Arguments[2]).ToArray();
        states.Should().HaveCount(3);
        foreach (var state in states)
        {
            state.ToString().Should().NotContain(text).And.NotContain(query.Token);
            ((IEnumerable<KeyValuePair<string, object?>>) state).Should().AllSatisfy(property =>
                (property.Value?.ToString() ?? "").Should().NotContain(text).And
                .NotContain(query.Token));
        }
    }
}