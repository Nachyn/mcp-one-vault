using AutoFixture.Xunit2;
using FluentResults;
using Grpc.Core;
using MediatR;
using Moq;
using Moq.Protected;
using OneVault.App.Errors;
using OneVault.Grpc.Protos;
using OneVault.Grpc.Services;
using OneVault.UnitTests.Shared;
using CreateSecret = OneVault.App.UseCases.CreateSecret.CreateSecret;
using GetSecret = OneVault.App.UseCases.GetSecret.GetSecret;

namespace OneVault.UnitTests.Grpc.Services;

public class SecretGrpcServiceTests
{
    [Theory]
    [InlineAutoMoqData(SecretError.InvalidInput, StatusCode.InvalidArgument)]
    [InlineAutoMoqData(SecretError.CapacityExceeded, StatusCode.ResourceExhausted)]
    public async Task CreateMapsResultFailures(SecretError error, StatusCode expected,
        [Frozen] Mock<ISender> sender,
        CreateSecret.Command command, string message, ServerCallContext context,
        SecretGrpcService sut)
    {
        sender.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<CreateSecret.Secret>(new SecretFailure(error, message)));

        var act = () =>
            sut.CreateSecret(
                new CreateSecretRequest {Text = command.Text, TtlSeconds = command.TtlSeconds},
                context);

        var exception = (await act.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(expected);
        exception.Status.Detail.Should().Be(message);
    }

    [Theory]
    [InlineAutoMoqData(SecretError.InvalidInput, StatusCode.InvalidArgument)]
    [InlineAutoMoqData(SecretError.NotFound, StatusCode.NotFound)]
    public async Task GetMapsResultFailures(SecretError error, StatusCode expected,
        [Frozen] Mock<ISender> sender,
        GetSecret.Query query, string message, ServerCallContext context, SecretGrpcService sut)
    {
        sender.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<GetSecret.Secret>(new SecretFailure(error, message)));

        var act = () => sut.GetSecret(new GetSecretRequest {Token = query.Token}, context);

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(expected);
    }

    [Theory]
    [AutoMoqData]
    public async Task UnknownFailureDoesNotLeakDetails(
        [Frozen] Mock<ISender> sender,
        GetSecret.Query query, string details, ServerCallContext context, SecretGrpcService sut)
    {
        sender.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<GetSecret.Secret>(details));

        var act = () => sut.GetSecret(new GetSecretRequest {Token = query.Token}, context);

        var exception = (await act.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.Internal);
        exception.Status.Detail.Should().Be("Failed to process secret.").And.NotContain(details);
    }

    [Theory]
    [AutoMoqData]
    public async Task MapsRequestsResultsAndCancellationThroughMediator(
        [Frozen] Mock<ISender> sender,
        Mock<ServerCallContext> context,
        CreateSecret.Command command, CreateSecret.Secret createdResult,
        GetSecret.Secret retrievedResult, SecretGrpcService sut)
    {
        using var cancellation = new CancellationTokenSource();
        context.Protected().SetupGet<CancellationToken>("CancellationTokenCore")
            .Returns(cancellation.Token);
        var query = new GetSecret.Query(createdResult.Token);
        sender.Setup(x => x.Send(command, cancellation.Token))
            .ReturnsAsync(Result.Ok(createdResult));
        sender.Setup(x => x.Send(query, cancellation.Token))
            .ReturnsAsync(Result.Ok(retrievedResult));

        var created =
            await sut.CreateSecret(
                new CreateSecretRequest {Text = command.Text, TtlSeconds = command.TtlSeconds},
                context.Object);
        var retrieved =
            await sut.GetSecret(new GetSecretRequest {Token = created.Token}, context.Object);

        created.Token.Should().Be(createdResult.Token);
        created.ExpiresAt.ToDateTimeOffset().Should().Be(createdResult.ExpiresAt);
        retrieved.Text.Should().Be(retrievedResult.Text);
        retrieved.ExpiresAt.ToDateTimeOffset().Should().Be(retrievedResult.ExpiresAt);
        sender.Verify(x => x.Send(command, cancellation.Token), Times.Once);
        sender.Verify(x => x.Send(query, cancellation.Token), Times.Once);
        sender.VerifyNoOtherCalls();
    }
}