using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OneVault.App.Errors;
using OneVault.App.Interfaces;

namespace OneVault.App.UseCases.GetSecret;

public static partial class GetSecret
{
    public sealed class Handler(
        ISecretStore store,
        ISecretEncryption encryption,
        TimeProvider time,
        IValidator<Query> validator,
        ILogger<Handler> logger)
        : IRequestHandler<Query, Result<Secret>>
    {
        public async Task<Result<Secret>> Handle(Query query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validation = await validator.ValidateAsync(query, cancellationToken);
            if (!validation.IsValid)
            {
                logger.ValidationRejected(validation.Errors.Count);
                return Result.Fail<Secret>(validation.Errors.Select(e =>
                    new SecretFailure(SecretError.InvalidInput, e.ErrorMessage)));
            }

            var secret = store.Get(query.Token);
            if (secret is null || secret.IsExpired(time.GetUtcNow()))
            {
                logger.SecretUnavailable();
                return Result.Fail<Secret>(new SecretFailure(SecretError.NotFound,
                    "Secret not found or expired."));
            }

            var text = encryption.Decrypt(secret.Payload);
            logger.SecretRetrieved();
            return Result.Ok(new Secret(text, secret.ExpiresAt));
        }
    }
}
