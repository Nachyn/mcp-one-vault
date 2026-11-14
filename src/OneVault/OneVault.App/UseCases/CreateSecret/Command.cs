using FluentResults;
using MediatR;

namespace OneVault.App.UseCases.CreateSecret;

public static partial class CreateSecret
{
    public sealed record Command(string Text, int TtlSeconds) : IRequest<Result<Secret>>;

    public sealed record Secret(string Token, DateTimeOffset ExpiresAt);
}