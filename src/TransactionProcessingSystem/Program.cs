using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Components;

var builder = Host.CreateApplicationBuilder(args);

// Configure application settings and secrets with validation
builder.Services.AddApplicationConfiguration(builder.Configuration);

var host = builder.Build();

// Demonstrate configuration validation working
try
{
    // This will trigger validation if any configuration is invalid
    var appSettings = host.Services.GetRequiredService<IOptions<AppSettings>>().Value;
    var openAISettings = host.Services.GetRequiredService<IOptions<OpenAISettings>>().Value;
    var transactionApiSettings = host.Services.GetRequiredService<IOptions<TransactionApiSettings>>().Value;
    
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("✅ Configuration validation passed successfully!");
    logger.LogInformation("OpenAI Model: {Model}", openAISettings.Model);
    logger.LogInformation("Transaction API Base URL: {BaseUrl}", transactionApiSettings.BaseUrl);
    logger.LogInformation("🎉 All configuration validation and demonstration completed successfully!");
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "❌ Configuration validation failed!");
    throw;
}

// Configuration validation has already been performed by AddOptionsWithValidateOnStart()
var finalLogger = host.Services.GetRequiredService<ILogger<Program>>();
finalLogger.LogInformation("Application completed successfully!");