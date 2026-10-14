using BrownPants.Cli;
using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class InstructionSourceAnalyzerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-instruction-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Analyze_FindsConventionalAndExplicitSourcesWithStableAnchorsAndDigest()
    {
        Write("AGENTS.md", "# Repository\n\n## Build and Test\nRun tests.");
        Write(".github/instructions/ui.instructions.md", "---\napplyTo: src/**/*.cs\n---\n# UI rules\nUse accessible labels.");
        Write(".cursor/rules/review.mdc", "# Review\n\n# Review\nFollow the scoped rule.");
        Write("notes/custom.txt", "Use the project formatter.");
        Write("docs/readme.md", "# Not an instruction convention");

        var report = InstructionSourceAnalyzer.Analyze(_root, new[] { "notes/custom.txt" });

        Assert.Equal(new[] { ".cursor/rules/review.mdc", ".github/instructions/ui.instructions.md", "AGENTS.md", "notes/custom.txt" }, report.Sources.Select(source => source.Path));
        var agents = Assert.Single(report.Sources, source => source.Path == "AGENTS.md");
        Assert.Equal("Readable", agents.Status);
        Assert.Equal("build-and-test", agents.Sections[1].Anchor);
        Assert.Equal(64, agents.ContentDigest!.Length);
        Assert.True(agents.ApproximateTokenCount > 0);
        Assert.Equal("Explicit", Assert.Single(report.Sources, source => source.Path == "notes/custom.txt").Recognition);
        var copilotRule = Assert.Single(report.Sources, source => source.Path == ".github/instructions/ui.instructions.md");
        Assert.Contains("src/**/*.cs", copilotRule.Scope);
        var cursorRule = Assert.Single(report.Sources, source => source.Path == ".cursor/rules/review.mdc");
        Assert.Equal(new[] { "review", "review-1" }, cursorRule.Sections.Select(section => section.Anchor));
    }

    [Fact]
    public void Analyze_RejectsCustomSourcesOutsideRepository()
    {
        Write("AGENTS.md", "# Instructions");

        Assert.Throws<ArgumentException>(() => InstructionSourceAnalyzer.Analyze(_root, new[] { "../outside.md" }));
    }

    [Fact]
    public void Analyze_ReportsUnsupportedEncodingAndOversizedSourcesWithoutDroppingReadableOnes()
    {
        Write("AGENTS.md", "# Valid instructions");
        var invalidPath = Path.Combine(_root, "GEMINI.md");
        Directory.CreateDirectory(_root);
        File.WriteAllBytes(invalidPath, new byte[] { 0xC3, 0x28 });
        Write(".claude/large.md", new string('x', 1_048_577));

        var report = InstructionSourceAnalyzer.Analyze(_root);

        Assert.Equal("Readable", Assert.Single(report.Sources, source => source.Path == "AGENTS.md").Status);
        Assert.Equal("UnsupportedEncoding", Assert.Single(report.Sources, source => source.Path == "GEMINI.md").Status);
        Assert.Equal("TooLarge", Assert.Single(report.Sources, source => source.Path == ".claude/large.md").Status);
    }

    [Fact]
    public void InstructionsCommand_EmitsJsonAndApproximationCaveat()
    {
        Write("AGENTS.md", "# Rules\nKeep sources linked.");
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "instructions", _root, "--format", "json" }, output, error));
        Assert.Empty(error.ToString());
        using var document = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Equal("AGENTS.md", document.RootElement.GetProperty("Sources")[0].GetProperty("Path").GetString());
    }

    [Fact]
    public void FrameCreate_ValidatesReferencesAndRefusesOverwriteByDefault()
    {
        Write("AGENTS.md", "# Repository\n\n## Safety\nPreserve canonical instructions.");
        Write("draft.json", """{"budget":{"unit":"words","requested":3},"points":[{"text":"Keep canonical sources authoritative.","sources":[{"path":"AGENTS.md","anchor":"safety"}]}],"reviewItems":["Review conflicting guidance before implementation."]}""");
        var sourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(_root, "AGENTS.md"))));
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "frame", "create", _root, Path.Combine(_root, "draft.json"), "context/frame.md" }, output, error));
        Assert.Empty(error.ToString());
        var framePath = Path.Combine(_root, "context", "frame.md");
        var frame = File.ReadAllText(framePath);
        Assert.Contains("AGENTS.md#safety", frame);
        Assert.Contains("SHA-256", frame);
        Assert.Contains("does not guarantee", frame);
        Assert.Contains("Budget exceeded", frame);
        Assert.Contains("Review conflicting guidance", frame);
        Assert.Equal(sourceHash, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(_root, "AGENTS.md")))));

        using var validationOutput = new StringWriter();
        using var validationError = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "frame", "validate", _root, "context/frame.md" }, validationOutput, validationError));
        Write("AGENTS.md", "# Repository\n\n## Safety\nChanged source.");
        using var staleOutput = new StringWriter();
        using var staleError = new StringWriter();
        Assert.Equal(1, Program.Run(new[] { "frame", "validate", _root, "context/frame.md" }, staleOutput, staleError));
        Assert.Contains("stale", staleOutput.ToString(), StringComparison.OrdinalIgnoreCase);

        using var secondOutput = new StringWriter();
        using var secondError = new StringWriter();
        Assert.Equal(1, Program.Run(new[] { "frame", "create", _root, Path.Combine(_root, "draft.json"), "context/frame.md" }, secondOutput, secondError));
        Assert.Contains("already exists", secondError.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FrameCreate_RejectsUnknownSectionsAndEscapingOutput()
    {
        Write("AGENTS.md", "# Repository");
        Write("draft.json", """{"budget":{"unit":"tokens","requested":20},"points":[{"text":"Use sources.","sources":[{"path":"AGENTS.md","anchor":"missing"}]}]}""");
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(1, Program.Run(new[] { "frame", "create", _root, Path.Combine(_root, "draft.json"), "frame.md" }, output, error));
        Assert.Contains("was not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
        using var outsideOutput = new StringWriter();
        using var outsideError = new StringWriter();
        Assert.Equal(2, Program.Run(new[] { "frame", "create", _root, Path.Combine(_root, "draft.json"), "../frame.md" }, outsideOutput, outsideError));
    }

    private void Write(string path, string content)
    {
        var fullPath = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
