using FluentResults;
using MediatR;

namespace OneVault.App.UseCases.GetSecret;

public static partial class GetSecret
{
    public sealed record Query(string Token) : IRequest<Result<Secret>>;

    public sealed record Secret(string Text, DateTimeOffset ExpiresAt);
}