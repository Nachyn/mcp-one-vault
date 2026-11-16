using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using OneVault.Mcp.Tools;

namespace OneVault.Mcp;

public static class Registrar
{
    public static IServiceCollection AddOneVaultMcp(this IServiceCollection services)
    {
        services.AddMcpServer()
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            .WithTools<SecretTools>();
        return services;
    }

    public static IEndpointRouteBuilder MapOneVaultMcp(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMcp("/mcp");
        return endpoints;
    }
}
