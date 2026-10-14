using BrownPants.Cli;
using System.Text.Json;

namespace BrownPants.Tests;

public sealed class PlacementGuidanceTests
{
    [Theory]
    [InlineData("repository", "enduring", "principle", "constitution")]
    [InlineData("feature", "feature", "outcome", "feature_spec")]
    [InlineData("repository", "enduring", "agent_workflow", "agent_guidance")]
    [InlineData("feature", "enduring", "principle", "feature_spec")]
    public void Placement_RecommendsCanonicalDestinations(string scope, string durability, string purpose, string expected)
    {
        var report = RunJson("All project contributors follow the proposed rule.", scope, durability, purpose);

        Assert.Equal(expected, report.RootElement.GetProperty("Recommendation").GetString());
        Assert.Contains(report.RootElement.GetProperty("Rationale").EnumerateArray(), item =>
            item.GetString()!.Contains(scope, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.RootElement.GetProperty("Rationale").EnumerateArray(), item =>
            item.GetString()!.Contains(durability, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Placement_ClarifiesUnknownInputsWithoutInventingDestination()
    {
        var report = RunJson("A proposed rule.", "unknown", "unknown", "unknown");

        Assert.Equal("clarify", report.RootElement.GetProperty("Recommendation").GetString());
        Assert.Empty(report.RootElement.GetProperty("Destinations").EnumerateArray());
        Assert.Equal(3, report.RootElement.GetProperty("Questions").GetArrayLength());
    }

    [Fact]
    public void Placement_SplitsMixedRulesAndNamesLikelyDestinations()
    {
        var report = RunJson("A general rule plus one feature's outcome.", "repository", "enduring", "mixed");

        Assert.Equal("split", report.RootElement.GetProperty("Recommendation").GetString());
        var destinations = report.RootElement.GetProperty("Destinations").EnumerateArray()
            .Select(item => item.GetString()).ToArray();
        Assert.Contains("constitution", destinations);
        Assert.Contains("feature_spec", destinations);
        Assert.Contains(report.RootElement.GetProperty("Rationale").EnumerateArray(), item =>
            item.GetString()!.Contains("Separate", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("principle", "repository", "feature")]
    [InlineData("outcome", "repository", "feature")]
    public void Placement_ClarifiesContradictoryScopeAndDurability(string purpose, string scope, string durability)
    {
        var report = RunJson("A rule with conflicting boundaries.", scope, durability, purpose);

        Assert.Equal("clarify", report.RootElement.GetProperty("Recommendation").GetString());
        Assert.NotEmpty(report.RootElement.GetProperty("Questions").EnumerateArray());
        Assert.Contains(report.RootElement.GetProperty("Rationale").EnumerateArray(), item =>
            item.GetString()!.Contains("conflict", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Placement_SplitsDurableRepositoryOutcomeIntoPrincipleAndFeatureSpecs()
    {
        var report = RunJson("All features achieve the same broad outcome.", "repository", "enduring", "outcome");

        Assert.Equal("split", report.RootElement.GetProperty("Recommendation").GetString());
        var destinations = report.RootElement.GetProperty("Destinations").EnumerateArray()
            .Select(item => item.GetString()).ToArray();
        Assert.Equal(new[] { "constitution", "feature_spec" }, destinations);
    }

    [Fact]
    public void Placement_ProducesDeterministicMarkdownAndJsonWithTheSameDecision()
    {
        var args = new[] { "placement", "--rule", "A stable rule", "--scope", "repository", "--durability", "enduring", "--purpose", "principle" };
        using var firstOutput = new StringWriter();
        using var firstError = new StringWriter();
        using var repeatOutput = new StringWriter();
        using var repeatError = new StringWriter();
        using var jsonOutput = new StringWriter();
        using var jsonError = new StringWriter();
        using var repeatJsonOutput = new StringWriter();
        using var repeatJsonError = new StringWriter();

        Assert.Equal(0, Program.Run(args, firstOutput, firstError));
        Assert.Equal(0, Program.Run(args, repeatOutput, repeatError));
        Assert.Equal(firstOutput.ToString(), repeatOutput.ToString());
        Assert.Equal(0, Program.Run(args.Concat(new[] { "--format", "json" }).ToArray(), jsonOutput, jsonError));
        Assert.Equal(0, Program.Run(args.Concat(new[] { "--format", "json" }).ToArray(), repeatJsonOutput, repeatJsonError));
        Assert.Equal(jsonOutput.ToString(), repeatJsonOutput.ToString());
        using var json = JsonDocument.Parse(jsonOutput.ToString());
        Assert.Equal(1, json.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal("constitution", json.RootElement.GetProperty("Recommendation").GetString());
        Assert.Contains("constitution", firstOutput.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not infer", firstOutput.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not infer", json.RootElement.GetProperty("EvidenceLimits").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("feature specifications", firstOutput.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operational agent or contributor procedures", firstOutput.ToString(), StringComparison.OrdinalIgnoreCase);
        foreach (var rationale in json.RootElement.GetProperty("Rationale").EnumerateArray())
            Assert.Contains(rationale.GetString()!, firstOutput.ToString(), StringComparison.Ordinal);
        Assert.Empty(firstError.ToString());
        Assert.Empty(repeatError.ToString());
        Assert.Empty(jsonError.ToString());
        Assert.Empty(repeatJsonError.ToString());
    }

    [Fact]
    public void Placement_MarkdownIncludesTheSameClarificationQuestionsAsJson()
    {
        using var markdownOutput = new StringWriter();
        using var markdownError = new StringWriter();
        using var jsonOutput = new StringWriter();
        using var jsonError = new StringWriter();
        var baseArgs = new[] { "placement", "--rule", "A rule with unspecified attributes" };

        Assert.Equal(0, Program.Run(baseArgs, markdownOutput, markdownError));
        Assert.Equal(0, Program.Run(baseArgs.Concat(new[] { "--format", "json" }).ToArray(), jsonOutput, jsonError));
        using var json = JsonDocument.Parse(jsonOutput.ToString());
        foreach (var question in json.RootElement.GetProperty("Questions").EnumerateArray())
            Assert.Contains(question.GetString()!, markdownOutput.ToString(), StringComparison.Ordinal);
        Assert.Empty(markdownError.ToString());
        Assert.Empty(jsonError.ToString());
    }

    [Fact]
    public void Placement_RejectsInvalidInputs()
    {
        Assert.Equal(2, RunExit("--rule", "Rule", "--scope", "workspace", "--durability", "enduring", "--purpose", "principle"));
        Assert.Equal(2, RunExit("--rule", "", "--scope", "repository", "--durability", "enduring", "--purpose", "principle"));
        Assert.Equal(2, RunExit("--rule", new string('x', 4001), "--scope", "repository", "--durability", "enduring", "--purpose", "principle"));
    }

    [Fact]
    public void Placement_JsonFieldsMatchDocumentedContract()
    {
        var report = RunJson("A rule", "repository", "enduring", "principle");
        var schemaPath = Path.Combine(FindRepositoryRoot(), "specs", "020-requirement-placement-guidance", "contracts", "placement-guidance.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var required = schema.RootElement.GetProperty("required").EnumerateArray().Select(item => item.GetString()!).ToArray();
        var actual = report.RootElement.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

        Assert.All(required, field => Assert.Contains(field, actual));
        Assert.Equal(1, report.RootElement.GetProperty("SchemaVersion").GetInt32());
        var inputSchema = schema.RootElement.GetProperty("properties").GetProperty("Inputs").GetProperty("required")
            .EnumerateArray().Select(item => item.GetString()!).ToArray();
        var actualInputs = report.RootElement.GetProperty("Inputs").EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        Assert.All(inputSchema, field => Assert.Contains(field, actualInputs));
    }

    private static JsonDocument RunJson(string rule, string scope, string durability, string purpose)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = Program.Run(new[]
        {
            "placement", "--rule", rule, "--scope", scope, "--durability", durability,
            "--purpose", purpose, "--format", "json"
        }, output, error);
        Assert.Equal(0, exit);
        Assert.Empty(error.ToString());
        return JsonDocument.Parse(output.ToString());
    }

    private static int RunExit(params string[] values)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        return Program.Run(new[] { "placement" }.Concat(values).ToArray(), output, error);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BrownPants.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate BrownPants repository root.");
    }
}
