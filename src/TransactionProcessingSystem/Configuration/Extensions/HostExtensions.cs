using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration.Secrets;
using TransactionProcessingSystem.Configuration.Settings;

namespace TransactionProcessingSystem.Configuration.Extensions;

public static class HostExtensions
{
    public static void ValidateAllOptions(this IHost host)
    {
        host.ValidateOptions<LlmOptions>();
        host.ValidateOptions<Neo4jOptions>();
        host.ValidateOptions<FetcherOptions>();
    }

    public static void ValidateAllSecrets(this IHost host)
    {
        host.ValidateOptions<OpenAISecrets>();
        host.ValidateOptions<Neo4jSecrets>();
    }

    private static IHost ValidateOptions<TOptions>(this IHost host)
        where TOptions : class
    {
        var options = host.Services.GetRequiredService<IOptions<TOptions>>();
        try
        {
            _ = options.Value;
        }
        catch (OptionsValidationException ex)
        {
            throw new InvalidOperationException($"Options validation failed for {typeof(TOptions).Name}", ex);
        }

        return host;
    }
}
