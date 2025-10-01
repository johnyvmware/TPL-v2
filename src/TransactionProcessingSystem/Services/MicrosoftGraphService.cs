using TransactionProcessingSystem.Configuration.Secrets;
using TransactionProcessingSystem.Configuration.Settings;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Models;

namespace TransactionProcessingSystem.Services;

public sealed class MicrosoftGraphService : IDisposable
{
    private readonly GraphServiceClient _graphClient;

    public MicrosoftGraphService(MicrosoftGraphOptions settings, MicrosoftGraphSecrets secrets)
    {
        var credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
        {
            ClientId = secrets.ClientId,
            TenantId = secrets.TenantId,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = "msal_cache",
            },
            DeviceCodeCallback = (code, cancellation) =>
            {
                Console.WriteLine("\n🔐 Authentication Required");
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine($"Please visit: {code.VerificationUri}");
                Console.WriteLine($"Enter code: {code.UserCode}");
                Console.WriteLine("═══════════════════════════════════════\n");
                return Task.CompletedTask;
            }
        });

        _graphClient = new GraphServiceClient(credential, settings.Scopes);
    }

    public async Task<MessageCollectionResponse?> GetEmailsAsync(DateTime from, DateTime to)
    {
        var messages = await _graphClient.Me.Messages
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter =
                    $"receivedDateTime ge {from:yyyy-MM-ddTHH:mm:ssZ} and receivedDateTime le {to:yyyy-MM-ddTHH:mm:ssZ}";
                requestConfiguration.QueryParameters.Select = ["subject", "from", "receivedDateTime", "bodyPreview"];
                requestConfiguration.QueryParameters.Top = 50;
                requestConfiguration.QueryParameters.Orderby = ["receivedDateTime DESC"];
            });

        return messages;
    }

    public void Dispose()
    {
        _graphClient?.Dispose();
    }
}
