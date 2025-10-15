using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace TransactionProcessingSystem.Services;

public class MsalAuthenticationProvider(IPublicClientApplication clientApplication, string[] scopes) : IAuthenticationProvider
{
    public static async Task<MsalAuthenticationProvider> CreateAsync(
        string clientId,
        string tenantId,
        string[] scopes,
        string cacheFileName,
        string cacheDirectory)
    {
        var storageProperties = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory)
            .WithUnprotectedFile()
            .Build();

        var app = PublicClientApplicationBuilder.Create(clientId)
            .WithTenantId(tenantId)
            .WithDefaultRedirectUri()
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);

        return new MsalAuthenticationProvider(app, scopes);
    }

    public async Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken);
        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", $"Bearer {token}");
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var accounts = await clientApplication.GetAccountsAsync();
        AuthenticationResult? result = null;

        try
        {
            result = await clientApplication.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync(cancellationToken);
        }
        catch (MsalUiRequiredException)
        {
            result = await clientApplication.AcquireTokenWithDeviceCode(scopes, deviceCodeResult =>
            {
                Console.WriteLine(deviceCodeResult.Message);
                return Task.CompletedTask;
            }).ExecuteAsync(cancellationToken);
        }
        catch (MsalException msalex)
        {
            Console.WriteLine($"Error acquiring token: {msalex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while acquiring token: {ex.Message}");
            throw;
        }

        return result?.AccessToken ?? throw new InvalidOperationException("Could not acquire token.");
    }
}
