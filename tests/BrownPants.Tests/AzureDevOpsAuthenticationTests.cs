using BrownPants.Cli;
using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class AzureDevOpsAuthenticationTests
{
    [Fact]
    public async Task AcquireToken_UsesInteractiveProviderForConfiguredBrowserMode()
    {
        var provider = new FakeInteractiveProvider("short-lived-secret-token");
        var config = BrowserConfiguration();

        var token = await AzureDevOpsAuthentication.AcquireTokenAsync(config, provider, () => null, TestContext.Current.CancellationToken);

        Assert.Equal("short-lived-secret-token", token);
        Assert.Equal(1, provider.CallCount);
        Assert.Same(config, provider.Configuration);
    }

    [Fact]
    public async Task AcquireToken_PropagatesCancellationWithoutFallingBackToEnvironmentToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new FakeInteractiveProvider("ignored", cancellation.Token);
        var readEnvironment = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AzureDevOpsAuthentication.AcquireTokenAsync(
            BrowserConfiguration(), provider, () => { readEnvironment = true; return "fallback-token"; }, cancellation.Token));

        Assert.Equal(1, provider.CallCount);
        Assert.False(readEnvironment);
    }

    [Fact]
    public async Task AcquireToken_UsesExternalTokenReaderAndDoesNotEchoMissingTokenValue()
    {
        var config = TokenConfiguration();
        Assert.Equal("fixture-token", await AzureDevOpsAuthentication.AcquireTokenAsync(config,
            tokenReader: () => "fixture-token", cancellationToken: TestContext.Current.CancellationToken));
        var exception = await Assert.ThrowsAsync<AzureDevOpsAuthenticationException>(() =>
            AzureDevOpsAuthentication.AcquireTokenAsync(config, tokenReader: () => null,
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains(AzureDevOpsAuthentication.TokenEnvironmentVariable, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("fixture-token", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RequiresOnlineBeforeLoadingConfigOrStartingAuthentication()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = Program.Run(new[] { "devops", "trace", "C:\\missing-repository", "--feature", "specs/a" }, output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("explicit --online", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void Configuration_LoadRequiresClientAndTenantForBrowserMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "brownpants-ado-auth-config", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".brownpants"));
        try
        {
            var path = Path.Combine(root, ".brownpants", "azure-devops.json");
            File.WriteAllText(path, """
                { "schemaVersion": 1, "organizationUrl": "https://dev.azure.com/example", "project": "p",
                  "repository": "r", "authenticationScheme": "entra-browser", "features": {},
                  "entraClientId": "10203040-5060-7080-90a0-b0c0d0e0f000", "entraTenantId": "example.onmicrosoft.com" }
                """);

            var config = AzureDevOpsConfiguration.Load(root);

            Assert.Equal("entra-browser", config.AuthenticationScheme);
            Assert.Equal("example.onmicrosoft.com", config.EntraTenantId);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static AzureDevOpsConfiguration BrowserConfiguration() => new(
        new Uri("https://dev.azure.com/example/"), "project", "repo", "entra-browser",
        new Dictionary<string, IReadOnlyList<string>>(), "10203040-5060-7080-90a0-b0c0d0e0f000", "example.onmicrosoft.com");

    private static AzureDevOpsConfiguration TokenConfiguration() => new(
        new Uri("https://dev.azure.com/example/"), "project", "repo", "bearer", new Dictionary<string, IReadOnlyList<string>>());

    private sealed class FakeInteractiveProvider(string token, CancellationToken cancellationToken = default) : IAzureDevOpsInteractiveTokenProvider
    {
        public int CallCount { get; private set; }
        public AzureDevOpsConfiguration? Configuration { get; private set; }
        public Task<string> AcquireTokenAsync(AzureDevOpsConfiguration configuration, CancellationToken requestCancellationToken)
        {
            CallCount++;
            Configuration = configuration;
            if (cancellationToken.IsCancellationRequested || requestCancellationToken.IsCancellationRequested)
                return Task.FromCanceled<string>(cancellationToken.IsCancellationRequested ? cancellationToken : requestCancellationToken);
            return Task.FromResult(token);
        }
    }
}
