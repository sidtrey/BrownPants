using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class ModelRoutingProfileExporterTests
{
    [Theory]
    [InlineData("vscode", ".github/agents/brownpants-implementation.agent.md", "model: \\\"Example Model Name (copilot)\\\"")]
    [InlineData("visual-studio", ".github/agents/brownpants-visual-studio-implementation.agent.md", "model: \\\"Example Model Name (copilot)\\\"")]
    [InlineData("claude", ".claude/agents/brownpants-implementation.md", "model: \\\"opus\\\"")]
    [InlineData("codex", ".codex/agents/brownpants-implementation.toml", "model = \\\"example-model-id\\\"")]
    public void Export_CreatesNativeProjectAgentWithConfiguredModel(string host, string relativePath, string modelSetting)
    {
        using var repository = new TemporaryRepository();
        var policy = LoadPolicy(repository);

        var result = ModelRoutingProfileExporter.Export(repository.Root, policy, host);
        var file = Path.Combine(repository.Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var text = File.ReadAllText(file);

        Assert.Equal(2, result.Created);
        Assert.Equal(0, result.Unchanged);
        Assert.Contains(modelSetting.Replace("\\\"", "\"", StringComparison.Ordinal), text, StringComparison.Ordinal);
        Assert.Contains("Use the reviewed plan.", text, StringComparison.Ordinal);
        Assert.Contains("high", text, StringComparison.Ordinal);
        if (host == "visual-studio")
        {
            Assert.Contains(@"name: ""BrownPants Visual Studio implementation""", text, StringComparison.Ordinal);
            Assert.Contains("Visual Studio task profile.", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Export_RepeatedIdenticalPolicy_IsIdempotent()
    {
        using var repository = new TemporaryRepository();
        var policy = LoadPolicy(repository);

        var first = ModelRoutingProfileExporter.Export(repository.Root, policy, "claude");
        var second = ModelRoutingProfileExporter.Export(repository.Root, policy, "claude");

        Assert.Equal(2, first.Created);
        Assert.Equal(0, first.Unchanged);
        Assert.Equal(0, second.Created);
        Assert.Equal(2, second.Unchanged);
    }

    [Fact]
    public void Export_VisualStudioAndVsCode_UseDistinctFilesAndDisplayNames()
    {
        using var repository = new TemporaryRepository();
        var policy = LoadPolicy(repository);

        ModelRoutingProfileExporter.Export(repository.Root, policy, "vscode");
        ModelRoutingProfileExporter.Export(repository.Root, policy, "visual-studio");

        var vscode = Path.Combine(repository.Root, ".github", "agents", "brownpants-implementation.agent.md");
        var visualStudio = Path.Combine(repository.Root, ".github", "agents", "brownpants-visual-studio-implementation.agent.md");
        Assert.True(File.Exists(vscode));
        Assert.True(File.Exists(visualStudio));
        Assert.Contains(@"name: ""BrownPants implementation""", File.ReadAllText(vscode), StringComparison.Ordinal);
        Assert.Contains(@"name: ""BrownPants Visual Studio implementation""", File.ReadAllText(visualStudio), StringComparison.Ordinal);
    }

    [Fact]
    public void Export_ConflictingLaterFile_PreservesItAndWritesNoEarlierProfile()
    {
        using var repository = new TemporaryRepository();
        var policy = LoadPolicy(repository);
        var directory = Path.Combine(repository.Root, ".codex", "agents");
        Directory.CreateDirectory(directory);
        var conflict = Path.Combine(directory, "brownpants-documentation.toml");
        File.WriteAllText(conflict, "team-owned profile");

        var exception = Assert.Throws<IOException>(() => ModelRoutingProfileExporter.Export(repository.Root, policy, "codex"));

        Assert.Contains("different content", exception.Message, StringComparison.Ordinal);
        Assert.Equal("team-owned profile", File.ReadAllText(conflict));
        Assert.False(File.Exists(Path.Combine(directory, "brownpants-implementation.toml")));
    }

    [Fact]
    public void Export_MissingHostModel_RefusesWholeBatchBeforeCreatingDirectories()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(PolicyWithoutCodex);
        var policy = ModelRoutingPolicyReader.Load(repository.Root);

        var exception = Assert.Throws<InvalidDataException>(() => ModelRoutingProfileExporter.Export(repository.Root, policy, "codex"));

        Assert.Contains("no model identifier", exception.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(repository.Root, ".codex")));
    }

    [Fact]
    public void Export_ModelTextIsEscapedForHostConfiguration()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(System.Text.Json.JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            profiles = new[]
            {
                new
                {
                    id = "implementation",
                    categories = new[] { "implementation" },
                    qualityTier = "high",
                    rationale = "Complex code work.",
                    models = new Dictionary<string, string> { ["codex"] = "model \"quoted\"\nnext-line" },
                    instructions = "Implement."
                }
            }
        }));
        var policy = ModelRoutingPolicyReader.Load(repository.Root);

        ModelRoutingProfileExporter.Export(repository.Root, policy, "codex");
        var text = File.ReadAllText(Path.Combine(repository.Root, ".codex", "agents", "brownpants-implementation.toml"));

        Assert.Contains("model = \"model \\\"quoted\\\"\\nnext-line\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\nnext-line\"\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_PathTraversalAndSymbolicLinkDirectory_AreRefused()
    {
        using var repository = new TemporaryRepository();
        var policy = LoadPolicy(repository);
        Assert.Throws<ArgumentException>(() => ModelRoutingPolicyReader.ResolveInside(repository.Root, "../outside", "Export"));

        var outside = Path.Combine(Path.GetTempPath(), "brownpants-model-export-outside", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(repository.Root, ".github"), outside);
            var exception = Assert.Throws<ArgumentException>(() => ModelRoutingProfileExporter.Export(repository.Root, policy, "vscode"));
            Assert.Contains("symbolic link", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside));
        }
        catch (UnauthorizedAccessException)
        {
            // Some Windows configurations disable symbolic-link creation for test users.
        }
        catch (PlatformNotSupportedException)
        {
            // The traversal assertion above still runs on platforms without symbolic links.
        }
        finally
        {
            try { Directory.Delete(outside, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static ModelRoutingPolicy LoadPolicy(TemporaryRepository repository)
    {
        repository.WritePolicy(ValidPolicy);
        return ModelRoutingPolicyReader.Load(repository.Root);
    }

    private const string ValidPolicy = """
        {
          "schemaVersion": 1,
          "profiles": [
            {"id":"implementation","categories":["implementation"],"qualityTier":"high","rationale":"Complex code work.","models":{"vscode":"Example Model Name (copilot)","visual-studio":"Example Model Name (copilot)","claude":"opus","codex":"example-model-id"},"instructions":"Use the reviewed plan."},
            {"id":"documentation","categories":["documentation"],"qualityTier":"economy","rationale":"Routine documentation.","models":{"vscode":"Example Model Name (copilot)","visual-studio":"Example Model Name (copilot)","claude":"haiku","codex":"example-model-id"},"instructions":"Keep updates concise."}
          ]
        }
        """;

    private const string PolicyWithoutCodex = """
        {"schemaVersion":1,"profiles":[{"id":"implementation","categories":["implementation"],"qualityTier":"high","rationale":"Complex code work.","models":{"vscode":"Copilot Model"},"instructions":"Implement."}]}
        """;

    private sealed class TemporaryRepository : IDisposable
    {
        public TemporaryRepository()
        {
            Root = Path.Combine(Path.GetTempPath(), "brownpants-model-export-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WritePolicy(string content)
        {
            var directory = Path.Combine(Root, ".brownpants");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "models.json"), content);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
