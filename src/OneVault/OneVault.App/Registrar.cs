using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using CreateSecret = OneVault.App.UseCases.CreateSecret.CreateSecret;
using GetSecret = OneVault.App.UseCases.GetSecret.GetSecret;

namespace OneVault.App;

public static class Registrar
{
    public static IServiceCollection AddOneVaultApp(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateSecret.Command>, CreateSecret.Validator>();
        services.AddSingleton<IValidator<GetSecret.Query>, GetSecret.Validator>();
        services.AddMediatR(options =>
            options.RegisterServicesFromAssemblyContaining<CreateSecret.Command>());
        return services;
    }
}