using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem;
using TransactionProcessingSystem.Configuration.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();

using IHost host = builder.Build();
var worker = host.Services.GetRequiredService<Worker>();

await worker.ExecuteAsync();
