using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration.Extensions;

public static class HostExtensions
{
    public static void ValidateAllOptions(this IHost host)
    {
        host.ValidateOptions<LlmOptions>();
        host.ValidateOptions<MicrosoftGraphOptions>();
        host.ValidateOptions<ExportOptions>();
        host.ValidateOptions<PipelineOptions>();
        host.ValidateOptions<Neo4jOptions>();
        host.ValidateOptions<FetcherOptions>();
        host.ValidateOptions<CategoriesOptions>();
    }

    public static void ValidateAllSecrets(this IHost host)
    {
        host.ValidateOptions<OpenAISecrets>();
        host.ValidateOptions<MicrosoftGraphSecrets>();
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
