using Microsoft.OpenApi.Models;

namespace OneVault;

public static class Registrar
{
    public static IServiceCollection AddOneVaultHost(this IServiceCollection services)
    {
        services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "OneVault API",
            Version = "v1"
        }));
        return services;
    }

    public static WebApplication UseOneVaultHost(this WebApplication app)
    {
        // Tokens and plaintext must not be cached by browsers or intermediary proxies.
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            await next(context);
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "OneVault v1");
                options.DocumentTitle = "OneVault API";
            });
        }

        return app;
    }
}