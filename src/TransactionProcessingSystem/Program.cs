using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem.Configuration.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();

using IHost host = builder.Build();

host.ValidateAllOptions();
host.ValidateAllSecrets();

await host.RunAsync();
