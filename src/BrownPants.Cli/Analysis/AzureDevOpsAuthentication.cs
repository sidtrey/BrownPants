using Microsoft.Identity.Client;

namespace BrownPants.Cli.Analysis;

public interface IAzureDevOpsInteractiveTokenProvider
{
    Task<string> AcquireTokenAsync(AzureDevOpsConfiguration configuration, CancellationToken cancellationToken);
}

public sealed class AzureDevOpsAuthenticationException(string message) : Exception(message);

public static class AzureDevOpsAuthentication
{
    public const string TokenEnvironmentVariable = "BROWNPANTS_AZURE_DEVOPS_TOKEN";

    public static async Task<string> AcquireTokenAsync(
        AzureDevOpsConfiguration configuration,
        IAzureDevOpsInteractiveTokenProvider? interactiveProvider = null,
        Func<string?>? tokenReader = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.AuthenticationScheme == "entra-browser")
        {
            if (string.IsNullOrWhiteSpace(configuration.EntraClientId) || string.IsNullOrWhiteSpace(configuration.EntraTenantId))
                throw new AzureDevOpsAuthenticationException("Browser sign-in requires entraClientId and entraTenantId in the repository configuration.");
            try
            {
                return await (interactiveProvider ?? new MsalBrowserTokenProvider())
                    .AcquireTokenAsync(configuration, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (MsalException)
            {
                throw new AzureDevOpsAuthenticationException("Microsoft Entra browser sign-in was cancelled, denied, or blocked by tenant policy.");
            }
        }

        var token = (tokenReader ?? (() => Environment.GetEnvironmentVariable(TokenEnvironmentVariable)))();
        if (string.IsNullOrWhiteSpace(token))
            throw new AzureDevOpsAuthenticationException($"Set {TokenEnvironmentVariable} using the approved credential process, or configure entra-browser authentication for Azure DevOps Services.");
        return token;
    }
}

public sealed class MsalBrowserTokenProvider : IAzureDevOpsInteractiveTokenProvider
{
    private static readonly string[] Scopes = ["https://app.vssps.visualstudio.com/.default"];

    public async Task<string> AcquireTokenAsync(AzureDevOpsConfiguration configuration, CancellationToken cancellationToken)
    {
        var application = PublicClientApplicationBuilder
            .Create(configuration.EntraClientId!)
            .WithAuthority($"https://login.microsoftonline.com/{configuration.EntraTenantId}")
            .WithRedirectUri("http://localhost")
            .Build();

        var result = await application.AcquireTokenInteractive(Scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        return result.AccessToken;
    }
}
