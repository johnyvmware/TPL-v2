using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using TransactionProcessingSystem.Configuration.Secrets;
using TransactionProcessingSystem.Configuration.Settings;

namespace TransactionProcessingSystem.Services;

public sealed class MicrosoftGraphService(MicrosoftGraphOptions settings, MicrosoftGraphSecrets secrets) : IDisposable
{
    private GraphServiceClient? _graphClient;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    public async Task<MessageCollectionResponse?> GetEmailsAsync(DateTime from, DateTime to)
    {
        try
        {
            var graphClient = await InitializeGraphClientAsync();

            var user = await graphClient.Me.GetAsync();
            Console.WriteLine($"✓ Authenticated as: {user?.DisplayName}");

            var messages = await graphClient.Me.Messages
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
        catch (ODataError odataError)
        {
            Console.WriteLine($"❌ Graph API error: {odataError.Error?.Code} - {odataError.Error?.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ An unexpected error occurred: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _graphClient?.Dispose();
        _initializationLock.Dispose();
    }

    private async Task<GraphServiceClient> InitializeGraphClientAsync()
    {
        if (_graphClient is not null)
        {
            return _graphClient;
        }

        await _initializationLock.WaitAsync();

        try
        {
            if (_graphClient is null)
            {
                var authProvider = await MsalAuthenticationProvider.CreateAsync(
                    secrets.ClientId,
                    secrets.TenantId,
                    settings.Scopes,
                    settings.CacheFileName,
                    settings.CacheDirectory);

                _graphClient = new GraphServiceClient(authProvider);
            }
        }
        finally
        {
            _initializationLock.Release();
        }

        return _graphClient;
    }
}
