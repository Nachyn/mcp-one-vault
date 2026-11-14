using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneVault.App.Interfaces;

namespace OneVault.Infrastructure;

public static class Registrar
{
    public static IServiceCollection AddOneVaultInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ISecretStore>(provider => new MemorySecretStore(
            provider.GetRequiredService<TimeProvider>(),
            configuration.GetValue<long?>("Storage:CapacityBytes") ?? 16 * 1024 * 1024));
        services.AddSingleton<ISecretEncryption, AesGcmEncryption>();
        services.AddSingleton<ITokenGenerator, CryptoTokenGenerator>();
        return services;
    }
}