using System.ComponentModel;
using FluentResults;
using MediatR;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneVault.App.Errors;
using CreateSecret = OneVault.App.UseCases.CreateSecret.CreateSecret;
using GetSecret = OneVault.App.UseCases.GetSecret.GetSecret;

namespace OneVault.Mcp.Tools;

[McpServerToolType]
public sealed class SecretTools(ISender sender)
{
    [McpServerTool(
        Name = "create_secret",
        UseStructuredContent = true,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Store a temporary encrypted secret and return its access token and UTC expiry. " +
                 "The token grants access to the secret; treat it as a password.")]
    public async Task<CreateSecret.Secret> CreateAsync(
        [Description("Nonempty secret text, at most 65536 UTF-8 bytes.")]
        string text,
        [Description("Lifetime in seconds, from 1 to 86400 inclusive.")]
        int ttlSeconds,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateSecret.Command(text, ttlSeconds), cancellationToken);
        return GetValue(result);
    }

    [McpServerTool(
        Name = "get_secret",
        UseStructuredContent = true,
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Read a secret by its access token and return its text and UTC expiry. " +
                 "Reading does not delete it. Missing and expired secrets have the same error.")]
    public async Task<GetSecret.Secret> GetAsync(
        [Description("The access token returned by create_secret.")]
        string token,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSecret.Query(token), cancellationToken);
        return GetValue(result);
    }

    private static T GetValue<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        var failure = result.Errors.FirstOrDefault() as SecretFailure;
        var message = failure?.Code switch
        {
            SecretError.InvalidInput or SecretError.NotFound or SecretError.CapacityExceeded =>
                $"{failure.Code}: {string.Join(" ", result.Errors.Select(error => error.Message))}",
            _ => "Failed to process secret."
        };
        throw new McpException(message);
    }
}