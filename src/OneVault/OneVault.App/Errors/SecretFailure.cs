using FluentResults;

namespace OneVault.App.Errors;

public sealed class SecretFailure(SecretError code, string message) : Error(message)
{
    public SecretError Code { get; } = code;
}