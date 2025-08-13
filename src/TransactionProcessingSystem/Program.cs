using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem.Configuration;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args); // This configure the ILoggerFactory to log to the console, debug, and event source output
builder.Services.AddApplicationConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();

using IHost host = builder.Build();

// Keep options internal, build the real service from them?
host.ValidateAllOptions();
host.ValidateAllSecrets();

await host.RunAsync();