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
    public void InstructionsCommand_ExtractsOneSectionWithNestedContentAndStableMetadata()
    {
        Write("AGENTS.md", "# Root\nintro\n\n## Rules\nA\n\n### Detail\nB\n\n## Other\nSECRET\n");
        using var markdown = new StringWriter();
        using var markdownError = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--section", "rules" }, markdown, markdownError));
        Assert.Empty(markdownError.ToString());
        Assert.Contains("AGENTS.md", markdown.ToString());
        Assert.Contains("Anchor: `rules`", markdown.ToString());
        Assert.Contains("Lines: 4-9", markdown.ToString());
        Assert.Contains("### Detail", markdown.ToString());
        Assert.DoesNotContain("SECRET", markdown.ToString());

        using var repeatedMarkdown = new StringWriter();
        using var repeatedMarkdownError = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--section", "rules" }, repeatedMarkdown, repeatedMarkdownError));
        Assert.Equal(markdown.ToString(), repeatedMarkdown.ToString());

        using var json = new StringWriter();
        using var jsonError = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--section", "rules", "--format", "json" }, json, jsonError));
        Assert.Empty(jsonError.ToString());
        using var document = System.Text.Json.JsonDocument.Parse(json.ToString());
        var result = document.RootElement;
        Assert.Equal(1, result.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal("AGENTS.md", result.GetProperty("SourcePath").GetString());
        Assert.Equal("rules", result.GetProperty("Anchor").GetString());
        Assert.Equal(4, result.GetProperty("StartLine").GetInt32());
        Assert.Equal(9, result.GetProperty("EndLine").GetInt32());
        Assert.Contains("### Detail", result.GetProperty("Content").GetString());
        Assert.DoesNotContain("SECRET", result.GetProperty("Content").GetString());

        using var repeatedJson = new StringWriter();
        using var repeatedJsonError = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--section", "rules", "--format", "json" }, repeatedJson, repeatedJsonError));
        Assert.Equal(json.ToString(), repeatedJson.ToString());
    }

    [Fact]
    public void Help_ListsInstructionSectionRetrieval()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "help" }, output, error));

        Assert.Contains("--section <anchor>", output.ToString());
    }

    [Fact]
    public void Analyze_IgnoresHeadingsInsideBacktickAndTildeFences()
    {
        Write("AGENTS.md", "# Root\n\n```markdown\n## Fake backtick\n```\n~~~markdown\n## Fake tilde\n~~~\n## Real\nText.\n");

        var report = InstructionSourceAnalyzer.Analyze(_root);

        Assert.Equal(new[] { "root", "real" }, Assert.Single(report.Sources).Sections.Select(section => section.Anchor));
    }

    [Fact]
    public void InstructionsCommand_UsesInventoryDuplicateAnchorsToSelectOccurrence()
    {
        Write("AGENTS.md", "# Repeated\nfirst occurrence\n\n# Repeated\nsecond occurrence\n");
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--section", "repeated-1" }, output, error));

        Assert.Empty(error.ToString());
        Assert.Contains("second occurrence", output.ToString());
        Assert.DoesNotContain("first occurrence", output.ToString());
    }

    [Fact]
    public void InstructionsCommand_RejectsMissingAndUnsafeSectionsWithoutFallbackContent()
    {
        Write("AGENTS.md", "# Private\nDO NOT RETURN THIS\n");
        using var missingOutput = new StringWriter();
        using var missingError = new StringWriter();

        Assert.Equal(1, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--section", "missing" }, missingOutput, missingError));
        Assert.Empty(missingOutput.ToString());
        Assert.Contains("section", missingError.ToString(), StringComparison.OrdinalIgnoreCase);

        using var unsafeOutput = new StringWriter();
        using var unsafeError = new StringWriter();
        Assert.Equal(1, Program.Run(new[] { "instructions", _root, "--source", "../outside.md", "--section", "private" }, unsafeOutput, unsafeError));
        Assert.Empty(unsafeOutput.ToString());
        Assert.DoesNotContain("DO NOT RETURN THIS", unsafeError.ToString());
    }

    [Fact]
    public void InstructionsCommand_RequiresOneSourceAndValidFormatForSectionRequests()
    {
        Write("AGENTS.md", "# Rules\nBody.\n");
        using var missingSourceOutput = new StringWriter();
        using var missingSourceError = new StringWriter();
        Assert.Equal(2, Program.Run(new[] { "instructions", _root, "--section", "rules" }, missingSourceOutput, missingSourceError));

        using var multipleSourceOutput = new StringWriter();
        using var multipleSourceError = new StringWriter();
        Assert.Equal(2, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--source", "AGENTS.md", "--section", "rules" }, multipleSourceOutput, multipleSourceError));

        using var formatOutput = new StringWriter();
        using var formatError = new StringWriter();
        Assert.Equal(2, Program.Run(new[] { "instructions", _root, "--source", "AGENTS.md", "--section", "rules", "--format", "xml" }, formatOutput, formatError));
    }

    [Fact]
    public void InstructionsCommand_RejectsUnsupportedEncodingAndOversizedSectionsWithoutContent()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".claude"));
        File.WriteAllBytes(Path.Combine(_root, ".claude", "invalid.md"), new byte[] { 0xC3, 0x28 });
        Write(".claude/large.md", new string('x', 1_048_577));

        using var encodingOutput = new StringWriter();
        using var encodingError = new StringWriter();
        Assert.Equal(1, Program.Run(new[] { "instructions", _root, "--source", ".claude/invalid.md", "--section", "rules" }, encodingOutput, encodingError));
        Assert.Empty(encodingOutput.ToString());
        Assert.Contains("UnsupportedEncoding", encodingError.ToString());

        using var sizeOutput = new StringWriter();
        using var sizeError = new StringWriter();
        Assert.Equal(1, Program.Run(new[] { "instructions", _root, "--source", ".claude/large.md", "--section", "rules" }, sizeOutput, sizeError));
        Assert.Empty(sizeOutput.ToString());
        Assert.Contains("TooLarge", sizeError.ToString());
    }

    [Fact]
    public void InstructionsCommand_RejectsSourceThroughSymbolicLinkWhenSupported()
    {
        Write("target.md", "# Rules\nSECRET\n");
        try
        {
            File.CreateSymbolicLink(Path.Combine(_root, "linked.md"), Path.Combine(_root, "target.md"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or PlatformNotSupportedException or ArgumentException)
        {
            return;
        }

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(1, Program.Run(new[] { "instructions", _root, "--source", "linked.md", "--section", "rules" }, output, error));
        Assert.Empty(output.ToString());
        Assert.DoesNotContain("SECRET", error.ToString());
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
