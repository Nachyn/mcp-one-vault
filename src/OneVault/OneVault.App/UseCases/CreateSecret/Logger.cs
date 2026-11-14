using Microsoft.Extensions.Logging;

namespace OneVault.App.UseCases.CreateSecret;

internal static partial class Logger
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "Secret created with TTL {TtlSeconds} seconds.")]
    internal static partial void SecretCreated(this ILogger<CreateSecret.Handler> logger, int ttlSeconds);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Secret creation rejected with {ErrorCount} validation errors.")]
    internal static partial void ValidationRejected(this ILogger<CreateSecret.Handler> logger, int errorCount);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Secret creation rejected because storage is full.")]
    internal static partial void StorageFull(this ILogger<CreateSecret.Handler> logger);
}
