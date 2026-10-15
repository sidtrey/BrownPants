using System.Text.Json;
using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class AzureDevOpsConfigurationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-ado-config", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_ReadsVersionedConnectionAndFeatureMappings()
    {
        WriteConfig("""
            {
              "schemaVersion": 1,
              "organizationUrl": "https://dev.azure.com/example",
              "project": "Project A",
              "repository": "repo-id",
              "authenticationScheme": "bearer",
              "features": { "specs/feature-a": ["42", "43"] }
            }
            """);

        var config = AzureDevOpsConfiguration.Load(_root);

        Assert.Equal("https://dev.azure.com/example/", config.OrganizationUrl.AbsoluteUri);
        Assert.Equal("Project A", config.Project);
        Assert.Equal("repo-id", config.Repository);
        Assert.Equal(new[] { "42", "43" }, config.Features["specs/feature-a"]);
    }

    [Fact]
    public void Load_RejectsSecretPropertiesAndUnknownSchemaFields()
    {
        WriteConfig("""
            { "schemaVersion": 1, "organizationUrl": "https://dev.azure.com/example", "project": "p",
              "repository": "r", "authenticationScheme": "bearer", "features": {}, "token": "never-store-me" }
            """);

        var exception = Assert.Throws<InvalidDataException>(() => AzureDevOpsConfiguration.Load(_root));
        Assert.DoesNotContain("never-store-me", exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://dev.azure.com/example")]
    [InlineData("https://untrusted.example/org")]
    [InlineData("https://user:password@dev.azure.com/example")]
    public void Load_RejectsInsecureOrUnapprovedHosts(string organizationUrl)
    {
        WriteConfig(ConfigJson(organizationUrl));

        Assert.Throws<InvalidDataException>(() => AzureDevOpsConfiguration.Load(_root));
    }

    [Fact]
    public void Load_AllowsCustomHttpsHostOnlyWithExplicitFlag()
    {
        WriteConfig(ConfigJson("https://ado.internal.example/collection"));

        Assert.Throws<InvalidDataException>(() => AzureDevOpsConfiguration.Load(_root));
        var config = AzureDevOpsConfiguration.Load(_root, allowCustomHost: true);
        Assert.Equal("ado.internal.example", config.OrganizationUrl.Host);
    }

    [Theory]
    [InlineData("not-a-client-id", "tenant.onmicrosoft.com")]
    [InlineData("10203040-5060-7080-90a0-b0c0d0e0f000", "")]
    public void Load_RejectsBrowserAuthenticationWithoutValidPublicClientConfiguration(string clientId, string tenantId)
    {
        WriteConfig(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            organizationUrl = "https://dev.azure.com/example",
            project = "p",
            repository = "r",
            authenticationScheme = "entra-browser",
            features = new { },
            entraClientId = clientId,
            entraTenantId = tenantId
        }));

        Assert.Throws<InvalidDataException>(() => AzureDevOpsConfiguration.Load(_root));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("/absolute")]
    [InlineData("specs/../outside")]
    public void Load_RejectsFeaturePathsOutsideOrNotNormalized(string featurePath)
    {
        WriteConfig(ConfigJson("https://dev.azure.com/example", $"\"{featurePath}\": [\"42\"]"));

        Assert.Throws<InvalidDataException>(() => AzureDevOpsConfiguration.Load(_root));
    }

    [Fact]
    public void Load_RejectsDuplicateWorkItemIdsAndInvalidJsonWithoutEchoingInput()
    {
        WriteConfig(ConfigJson("https://dev.azure.com/example", "\"specs/a\": [\"42\", \"42\"]"));
        Assert.Throws<InvalidDataException>(() => AzureDevOpsConfiguration.Load(_root));

        WriteConfig("{ invalid private-value }");
        var exception = Assert.Throws<InvalidDataException>(() => AzureDevOpsConfiguration.Load(_root));
        Assert.DoesNotContain("private-value", exception.ToString(), StringComparison.Ordinal);
    }

    private string ConfigJson(string organizationUrl, string mappings = "\"specs/a\": [\"42\"]") => $$"""
        {
          "schemaVersion": 1,
          "organizationUrl": "{{organizationUrl}}",
          "project": "project",
          "repository": "repo",
          "authenticationScheme": "bearer",
          "features": { {{mappings}} }
        }
        """;

    private void WriteConfig(string content)
    {
        Directory.CreateDirectory(Path.Combine(_root, ".brownpants"));
        File.WriteAllText(Path.Combine(_root, ".brownpants", "azure-devops.json"), content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
