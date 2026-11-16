using OneVault;
using OneVault.App;
using OneVault.Grpc;
using OneVault.Infrastructure;
using OneVault.Mcp;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOneVaultApp()
    .AddOneVaultInfrastructure(builder.Configuration)
    .AddOneVaultGrpc()
    .AddOneVaultMcp()
    .AddOneVaultHost();

var app = builder.Build();
app.UseOneVaultHost();
app.MapOneVaultGrpc();
app.MapOneVaultMcp();
app.Run();
