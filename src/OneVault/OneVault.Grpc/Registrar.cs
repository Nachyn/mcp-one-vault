using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OneVault.Grpc.Services;

namespace OneVault.Grpc;

public static class Registrar
{
    public static IServiceCollection AddOneVaultGrpc(this IServiceCollection services)
    {
        services.AddGrpc(options => options.MaxReceiveMessageSize = 300 * 1024)
            .AddJsonTranscoding();
        services.AddGrpcReflection();
        services.AddGrpcSwagger();
        return services;
    }

    public static IEndpointRouteBuilder MapOneVaultGrpc(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<SecretGrpcService>();
        endpoints.MapGrpcReflectionService();
        return endpoints;
    }
}