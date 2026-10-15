using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class ModelRoutingPolicyTests
{
    [Fact]
    public void Load_ValidPolicy_PreservesTeamModelIdentifiers()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(ValidPolicy);

        var policy = ModelRoutingPolicyReader.Load(repository.Root);

        Assert.Equal(".brownpants/models.json", policy.RelativePath);
        Assert.Equal(2, policy.Profiles.Count);
        Assert.Equal("Example Model Name (copilot)", policy.Profiles[0].Models["vscode"]);
        Assert.Equal("Example Model Name (copilot)", policy.Profiles[0].Models["visual-studio"]);
        Assert.Equal("exact/user-provided-model", policy.Profiles[0].Models["other-host"]);
        Assert.Equal("high", policy.Profiles[0].QualityTier);
    }

    [Theory]
    [InlineData("\"schemaVersion\": 1", "\"schemaVersion\": 2", "schemaVersion")]
    [InlineData("{", "{\n  \"unexpected\": true,", "unexpected")]
    [InlineData("\"categories\": [\"implementation\"]", "\"categories\": [\"implemntation\"]", "unknown category")]
    [InlineData("\"models\": {\"vscode\":\"Example Model Name (copilot)\",\"visual-studio\":\"Example Model Name (copilot)\",\"claude\":\"opus\",\"codex\":\"example-model-id\",\"other-host\":\"exact/user-provided-model\"}", "\"models\": {}", "at least one host model")]
    [InlineData("\"qualityTier\": \"high\"", "\"qualityTier\": \"premium\"", "economy, balanced, or high")]
    public void Load_InvalidPolicy_ReportsPolicyLocation(string find, string replacement, string expected)
    {
        using var repository = new TemporaryRepository();
        var policy = ValidPolicy.Replace(find, replacement, StringComparison.Ordinal);
        repository.WritePolicy(policy);

        var exception = Assert.Throws<InvalidDataException>(() => ModelRoutingPolicyReader.Load(repository.Root));

        Assert.Contains(".brownpants/models.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DuplicateTaskCategory_RejectsAmbiguousAssignment()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(ValidPolicy.Replace("\"categories\": [\"documentation\"]", "\"categories\": [\"implementation\"]", StringComparison.Ordinal));

        var exception = Assert.Throws<InvalidDataException>(() => ModelRoutingPolicyReader.Load(repository.Root));

        Assert.Contains("assigned more than once", exception.Message, StringComparison.Ordinal);
        Assert.Contains("profiles[1].categories[0]", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_PolicyOutsideRepository_RejectsPathTraversal()
    {
        using var repository = new TemporaryRepository();
        var exception = Assert.Throws<ArgumentException>(() => ModelRoutingPolicyReader.Load(repository.Root, "../outside.json"));
        Assert.Contains("remain inside the selected repository", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_OversizedPolicy_RejectsBeforeParsing()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(new string(' ', ModelRoutingPolicyReader.MaximumPolicyBytes + 1));

        var exception = Assert.Throws<InvalidDataException>(() => ModelRoutingPolicyReader.Load(repository.Root));

        Assert.Contains("byte limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MalformedJson_IncludesLineInformation()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy("{\n  \"schemaVersion\": 1,\n  \"profiles\": [\n}");

        var exception = Assert.Throws<InvalidDataException>(() => ModelRoutingPolicyReader.Load(repository.Root));

        Assert.Contains("models.json:", exception.Message, StringComparison.Ordinal);
        Assert.Contains("invalid JSON", exception.Message, StringComparison.Ordinal);
    }

    private const string ValidPolicy = """
        {
          "schemaVersion": 1,
          "profiles": [
            {
              "id": "implementation",
              "categories": ["implementation"],
              "qualityTier": "high",
              "rationale": "Complex code work.",
              "models": {"vscode":"Example Model Name (copilot)","visual-studio":"Example Model Name (copilot)","claude":"opus","codex":"example-model-id","other-host":"exact/user-provided-model"},
              "instructions": "Use the reviewed plan."
            },
            {
              "id": "documentation",
              "categories": ["documentation"],
              "qualityTier": "economy",
              "rationale": "Routine documentation.",
              "models": {"vscode":"Example Model Name (copilot)","visual-studio":"Example Model Name (copilot)","claude":"haiku","codex":"example-model-id"},
              "instructions": "Keep the update concise."
            }
          ]
        }
        """;

    private sealed class TemporaryRepository : IDisposable
    {
        public TemporaryRepository()
        {
            Root = Path.Combine(Path.GetTempPath(), "brownpants-model-policy-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WritePolicy(string content, string relativePath = ".brownpants/models.json")
        {
            var path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
