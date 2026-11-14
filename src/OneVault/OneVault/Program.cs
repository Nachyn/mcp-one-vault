using OneVault;
using OneVault.App;
using OneVault.Grpc;
using OneVault.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOneVaultApp()
    .AddOneVaultInfrastructure(builder.Configuration)
    .AddOneVaultGrpc()
    .AddOneVaultHost();

var app = builder.Build();
app.UseOneVaultHost();
app.MapOneVaultGrpc();
app.Run();