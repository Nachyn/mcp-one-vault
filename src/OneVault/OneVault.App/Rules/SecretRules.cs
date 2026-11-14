namespace OneVault.App.Rules;

public static class SecretRules
{
    public const int MaxTextBytes = 64 * 1024;
    public const int MinTtlSeconds = 1;
    public const int MaxTtlSeconds = 24 * 60 * 60;
}