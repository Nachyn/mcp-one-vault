using Microsoft.Extensions.Logging;

namespace OneVault.App.UseCases.GetSecret;

internal static partial class Logger
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Secret retrieved.")]
    internal static partial void SecretRetrieved(this ILogger<GetSecret.Handler> logger);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Warning,
        Message = "Secret retrieval rejected with {ErrorCount} validation errors.")]
    internal static partial void ValidationRejected(this ILogger<GetSecret.Handler> logger, int errorCount);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Information,
        Message = "Requested secret not found or expired.")]
    internal static partial void SecretUnavailable(this ILogger<GetSecret.Handler> logger);
}
