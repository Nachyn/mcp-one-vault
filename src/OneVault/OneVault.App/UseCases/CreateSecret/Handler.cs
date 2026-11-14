using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OneVault.App.Errors;
using OneVault.App.Interfaces;
using OneVault.App.Models;

namespace OneVault.App.UseCases.CreateSecret;

public static partial class CreateSecret
{
    public sealed class Handler(
        ISecretStore store,
        ISecretEncryption encryption,
        ITokenGenerator tokens,
        TimeProvider time,
        IValidator<Command> validator,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Secret>>
    {
        public async Task<Result<Secret>> Handle(Command command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                logger.ValidationRejected(validation.Errors.Count);
                return Result.Fail<Secret>(validation.Errors.Select(e =>
                    new SecretFailure(SecretError.InvalidInput, e.ErrorMessage)));
            }

            var expiresAt = time.GetUtcNow().AddSeconds(command.TtlSeconds);
            var token = tokens.Generate();
            var secret = new StoredSecret(encryption.Encrypt(command.Text), expiresAt);
            if (!store.TryAdd(token, secret))
            {
                logger.StorageFull();
                return Result.Fail<Secret>(new SecretFailure(SecretError.CapacityExceeded,
                    "Secret storage is full. Try again later."));
            }

            logger.SecretCreated(command.TtlSeconds);
            return Result.Ok(new Secret(token, expiresAt));
        }
    }
}
