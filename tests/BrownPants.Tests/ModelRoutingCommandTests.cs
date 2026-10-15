using System.Text.Json;
using BrownPants.Cli;

namespace BrownPants.Tests;

public sealed class ModelRoutingCommandTests
{
    [Fact]
    public void Report_MarkdownAndJson_ExposeEquivalentConfiguredValues()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(Policy);
        using var markdown = new StringWriter();
        using var markdownError = new StringWriter();
        using var json = new StringWriter();
        using var jsonError = new StringWriter();

        var markdownExit = Program.Run(["models", "report", repository.Root], markdown, markdownError);
        var jsonExit = Program.Run(["models", "report", repository.Root, "--format", "json"], json, jsonError);
        using var document = JsonDocument.Parse(json.ToString());

        Assert.Equal(0, markdownExit);
        Assert.Equal(0, jsonExit);
        Assert.Contains("implementation", markdown.ToString(), StringComparison.Ordinal);
        Assert.Contains("Example Model Name (copilot)", markdown.ToString(), StringComparison.Ordinal);
        Assert.Contains("not checked", markdown.ToString(), StringComparison.OrdinalIgnoreCase);
        var profile = document.RootElement.GetProperty("profiles").EnumerateArray()
            .Single(element => element.GetProperty("id").GetString() == "implementation");
        var vscodeAssignment = profile.GetProperty("assignments").EnumerateArray()
            .Single(element => element.GetProperty("host").GetString() == "vscode");
        Assert.Equal("implementation", profile.GetProperty("id").GetString());
        Assert.Equal("Example Model Name (copilot)", vscodeAssignment.GetProperty("model").GetString());
        var visualStudioAssignment = profile.GetProperty("assignments").EnumerateArray()
            .Single(element => element.GetProperty("host").GetString() == "visual-studio");
        Assert.Equal("Example Model Name (copilot)", visualStudioAssignment.GetProperty("model").GetString());
        Assert.Contains("Native profile export supported", visualStudioAssignment.GetProperty("supportStatus").GetString(), StringComparison.Ordinal);
        var visualStudioCapability = document.RootElement.GetProperty("hostCapabilities").EnumerateArray()
            .Single(element => element.GetProperty("host").GetString() == "visual-studio");
        Assert.Contains("18.4+", visualStudioCapability.GetProperty("exportStatus").GetString(), StringComparison.Ordinal);
        Assert.Contains("Older versions are unverified", visualStudioCapability.GetProperty("exportStatus").GetString(), StringComparison.Ordinal);
        Assert.Contains("No native exporter", json.ToString(), StringComparison.Ordinal);
        Assert.Contains("visual-studio", json.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, markdownError.ToString());
        Assert.Equal(string.Empty, jsonError.ToString());
    }

    [Fact]
    public void Report_MissingPolicy_ReportsNoAssignmentsWithoutWritingFiles()
    {
        using var repository = new TemporaryRepository();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(["models", "report", repository.Root], output, error);
        using var validateOutput = new StringWriter();
        using var validateError = new StringWriter();
        var validateExit = Program.Run(["models", "validate", repository.Root], validateOutput, validateError);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, validateExit);
        Assert.Contains("No model assignments are configured", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("No model assignments are configured", validateOutput.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(repository.Root, ".brownpants")));
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal(string.Empty, validateError.ToString());
    }

    [Fact]
    public void Validate_ValidPolicy_ReturnsSuccessAndDoesNotModifyIt()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(Policy);
        var before = File.ReadAllBytes(repository.PolicyPath);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(["models", "validate", repository.Root], output, error);

        Assert.Equal(0, exitCode);
        Assert.Contains("2 profiles", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(before, File.ReadAllBytes(repository.PolicyPath));
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void Export_RequiresExplicitKnownHost_BeforeWriting()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(Policy);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var noHost = Program.Run(["models", "export", repository.Root], output, error);
        var unsupported = Program.Run(["models", "export", repository.Root, "--host", "unicorn"], output, error);

        Assert.Equal(2, noHost);
        Assert.Equal(1, unsupported);
        Assert.False(Directory.Exists(Path.Combine(repository.Root, ".github")));
        Assert.False(Directory.Exists(Path.Combine(repository.Root, ".codex")));
        Assert.Contains("Unsupported model host", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Export_VisualStudio_IsAnExplicitSupportedHost()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy(Policy);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(["models", "export", repository.Root, "--host", "visual-studio"], output, error);

        Assert.Equal(0, exitCode);
        Assert.Contains("2 created", output.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(repository.Root, ".github", "agents", "brownpants-visual-studio-implementation.agent.md")));
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void Validate_ReportsMalformedPolicyWithNonzeroExit()
    {
        using var repository = new TemporaryRepository();
        repository.WritePolicy("{ invalid }");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(["models", "validate", repository.Root], output, error);

        Assert.Equal(1, exitCode);
        Assert.Contains("models.json", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("invalid JSON", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Help_ListsSupportedModelOperations()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = Program.Run(["models", "--help"], output, error);
        Assert.Equal(0, exitCode);
        Assert.Contains("validate", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("export", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("visual-studio", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("required to prevent guessing", output.ToString(), StringComparison.Ordinal);
    }

    private const string Policy = """
        {
          "schemaVersion": 1,
          "profiles": [
            {"id":"implementation","categories":["implementation"],"qualityTier":"high","rationale":"Complex code work.","models":{"vscode":"Example Model Name (copilot)","visual-studio":"Example Model Name (copilot)","claude":"opus","codex":"example-model-id","other-host":"manual model"},"instructions":"Use the reviewed plan."},
            {"id":"documentation","categories":["documentation"],"qualityTier":"economy","rationale":"Routine documentation.","models":{"vscode":"Example Model Name (copilot)","visual-studio":"Example Model Name (copilot)","claude":"haiku","codex":"example-model-id"},"instructions":"Keep updates concise."}
          ]
        }
        """;

    private sealed class TemporaryRepository : IDisposable
    {
        public TemporaryRepository()
        {
            Root = Path.Combine(Path.GetTempPath(), "brownpants-model-command-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }
        public string PolicyPath => Path.Combine(Root, ".brownpants", "models.json");

        public void WritePolicy(string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PolicyPath)!);
            File.WriteAllText(PolicyPath, content);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
