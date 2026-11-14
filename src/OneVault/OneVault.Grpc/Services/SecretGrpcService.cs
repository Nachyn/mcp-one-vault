using FluentResults;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using OneVault.App.Errors;
using OneVault.Grpc.Protos;
using GetSecret = OneVault.App.UseCases.GetSecret.GetSecret;
using CreateSecret = OneVault.App.UseCases.CreateSecret.CreateSecret;

namespace OneVault.Grpc.Services;

public sealed class SecretGrpcService(ISender sender) : OneVaultService.OneVaultServiceBase
{
    public override async Task<CreateSecretResponse> CreateSecret(CreateSecretRequest request,
        ServerCallContext context)
    {
        var result = await sender.Send(new CreateSecret.Command(request.Text, request.TtlSeconds),
            context.CancellationToken);
        if (result.IsFailed)
        {
            throw ToRpcException(result.Errors);
        }

        return new CreateSecretResponse
        {
            Token = result.Value.Token,
            ExpiresAt = Timestamp.FromDateTimeOffset(result.Value.ExpiresAt)
        };
    }

    public override async Task<GetSecretResponse> GetSecret(GetSecretRequest request,
        ServerCallContext context)
    {
        var result =
            await sender.Send(new GetSecret.Query(request.Token), context.CancellationToken);
        if (result.IsFailed)
        {
            throw ToRpcException(result.Errors);
        }

        return new GetSecretResponse
        {
            Text = result.Value.Text,
            ExpiresAt = Timestamp.FromDateTimeOffset(result.Value.ExpiresAt)
        };
    }

    private static RpcException ToRpcException(IReadOnlyList<IError> errors)
    {
        var code = (errors.FirstOrDefault() as SecretFailure)?.Code switch
        {
            SecretError.InvalidInput => StatusCode.InvalidArgument,
            SecretError.NotFound => StatusCode.NotFound,
            SecretError.CapacityExceeded => StatusCode.ResourceExhausted,
            _ => StatusCode.Internal
        };
        var message = code == StatusCode.Internal
            ? "Failed to process secret."
            : string.Join(" ", errors.Select(error => error.Message));
        return new RpcException(new Status(code, message));
    }
}