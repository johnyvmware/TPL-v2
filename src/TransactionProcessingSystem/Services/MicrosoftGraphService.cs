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
        var options = new DeviceCodeCredentialOptions
        {
            TenantId = secrets.TenantId,
            ClientId = secrets.ClientId,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = settings.TokenName,
                UnsafeAllowUnencryptedStorage = true
            },
            DeviceCodeCallback = (code, _) =>
            {
                Console.WriteLine(code.Message);
                return Task.CompletedTask;
            }
        };

        var credential = new DeviceCodeCredential(options);
        _graphClient = new GraphServiceClient(credential, settings.Scopes);
    }

    public async Task<MessageCollectionResponse?> GetEmailsAsync(DateTime from, DateTime to)
    {
        try
        {
            // Test authentication first
            var user = await _graphClient.Me.GetAsync();
            Console.WriteLine($"✓ Authenticated as: {user?.DisplayName}");

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
        catch (Azure.Identity.AuthenticationFailedException ex)
        {
            Console.WriteLine($"❌ Authentication failed: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _graphClient?.Dispose();
    }
}
