using BrownPants.Cli;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BrownPants.Tests;

public sealed class RepositoryScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Help_DescribesWorkflowNeutralPurpose()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "--help" }, output, error));
        Assert.Contains("spec-driven development", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("investigate [path] --clue", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("for Spec Kit", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("inspect", "--help", "--semantic")]
    [InlineData("investigate", "-h", "--clue")]
    [InlineData("impact", "--help", "--changed")]
    [InlineData("evidence", "--help", "candidate")]
    [InlineData("instructions", "-h", "--source")]
    [InlineData("workbench", "--help", "network requests")]
    [InlineData("workbench", "--help", "openspec/changes")]
    [InlineData("placement", "--help", "--rule")]
    [InlineData("frame", "--help", "create")]
    [InlineData("frame create", "-h", "--overwrite")]
    [InlineData("frame validate", "--help", "validate")]
    public void Help_CommandTopicsPrintStaticCommandHelp(string topic, string option, string expectedText)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var args = topic.Split(' ').Concat(new[] { option, "C:\\missing-brownpants-repository" }).ToArray();

        Assert.Equal(0, Program.Run(args, output, error));
        Assert.Contains(expectedText, output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void Help_UnknownTopicSuggestsGlobalHelp()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(2, Program.Run(new[] { "help", "unknown" }, output, error));
        Assert.Contains("unknown", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--help", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Help_AfterInvalidRepositoryPathDoesNotInspectPath()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "inspect", "C:\\missing-brownpants-repository", "--help" }, output, error));
        Assert.Contains("syntax-only", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void DiscoverFiles_ReturnsStablePathsAndSkipsIgnoredDirectories()
    {
        Write("src/z.cs", "class Z {}");
        Write("src/A.cs", "class A {}");
        Write("obj/Generated.cs", "class Generated {}");
        Write(".git/config", "internal");

        var files = RepositoryScanner.DiscoverFiles(_root);

        Assert.Equal(new[] { "src/A.cs", "src/z.cs" }, files);
    }

    [Fact]
    public void DiscoverFiles_SkipsGeneratedTestOutputsButKeepsAuthoredFixturesAndAllowsExplicitRoot()
    {
        Write("TestResults/run.trx", "generated");
        Write("test-results/junit.xml", "generated");
        Write("testresults/summary.json", "generated");
        Write("bin/app.dll", "generated");
        Write("obj/project.assets.json", "generated");
        Write("coverage/summary.json", "generated");
        Write("artifacts/tool.nupkg", "generated");
        Write("tests/fixtures/result.json", "authored fixture");
        Write("src/App.cs", "class App { }");
        var before = Directory.GetFiles(_root, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var resultsNameAliasesShareDirectory = File.Exists(Path.Combine(_root, "testresults", "run.trx"));

        var files = RepositoryScanner.DiscoverFiles(_root);
        var repeated = RepositoryScanner.DiscoverFiles(_root);

        Assert.Equal(files, repeated);
        Assert.Contains("src/App.cs", files);
        Assert.Contains("tests/fixtures/result.json", files);
        Assert.DoesNotContain(files, path => path.StartsWith("TestResults/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, path => path.StartsWith("test-results/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, path => path.StartsWith("testresults/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, path => path.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, path => path.StartsWith("bin/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, path => path.StartsWith("obj/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, path => path.StartsWith("coverage/", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(before, Directory.GetFiles(_root, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).ToArray());

        var selectedResultsRoot = RepositoryScanner.DiscoverFiles(Path.Combine(_root, "TestResults"));
        Assert.Contains("run.trx", selectedResultsRoot);
        if (resultsNameAliasesShareDirectory)
            Assert.Contains("summary.json", selectedResultsRoot); // Some Windows and macOS filesystems are case-insensitive.
        else
            Assert.Equal(new[] { "run.trx" }, selectedResultsRoot);

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", Path.Combine(_root, "TestResults"), "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Equal(resultsNameAliasesShareDirectory ? 2 : 1, json.RootElement.GetProperty("FileCount").GetInt32());
    }

    [Fact]
    public void DiscoverFiles_IncludesPackagesOnlyWhenAJavaScriptWorkspaceDeclaresThem()
    {
        Write("packages/sample/index.ts", "export function run() {}\n");
        Assert.DoesNotContain("packages/sample/index.ts", RepositoryScanner.DiscoverFiles(_root));

        Write("package.json", "{\"private\":true,\"workspaces\":[\"packages/*\"]}");

        Assert.Contains("packages/sample/index.ts", RepositoryScanner.DiscoverFiles(_root));
    }

    [Fact]
    public void DiscoverFiles_DoesNotFollowDirectorySymlinksWhenSupported()
    {
        var scanRoot = Path.Combine(_root, "repository");
        var target = Path.Combine(_root, "external");
        Directory.CreateDirectory(scanRoot);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "outside.txt"), "external");

        try
        {
            Directory.CreateSymbolicLink(Path.Combine(scanRoot, "linked"), target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or PlatformNotSupportedException or ArgumentException)
        {
            return;
        }

        var files = RepositoryScanner.DiscoverFiles(scanRoot);

        Assert.DoesNotContain(files, path => path.StartsWith("linked/", StringComparison.Ordinal));
    }

    [Fact]
    public void Investigate_ReportsLiteralPathAndContentMatchesAndTestCandidates()
    {
        Write("src/OrderService.cs", "class OrderService { void Run() { throw new TimeoutException(); } }\n// ERR-42");
        Write("tests/OrderServiceTests.cs", "// TimeoutException regression check");
        Write("obj/Generated.cs", "TimeoutException");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = Program.Run(new[]
        {
            "investigate", _root, "--clue", "timeoutexception", "--clue", "orderservice", "--clue", "not-present", "--clue", "timeoutexception", "--format", "json"
        }, output, error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var report = json.RootElement;
        Assert.Equal(1, report.GetProperty("SchemaVersion").GetInt32());
        var clues = report.GetProperty("Clues").EnumerateArray().ToArray();
        Assert.Equal(4, clues.Length);
        var timeoutMatches = clues[0].GetProperty("Matches").EnumerateArray().ToArray();
        Assert.Contains(timeoutMatches, match => match.GetProperty("Path").GetString() == "src/OrderService.cs"
            && match.GetProperty("Kind").GetString() == "Content"
            && match.GetProperty("Line").GetInt32() == 1
            && !match.GetProperty("IsTestCandidate").GetBoolean());
        Assert.Contains(timeoutMatches, match => match.GetProperty("Path").GetString() == "tests/OrderServiceTests.cs"
            && match.GetProperty("IsTestCandidate").GetBoolean());
        Assert.Contains(clues[1].GetProperty("Matches").EnumerateArray(), match =>
            match.GetProperty("Path").GetString() == "src/OrderService.cs"
            && match.GetProperty("Kind").GetString() == "Path"
            && match.GetProperty("Line").ValueKind == System.Text.Json.JsonValueKind.Null);
        Assert.Equal(0, clues[2].GetProperty("MatchCount").GetInt32());
        Assert.Equal("timeoutexception", clues[3].GetProperty("Clue").GetString());
        Assert.Equal(clues[0].GetProperty("MatchCount").GetInt32(), clues[3].GetProperty("MatchCount").GetInt32());
        Assert.DoesNotContain("throw new TimeoutException", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("ERR-42", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Investigate_ProducesStableMarkdownAndJsonAndDoesNotExecuteSource()
    {
        var markerPath = Path.Combine(_root, "executed.marker");
        Write("src/Feature.cs", $"System.IO.File.WriteAllText(@\"{markerPath}\", \"executed\");\nclass Feature {{ }}");
        Write("tests/FeatureTests.cs", "// Feature regression test");

        using var markdown = new StringWriter();
        using var repeatedMarkdown = new StringWriter();
        using var json = new StringWriter();
        using var error = new StringWriter();
        var args = new[] { "investigate", _root, "--clue", "Feature", "--limit", "3" };

        Assert.Equal(0, Program.Run(args, markdown, error));
        Assert.Equal(0, Program.Run(args, repeatedMarkdown, error));
        Assert.Equal(markdown.ToString(), repeatedMarkdown.ToString());
        Assert.DoesNotContain("File.WriteAllText", markdown.ToString(), StringComparison.Ordinal);
        Assert.Contains("candidate", markdown.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("additional matches omitted", markdown.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(markerPath));

        Assert.Equal(0, Program.Run(new[] { "investigate", _root, "--clue", "Feature", "--limit", "3", "--format", "json" }, json, error));
        using var jsonDocument = System.Text.Json.JsonDocument.Parse(json.ToString());
        Assert.Equal(3, jsonDocument.RootElement.GetProperty("SampleLimit").GetInt32());
        foreach (var match in jsonDocument.RootElement.GetProperty("Clues")[0].GetProperty("Matches").EnumerateArray())
        {
            var location = match.GetProperty("Path").GetString()!;
            if (match.GetProperty("Line").ValueKind == System.Text.Json.JsonValueKind.Number)
                location += $":{match.GetProperty("Line").GetInt32()}";
            Assert.Contains(location, markdown.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Investigate_SkipsBinaryAndOversizedFilesWithStableDiagnostics()
    {
        Write("assets/binary.dat", "prefix\0Needle");
        var largeFile = Path.Combine(_root, "assets", "large.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(largeFile)!);
        using (var stream = File.Create(largeFile))
            stream.SetLength(10 * 1024 * 1024 + 1);

        var first = RepositoryInvestigator.Investigate(_root, new[] { "Needle", "large.txt" }, sampleLimit: 5);
        var second = RepositoryInvestigator.Investigate(_root, new[] { "Needle", "large.txt" }, sampleLimit: 5);

        Assert.Equal(2, first.ReadIssueCount);
        Assert.Equal(first.ReadIssues, second.ReadIssues);
        Assert.Contains(first.ReadIssues, issue => issue.Path == "assets/binary.dat" && issue.Code == "BinaryFileSkipped");
        Assert.Contains(first.ReadIssues, issue => issue.Path == "assets/large.txt" && issue.Code == "FileTooLarge");
        Assert.DoesNotContain(first.Clues[0].Matches, match => match.Kind == "Content");
        Assert.Contains(first.Clues[1].Matches, match => match.Kind == "Path" && match.Path == "assets/large.txt");
    }

    [Theory]
    [MemberData(nameof(InvalidInvestigationArguments))]
    public void Investigate_RejectsMissingOrInvalidArguments(string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(2, Program.Run(args, output, error));
        Assert.NotEmpty(error.ToString());
    }

    [Fact]
    public void Investigate_RejectsExcessiveCluesAndClueLength()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var tooManyClues = new List<string> { "investigate", _root };
        for (var index = 0; index < 21; index++)
        {
            tooManyClues.Add("--clue");
            tooManyClues.Add("x");
        }

        Assert.Equal(2, Program.Run(tooManyClues.ToArray(), output, error));
        Assert.Contains("no more than 20", error.ToString(), StringComparison.OrdinalIgnoreCase);

        error.GetStringBuilder().Clear();
        Assert.Equal(2, Program.Run(new[] { "investigate", _root, "--clue", new string('x', 1001) }, output, error));
        Assert.Contains("1000 characters", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> InvalidInvestigationArguments => new[]
    {
        new object[] { new[] { "investigate" } },
        new object[] { new[] { "investigate", "." } },
        new object[] { new[] { "investigate", ".", "--clue", "--format", "json" } },
        new object[] { new[] { "investigate", ".", "--clue", "  " } },
        new object[] { new[] { "investigate", ".", "--clue", "x", "--format", "xml" } }
    };

    [Fact]
    public void Scan_CollectsProjectSignalsAndExcludesGeneratedDirectories()
    {
        Write("src/App.csproj", "<Project />");
        Write("src/Program.cs", "class Program {}");
        Write("tests/AppTests.cs", "test");
        Write(".specify/memory/constitution.md", "principles");
        Write("specs/001-feature/spec.md", "feature");
        Write("openspec/config.yaml", "schema: spec-driven");
        Write("openspec/project.md", "project context");
        Write("openspec/specs/auth/spec.md", "current behavior");
        Write("openspec/changes/add-login/proposal.md", "new behavior");
        Write("openspec/changes/add-login/tasks.md", "- [ ] implement");
        Write("openspec/changes/archive/old-change/proposal.md", "archived");
        Write("obj/Generated.cs", "generated");
        Write(".git/config", "internal");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 2);

        Assert.Equal(11, inventory.FileCount);
        Assert.Contains("src/App.csproj", inventory.ProjectMarkers);
        Assert.Contains("tests/AppTests.cs", inventory.TestPaths);
        Assert.DoesNotContain(inventory.TestPaths, path => path.Contains("Generated", StringComparison.Ordinal));
        var specKit = Assert.Single(inventory.SddWorkflows, workflow => workflow.Name == "Spec Kit");
        Assert.True(specKit.IsInitialized);
        Assert.Equal(1, specKit.SpecificationDirectoryCount);
        Assert.Null(specKit.ActiveChangeDirectoryCount);
        Assert.Contains(".specify/memory/constitution.md", specKit.ArtifactPaths);
        var openSpec = Assert.Single(inventory.SddWorkflows, workflow => workflow.Name == "OpenSpec");
        Assert.Equal(1, openSpec.SpecificationDirectoryCount);
        Assert.Equal(1, openSpec.ActiveChangeDirectoryCount);
        Assert.Equal(6, openSpec.ArtifactFileCount);
        Assert.Equal(2, openSpec.ArtifactPaths.Count);
        Assert.Contains("openspec/changes/add-login/proposal.md", openSpec.ArtifactPaths);
        Assert.Contains("## SDD workflows", MarkdownReport.Render(inventory));
        Assert.Contains("### OpenSpec", MarkdownReport.Render(inventory));
        Assert.Contains("4 additional artifacts omitted by the sample limit", MarkdownReport.Render(inventory));
    }

    [Fact]
    public void Scan_ClassifiesTestPathsWithoutSubstringFilenameFalsePositives()
    {
        Write("src/ContestController.cs", "public sealed class ContestController { }");
        Write("src/Latest.cs", "public sealed class Latest { }");
        Write("src/Testimonial.cs", "public sealed class Testimonial { }");
        Write("src/ContestTests.cs", "public sealed class ContestTests { }");
        Write("src/TestWidget.cs", "public sealed class TestWidget { }");
        Write("src/test_widget.py", "class TestWidget: pass");
        Write("src/test.widget.js", "export const testWidget = true;");
        Write("src/widget.test.ts", "export const testWidget = true;");
        Write("tests/RepositoryScannerTests.cs", "public sealed class RepositoryScannerTests { }");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 2);

        Assert.DoesNotContain("src/ContestController.cs", inventory.TestPaths);
        Assert.DoesNotContain("src/Latest.cs", inventory.TestPaths);
        Assert.DoesNotContain("src/Testimonial.cs", inventory.TestPaths);
        Assert.Contains("src/ContestTests.cs", inventory.TestPaths);
        Assert.Contains("src/TestWidget.cs", inventory.TestPaths);
        Assert.Contains("src/test_widget.py", inventory.TestPaths);
        Assert.Contains("src/test.widget.js", inventory.TestPaths);
        Assert.Contains("src/widget.test.ts", inventory.TestPaths);
        Assert.Contains("tests/RepositoryScannerTests.cs", inventory.TestPaths);
    }

    [Fact]
    public void Scan_DetectsOpenSpecWithoutSpecKit()
    {
        Write("openspec/config.yaml", "schema: spec-driven");
        Write("openspec/specs/catalog/spec.md", "current behavior");

        var inventory = RepositoryScanner.Scan(_root);

        var workflow = Assert.Single(inventory.SddWorkflows);
        Assert.Equal("OpenSpec", workflow.Name);
        Assert.True(workflow.IsInitialized);
        Assert.Equal(1, workflow.SpecificationDirectoryCount);
        Assert.Equal(0, workflow.ActiveChangeDirectoryCount);
        Assert.DoesNotContain(MarkdownReport.Render(inventory), "## Spec Kit state", StringComparison.Ordinal);

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var jsonWorkflow = Assert.Single(json.RootElement.GetProperty("SddWorkflows").EnumerateArray());
        Assert.Equal("OpenSpec", jsonWorkflow.GetProperty("Name").GetString());
        Assert.Equal(1, jsonWorkflow.GetProperty("SpecificationDirectoryCount").GetInt32());
        Assert.Equal(0, jsonWorkflow.GetProperty("ActiveChangeDirectoryCount").GetInt32());
    }

    [Fact]
    public void Scan_ReportsValidatedSpecKitFeaturePointerInMarkdownAndJson()
    {
        Write(".specify/feature.json", System.Text.Json.JsonSerializer.Serialize(
            new { feature_directory = @"specs\001-active-feature" }));
        Write("specs/001-active-feature/spec.md", "active specification");
        Write("specs/001-active-feature/plan.md", "active plan");
        Write("specs/002-other-feature/spec.md", "another specification");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 1);
        var workflow = Assert.Single(inventory.SddWorkflows, item => item.Name == "Spec Kit");
        var markdown = MarkdownReport.Render(inventory);

        Assert.Contains("- Active work item: `specs/001-active-feature` (SpecKitFeaturePointer)", markdown);
        Assert.Contains("3 additional artifacts omitted by the sample limit", markdown);
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Equal(1, json.RootElement.GetProperty("SchemaVersion").GetInt32());
        var jsonWorkflow = Assert.Single(json.RootElement.GetProperty("SddWorkflows").EnumerateArray(),
            item => item.GetProperty("Name").GetString() == "Spec Kit");
        Assert.Equal("specs/001-active-feature", jsonWorkflow.GetProperty("ActiveWorkItemPath").GetString());
        Assert.Equal("SpecKitFeaturePointer", jsonWorkflow.GetProperty("ActiveWorkItemEvidence").GetString());
        Assert.Null(workflow.ActiveChangeDirectoryCount);
    }

    [Fact]
    public void Scan_ReportsOnlyUniqueOpenSpecChangeAsActive()
    {
        Write("openspec/config.yaml", "schema: spec-driven");
        Write("openspec/changes/add-search/proposal.md", "change proposal");
        Write("openspec/changes/archive/old-change/proposal.md", "archived proposal");
        Write(".brownpants/sdd-workflows.json", """
            { "schemaVersion": 1, "workflows": [
              { "name": "CustomSDD", "artifactRoots": ["custom-plans"], "changeRoots": ["custom-plans/changes"] }
            ] }
            """);
        Write("custom-plans/changes/custom-change/proposal.md", "custom proposal");

        var unique = RepositoryScanner.Scan(_root);
        var openSpec = Assert.Single(unique.SddWorkflows, item => item.Name == "OpenSpec");
        Assert.Equal("openspec/changes/add-search", openSpec.ActiveWorkItemPath);
        Assert.Equal("UniqueOpenSpecChange", openSpec.ActiveWorkItemEvidence);
        var custom = Assert.Single(unique.SddWorkflows, item => item.Name == "CustomSDD");
        Assert.Null(custom.ActiveWorkItemPath);
        Assert.Null(custom.ActiveWorkItemEvidence);

        Write("openspec/changes/update-search/proposal.md", "another proposal");
        var ambiguous = RepositoryScanner.Scan(_root);
        var ambiguousOpenSpec = Assert.Single(ambiguous.SddWorkflows, item => item.Name == "OpenSpec");
        Assert.Equal(2, ambiguousOpenSpec.ActiveChangeDirectoryCount);
        Assert.Null(ambiguousOpenSpec.ActiveWorkItemPath);
        Assert.Null(ambiguousOpenSpec.ActiveWorkItemEvidence);
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var jsonWorkflow = Assert.Single(json.RootElement.GetProperty("SddWorkflows").EnumerateArray(),
            item => item.GetProperty("Name").GetString() == "OpenSpec");
        Assert.Null(jsonWorkflow.GetProperty("ActiveWorkItemPath").GetString());
        Assert.Null(jsonWorkflow.GetProperty("ActiveWorkItemEvidence").GetString());
        Assert.Contains("- Active work item: `openspec/changes/add-search` (UniqueOpenSpecChange)",
            MarkdownReport.Render(unique));
        Assert.DoesNotContain("Active work item:", MarkdownReport.Render(ambiguous));
    }

    [Fact]
    public void Scan_RejectsUnsafeAndStaleSpecKitFeaturePointers()
    {
        Write(".specify/feature.json", """{ "feature_directory": "../outside" }""");

        var unsafeInventory = RepositoryScanner.Scan(_root);
        var unsafeSpecKit = Assert.Single(unsafeInventory.SddWorkflows, item => item.Name == "Spec Kit");
        Assert.Null(unsafeSpecKit.ActiveWorkItemPath);
        Assert.Contains(unsafeInventory.SddWorkflowIssues, issue => issue.Code == "UnsafeActiveFeaturePath");

        Write(".specify/feature.json", """{ "feature_directory": "specs/missing-feature" }""");
        var staleInventory = RepositoryScanner.Scan(_root);
        var staleSpecKit = Assert.Single(staleInventory.SddWorkflows, item => item.Name == "Spec Kit");
        Assert.Null(staleSpecKit.ActiveWorkItemPath);
        Assert.Contains(staleInventory.SddWorkflowIssues, issue => issue.Code == "ActiveFeatureNotFound");
    }

    [Fact]
    public void Scan_ReportsMalformedSpecKitFeaturePointerAndKeepsStableDiagnostics()
    {
        Write(".specify/feature.json", "{ not valid JSON }");

        var first = RepositoryScanner.Scan(_root);
        var second = RepositoryScanner.Scan(_root);
        var workflow = Assert.Single(first.SddWorkflows, item => item.Name == "Spec Kit");
        var issue = Assert.Single(first.SddWorkflowIssues);

        Assert.Null(workflow.ActiveWorkItemPath);
        Assert.Equal(".specify/feature.json", issue.Path);
        Assert.Equal("InvalidActiveFeatureJson", issue.Code);
        Assert.Equal(first.SddWorkflowIssues, second.SddWorkflowIssues);
        Assert.Equal(MarkdownReport.Render(first), MarkdownReport.Render(second));
    }

    [Fact]
    public void Scan_RejectsCaseVariantDuplicateSpecKitFeaturePointers()
    {
        Write(".specify/feature.json", """{ "feature_directory": "specs/001-feature" }""");
        Write("specs/001-feature/spec.md", "feature");

        var analysis = SddWorkflowAnalyzer.Analyze(_root,
            new[] { ".specify/feature.json", ".SPECIFY/FEATURE.JSON", "specs/001-feature/spec.md" }, 10);

        Assert.Null(Assert.Single(analysis.Workflows, item => item.Name == "Spec Kit").ActiveWorkItemPath);
        Assert.Contains(analysis.Issues, issue => issue.Code == "DuplicateActiveFeatureFile");
    }

    [Fact]
    public void Scan_RejectsOversizedSpecKitFeaturePointer()
    {
        Write(".specify/feature.json", new string(' ', 65_537));

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Null(Assert.Single(inventory.SddWorkflows, item => item.Name == "Spec Kit").ActiveWorkItemPath);
        Assert.Contains(inventory.SddWorkflowIssues, issue => issue.Code == "ActiveFeatureTooLarge");
    }

    [Fact]
    public void Scan_ReportsCrossEcosystemMarkersAndHonestLanguageCoverage()
    {
        Write("dotnet/App.csproj", "<Project />");
        Write("dotnet/Directory.Build.props", "<Project />");
        Write("native/Legacy.vcxproj", "<Project />");
        Write("native/CMakeLists.txt", "project(native)");
        Write("native/Makefile", "all:");
        Write("python/pyproject.toml", "[project]");
        Write("web/package.json", "{}");
        Write("web/pnpm-lock.yaml", "lockfileVersion: 9");
        Write("web/main.js", "export const active = true;");
        Write("web/main.ts", "export const ready = true;");
        Write("service/go.mod", "module example");
        Write("service/main.go", "package main");
        Write("rust/Cargo.toml", "[package]");
        Write("java/pom.xml", "<project />");
        Write("java/build.gradle.kts", "plugins {}");
        Write("kotlin/src/Module.kt", "fun sample() = Unit");
        Write("ruby/Gemfile", "source 'https://example.invalid'");
        Write("php/composer.json", "{}");
        Write("apple/Package.swift", "// swift-tools-version: 5.9");
        Write("flutter/pubspec.yaml", "name: sample");
        Write("flutter/main.dart", "void main() {}");
        Write("elixir/mix.exs", "defmodule Sample.MixProject do end");
        Write("scala/build.sbt", "name := \"sample\"");
        Write("containers/Dockerfile", "FROM scratch");
        Write("infra/main.tf", "terraform {}");
        Write("src/Program.cs", "class Program {}");
        Write("scripts/task.py", "print('ok')");
        Write("lib/module.rs", "fn main() {}");
        Write("README", "extensionless");
        Write("assets/blob.oddext", "unknown extension");
        Write(".gradle/caches/generated.kt", "generated");
        Write(".next/server/generated.js", "generated");
        Write(".terraform/providers/generated.go", "generated");
        Write(".dart_tool/package_config.json", "generated");
        Write("Pods/Vendor/Generated.swift", "third-party");

        var inventory = RepositoryScanner.Scan(_root);
        var systems = inventory.ProjectSystems.ToDictionary(system => system.Name, system => system.ManifestPaths);
        var languages = inventory.LanguageCoverage.ToDictionary(language => language.Language);

        Assert.Equal(30, inventory.FileCount);
        Assert.DoesNotContain(inventory.ProjectMarkers, path => path.Contains("generated", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { ".NET", "Apple", "C/C++", "Containers", "Dart", "Elixir", "Go", "Infrastructure",
                "JVM", "JavaScript/TypeScript", "Make", "PHP", "Python", "Ruby", "Rust", "Scala" },
            systems.Keys.Order(StringComparer.Ordinal));
        Assert.Contains("dotnet/App.csproj", systems[".NET"]);
        Assert.Contains("native/Legacy.vcxproj", systems["C/C++"]);
        Assert.Contains("native/CMakeLists.txt", systems["C/C++"]);
        Assert.Contains("native/Makefile", systems["Make"]);
        Assert.Contains("web/pnpm-lock.yaml", systems["JavaScript/TypeScript"]);
        Assert.Contains("Cargo.toml", systems["Rust"].Select(Path.GetFileName));
        Assert.Contains("main.tf", systems["Infrastructure"].Select(Path.GetFileName));
        Assert.Equal(1, languages["C#"].SourceFileCount);
        Assert.Equal("Roslyn syntax and candidate patterns", languages["C#"].AnalysisLevel);
        Assert.Equal("Lexical candidate patterns", languages["Python"].AnalysisLevel);
        Assert.Equal(1, languages["TypeScript"].SourceFileCount);
        Assert.Equal("Bounded syntax and lexical candidate patterns", languages["TypeScript"].AnalysisLevel);
        Assert.Equal(2, languages["Kotlin"].SourceFileCount);
        Assert.Equal("Lexical candidate patterns", languages["Kotlin"].AnalysisLevel);
        Assert.Equal(1, languages["JavaScript"].SourceFileCount);
        Assert.Equal("Bounded syntax and lexical candidate patterns", languages["JavaScript"].AnalysisLevel);
        Assert.Equal(1, languages["Go"].SourceFileCount);
        Assert.Equal("Lexical candidate patterns", languages["Go"].AnalysisLevel);
        Assert.Equal(1, languages["Swift"].SourceFileCount);
        Assert.Equal(1, languages["Dart"].SourceFileCount);
        Assert.Equal(4, inventory.Extensions.Single(extension => extension.Extension == "[no extension]").Count);
        Assert.Equal(inventory.FileCount, inventory.Extensions.Sum(extension => extension.Count));
        Assert.Contains(inventory.Extensions, extension => extension.Extension == ".oddext");

        var markdown = MarkdownReport.Render(inventory);
        Assert.Contains("## Project ecosystems", markdown);
        Assert.Contains("## Language coverage", markdown);
        Assert.Contains("`Python` — 1 source file — Lexical candidate patterns", markdown);
        Assert.Contains("`native/Legacy.vcxproj`", markdown);
        Assert.All(inventory.ProjectSystems.SelectMany(system => system.ManifestPaths), path =>
            Assert.Contains($"`{path}`", markdown));
        Assert.All(inventory.LanguageCoverage, language =>
            Assert.Contains($"`{language.Language}` — {language.SourceFileCount}", markdown));

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Equal(1, json.RootElement.GetProperty("SchemaVersion").GetInt32());
        var jsonSystems = json.RootElement.GetProperty("ProjectSystems").EnumerateArray().ToArray();
        var jsonLanguages = json.RootElement.GetProperty("LanguageCoverage").EnumerateArray().ToArray();
        Assert.Equal(systems.Count, jsonSystems.Length);
        Assert.Equal(languages.Count, jsonLanguages.Length);
        foreach (var system in inventory.ProjectSystems)
        {
            var serialized = Assert.Single(jsonSystems, item => item.GetProperty("Name").GetString() == system.Name);
            Assert.Equal(system.ManifestPaths, serialized.GetProperty("ManifestPaths").EnumerateArray()
                .Select(path => path.GetString()!).ToArray());
        }
        foreach (var language in inventory.LanguageCoverage)
        {
            var serialized = Assert.Single(jsonLanguages, item => item.GetProperty("Language").GetString() == language.Language);
            Assert.Equal(language.SourceFileCount, serialized.GetProperty("SourceFileCount").GetInt32());
            Assert.Equal(language.AnalysisLevel, serialized.GetProperty("AnalysisLevel").GetString());
        }
        Assert.Contains(jsonLanguages, language => language.GetProperty("Language").GetString() == "Python"
            && language.GetProperty("AnalysisLevel").GetString() == "Lexical candidate patterns");
    }

    [Fact]
    public void Scan_ReportsDeclaredCustomSddWorkflowInJsonAndMarkdown()
    {
        Write(".brownpants/sdd-workflows.json", """
            {
              "schemaVersion": 1,
              "workflows": [
                {
                  "name": "CustomSDD",
                  "artifactRoots": ["planning", "planning/specs"],
                  "specificationRoots": ["planning/specs"],
                  "changeRoots": ["planning/changes"],
                  "archivedChangeDirectoryNames": ["archive"]
                }
              ]
            }
            """);
        Write("planning/specs/search/spec.md", "spec");
        Write("planning/specs/search/notes.md", "notes");
        Write("planning/changes/add-search/proposal.md", "proposal");
        Write("planning/changes/ARCHIVE/old/proposal.md", "archived");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 2);
        var workflow = Assert.Single(inventory.SddWorkflows, item => item.Name == "CustomSDD");

        Assert.True(workflow.IsInitialized);
        Assert.Equal(1, workflow.SpecificationDirectoryCount);
        Assert.Equal(1, workflow.ActiveChangeDirectoryCount);
        Assert.Equal(4, workflow.ArtifactFileCount);
        Assert.Equal(2, workflow.ArtifactPaths.Count);
        Assert.Null(workflow.ActiveWorkItemPath);
        Assert.Null(workflow.ActiveWorkItemEvidence);
        Assert.Empty(inventory.SddWorkflowIssues);
        var markdown = MarkdownReport.Render(inventory);
        Assert.Contains("### CustomSDD", markdown);
        Assert.Contains("2 additional artifacts omitted by the sample limit", markdown);

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "2" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Equal(1, json.RootElement.GetProperty("SchemaVersion").GetInt32());
        var jsonWorkflow = Assert.Single(json.RootElement.GetProperty("SddWorkflows").EnumerateArray(),
            item => item.GetProperty("Name").GetString() == "CustomSDD");
        Assert.Equal(4, jsonWorkflow.GetProperty("ArtifactFileCount").GetInt32());
        Assert.Equal(2, jsonWorkflow.GetProperty("ArtifactPaths").GetArrayLength());
        Assert.Empty(json.RootElement.GetProperty("SddWorkflowIssues").EnumerateArray());
    }

    [Fact]
    public void Scan_ReportsDeclaredCustomActiveWorkItemInMarkdownAndJson()
    {
        Write(".brownpants/sdd-workflows.json", System.Text.Json.JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            workflows = new[]
            {
                new
                {
                    name = "CustomSDD",
                    artifactRoots = new[] { "planning" },
                    specificationRoots = new[] { "planning/specs" },
                    activeWorkItemPath = @"planning\changes\add search"
                }
            }
        }));
        Write("planning/changes/add search/proposal.md", "active proposal");
        Write("planning/changes/add search/tasks.md", "active tasks");
        Write("planning/specs/catalog/spec.md", "current behavior");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 1);
        var workflow = Assert.Single(inventory.SddWorkflows, item => item.Name == "CustomSDD");
        Assert.Equal("planning/changes/add search", workflow.ActiveWorkItemPath);
        Assert.Equal("CustomWorkflowDeclaration", workflow.ActiveWorkItemEvidence);
        Assert.Empty(inventory.SddWorkflowIssues);
        Assert.Contains("- Active work item: `planning/changes/add search` (CustomWorkflowDeclaration)",
            MarkdownReport.Render(inventory));

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "1" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Equal(1, json.RootElement.GetProperty("SchemaVersion").GetInt32());
        var jsonWorkflow = Assert.Single(json.RootElement.GetProperty("SddWorkflows").EnumerateArray(),
            item => item.GetProperty("Name").GetString() == "CustomSDD");
        Assert.Equal("planning/changes/add search", jsonWorkflow.GetProperty("ActiveWorkItemPath").GetString());
        Assert.Equal("CustomWorkflowDeclaration", jsonWorkflow.GetProperty("ActiveWorkItemEvidence").GetString());
    }

    [Fact]
    public void Scan_RejectsInvalidCustomActiveWorkItemsAndPreservesWorkflowInventory()
    {
        Write(".brownpants/sdd-workflows.json", """
            {
              "schemaVersion": 1,
              "workflows": [
                { "name": "UnsafeSDD", "artifactRoots": ["unsafe"], "activeWorkItemPath": "../outside" },
                { "name": "AbsoluteSDD", "artifactRoots": ["absolute"], "activeWorkItemPath": "/outside" },
                { "name": "WildcardSDD", "artifactRoots": ["wildcard"], "activeWorkItemPath": "wildcard/*" },
                { "name": "OutsideSDD", "artifactRoots": ["inside"], "activeWorkItemPath": "outside/active" },
                { "name": "StaleSDD", "artifactRoots": ["stale"], "activeWorkItemPath": "stale/missing" },
                { "name": "TypedSDD", "artifactRoots": ["typed"], "activeWorkItemPath": 42 }
              ]
            }
            """);
        Write("unsafe/readme.md", "unsafe workflow artifacts");
        Write("absolute/readme.md", "absolute workflow artifacts");
        Write("wildcard/readme.md", "wildcard workflow artifacts");
        Write("inside/readme.md", "inside workflow artifacts");
        Write("outside/active/spec.md", "outside active path");
        Write("stale/readme.md", "stale workflow artifacts");
        Write("typed/readme.md", "typed workflow artifacts");

        var first = RepositoryScanner.Scan(_root);
        var second = RepositoryScanner.Scan(_root);
        foreach (var name in new[] { "UnsafeSDD", "AbsoluteSDD", "WildcardSDD", "OutsideSDD", "StaleSDD", "TypedSDD" })
        {
            var workflow = Assert.Single(first.SddWorkflows, item => item.Name == name);
            Assert.True(workflow.IsInitialized);
            Assert.True(workflow.ArtifactFileCount > 0);
            Assert.Null(workflow.ActiveWorkItemPath);
            Assert.Null(workflow.ActiveWorkItemEvidence);
        }
        Assert.Equal(4, first.SddWorkflowIssues.Count(issue => issue.Code == "InvalidActiveWorkItemPath"));
        Assert.Contains(first.SddWorkflowIssues, issue => issue.Code == "ActiveWorkItemOutsideArtifactRoots");
        Assert.Contains(first.SddWorkflowIssues, issue => issue.Code == "ActiveWorkItemNotFound");
        Assert.All(first.SddWorkflowIssues, issue => Assert.Equal(".brownpants/sdd-workflows.json", issue.Path));
        Assert.DoesNotContain("../outside", string.Join(' ', first.SddWorkflowIssues.Select(issue => issue.Message)),
            StringComparison.Ordinal);
        Assert.Equal(first.SddWorkflowIssues, second.SddWorkflowIssues);
        var markdown = MarkdownReport.Render(first);
        Assert.Equal(markdown, MarkdownReport.Render(second));
        Assert.Contains("InvalidActiveWorkItemPath", markdown);

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Contains(json.RootElement.GetProperty("SddWorkflowIssues").EnumerateArray(),
            issue => issue.GetProperty("Code").GetString() == "InvalidActiveWorkItemPath");
        var jsonWorkflow = Assert.Single(json.RootElement.GetProperty("SddWorkflows").EnumerateArray(),
            workflow => workflow.GetProperty("Name").GetString() == "OutsideSDD");
        Assert.Null(jsonWorkflow.GetProperty("ActiveWorkItemPath").GetString());
        Assert.Null(jsonWorkflow.GetProperty("ActiveWorkItemEvidence").GetString());
    }

    [Fact]
    public void Scan_ReportsCustomWorkflowWithoutChangeRootsAsNotInitialized()
    {
        Write(".brownpants/sdd-workflows.json", """
            { "schemaVersion": 1, "workflows": [
              { "name": "EmptySDD", "artifactRoots": ["proposals"], "specificationRoots": ["proposals/specs"] }
            ] }
            """);

        var inventory = RepositoryScanner.Scan(_root);
        var workflow = Assert.Single(inventory.SddWorkflows, item => item.Name == "EmptySDD");

        Assert.False(workflow.IsInitialized);
        Assert.Equal(0, workflow.SpecificationDirectoryCount);
        Assert.Null(workflow.ActiveChangeDirectoryCount);
        Assert.Equal(0, workflow.ArtifactFileCount);
        Assert.Null(workflow.ActiveWorkItemPath);
        Assert.Null(workflow.ActiveWorkItemEvidence);
    }

    [Fact]
    public void Scan_DoesNotCountItsOwnWorkflowDeclarationAsAnArtifact()
    {
        Write(".brownpants/sdd-workflows.json", """
            { "schemaVersion": 1, "workflows": [
              { "name": "CustomSDD", "artifactRoots": [".brownpants"] }
            ] }
            """);

        var workflow = Assert.Single(RepositoryScanner.Scan(_root).SddWorkflows,
            item => item.Name == "CustomSDD");

        Assert.False(workflow.IsInitialized);
        Assert.Equal(0, workflow.ArtifactFileCount);
        Assert.Empty(workflow.ArtifactPaths);
    }

    [Fact]
    public void Scan_ReportsMalformedCustomWorkflowConfigurationAndKeepsBuiltInWorkflows()
    {
        Write(".specify/integration.json", "{}");
        Write("openspec/config.yaml", "schema: spec-driven");
        Write(".brownpants/sdd-workflows.json", "{ not valid json }");

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Contains(inventory.SddWorkflows, workflow => workflow.Name == "Spec Kit");
        Assert.Contains(inventory.SddWorkflows, workflow => workflow.Name == "OpenSpec");
        var issue = Assert.Single(inventory.SddWorkflowIssues);
        Assert.Equal("InvalidJson", issue.Code);
        Assert.Equal(".brownpants/sdd-workflows.json", issue.Path);
        var markdown = MarkdownReport.Render(inventory);
        Assert.Contains("### Configuration diagnostics", markdown);
        Assert.Contains("InvalidJson", markdown);
    }

    [Fact]
    public void Scan_RejectsUnsupportedSchemaUnknownFieldsAndWorkflowNameCollisions()
    {
        Write("openspec/config.yaml", "schema: spec-driven");
        Write(".brownpants/sdd-workflows.json", """
            { "schemaVersion": 2, "workflows": [
              { "name": "CustomSDD", "artifactRoots": ["planning"] }
            ] }
            """);
        var unsupported = RepositoryScanner.Scan(_root);
        Assert.Contains(unsupported.SddWorkflowIssues, issue => issue.Code == "UnsupportedSchemaVersion");
        Assert.Contains(unsupported.SddWorkflows, workflow => workflow.Name == "OpenSpec");

        Write(".brownpants/sdd-workflows.json", """
            { "schemaVersion": 1, "workflows": [
              { "name": "CustomSDD", "artifactRoots": ["planning"] },
              { "name": "openspec", "artifactRoots": ["other"] },
              { "name": "customsdd", "artifactRoots": ["duplicate"] },
              { "name": "Unknown", "artifactRoots": ["invalid"], "typo": true }
            ] }
            """);
        var invalid = RepositoryScanner.Scan(_root);

        Assert.Contains(invalid.SddWorkflowIssues, issue => issue.Code == "UnknownProperty");
        Assert.Equal(2, invalid.SddWorkflowIssues.Count(issue => issue.Code == "DuplicateWorkflowName"));
        Assert.Single(invalid.SddWorkflows, workflow => workflow.Name == "CustomSDD");
        Assert.DoesNotContain(invalid.SddWorkflows, workflow => workflow.Name.Equals("OpenSpec", StringComparison.OrdinalIgnoreCase)
            && workflow.Name != "OpenSpec");
        Assert.Contains(invalid.SddWorkflows, workflow => workflow.Name == "OpenSpec");
    }

    [Fact]
    public void Scan_RejectsTraversalRootsWithoutReadingOutsideRepository()
    {
        var externalRoot = Path.Combine(Path.GetDirectoryName(_root)!, $"external-{Guid.NewGuid():N}");
        Directory.CreateDirectory(externalRoot);
        File.WriteAllText(Path.Combine(externalRoot, "secret.md"), "outside repository");
        try
        {
            var externalName = Path.GetFileName(externalRoot);
            Write(".brownpants/sdd-workflows.json", $$"""
                { "schemaVersion": 1, "workflows": [
                  { "name": "Unsafe", "artifactRoots": ["../{{externalName}}"] }
                ] }
                """);

            var inventory = RepositoryScanner.Scan(_root);

            Assert.Contains(inventory.SddWorkflowIssues, issue => issue.Code == "InvalidRoot");
            Assert.DoesNotContain(inventory.SddWorkflows.SelectMany(workflow => workflow.ArtifactPaths),
                path => path.Contains("secret.md", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(externalRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("/outside")]
    [InlineData("C:/outside")]
    [InlineData("planning/../outside")]
    [InlineData("planning/*")]
    [InlineData("planning\\outside")]
    public void Scan_RejectsUnsafeCustomWorkflowRoots(string unsafeRoot)
    {
        var config = System.Text.Json.JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            workflows = new[] { new { name = "Unsafe", artifactRoots = new[] { unsafeRoot } } }
        });
        Write(".brownpants/sdd-workflows.json", config);

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Contains(inventory.SddWorkflowIssues, issue => issue.Code == "InvalidRoot");
        Assert.DoesNotContain(inventory.SddWorkflows, workflow => workflow.Name == "Unsafe");
    }

    [Fact]
    public void Scan_RejectsCustomWorkflowConfigurationLargerThanOneMiB()
    {
        Write(".brownpants/sdd-workflows.json", new string(' ', 1_048_577));

        var inventory = RepositoryScanner.Scan(_root);

        var issue = Assert.Single(inventory.SddWorkflowIssues);
        Assert.Equal("ConfigurationTooLarge", issue.Code);
    }

    [Fact]
    public void Scan_RejectsMoreThanOneHundredWorkflowDefinitions()
    {
        var workflows = Enumerable.Range(0, 101)
            .Select(index => new { name = $"Engine{index}", artifactRoots = new[] { $"engine{index}" } })
            .ToArray();
        var config = System.Text.Json.JsonSerializer.Serialize(new { schemaVersion = 1, workflows });
        Write("openspec/config.yaml", "schema: spec-driven");
        Write(".brownpants/sdd-workflows.json", config);

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Contains(inventory.SddWorkflowIssues, issue => issue.Code == "TooManyWorkflowDefinitions");
        Assert.Contains(inventory.SddWorkflows, workflow => workflow.Name == "OpenSpec");
        Assert.DoesNotContain(inventory.SddWorkflows, workflow => workflow.Name.StartsWith("Engine", StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_KeepsCustomWorkflowDiagnosticsStableAcrossRuns()
    {
        Write("openspec/config.yaml", "schema: spec-driven");
        Write(".brownpants/sdd-workflows.json", """
            { "schemaVersion": 1, "workflows": [
              { "name": "ValidCustom", "artifactRoots": ["plans"] },
              { "name": "InvalidCustom", "artifactRoots": ["../outside"] }
            ] }
            """);
        Write("plans/change/spec.md", "custom spec");

        var first = RepositoryScanner.Scan(_root);
        var second = RepositoryScanner.Scan(_root);

        Assert.Equal(MarkdownReport.Render(first), MarkdownReport.Render(second));
        Assert.Equal(first.SddWorkflowIssues, second.SddWorkflowIssues);
        Assert.Contains(first.SddWorkflows, workflow => workflow.Name == "OpenSpec");
        Assert.Contains(first.SddWorkflows, workflow => workflow.Name == "ValidCustom");
        Assert.DoesNotContain(first.SddWorkflows, workflow => workflow.Name == "InvalidCustom");
    }

    [Fact]
    public void Scan_IsStableAcrossRepeatedRuns()
    {
        Write("z/Readme.md", "hello");
        Write("a/a.csproj", "lower project");
        Write("a/app.py", "print('hello')");
        Write("openspec/config.yaml", "schema: spec-driven");
        Write("openspec/specs/catalog/spec.md", "current behavior");
        Write("openspec/changes/update-catalog/proposal.md", "change behavior");

        var first = RepositoryScanner.Scan(_root);
        var second = RepositoryScanner.Scan(_root);

        Assert.Equal(MarkdownReport.Render(first), MarkdownReport.Render(second));

        using var firstJson = new StringWriter();
        using var secondJson = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, firstJson, error));
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, secondJson, error));
        Assert.Equal(firstJson.ToString(), secondJson.ToString());
    }

    [Fact]
    public void Run_EmitsAllConfiguredArchitectureSamplesInMarkdownAndJson()
    {
        var factoryTypes = Enumerable.Range(0, 60)
            .Select(index => $"public class WidgetFactory{index} {{ }}");
        Write("src/Factories.cs", string.Join(Environment.NewLine, factoryTypes));
        var projectReferences = Enumerable.Range(0, 25)
            .Select(index => $"<ProjectReference Include=\"Project{index}.csproj\" />");
        Write("src/Root.csproj", $"<Project><ItemGroup>{string.Join(string.Empty, projectReferences)}</ItemGroup></Project>");

        using var markdown = new StringWriter();
        using var json = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--limit", "100" }, markdown, error));
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "100" }, json, error));

        using var document = System.Text.Json.JsonDocument.Parse(json.ToString());
        var signals = document.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals").EnumerateArray().ToArray();
        var jsonReferences = document.RootElement.GetProperty("DotNetProjects")[0]
            .GetProperty("ProjectReferences").EnumerateArray().Count();
        var markdownSignals = markdown.ToString().Split(Environment.NewLine)
            .Count(line => line.StartsWith("- Factory type [Candidate] (C#):", StringComparison.Ordinal));

        Assert.Equal(60, signals.Length);
        Assert.Equal(signals.Length, markdownSignals);
        Assert.Equal(25, jsonReferences);
        Assert.Contains("ProjectReference: `Project24.csproj`", markdown.ToString());
    }

    [Fact]
    public void Run_EmitsJsonAndRejectsUnknownFormat()
    {
        Write("app.py", "print('hello')");
        Write("src/App.cs", "class First {} class Second {}");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var jsonExit = Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "1" }, output, error);
        Assert.Equal(0, jsonExit);
        using (var document = System.Text.Json.JsonDocument.Parse(output.ToString()))
        {
            Assert.Equal(1, document.RootElement.GetProperty("SchemaVersion").GetInt32());
            Assert.Equal("SchemaVersion", document.RootElement.EnumerateObject().First().Name);
            Assert.Equal(2, document.RootElement.GetProperty("FileCount").GetInt32());
            var csharp = document.RootElement.GetProperty("CSharp");
            Assert.Equal(2, csharp.GetProperty("TypeCount").GetInt32());
            Assert.Equal(1, csharp.GetProperty("Types").GetArrayLength());
        }

        output.GetStringBuilder().Clear();
        var invalidExit = Program.Run(new[] { "inspect", _root, "--format", "xml" }, output, error);
        Assert.Equal(2, invalidExit);
        Assert.Contains("Expected markdown or json", error.ToString());
    }

    [Fact]
    public void Run_EmitsSchemaVersionForEmptyAndSemanticJsonAndKeepsMarkdownUnversioned()
    {
        Directory.CreateDirectory(_root);
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using (var emptyJson = System.Text.Json.JsonDocument.Parse(output.ToString()))
        {
            Assert.Equal(1, emptyJson.RootElement.GetProperty("SchemaVersion").GetInt32());
        }

        output.GetStringBuilder().Clear();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--semantic" }, output, error));
        using (var semanticJson = System.Text.Json.JsonDocument.Parse(output.ToString()))
        {
            Assert.Equal(1, semanticJson.RootElement.GetProperty("SchemaVersion").GetInt32());
        }

        output.GetStringBuilder().Clear();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root }, output, error));
        Assert.DoesNotContain("SchemaVersion", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Scan_ParsesLegacyFrameworkProjectsAndRoslynSyntaxWithoutBuildingThem()
    {
        Write("Legacy.App.csproj", """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup>
                <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
                <LangVersion>5</LangVersion>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Legacy.Data\Legacy.Data.csproj" />
              </ItemGroup>
            </Project>
            """);
        Write("src/OrderService.cs", """
            using System;
            using DataAccess = Legacy.Data.Repository;
            namespace Legacy.App
            {
                public class OrderService
                {
                    public string Load(int id)
                    {
                        return DataAccess.Fetch(id).ToString();
                    }

                    public int Count { get; set; }

                    public string UnsupportedForCSharp5() => $"{Count}";
                }
            }
            #if NET45
            public class FrameworkSpecificService { }
            #endif
            """);
        Write("src/Broken.cs", "namespace Legacy.App { public class BrokenType {");

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Single(inventory.DotNetProjects);
        Assert.True(inventory.DotNetProjects[0].IsLegacyFramework);
        Assert.Contains("v4.8", inventory.DotNetProjects[0].TargetFrameworks);
        Assert.Contains("5", inventory.DotNetProjects[0].LanguageVersions);
        Assert.Contains("..\\Legacy.Data\\Legacy.Data.csproj", inventory.DotNetProjects[0].ProjectReferences);
        Assert.Equal(2, inventory.CSharp.SourceFileCount);
        Assert.Contains(inventory.CSharp.Types, item => item.Name == "OrderService" && item.Namespace == "Legacy.App");
        Assert.DoesNotContain(inventory.CSharp.Types, item => item.Name == "FrameworkSpecificService");
        Assert.Contains(inventory.CSharp.Members, item => item.Signature.StartsWith("string Load", StringComparison.Ordinal));
        Assert.Contains(inventory.CSharp.Usings, item => item.Alias == "DataAccess");
        Assert.Contains(inventory.CSharp.Invocations, item => item.Expression == "DataAccess.Fetch");
        Assert.Contains(inventory.CSharp.Issues, issue => issue.Code == "CS1513");
        Assert.True(inventory.CSharp.InactiveCodeRegionCount > 0);
        Assert.True(inventory.CSharp.ParseErrorCount > 0);
    }

    [Fact]
    public void Scan_ParsesVisualBasicLegacySourceWithRoslyn()
    {
        Write("LegacyVB/App.vbproj", "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><PropertyGroup><TargetFrameworkVersion>v4.8</TargetFrameworkVersion></PropertyGroup></Project>");
        Write("LegacyVB/Services/OrderService.vb", """
            Namespace Legacy.Services
                Public Class OrderService
                    Public Function Load(id As Integer) As String
                        Return Repository.Fetch(id).ToString()
                    End Function

                    Public Property Count As Integer
                End Class
            End Namespace
            """);

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Equal(1, inventory.VisualBasic.SourceFileCount);
        Assert.Single(inventory.DotNetProjects);
        Assert.True(inventory.DotNetProjects[0].IsLegacyFramework);
        Assert.Contains("LegacyVB/App.vbproj", inventory.ProjectMarkers);
        Assert.Contains(inventory.VisualBasic.Types, type => type.Name == "OrderService"
            && type.Namespace == "Legacy.Services" && type.Kind == "Class");
        Assert.Contains(inventory.VisualBasic.Members, member => member.ContainingType == "Legacy.Services.OrderService"
            && member.Signature.Contains("Load", StringComparison.Ordinal));
        Assert.Contains(inventory.VisualBasic.Invocations, invocation => invocation.Expression == "Repository.Fetch");
        Assert.Equal(0, inventory.VisualBasic.ParseErrorCount);
        Assert.Contains("Roslyn Visual Basic structure", MarkdownReport.Render(inventory));
    }

    [Fact]
    public void Scan_ReportsFactoryDependencyInjectionAndMfcInteropSignals()
    {
        Write("client/Composition.cs", """
            public interface IWidgetFactory { }
            public class WidgetFactory : IWidgetFactory { }
            services.AddTransient<IWidget, Widget>();
            [DllImport("legacy.dll")]
            public static extern int LoadWidget();
            """);
        Write("client/Noise.cs", """
            // public class CommentOnlyFactory { }
            var example = "services.AddTransient<IClock, Clock>(); [DllImport(\"old.dll\")]";
            """);
        Write("native/MainFrame.cpp", """
            IMPLEMENT_DYNCREATE(CMainFrame, CFrameWnd)
            BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
            auto widget = CObject::CreateObject(RUNTIME_CLASS(CWidget));
            CoCreateInstance(clsid, nullptr, CLSCTX_INPROC_SERVER, iid, result);
            public ref class ManagedWidget {};
            auto bridge = gcnew ManagedWidget();
            """);
        Write("native/Noise.cpp", """
            // IMPLEMENT_DYNCREATE(CNoise, CObject)
            const char* sample = "BEGIN_MESSAGE_MAP(CNoise, CObject)";
            /* CObject::CreateObject(RUNTIME_CLASS(CNoise)) */
            const char* raw = R"fixture(
            IMPLEMENT_DYNCREATE(CRawNoise, CObject)
            )fixture";
            """);
        Write("native/Native.vcxproj", """
            <Project>
              <PropertyGroup>
                <UseOfMfc>Dynamic</UseOfMfc>
                <CLRSupport>true</CLRSupport>
                <PlatformToolset>v143</PlatformToolset>
                <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>
              </PropertyGroup>
            </Project>
            """);

        var inventory = RepositoryScanner.Scan(_root);
        var categories = inventory.ArchitecturePatterns.Signals.Select(signal => signal.Category).ToArray();

        Assert.Contains("Factory type", categories);
        Assert.Contains("DI registration", categories);
        Assert.Contains("Native interop", categories);
        Assert.Contains("MFC runtime type", categories);
        Assert.Contains("MFC message map", categories);
        Assert.Contains("MFC object factory", categories);
        Assert.Contains("COM activation", categories);
        Assert.Contains("C++/CLI bridge", categories);
        Assert.DoesNotContain(inventory.ArchitecturePatterns.Signals, signal => signal.Path.EndsWith("Noise.cs", StringComparison.Ordinal)
            || signal.Path.EndsWith("Noise.cpp", StringComparison.Ordinal));
        Assert.Contains("native/Native.vcxproj", inventory.ProjectMarkers);
        Assert.Single(inventory.NativeProjects);
        Assert.True(inventory.NativeProjects[0].HasMfcSignal);
        Assert.True(inventory.NativeProjects[0].HasClrSupport);
        Assert.Contains("v143", inventory.NativeProjects[0].PlatformToolsets);
        Assert.Contains("Architecture pattern signals", MarkdownReport.Render(inventory));
    }

    [Fact]
    public void Scan_ReportsMfcClassInheritanceCandidatesAndFiltersNoise()
    {
        Write("native/MainFrame.h", """
            class CMainFrame : public CFrameWndEx {};
            class CAccountDocument
                : public CDocument
            {};
            struct CToolbar : public CWnd {};
            class CSettingsDialog : CDialogEx {};
            class CWorkspace : public IWorkspace, public CWnd {};
            class UnrelatedWindow : public BaseWindow {};
            """);
        Write("native/Noise.h", """
            // class CommentFrame : public CFrameWnd {};
            const char* sample = "class StringDialog : public CDialogEx {};";
            const char* raw = R"fixture(
            class RawDocument : public CDocument {};
            )fixture";
            class OrdinaryWindow : public BaseWindow {};
            """);

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var repeatedInventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var signals = inventory.ArchitecturePatterns.Signals
            .Where(signal => signal.Category == "MFC class inheritance")
            .ToArray();

        Assert.Equal(5, signals.Length);
        Assert.Equal(new[] { 1, 2, 5, 6, 7 }, signals.Select(signal => signal.Line).ToArray());
        Assert.All(signals, signal =>
        {
            Assert.Equal("Candidate", signal.EvidenceKind);
            Assert.Equal("C++", signal.Language);
            Assert.Equal("native/MainFrame.h", signal.Path);
        });
        Assert.Contains(signals, signal => signal.Evidence.Contains("CDocument", StringComparison.Ordinal));
        Assert.DoesNotContain(inventory.ArchitecturePatterns.Signals,
            signal => signal.Path == "native/Noise.h" && signal.Category == "MFC class inheritance");
        Assert.Contains("MFC class inheritance", MarkdownReport.Render(inventory));
        Assert.Equal(MarkdownReport.Render(inventory), MarkdownReport.Render(repeatedInventory));

        using var output = new StringWriter();
        using var repeatedOutput = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "50" }, output, error));
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "50" }, repeatedOutput, error));
        Assert.Equal(output.ToString(), repeatedOutput.ToString());
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Contains(json.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals").EnumerateArray(),
            signal => signal.GetProperty("Category").GetString() == "MFC class inheritance"
                && signal.GetProperty("Path").GetString() == "native/MainFrame.h"
                && signal.GetProperty("Line").GetInt32() == 2
                && signal.GetProperty("EvidenceKind").GetString() == "Candidate");
    }

    [Fact]
    public void Scan_ConnectsTypedCppCliHandlesToManagedMethodsWithoutResolvingAmbiguity()
    {
        Write("managed/Workflow.cs", """
            using System.Collections.Generic;
            namespace Demo.App;
            public class Order { }
            public class OrderImportWorkflow<T>
            {
                public void ImportBatch(IEnumerable<T> rows) { }
                public void ImportMapped(T row) { }
                public void ImportMapped(IEnumerable<T> rows) { }
            }
            """);
        Write("managed/VisualBasicWorkflow.vb", """
            Namespace Demo.App
                Public Class VisualBasicWorkflow
                    Public Sub Run()
                    End Sub
                End Class
            End Namespace
            """);
        Write("native/View.cpp", """
            BEGIN_MESSAGE_MAP(CView, CView)
                ON_COMMAND(ID_IMPORT, &CView::OnImport)
            END_MESSAGE_MAP()
            void CView::OnImport() { CManagedBridge::ImportSelectedOrders(); }
            """);
        Write("native/Bridge.cpp", """
            using namespace Demo::App;
            public ref class CManagedBridge
            {
            public:
                static void ImportSelectedOrders()
                {
                    OrderImportWorkflow<Order^>^ workflow = gcnew OrderImportWorkflow<Order^>();
                    workflow->ImportBatch(gcnew List<Order^>());
                    workflow->ImportMapped(gcnew Order());
                    workflow->NoSuchMethod();
                    Demo::App::OrderImportWorkflow<Order^>^ qualified = gcnew Demo::App::OrderImportWorkflow<Order^>();
                    qualified->MissingExplicit();
                    VisualBasicWorkflow^ vbWorkflow = gcnew VisualBasicWorkflow();
                    vbWorkflow->Run();
                    NativeOnly^ unrelated = gcnew NativeOnly();
                    unrelated->ImportBatch();
                    // workflow->ImportBatch(gcnew List<Order^>());
                    const char* decoy = "workflow->ImportBatch()";
                }
            };
            public ref class NativeOnly
            {
            public:
                void ImportBatch() {}
            };
            """);
        var filesBefore = Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => (Path: path, Content: File.ReadAllText(path))).ToArray();

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var repeatedInventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var flow = inventory.ArchitecturePatterns.NativeFlow
            ?? throw new InvalidOperationException("Native flow analysis was absent.");
        var managedCalls = flow.Relations.Where(item => item.Kind == "cppcli-managed-call").ToArray();

        Assert.Contains(managedCalls, item => item.Status == "candidate"
            && item.SourceSymbol == "CManagedBridge::ImportSelectedOrders"
            && item.TargetSymbol == "Demo.App.OrderImportWorkflow<T>.ImportBatch"
            && item.TargetPath == "managed/Workflow.cs" && item.TargetLine == 6
            && item.CandidateTargetCount == 1);
        Assert.Contains(managedCalls, item => item.Status == "ambiguous"
            && item.TargetSymbol == "Demo.App.OrderImportWorkflow<T>.ImportMapped"
            && item.TargetPath is null && item.CandidateTargetCount == 2
            && item.CandidateTargets?.Count == 2);
        Assert.Contains(managedCalls, item => item.Status == "unresolved"
            && item.TargetSymbol == "Demo.App.OrderImportWorkflow<T>.NoSuchMethod"
            && item.CandidateTargetCount == 0);
        Assert.Contains(managedCalls, item => item.Status == "unresolved"
            && item.TargetSymbol == "Demo.App.OrderImportWorkflow<T>.MissingExplicit"
            && item.CandidateTargetCount == 0);
        Assert.Contains(managedCalls, item => item.Status == "candidate"
            && item.TargetSymbol == "Demo.App.VisualBasicWorkflow.Run"
            && item.TargetPath == "managed/VisualBasicWorkflow.vb");
        Assert.DoesNotContain(managedCalls, item => item.TargetSymbol.Contains("NativeOnly.ImportBatch", StringComparison.Ordinal));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(inventory),
            System.Text.Json.JsonSerializer.Serialize(repeatedInventory));
        Assert.Equal(MarkdownReport.Render(inventory), MarkdownReport.Render(repeatedInventory));
        using var inventoryJson = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(inventory));
        var jsonManagedCall = Assert.Single(inventoryJson.RootElement.GetProperty("ArchitecturePatterns")
            .GetProperty("NativeFlow").GetProperty("Relations").EnumerateArray(), item =>
            item.GetProperty("Kind").GetString() == "cppcli-managed-call"
                && item.GetProperty("Status").GetString() == "ambiguous");
        Assert.Equal(2, jsonManagedCall.GetProperty("CandidateTargets").GetArrayLength());
        Assert.Contains("possible target locations", MarkdownReport.Render(inventory), StringComparison.Ordinal);
        Assert.Equal(managedCalls.Select(item => item.Id), RepositoryScanner.Scan(_root, sampleLimit: 50)
            .ArchitecturePatterns.NativeFlow!.Relations.Where(item => item.Kind == "cppcli-managed-call").Select(item => item.Id));
        Assert.Equal(filesBefore, Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal).Select(path => (Path: path, Content: File.ReadAllText(path))).ToArray());
    }

    [Fact]
    public void Scan_TracesMfcMessageHandlersAndQualifiedCallsAsBoundedNativeCandidates()
    {
        Write("native/View.cpp", """
            BEGIN_MESSAGE_MAP(CView, CFormView)
                ON_COMMAND(ID_RUN, &CView::OnRun)
                ON_COMMAND(ID_MISSING, &CView::OnMissing)
                ON_COMMAND(ID_DUPLICATE, &CView::OnDuplicate)
            END_MESSAGE_MAP()

            void CView::OnRun()
            {
                CBridge::Invoke(); CInlineBridge::Invoke();
                // CNoise::Fake();
                const char* text = "CNoise::AlsoFake()";
                const char* raw = R"tag(CNoise::RawFake())tag";
            }

            void CView::OnDuplicate() {}
            """);
        Write("native/Bridge.cpp", """
            public ref class CInlineBridge
            {
            public:
                static void Invoke() {}
            };
            void CBridge::Invoke() {}
            """);
        Write("native/Bridge2.cpp", "void CBridge::Invoke(int mode) {}\n");
        Write("native/ViewDuplicate.cpp", "void CView::OnDuplicate(int mode) {}\n");
        var filesBefore = Directory.GetFiles(_root, "*", SearchOption.AllDirectories).OrderBy(path => path).ToArray();

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var flow = inventory.ArchitecturePatterns.NativeFlow ?? throw new InvalidOperationException("Native flow analysis was absent.");
        var repeated = RepositoryScanner.Scan(_root, sampleLimit: 50).ArchitecturePatterns.NativeFlow
            ?? throw new InvalidOperationException("Repeated native flow analysis was absent.");
        var bounded = RepositoryScanner.Scan(_root, sampleLimit: 1).ArchitecturePatterns.NativeFlow
            ?? throw new InvalidOperationException("Bounded native flow analysis was absent.");
        var dispatch = flow.Relations.Where(item => item.Kind == "mfc-dispatch").ToArray();
        var calls = flow.Relations.Where(item => item.Kind == "qualified-native-call").ToArray();

        Assert.Equal(3, dispatch.Length);
        Assert.Contains(dispatch, item => item.CommandId == "ID_RUN" && item.Status == "candidate"
            && item.SourcePath == "native/View.cpp" && item.SourceLine == 2
            && item.TargetPath == "native/View.cpp" && item.TargetLine == 7
            && item.TargetSymbol == "CView::OnRun");
        Assert.Contains(dispatch, item => item.CommandId == "ID_MISSING" && item.Status == "unresolved"
            && item.TargetPath is null && item.CandidateTargetCount == 0);
        Assert.Contains(dispatch, item => item.CommandId == "ID_DUPLICATE" && item.Status == "ambiguous"
            && item.TargetPath is null && item.CandidateTargetCount == 2);
        Assert.Contains(calls, item => item.Status == "ambiguous" && item.SourceLine == 9
            && item.TargetSymbol == "CBridge::Invoke" && item.CandidateTargetCount == 2);
        Assert.Contains(calls, item => item.Status == "candidate" && item.SourceLine == 9
            && item.TargetSymbol == "CInlineBridge::Invoke" && item.TargetPath == "native/Bridge.cpp"
            && item.TargetLine == 4);
        Assert.DoesNotContain(flow.Relations, item => item.TargetSymbol.Contains("Fake", StringComparison.Ordinal));
        Assert.Equal(flow.Relations.Select(item => item.Id), repeated.Relations.Select(item => item.Id));
        Assert.Equal(flow.AmbiguousCount, repeated.AmbiguousCount);
        Assert.Equal(flow.UnresolvedCount, repeated.UnresolvedCount);
        Assert.Equal(flow.RelationCount - 1, bounded.OmittedCount);
        Assert.Single(bounded.Relations);
        Assert.Equal(filesBefore, Directory.GetFiles(_root, "*", SearchOption.AllDirectories).OrderBy(path => path).ToArray());

        var markdown = MarkdownReport.Render(inventory);
        Assert.Contains("Native flow candidates", markdown);
        Assert.Contains("ID_RUN", markdown);
        Assert.Contains("ambiguous", markdown, StringComparison.OrdinalIgnoreCase);

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(["evidence", _root, "--limit", "50"], output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var evidence = json.RootElement.GetProperty("findings").EnumerateArray().ToArray();
        Assert.Contains(evidence, item => item.GetProperty("category").GetString() == "native-flow-candidate"
            && item.GetProperty("targetPath").GetString() == "native/View.cpp"
            && item.GetProperty("targetLine").GetInt32() == 7);
        Assert.Contains(evidence, item => item.GetProperty("category").GetString() == "native-flow-candidate"
            && item.GetProperty("targetSymbol").GetString() == "CBridge::Invoke");
        Assert.Contains(evidence, item => item.GetProperty("category").GetString() == "native-flow-candidate"
            && item.GetProperty("targetSymbol").GetString() == "CInlineBridge::Invoke");
        Assert.DoesNotContain(evidence, item => item.GetProperty("status").GetString() == "resolved"
            && item.GetProperty("category").GetString() == "native-flow-candidate");

        using var inventoryOutput = new StringWriter();
        Assert.Equal(0, Program.Run(["inspect", _root, "--format", "json", "--limit", "50"], inventoryOutput, error));
        using var inventoryJson = System.Text.Json.JsonDocument.Parse(inventoryOutput.ToString());
        Assert.Contains(inventoryJson.RootElement.GetProperty("ArchitecturePatterns").GetProperty("NativeFlow")
            .GetProperty("Relations").EnumerateArray(), item => item.GetProperty("TargetSymbol").GetString() == "CInlineBridge::Invoke"
                && item.GetProperty("Status").GetString() == "candidate");
    }

    [Fact]
    public void Scan_OmitsNativeFlowSectionWhenRepositoryHasNoNativeSources()
    {
        Write("src/Program.cs", "internal static class Program { }");

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Equal(0, inventory.ArchitecturePatterns.NativeFlow?.ScannedFileCount);
        Assert.DoesNotContain("Native flow candidates", MarkdownReport.Render(inventory), StringComparison.Ordinal);
    }

    [Fact]
    public void Scan_ReportsPythonFactoryAndDependencyCandidatesAndFiltersNoise()
    {
        var pythonSource = """
            from fastapi import Depends
            class ReportFactory:
                def create_report(self): ...
                def build_index(self): ...
                report_factory = ReportFactory()
                current = report_factory.create_report()
                user_provider = providers.Factory(UserService)
                db_provider = providers.Singleton(Database)
            def endpoint(user = Depends(get_user)): ...
            @inject
            def handler(service = Provide[Container.service]): ...
            # class CommentFactory: def create_fake(): ...
            example = "providers.Factory(Fake); fake_factory()"
            description = r'''class StringFactory:
            def build_string(self): ...
            providers.Singleton(Fake)
            Depends(fake)
            '''
            ordinary_call = create_user()
            joined_text = "continued \
            class JoinedFactory: def make_fake(): ..."
            """;
        pythonSource = pythonSource.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", "\r\n", StringComparison.Ordinal);
        Write("app/services.py", pythonSource);

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var signals = inventory.ArchitecturePatterns.Signals
            .Where(signal => signal.Path == "app/services.py")
            .ToArray();

        Assert.True(signals.Length == 10,
            $"Expected ten Python signals, found: {string.Join("; ", signals.Select(signal => $"{signal.Category}@{signal.Line}: {signal.Evidence}"))}");
        Assert.Equal(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, signals.Select(signal => signal.Line).ToArray());
        Assert.Contains(signals, signal => signal.Category == "Factory type");
        Assert.Contains(signals, signal => signal.Category == "Factory method");
        Assert.Contains(signals, signal => signal.Category == "Factory invocation");
        Assert.Equal(2, signals.Count(signal => signal.Category == "DI registration"));
        Assert.Contains(signals, signal => signal.Category == "Dependency injection");
        Assert.All(signals, signal =>
        {
            Assert.Equal("Candidate", signal.EvidenceKind);
            Assert.Equal("Python", signal.Language);
        });
        Assert.DoesNotContain(signals, signal => signal.Evidence.Contains("Fake", StringComparison.Ordinal));
        Assert.Contains(inventory.LanguageCoverage, language => language.Language == "Python"
            && language.AnalysisLevel == "Lexical candidate patterns");

        var repeatedInventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var markdown = MarkdownReport.Render(inventory);
        Assert.Equal(markdown, MarkdownReport.Render(repeatedInventory));
        Assert.All(signals, signal => Assert.Contains(
            $"{signal.Category} [Candidate] (Python): `app/services.py:{signal.Line}`", markdown));
        using var output = new StringWriter();
        using var repeatedOutput = new StringWriter();
        using var error = new StringWriter();
        var args = new[] { "inspect", _root, "--format", "json", "--limit", "50" };
        Assert.Equal(0, Program.Run(args, output, error));
        Assert.Equal(0, Program.Run(args, repeatedOutput, error));
        Assert.Equal(output.ToString(), repeatedOutput.ToString());
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var jsonSignals = json.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals")
            .EnumerateArray().Where(signal => signal.GetProperty("Path").GetString() == "app/services.py")
            .Select(signal => (
                Category: signal.GetProperty("Category").GetString()!,
                EvidenceKind: signal.GetProperty("EvidenceKind").GetString()!,
                Language: signal.GetProperty("Language").GetString()!,
                Line: signal.GetProperty("Line").GetInt32(),
                Evidence: signal.GetProperty("Evidence").GetString()!))
            .ToArray();
        Assert.Equal(signals.Select(signal => (signal.Category, signal.EvidenceKind, signal.Language,
            signal.Line, signal.Evidence)), jsonSignals);
    }

    [Fact]
    public void Scan_ReportsJavaScriptExtensionsOverloadAmbiguityAndBoundedSamples()
    {
        foreach (var extension in new[] { ".js", ".jsx", ".mjs", ".cjs", ".ts", ".tsx", ".mts", ".cts" })
            Write("sources/sample" + extension, "export function sample() { return 1; }\n");
        Write("sources/use.js", "import { sample } from './sample.js'; import { sample as second } from './sample.ts'; sample(); second();\n");
        Write("sources/overloads.ts", """
            export function pick(value: string): string;
            export function pick(value: number): number;
            export function pick(value: unknown): unknown { return value; }
            export function use() { return pick(1); }
            """);

        var analysis = RepositoryScanner.Scan(_root, sampleLimit: 1).JavaScriptTypeScript;
        Assert.Equal(10, analysis.ScannedFileCount);
        Assert.Single(analysis.Declarations);
        Assert.Equal(analysis.DeclarationCount - 1, analysis.OmittedDeclarationCount);
        Assert.True(analysis.OmittedDeclarationCount > 0);
        Assert.Single(analysis.ModuleRelationships);
        Assert.Equal(1, analysis.OmittedModuleRelationshipCount);
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "pick" && item.Status == "ambiguous"
            && item.CandidateTargets.Count == 3);
        Assert.Single(analysis.CallRelationships);
        Assert.Equal(analysis.CallRelationshipCount - 1, analysis.OmittedCallRelationshipCount);
        Assert.Contains("declarations omitted by the sample limit",
            MarkdownReport.Render(RepositoryScanner.Scan(_root, sampleLimit: 1)));

        foreach (var extension in new[] { ".js", ".jsx", ".mjs", ".cjs", ".ts", ".tsx", ".mts", ".cts" })
        {
            var path = "sources/sample" + extension;
            var singleExtension = JavaScriptTypeScriptAnalyzer.Analyze(_root, new[] { path }, sampleLimit: 10);
            var declaration = Assert.Single(singleExtension.Declarations);
            Assert.Equal(path, declaration.Path);
            Assert.Equal("sample", declaration.Name);
            Assert.Equal(1, declaration.Line);
        }
    }

    [Fact]
    public void Scan_ResolvesPnpmWorkspacePackagesAndReportsMalformedTypeScriptConfiguration()
    {
        Write("pnpm-workspace.yaml", "packages:\n  - 'services/*'\n");
        Write("services/data/package.json", "{\"name\":\"@acme/data\",\"main\":\"src/index.js\"}");
        Write("services/data/src/index.js", "export function getRecord() { return {}; }\n");
        Write("apps/server/tsconfig.json", "{ this is not valid json }");
        Write("apps/client/tsconfig.json", "{ also invalid }");
        Write("apps/server/index.mts", "import { getRecord } from '@acme/data'; getRecord();\n");

        var analysis = RepositoryScanner.Scan(_root, sampleLimit: 1).JavaScriptTypeScript;

        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "@acme/data"
            && item.Status == "syntax-resolved" && item.TargetPath == "services/data/src/index.js");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "getRecord"
            && item.Status == "syntax-resolved" && item.TargetPath == "services/data/src/index.js");
        Assert.Contains(analysis.Issues, item => item.Path == "apps/client/tsconfig.json"
            && item.Code == "InvalidTsConfig");
        Assert.Equal(2, analysis.IssueCount);
        Assert.Single(analysis.Issues);
        Assert.Equal(1, analysis.OmittedIssueCount);
    }

    [Fact]
    public void Scan_LeavesDuplicateWorkspacePackageNamesAmbiguous()
    {
        Write("package.json", "{\"workspaces\":[\"packages/*\"]}");
        Write("packages/one/package.json", "{\"name\":\"@demo/shared\",\"main\":\"index.js\"}");
        Write("packages/one/index.js", "export function open() {}\n");
        Write("packages/two/package.json", "{\"name\":\"@demo/shared\",\"main\":\"index.js\"}");
        Write("packages/two/index.js", "export function open() {}\n");
        Write("app/main.ts", "import { open } from '@demo/shared'; open();\n");

        var analysis = RepositoryScanner.Scan(_root).JavaScriptTypeScript;

        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "@demo/shared"
            && item.Status == "ambiguous" && item.CandidateTargetPaths.Count == 2);
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "open" && item.Status == "unresolved");
    }

    [Fact]
    public void Scan_WorkspaceGlobStarMatchesOneSegmentAndDoubleStarMatchesNestedPackages()
    {
        Write("package.json", "{\"workspaces\":[\"packages/*\"]}");
        Write("packages/direct/package.json", "{\"name\":\"@demo/direct\"}");
        Write("packages/nested/deep/package.json", "{\"name\":\"@demo/nested\"}");
        var paths = RepositoryScanner.DiscoverFiles(_root);
        var issues = new List<JavaScriptTypeScriptIssue>();

        var oneSegment = JavaScriptTypeScriptAnalyzer.ReadWorkspacePackages(_root, paths, issues);

        Assert.Contains("@demo/direct", oneSegment.Keys);
        Assert.DoesNotContain("@demo/nested", oneSegment.Keys);

        Write("package.json", "{\"workspaces\":[\"packages/**\"]}");
        var recursive = JavaScriptTypeScriptAnalyzer.ReadWorkspacePackages(_root,
            RepositoryScanner.DiscoverFiles(_root), new List<JavaScriptTypeScriptIssue>());
        Assert.Contains("@demo/direct", recursive.Keys);
        Assert.Contains("@demo/nested", recursive.Keys);
    }

    [Fact]
    public void Scan_DoesNotResolveThisAcrossClassesOrImportedBindingsShadowedByParameters()
    {
        Write("lib.ts", "export function save() {}\n");
        Write("main.ts", """
            import { save } from './lib';
            function render(save) { save(); }
            save();
            class A { run() { this.persist(); } }
            class B { persist() {} }
            class C { persist() {} run() { this.persist(); } }
            class Dup { run() { this.persist(); } }
            class Dup { persist() {} }
            """);

        var analysis = RepositoryScanner.Scan(_root, sampleLimit: 50).JavaScriptTypeScript;

        Assert.Contains(analysis.CallRelationships, item => item.SourcePath == "main.ts" && item.Expression == "save"
            && item.SourceLine == 2 && item.Status == "unresolved");
        Assert.Contains(analysis.CallRelationships, item => item.SourcePath == "main.ts" && item.Expression == "save"
            && item.SourceLine == 3 && item.Status == "syntax-resolved" && item.TargetPath == "lib.ts");
        Assert.Contains(analysis.CallRelationships, item => item.SourcePath == "main.ts" && item.Expression == "this.persist"
            && item.SourceLine == 4 && item.Status == "unresolved");
        Assert.Contains(analysis.CallRelationships, item => item.SourcePath == "main.ts" && item.Expression == "this.persist"
            && item.SourceLine == 6 && item.Status == "syntax-resolved" && item.TargetName == "persist");
        Assert.Contains(analysis.CallRelationships, item => item.SourcePath == "main.ts" && item.Expression == "this.persist"
            && item.SourceLine == 7 && item.Status == "unresolved");
    }

    [Fact]
    public void Scan_DoesNotResolveImportedCallsShadowedByLocalVariablesOrAnnotatedGenericParameters()
    {
        Write("lib.ts", "export function save() {}\n");
        Write("main.ts", """
            import { save } from './lib';
            function parameter<T>(save: T): Promise<void> { save(); }
            function local() { const save = createLocal(); save(); }
            save();
            """);

        var calls = RepositoryScanner.Scan(_root).JavaScriptTypeScript.CallRelationships
            .Where(item => item.SourcePath == "main.ts" && item.Expression == "save").ToArray();

        Assert.Equal(3, calls.Length);
        Assert.Equal("unresolved", calls.Single(item => item.SourceLine == 2).Status);
        Assert.Equal("unresolved", calls.Single(item => item.SourceLine == 3).Status);
        Assert.Equal("syntax-resolved", calls.Single(item => item.SourceLine == 4).Status);
    }

    [Fact]
    public void Scan_TracksDestructuredBindingsWithoutTreatingControlConditionsAsParameters()
    {
        Write("lib.ts", "export function save() {}\n");
        Write("main.ts", """
            import { save } from './lib';
            function destructured() { const { save } = createLocal(); save(); }
            function aliased() { const { save: localSave } = createLocal(); save(); localSave(); }
            if (save) { save(); }
            save();
            """);

        var calls = RepositoryScanner.Scan(_root).JavaScriptTypeScript.CallRelationships
            .Where(item => item.SourcePath == "main.ts" && item.Expression is "save" or "localSave").ToArray();

        Assert.Equal("unresolved", calls.Single(item => item.SourceLine == 2).Status);
        Assert.Equal("syntax-resolved", calls.Single(item => item.SourceLine == 3 && item.Expression == "save").Status);
        Assert.Equal("unresolved", calls.Single(item => item.SourceLine == 3 && item.Expression == "localSave").Status);
        Assert.Equal("syntax-resolved", calls.Single(item => item.SourceLine == 4).Status);
        Assert.Equal("syntax-resolved", calls.Single(item => item.SourceLine == 5).Status);
    }

    [Fact]
    public void Scan_LeavesTypeOnlyAndUnsupportedThisTargetsUnresolved()
    {
        Write("lib.ts", "export function save() {}\n");
        Write("defaults.ts", "export default function load() {}\n");
        Write("type-import.ts", "import type { save } from './lib'; save();\n");
        Write("inline-type-import.ts", "import { type save } from './lib'; save();\n");
        Write("runtime-import.ts", "import { save } from './lib'; save();\n");
        Write("type-barrel.ts", "export type { save } from './lib';\n");
        Write("type-barrel-user.ts", "import { save } from './type-barrel'; save();\n");
        Write("default-barrel.ts", "export * from './defaults';\n");
        Write("default-barrel-user.ts", "import load from './default-barrel'; load();\n");
        Write("catch-user.ts", "import { save } from './lib'; try {} catch (save) { save(); } save();\n");
        Write("parameter-user.ts", "import { save as localSave } from './lib'; function run({ value: localSave }) { localSave(); }\n");
        Write("this-user.ts", "class Example { run() { function callback() { this.persist(); } const arrow = () => this.persist(); } persist() {} }\n");

        var analysis = RepositoryScanner.Scan(_root).JavaScriptTypeScript;

        Assert.Equal("unresolved", Assert.Single(analysis.CallRelationships, item => item.SourcePath == "type-import.ts").Status);
        Assert.Equal("unresolved", Assert.Single(analysis.CallRelationships, item => item.SourcePath == "inline-type-import.ts").Status);
        Assert.Equal("syntax-resolved", Assert.Single(analysis.CallRelationships, item => item.SourcePath == "runtime-import.ts").Status);
        Assert.Equal("unresolved", Assert.Single(analysis.CallRelationships, item => item.SourcePath == "type-barrel-user.ts").Status);
        Assert.Equal("unresolved", Assert.Single(analysis.CallRelationships, item => item.SourcePath == "default-barrel-user.ts").Status);
        var catchCalls = analysis.CallRelationships.Where(item => item.SourcePath == "catch-user.ts").OrderBy(item => item.SourceColumn).ToArray();
        Assert.Equal(2, catchCalls.Length);
        Assert.Equal("unresolved", catchCalls[0].Status);
        Assert.Equal("syntax-resolved", catchCalls[1].Status);
        Assert.Equal("unresolved", Assert.Single(analysis.CallRelationships, item => item.SourcePath == "parameter-user.ts").Status);
        Assert.Equal(2, analysis.CallRelationships.Count(item => item.SourcePath == "this-user.ts" && item.Expression == "this.persist"));
        Assert.Contains(analysis.CallRelationships, item => item.SourcePath == "this-user.ts" && item.Expression == "this.persist" && item.Status == "unresolved");
        Assert.Contains(analysis.CallRelationships, item => item.SourcePath == "this-user.ts" && item.Expression == "this.persist" && item.Status == "syntax-resolved");
    }

    [Fact]
    public void Scan_UsesFunctionScopeForVarBindingsWithoutLeakingAcrossFunctions()
    {
        Write("lib.ts", "export function save() {}\n");
        Write("main.ts", "import { save } from './lib';\nfunction run() { { var save = local; } save(); }\nfunction other() { save(); }\n");

        var calls = RepositoryScanner.Scan(_root).JavaScriptTypeScript.CallRelationships
            .Where(item => item.SourcePath == "main.ts" && item.Expression == "save")
            .OrderBy(item => item.SourceLine).ToArray();

        Assert.Equal(2, calls.Length);
        Assert.Equal("unresolved", calls[0].Status);
        Assert.Equal("syntax-resolved", calls[1].Status);
    }

    [Fact]
    public void Scan_EnforcesAggregateFileTokenAndRecordLimitsWithVisibleIssues()
    {
        Write("a.js", "export function first() {}\n");
        Write("b.js", "export function second() {}\n");
        Write("c.js", "export function third() {}\n");
        var files = RepositoryScanner.DiscoverFiles(_root);

        var analysis = JavaScriptTypeScriptAnalyzer.Analyze(_root, files, sampleLimit: 50,
            maxSourceFiles: 2, maxTotalSourceCharacters: 10_000, maxTotalTokens: 1_000,
            maxRecordsPerKind: 1, maxIssues: 10);

        Assert.Equal(2, analysis.ScannedFileCount);
        Assert.Equal(1, analysis.DeclarationCount);
        Assert.Contains(analysis.Issues, issue => issue.Code == "SourceFileLimit" && issue.Path == "c.js");
        Assert.Contains(analysis.Issues, issue => issue.Code == "AggregateRecordLimit");

        var diagnosticLimited = JavaScriptTypeScriptAnalyzer.Analyze(_root, files, sampleLimit: 50,
            maxSourceFiles: 2, maxTotalSourceCharacters: 10_000, maxTotalTokens: 1_000,
            maxRecordsPerKind: 1, maxIssues: 1);
        Assert.Equal(1, diagnosticLimited.IssueCount);
        Assert.Equal("IssueLimit", Assert.Single(diagnosticLimited.Issues).Code);

        var tokenLimited = JavaScriptTypeScriptAnalyzer.Analyze(_root, files, sampleLimit: 50,
            maxSourceFiles: 10, maxTotalSourceCharacters: 10_000, maxTotalTokens: 1,
            maxRecordsPerKind: 10, maxIssues: 10);
        Assert.Contains(tokenLimited.Issues, issue => issue.Code == "TotalTokenLimit");
        Assert.Contains(tokenLimited.EvidenceLimits, text => text.Contains("total tokens", StringComparison.Ordinal));

        var sourceLimited = JavaScriptTypeScriptAnalyzer.Analyze(_root, files, sampleLimit: 50,
            maxSourceFiles: 10, maxTotalSourceCharacters: 10, maxTotalTokens: 1_000,
            maxRecordsPerKind: 10, maxIssues: 10);
        Assert.Contains(sourceLimited.Issues, issue => issue.Code == "TotalSourceSizeLimit");

        var earlySourceLimited = JavaScriptTypeScriptAnalyzer.Analyze(_root, files, sampleLimit: 50,
            maxSourceFiles: 10, maxTotalSourceCharacters: 1, maxTotalTokens: 1_000,
            maxRecordsPerKind: 10, maxIssues: 10);
        Assert.Equal(1, earlySourceLimited.ScannedFileCount);

        var oversizedMetadata = "{\"workspaces\":[\"packages/*\"],\"pad\":\"" + new string('x', 1024 * 1024) + "\"}";
        File.WriteAllText(Path.Combine(_root, "package.json"), oversizedMetadata);
        var metadataIssues = new List<JavaScriptTypeScriptIssue>();
        JavaScriptTypeScriptAnalyzer.ReadWorkspacePackages(_root, new[] { "package.json" }, metadataIssues);
        Assert.Contains(metadataIssues, issue => issue.Path == "package.json" && issue.Code == "MetadataFileSizeLimit");

        Write("package.json", "{\"workspaces\":[\"packages/*\"]}");
        Write("packages/one/package.json", "{\"name\":\"@demo/one\"}");
        Write("packages/two/package.json", "{\"name\":\"@demo/two\"}");
        var totalMetadataIssues = new List<JavaScriptTypeScriptIssue>();
        JavaScriptTypeScriptAnalyzer.ReadWorkspacePackages(_root, RepositoryScanner.DiscoverFiles(_root),
            totalMetadataIssues, maxMetadataFileBytes: 1_000, maxTotalMetadataBytes: 60);
        Assert.Contains(totalMetadataIssues, issue => issue.Code == "TotalMetadataSizeLimit");

        Write("m1.ts", "export function one() {}\n");
        Write("m2.ts", "export function two() {}\n");
        Write("use.ts", "import { one } from './m1'; import { two } from './m2'; one(); two();\n");
        var relationLimited = JavaScriptTypeScriptAnalyzer.Analyze(_root, RepositoryScanner.DiscoverFiles(_root),
            sampleLimit: 50, maxSourceFiles: 20, maxTotalSourceCharacters: 10_000,
            maxTotalTokens: 1_000, maxRecordsPerKind: 1, maxIssues: 10);
        Assert.Equal(1, relationLimited.ModuleRelationshipCount);
        Assert.Equal(1, relationLimited.CallRelationshipCount);
        Assert.Contains(relationLimited.Issues, issue => issue.Code == "AggregateRecordLimit");
    }

    [Fact]
    public void Scan_DoesNotGuessConditionalPackageExportsOrReadAliasesOutsideTheRoot()
    {
        Write("package.json", "{\"workspaces\":[\"packages/*\"]}");
        Write("packages/conditional/package.json", "{\"name\":\"@demo/conditional\",\"exports\":{\".\":{\"import\":\"./esm.js\",\"require\":\"./cjs.js\"}}}");
        Write("packages/conditional/esm.js", "export function create() {}\n");
        Write("packages/conditional/cjs.js", "export function create() {}\n");
        Write("app/tsconfig.json", "{\"compilerOptions\":{\"baseUrl\":\"../../../outside\",\"paths\":{\"@unsafe/*\":[\"*\"]}}}");
        Write("app/main.ts", "import { create } from '@demo/conditional'; create(); import { outside } from '@unsafe/module'; outside();\n");

        var analysis = RepositoryScanner.Scan(_root).JavaScriptTypeScript;

        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "@demo/conditional"
            && item.Status == "unresolved" && item.Reason == "PackageExportsSelectionUnsupported");
        Assert.Contains(analysis.Issues, item => item.Path == "app/tsconfig.json" && item.Code == "TsConfigPathOutsideRoot");
        Assert.DoesNotContain(analysis.ModuleRelationships, item => item.TargetPath?.Contains("outside", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Scan_StopsCyclicBarrelResolutionWithoutInventingCallTargets()
    {
        Write("src/a.ts", "export { value } from './b';\n");
        Write("src/b.ts", "export { value } from './a';\n");
        Write("src/use.ts", "import { value } from './a'; value();\n");

        var first = RepositoryScanner.Scan(_root);
        var second = RepositoryScanner.Scan(_root);

        Assert.Equal(3, first.JavaScriptTypeScript.ScannedFileCount);
        Assert.Equal(3, first.JavaScriptTypeScript.ModuleRelationshipCount);
        Assert.All(first.JavaScriptTypeScript.ModuleRelationships, relationship =>
            Assert.Equal("syntax-resolved", relationship.Status));
        var call = Assert.Single(first.JavaScriptTypeScript.CallRelationships);
        Assert.Equal("value", call.Expression);
        Assert.Equal("unresolved", call.Status);
        Assert.Equal("NoUniqueInRepositoryDeclaration", call.Reason);
        Assert.Equal(MarkdownReport.Render(first), MarkdownReport.Render(second));
    }

    [Fact]
    public void Scan_PreservesDeclarationAndCallLinesAcrossCrLfAndIgnoresTemplateContents()
    {
        Write("src/module.js", ToCrLf("function first() { return 1; }\nfunction second() { return first(); }\nconst sample = `templateFake()`;\n"));
        Write("src/broken.js", "/* unterminated function hidden() {}\n");
        Write("src/unbalanced.ts", "export function incomplete(value: string\n");

        var analysis = RepositoryScanner.Scan(_root).JavaScriptTypeScript;

        Assert.Contains(analysis.Declarations, item => item.Name == "first" && item.Line == 1 && item.Column == 10);
        Assert.Contains(analysis.Declarations, item => item.Name == "second" && item.Line == 2 && item.Column == 10);
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "first" && item.SourceLine == 2
            && item.Status == "syntax-resolved");
        Assert.DoesNotContain(analysis.CallRelationships, item => item.Expression == "templateFake");
        Assert.DoesNotContain(analysis.Declarations, item => item.Name == "hidden");
        Assert.Contains(analysis.Issues, item => item.Path == "src/broken.js" && item.Code == "UnterminatedBlockComment");
        Assert.Contains(analysis.Issues, item => item.Path == "src/unbalanced.ts" && item.Code == "UnbalancedDelimiter");
    }

    [Fact]
    public void Scan_ReportsSourceLocatedJavaScriptTypeScriptDeclarationsAndStaticRelationships()
    {
        Write("package.json", "{\"private\":true,\"workspaces\":[\"packages/*\"]}");
        Write("packages/core/package.json", "{\"name\":\"@demo/core\",\"source\":\"src/index.ts\"}");
        Write("packages/core/src/loader.ts", "export function load<T>(value: T) { return value; }\nexport class Loader { save() {} run<T>(value: T) { this.save(); unknown.save(); return load<T>(value); } }\n");
        Write("packages/core/src/default-loader.ts", "export default function defaultLoader(value: number) { return value; }\n");
        Write("packages/core/src/index.ts", "export { load as loadRecord } from './loader';\n");
        Write("apps/tsconfig.base.json", "{\"compilerOptions\":{\"baseUrl\":\".\",\"paths\":{\"@app/*\":[\"web/src/*\"]}}}");
        Write("apps/web/tsconfig.json", "{\"extends\":\"../tsconfig.base.json\"}");
        Write("packages/app/src/main.tsx", """
            import { load } from '../../core/src/loader.js';
            import { loadRecord as runLoad } from '@demo/core';
            import { loadRecord as runLoadFromBarrel } from '../../core/src';
            import defaultLoader from '../../core/src/default-loader';
            import { absent } from './absent';
            export const render = <T,>(value: T) => load(value);
            render(1);
            runLoad(2);
            runLoadFromBarrel(4);
            defaultLoader(3);
            absent();
            const lazy = import('./lazy');
            const pattern = /regexFake\(\)/;
            const view = <div>jsxFake()</div>;
            // load(fake);
            const text = "load(fake)";
            """);
        Write("packages/app/src/lazy.ts", "export const lazyValue = 1;\n");
        Write("apps/web/src/main.ts", "import { helper } from '@app/helper'; helper();\n");
        Write("apps/web/src/helper.ts", "export function helper() { return true; }\n");

        var first = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var analysis = first.JavaScriptTypeScript;

        Assert.Equal(11, RepositoryScanner.DiscoverFiles(_root).Count);
        Assert.Contains("@demo/core", JavaScriptTypeScriptAnalyzer.ReadWorkspacePackages(_root,
            RepositoryScanner.DiscoverFiles(_root), new List<JavaScriptTypeScriptIssue>()).Keys);
        Assert.Equal(7, JavaScriptTypeScriptAnalyzer.Analyze(_root, RepositoryScanner.DiscoverFiles(_root), 50).ScannedFileCount);
        Assert.Equal(7, analysis.ScannedFileCount);
        Assert.Contains(analysis.Declarations, item => item.Path == "packages/core/src/loader.ts"
            && item.Name == "load" && item.Line == 1 && item.Kind == "function");
        Assert.Contains(analysis.Declarations, item => item.Path == "packages/core/src/loader.ts"
            && item.Name == "run" && item.Kind == "method" && item.Container == "Loader");
        Assert.Contains(analysis.Declarations, item => item.Path == "packages/app/src/main.tsx"
            && item.Name == "render" && item.Kind == "callable variable");
        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "../../core/src/loader.js"
            && item.Status == "syntax-resolved" && item.TargetPath == "packages/core/src/loader.ts");
        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "@demo/core"
            && item.Status == "syntax-resolved" && item.TargetPath == "packages/core/src/index.ts");
        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "../../core/src"
            && item.Status == "syntax-resolved" && item.TargetPath == "packages/core/src/index.ts");
        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "@app/helper"
            && item.Status == "syntax-resolved" && item.TargetPath == "apps/web/src/helper.ts");
        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "./absent"
            && item.Status == "unresolved" && item.Reason == "ModuleTargetNotFound");
        Assert.Contains(analysis.ModuleRelationships, item => item.Specifier == "./lazy"
            && item.Status == "unresolved" && item.Reason == "DynamicImport");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "load" && item.Status == "syntax-resolved"
            && item.TargetPath == "packages/core/src/loader.ts" && item.TargetName == "load");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "runLoad" && item.Status == "syntax-resolved"
            && item.TargetPath == "packages/core/src/loader.ts" && item.TargetName == "load");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "runLoadFromBarrel" && item.Status == "syntax-resolved"
            && item.TargetPath == "packages/core/src/loader.ts" && item.TargetName == "load");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "this.save" && item.Status == "syntax-resolved"
            && item.TargetPath == "packages/core/src/loader.ts" && item.TargetName == "save");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "unknown.save" && item.Status == "unresolved"
            && item.Reason == "ReceiverTypeNotResolved");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "defaultLoader" && item.Status == "syntax-resolved"
            && item.TargetPath == "packages/core/src/default-loader.ts" && item.TargetName == "defaultLoader");
        Assert.Contains(analysis.CallRelationships, item => item.Expression == "absent" && item.Status == "unresolved");
        Assert.DoesNotContain(analysis.CallRelationships, item => item.Expression.Contains("fake", StringComparison.Ordinal));
        Assert.DoesNotContain(analysis.CallRelationships, item => item.Expression is "regexFake" or "jsxFake");

        var markdown = MarkdownReport.Render(first);
        Assert.Contains("## JavaScript and TypeScript source evidence", markdown);
        Assert.Contains("function `load` (TypeScript): `packages/core/src/loader.ts:1:17`", markdown);
        var second = RepositoryScanner.Scan(_root, sampleLimit: 50);
        Assert.Equal(markdown, MarkdownReport.Render(second));
        using var json = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(first));
        var jsonEvidence = json.RootElement.GetProperty("JavaScriptTypeScript");
        Assert.Equal(analysis.DeclarationCount, jsonEvidence.GetProperty("DeclarationCount").GetInt32());
        Assert.Equal(analysis.ModuleRelationshipCount, jsonEvidence.GetProperty("ModuleRelationshipCount").GetInt32());
        Assert.Equal(analysis.CallRelationshipCount, jsonEvidence.GetProperty("CallRelationshipCount").GetInt32());
        var jsonLoad = Assert.Single(jsonEvidence.GetProperty("Declarations").EnumerateArray(),
            item => item.GetProperty("Name").GetString() == "load");
        Assert.Equal("packages/core/src/loader.ts", jsonLoad.GetProperty("Path").GetString());
        Assert.Equal(1, jsonLoad.GetProperty("Line").GetInt32());
        Assert.Equal(17, jsonLoad.GetProperty("Column").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("SchemaVersion").GetInt32());
    }

    [Fact]
    public void Scan_ReportsJavaScriptAndTypeScriptFactoryAndDependencyCandidates()
    {
        Write("web/legacy.js", """
            export class LegacyFactory {
                buildClient() { return {}; }
            }
            const legacyFactory = new LegacyFactory();
            legacyFactory.buildClient();
            const provider = { useFactory: legacyFactory };
            container.bind(Client).toSelf();
            container.get(Client);
            // class CommentFactory { createFake() {} } container.bind(Fake);
            const text = "container.bind(Fake); useFactory: fakeFactory";
            const template = `class TemplateFactory { buildFake() {} } container.get(Fake);`;
            /*
            @Injectable()
            container.bind(Fake);
            */
            """);
        Write("web/report.ts", """
            import { Inject, Injectable, inject } from '@angular/core';
            @Injectable({ providedIn: 'root' })
            export class ReportFactory {
                createReport(): Report { return new Report(); }
            }
            const reportFactory = new ReportFactory();
            reportFactory.createReport();
            const provider = { provide: Report, useFactory: reportFactory };
            const resolved = inject(ReportService);
            class Consumer { constructor(@Inject(ReportService) service: ReportService) {} }
            container.bind(ReportService).toSelf();
            container.get(ReportService);
            // class CommentFactory { createFake() {} } @Injectable()
            const text = "container.get(Fake); useFactory: fakeFactory";
            const template = `class TemplateFactory { makeFake() {} } inject(Fake);`;
            /* @Inject(Fake) container.bind(Fake); */
            """);

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var signals = inventory.ArchitecturePatterns.Signals
            .Where(signal => signal.Path is "web/legacy.js" or "web/report.ts")
            .ToArray();
        var javascriptSignals = signals.Where(signal => signal.Path == "web/legacy.js").ToArray();
        var typeScriptSignals = signals.Where(signal => signal.Path == "web/report.ts").ToArray();

        Assert.Equal(7, javascriptSignals.Length);
        Assert.Equal(10, typeScriptSignals.Length);
        Assert.All(javascriptSignals, signal => Assert.Equal("JavaScript", signal.Language));
        Assert.All(typeScriptSignals, signal => Assert.Equal("TypeScript", signal.Language));
        Assert.All(signals, signal => Assert.Equal("Candidate", signal.EvidenceKind));
        Assert.Contains(signals, signal => signal.Category == "Factory type");
        Assert.Contains(signals, signal => signal.Category == "Factory method");
        Assert.Contains(signals, signal => signal.Category == "Factory invocation");
        Assert.Contains(signals, signal => signal.Category == "DI registration");
        Assert.Contains(signals, signal => signal.Category == "DI resolution");
        Assert.Contains(signals, signal => signal.Category == "Dependency injection");
        Assert.DoesNotContain(signals, signal => signal.Evidence.Contains("Fake", StringComparison.Ordinal));
        Assert.Contains(inventory.LanguageCoverage, language => language.Language == "JavaScript"
            && language.AnalysisLevel == "Bounded syntax and lexical candidate patterns");
        Assert.Contains(inventory.LanguageCoverage, language => language.Language == "TypeScript"
            && language.AnalysisLevel == "Bounded syntax and lexical candidate patterns");

        var repeatedInventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var markdown = MarkdownReport.Render(inventory);
        Assert.Equal(markdown, MarkdownReport.Render(repeatedInventory));
        Assert.All(signals, signal => Assert.Contains(
            $"{signal.Category} [Candidate] ({signal.Language}): `{signal.Path}:{signal.Line}`", markdown));
        using var output = new StringWriter();
        using var repeatedOutput = new StringWriter();
        using var error = new StringWriter();
        var args = new[] { "inspect", _root, "--format", "json", "--limit", "50" };
        Assert.Equal(0, Program.Run(args, output, error));
        Assert.Equal(0, Program.Run(args, repeatedOutput, error));
        Assert.Equal(output.ToString(), repeatedOutput.ToString());
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var jsonSignals = json.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals")
            .EnumerateArray().Where(signal => signal.GetProperty("Path").GetString() is "web/legacy.js" or "web/report.ts")
            .Select(signal => (
                Category: signal.GetProperty("Category").GetString()!,
                EvidenceKind: signal.GetProperty("EvidenceKind").GetString()!,
                Language: signal.GetProperty("Language").GetString()!,
                Path: signal.GetProperty("Path").GetString()!,
                Line: signal.GetProperty("Line").GetInt32(),
                Evidence: signal.GetProperty("Evidence").GetString()!))
            .ToArray();
        Assert.Equal(signals.Select(signal => (signal.Category, signal.EvidenceKind, signal.Language,
            signal.Path, signal.Line, signal.Evidence)), jsonSignals);
    }

    [Fact]
    public void Scan_ReportsJvmFactoryAndDependencyCandidatesAndFiltersCommentsAndLiterals()
    {
        var tripleQuote = new string('"', 3);
        var javaSource = """
            package sample;
            @Configuration
            class ServiceFactory {
                @Bean Service createService() { return new Service(); }
                @Provides Service provideService() { return new Service(); }
            }
            @Service
            class Service {}
            class Consumer { @Inject Consumer(Service service) {} }
            class AutowiredConsumer { @Autowired Service service; }
            class FactoryUse { void run(ServiceFactory factory) { factory.createService(); } }
            class GuiceModule extends AbstractModule { void configure() { bind(Service.class).to(ServiceImpl.class); } }
            class Bootstrap { void run() { var injector = Guice.createInjector(new GuiceModule()); injector.getInstance(Service.class); } }
            // @Inject class CommentFactory { @Bean Object createFake() {} }
            /* @Autowired class BlockCommentFactory {} */
            String ignored = __TRIPLE__
                "@Inject class TextBlockFactory { @Provides Object makeFake(); }"
                __TRIPLE__;
            String quoted = "@Inject @Bean fakeFactory.createFake()";
            """.Replace("__TRIPLE__", tripleQuote, StringComparison.Ordinal);
        Write("jvm/Services.java", ToCrLf(javaSource));

        var kotlinSource = """
            package sample
            @Module
            class KotlinFactory {
                @Provides fun createService(): Service = Service()
            }
            class KotlinConsumer @Inject constructor(service: Service)
            class KotlinFactoryUse { fun run(factory: KotlinFactory) { factory.createService() } }
            // @Inject class CommentFactory { @Bean fun buildFake() = Any() }
            /* outer /* @Provides class NestedCommentFactory */ @Inject */
            val ignored = __TRIPLE__
                @Inject @Bean class RawStringFactory { @Provides fun makeFake() = Any() }
            __TRIPLE__
        """.Replace("__TRIPLE__", tripleQuote, StringComparison.Ordinal);
        Write("jvm/Services.kt", kotlinSource);
        Write("jvm/scripts.kts", """
            @Configuration
            class ScriptFactory {
                @Bean
                fun buildClient(): Client = Client()
            }
            """);

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var signals = inventory.ArchitecturePatterns.Signals
            .Where(signal => signal.Path.StartsWith("jvm/", StringComparison.Ordinal))
            .ToArray();
        Assert.Contains(signals, signal => signal.Language == "Java" && signal.Category == "Factory type");
        Assert.Contains(signals, signal => signal.Language == "Java" && signal.Category == "Factory method");
        Assert.Contains(signals, signal => signal.Language == "Java" && signal.Category == "Factory invocation");
        Assert.Contains(signals, signal => signal.Language == "Java" && signal.Category == "DI registration");
        Assert.Contains(signals, signal => signal.Language == "Java" && signal.Category == "DI resolution");
        Assert.Contains(signals, signal => signal.Language == "Java" && signal.Category == "Dependency injection");
        Assert.Contains(signals, signal => signal.Language == "Kotlin" && signal.Category == "Factory type");
        Assert.Contains(signals, signal => signal.Language == "Kotlin" && signal.Category == "Factory method");
        Assert.Contains(signals, signal => signal.Language == "Kotlin" && signal.Category == "Factory invocation");
        Assert.DoesNotContain(signals, signal => signal.Evidence.Contains("Fake", StringComparison.Ordinal)
            || signal.Evidence.Contains("Comment", StringComparison.Ordinal)
            || signal.Evidence.Contains("NestedComment", StringComparison.Ordinal)
            || signal.Evidence.Contains("RawString", StringComparison.Ordinal)
            || signal.Evidence.Contains("TextBlock", StringComparison.Ordinal));
        Assert.Contains(inventory.LanguageCoverage, language => language.Language == "Java"
            && language.AnalysisLevel == "Lexical candidate patterns");
        Assert.Contains(inventory.LanguageCoverage, language => language.Language == "Kotlin"
            && language.SourceFileCount == 2 && language.AnalysisLevel == "Lexical candidate patterns");
        Assert.All(signals, signal => Assert.Equal("Candidate", signal.EvidenceKind));
        Assert.All(signals, signal => Assert.True(signal.Line > 0));
        Assert.Contains(signals, signal => signal.Path == "jvm/Services.java" && signal.Category == "Factory type"
            && signal.Line == 3);
        Assert.Contains(signals, signal => signal.Path == "jvm/Services.java" && signal.Category == "Factory invocation"
            && signal.Line == 11);

        var markdown = MarkdownReport.Render(inventory);
        Assert.All(signals, signal => Assert.Contains(
            $"{signal.Category} [Candidate] ({signal.Language}): `{signal.Path}:{signal.Line}`", markdown));
        using var jsonOutput = new StringWriter();
        using var repeatedJsonOutput = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "50" }, jsonOutput, error));
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "50" }, repeatedJsonOutput, error));
        Assert.Equal(jsonOutput.ToString(), repeatedJsonOutput.ToString());
        using var json = System.Text.Json.JsonDocument.Parse(jsonOutput.ToString());
        var jsonSignals = json.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals")
            .EnumerateArray().Where(signal => signal.GetProperty("Path").GetString()!.StartsWith("jvm/", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(signals.Select(signal => (signal.Category, signal.EvidenceKind, signal.Language, signal.Path,
            signal.Line, signal.Evidence)), jsonSignals.Select(signal => (
            signal.GetProperty("Category").GetString()!,
            signal.GetProperty("EvidenceKind").GetString()!,
            signal.GetProperty("Language").GetString()!,
            signal.GetProperty("Path").GetString()!,
            signal.GetProperty("Line").GetInt32(),
            signal.GetProperty("Evidence").GetString()!)));
        Assert.Equal(markdown, MarkdownReport.Render(RepositoryScanner.Scan(_root, sampleLimit: 50)));
    }

    [Fact]
    public void Scan_ReportsGoFactoryAndDependencyCandidatesAndFiltersCommentsAndLiterals()
    {
        Write("go/service.go", """
            package service

            type ServiceFactory interface { Create() *Service }
            func NewServiceFactory() *ServiceFactoryImpl { return &ServiceFactoryImpl{} }
            func NewService() *Service { return &Service{} }
            func (f *ServiceFactoryImpl) BuildService() *Service { return NewService() }

            func Configure() {
                service := NewService()
                app := fx.New(fx.Provide(NewService), fx.Invoke(func(*Service) {}))
                _ = app
                container := dig.New()
                _ = container.Provide(NewService)
                _ = container.Invoke(func(*Service) {})
            }

            // type CommentFactory struct{}; func NewFake() *Fake { return nil }
            /* fx.Provide(NewFake); container.Invoke(func(*Fake) {}) */
            text := "func NewFake() *Fake { return nil }; fx.Provide(NewFake)"
            character := 'N'
            raw := `type RawStringFactory struct{}
            func NewFake() *Fake { return nil }
            fx.Invoke(func(*Fake) {})`
            """);
        var crlfSource = """
            package service

            func NewWidget() *Widget { return &Widget{} }
            func UseWidget() { _ = NewWidget() }
            """;
        Write("go/crlf.go", ToCrLf(crlfSource));

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 50);
        var signals = inventory.ArchitecturePatterns.Signals
            .Where(signal => signal.Path.StartsWith("go/", StringComparison.Ordinal))
            .ToArray();

        Assert.Contains(signals, signal => signal.Language == "Go" && signal.Category == "Factory type");
        Assert.Contains(signals, signal => signal.Language == "Go" && signal.Category == "Factory method"
            && signal.Evidence.Contains("NewService", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "Go" && signal.Category == "Factory invocation"
            && signal.Evidence.Contains("NewService()", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Path == "go/service.go" && signal.Category == "Factory invocation"
            && signal.Line == 6 && signal.Evidence.Contains("BuildService", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "Go" && signal.Category == "DI registration");
        Assert.Contains(signals, signal => signal.Language == "Go" && signal.Category == "DI resolution");
        Assert.DoesNotContain(signals, signal => signal.Evidence.Contains("Fake", StringComparison.Ordinal)
            || signal.Evidence.Contains("Comment", StringComparison.Ordinal)
            || signal.Evidence.Contains("RawString", StringComparison.Ordinal));
        Assert.All(signals, signal => Assert.Equal("Candidate", signal.EvidenceKind));
        Assert.All(signals, signal => Assert.True(signal.Line > 0));
        Assert.Contains(inventory.LanguageCoverage, language => language.Language == "Go"
            && language.SourceFileCount == 2 && language.AnalysisLevel == "Lexical candidate patterns");
        Assert.Contains(inventory.ArchitecturePatterns.Signals, signal => signal.Path == "go/crlf.go"
            && signal.Category == "Factory method" && signal.Line == 3);
        Assert.Contains(inventory.ArchitecturePatterns.Signals, signal => signal.Path == "go/crlf.go"
            && signal.Category == "Factory invocation" && signal.Line == 4);

        var markdown = MarkdownReport.Render(inventory);
        Assert.All(signals, signal => Assert.Contains(
            $"{signal.Category} [Candidate] ({signal.Language}): `{signal.Path}:{signal.Line}`", markdown));
        using var jsonOutput = new StringWriter();
        using var repeatedJsonOutput = new StringWriter();
        using var error = new StringWriter();
        var args = new[] { "inspect", _root, "--format", "json", "--limit", "50" };
        Assert.Equal(0, Program.Run(args, jsonOutput, error));
        Assert.Equal(0, Program.Run(args, repeatedJsonOutput, error));
        Assert.Equal(jsonOutput.ToString(), repeatedJsonOutput.ToString());
        using var json = System.Text.Json.JsonDocument.Parse(jsonOutput.ToString());
        var jsonSignals = json.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals")
            .EnumerateArray().Where(signal => signal.GetProperty("Path").GetString()!.StartsWith("go/", StringComparison.Ordinal))
            .Select(signal => (
                Category: signal.GetProperty("Category").GetString()!,
                EvidenceKind: signal.GetProperty("EvidenceKind").GetString()!,
                Language: signal.GetProperty("Language").GetString()!,
                Path: signal.GetProperty("Path").GetString()!,
                Line: signal.GetProperty("Line").GetInt32(),
                Evidence: signal.GetProperty("Evidence").GetString()!))
            .ToArray();
        Assert.Equal(signals.Select(signal => (signal.Category, signal.EvidenceKind, signal.Language, signal.Path,
            signal.Line, signal.Evidence)), jsonSignals);
        Assert.Equal(markdown, MarkdownReport.Render(RepositoryScanner.Scan(_root, sampleLimit: 50)));
    }

    [Fact]
    public void Scan_RecognizesLegacyContainerResolutionAndFiltersGenericGetCalls()
    {
        Write("client/LegacyComposition.cs", """
            class Composition
            {
                void Configure(IUnityContainer container, ContainerBuilder builder, IKernel kernel, IServiceCollection services)
                {
                    container.RegisterType<IClock, SystemClock>();
                    container.RegisterInstance<IClock>(new SystemClock());
                    builder.RegisterType<IClock>().As<IClock>();
                    kernel.Bind<IClock>().To<SystemClock>();
                    services.AddTransient<IClock, SystemClock>();
                }

                void Resolve(IUnityContainer container, IKernel kernel, ILifetimeScope scope, ResourceManager resources, Cache cache)
                {
                    var unity = container.Resolve<IClock>();
                    var ninject = kernel.Get<IClock>();
                    var autofac = scope.Resolve<IClock>();
                    var resource = resources.GetObject("title");
                    var cached = cache.Get("clock");
                }
            }
            """);
        Write("client/LegacyComposition.vb", """
            Public Class Composition
                Public Sub Resolve(container As IUnityContainer, kernel As IKernel, resources As ResourceManager)
                    Dim unity = container.Resolve(Of IClock)()
                    Dim ninject = kernel.Get(Of IClock)()
                    Dim resource = resources.GetObject("title")
                End Sub
            End Class
            """);

        var signals = RepositoryScanner.Scan(_root).ArchitecturePatterns.Signals;
        var csharpResolution = signals.Where(signal => signal.Language == "C#" && signal.Category == "DI resolution").ToArray();
        var visualBasicResolution = signals.Where(signal => signal.Language == "VB.NET" && signal.Category == "DI resolution").ToArray();

        Assert.Equal(3, csharpResolution.Length);
        Assert.Contains(csharpResolution, signal => signal.Evidence.Contains("container.Resolve", StringComparison.Ordinal));
        Assert.Contains(csharpResolution, signal => signal.Evidence.Contains("kernel.Get", StringComparison.Ordinal));
        Assert.Contains(csharpResolution, signal => signal.Evidence.Contains("scope.Resolve", StringComparison.Ordinal));
        Assert.Equal(2, visualBasicResolution.Length);
        Assert.Contains(visualBasicResolution, signal => signal.Evidence.Contains("container.Resolve", StringComparison.Ordinal));
        Assert.Contains(visualBasicResolution, signal => signal.Evidence.Contains("kernel.Get", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Category == "DI resolution"
            && (signal.Evidence.Contains("resources.GetObject", StringComparison.OrdinalIgnoreCase)
                || signal.Evidence.Contains("cache.Get", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(signals, signal => signal.Category == "DI registration" && signal.Evidence.Contains("RegisterType", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Category == "DI registration" && signal.Evidence.Contains("RegisterInstance", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Category == "DI registration" && signal.Evidence.Contains("Bind", StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_ReportsVisualBasicInteropFactoryAndDependencySignalsWithEvidenceKinds()
    {
        Write("client/Composition.vb", """
            Imports System.Runtime.InteropServices

            Public Class widgetfactory
                Public Function createwidget() As Object
                    Return New Object()
                End Function
            End Class

            Public Class Composition
                Public Sub Configure(services As IServiceCollection, provider As IServiceProvider)
                    services.addtransient(Of IWidget, Widget)()
                    Dim widget = provider.getservice(GetType(IWidget))
                    Dim created = widgetfactory.createwidget()
                    Dim legacy = createobject("Legacy.Widget")
                End Sub

                <dllimport("legacy.dll")>
                Public Shared Function LoadWidget() As Integer
                End Function
            End Class
            """);
        Write("client/Ordinary.cs", """
            class FolderLoader
            {
                void Load() { Directory.CreateDirectory("cache"); }
            }
            """);
        Write("client/Injected.cs", """
            public class InjectedConsumer
            {
                public InjectedConsumer(IRepository repository, Func<IWidget> widgetFactory) { }
            }

            public class PlainConsumer
            {
                public PlainConsumer(string name, int retryCount) { }
            }
            """);
        Write("client/Injected.vb", """
            Public Class InjectedConsumer
                Public Sub New(repository As IRepository, widgetFactory As Func(Of IWidget))
                End Sub
            End Class

            Public Class PlainConsumer
                Public Sub New(name As String, retryCount As Integer)
                End Sub
            End Class
            """);
        Write("client/ComInterop.cs", """
            using System;
            using System.Runtime.InteropServices;

            [ComImport, Guid("00000000-0000-0000-0000-000000000001"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            interface ILegacyWidget { }

            class ComBridge
            {
                object Activate() => Activator.CreateInstance(Type.GetTypeFromCLSID(Guid.Empty)!);
                object CreateOrdinary() => Activator.CreateInstance(typeof(Ordinary));
                object Wrap(IntPtr pointer) => Marshal.GetObjectForIUnknown(pointer);
            }
            """);
        Write("client/ComInterop.vb", """
            Imports System
            Imports System.Runtime.InteropServices

            <ComImport, Guid("00000000-0000-0000-0000-000000000001"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
            Public Interface ILegacyWidget
            End Interface

            Public Class ComBridge
                Public Function Activate() As Object
                    Return marshal.getactiveobject("Legacy.Widget")
                End Function
                Public Function CreateOrdinary() As Object
                    Return Activator.CreateInstance(GetType(Ordinary))
                End Function
                Public Function Wrap(pointer As IntPtr) As Object
                    Return Marshal.GetObjectForIUnknown(pointer)
                End Function
            End Class
            """);

        var inventory = RepositoryScanner.Scan(_root);
        var signals = inventory.ArchitecturePatterns.Signals;
        var categories = signals.Select(signal => signal.Category).ToArray();

        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Factory type"
            && signal.Path == "client/Composition.vb" && signal.Line > 0);
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Factory invocation");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "DI registration");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "DI resolution");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "COM activation");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Native interop");
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "Constructor injection"
            && signal.Path == "client/Injected.cs");
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "Factory delegate"
            && signal.Path == "client/Injected.cs");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Constructor injection"
            && signal.Path == "client/Injected.vb");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Factory delegate"
            && signal.Path == "client/Injected.vb");
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "COM activation"
            && signal.Evidence.Contains("GetTypeFromCLSID", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "COM marshalling"
            && signal.Evidence.Contains("GetObjectForIUnknown", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "COM activation"
            && signal.Evidence.Contains("getactiveobject", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(signals, signal => signal.Category == "COM activation"
            && (signal.Evidence.Contains("typeof(Ordinary)", StringComparison.Ordinal)
                || signal.Evidence.Contains("GetType(Ordinary)", StringComparison.Ordinal)));
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "COM marshalling"
            && signal.Evidence.Contains("GetObjectForIUnknown", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "Native interop"
            && signal.Evidence.Contains("InterfaceType", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Native interop"
            && signal.Evidence.Contains("ComImport", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Evidence.Contains("CreateDirectory", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Path.Contains("Injected", StringComparison.Ordinal)
            && signal.Evidence.Contains("PlainConsumer", StringComparison.Ordinal));
        Assert.All(signals, signal => Assert.Equal("Candidate", signal.EvidenceKind));

        var markdown = MarkdownReport.Render(inventory);
        Assert.Contains("Factory invocation [Candidate] (VB.NET)", markdown);
        Assert.Contains("Visual Basic invocation expressions (unresolved)", markdown);
        Assert.Contains("services.addtransient", markdown);
        Assert.DoesNotContain(categories, category => category == "Confirmed relationship");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var document = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Contains(document.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals").EnumerateArray(),
            signal => signal.GetProperty("EvidenceKind").GetString() == "Candidate"
                && signal.GetProperty("Language").GetString() == "VB.NET");
    }

    [Fact]
    public void Scan_ExcludesVoidCreateMethodsFromFactoryMethodCandidates()
    {
        Write("client/FactoryMethods.cs", """
            public class Widget { }

            public class WidgetProvider
            {
                public void Create() { }
            }

            public class WidgetFactory
            {
                public Widget CreateWidget() => new Widget();
            }
            """);
        Write("client/FactoryMethods.vb", """
            Public Class WidgetProvider
                Public Sub Create()
                End Sub
            End Class

            Public Class WidgetFactory
                Public Function CreateWidget() As Object
                    Return New Object()
                End Function
            End Class
            """);

        var signals = RepositoryScanner.Scan(_root).ArchitecturePatterns.Signals;

        Assert.DoesNotContain(signals, signal => signal.Category == "Factory method"
            && signal.Evidence.Contains("void Create", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Category == "Factory method"
            && signal.Evidence.Contains("Sub Create", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(signals, signal => signal.Category == "Factory method" && signal.Language == "C#"
            && signal.Evidence.Contains("CreateWidget", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Category == "Factory method" && signal.Language == "VB.NET"
            && signal.Evidence.Contains("CreateWidget", StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_ExcludesStringHelpersAndKeepsObjectFactoryMethodsInCSharpAndVisualBasic()
    {
        Write("client/FactoryReturnTypes.cs", """
            public class Product { }
            public static class FactoryMethods
            {
                public static string? ResolveCustomActiveWorkItemPath() => null;
                public static global::System.String? ResolveVersionPath() => null;
                public static string CreateLabel() => "label";
                public static string BuildLabel() => "label";
                public static Product BuildProduct() => new Product();
            }
            """);
        Write("client/FactoryReturnTypes.vb", """
            Public Class Product
            End Class

            Public Class FactoryMethods
                Public Function ResolveOutputPath() As String
                    Return "output"
                End Function

                Public Function CreateDisplayName() As Global.System.String
                    Return "display"
                End Function

                Public Function CreateLabel() As String
                    Return "label"
                End Function

                Public Function BuildLabel() As String
                    Return "label"
                End Function

                Public Function BuildProduct() As Product
                    Return New Product()
                End Function
            End Class
            """);

        using var jsonOutput = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, jsonOutput, error));
        using var report = System.Text.Json.JsonDocument.Parse(jsonOutput.ToString());
        var signals = report.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals")
            .EnumerateArray()
            .Where(signal => signal.GetProperty("Category").GetString() == "Factory method")
            .ToArray();

        Assert.DoesNotContain(signals, signal => signal.GetProperty("Evidence").GetString()!
            .Contains("ResolveCustomActiveWorkItemPath", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.GetProperty("Evidence").GetString()!
            .Contains("ResolveOutputPath", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.GetProperty("Evidence").GetString()!
            .Contains("ResolveVersionPath", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.GetProperty("Evidence").GetString()!
            .Contains("CreateDisplayName", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.GetProperty("Evidence").GetString()!
            .Contains("CreateLabel", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.GetProperty("Evidence").GetString()!
            .Contains("BuildLabel", StringComparison.Ordinal));
        var csharpFactory = Assert.Single(signals, signal => signal.GetProperty("Language").GetString() == "C#");
        Assert.Equal("client/FactoryReturnTypes.cs", csharpFactory.GetProperty("Path").GetString());
        Assert.True(csharpFactory.GetProperty("Line").GetInt32() > 0);
        Assert.Equal("Candidate", csharpFactory.GetProperty("EvidenceKind").GetString());
        Assert.Contains("BuildProduct", csharpFactory.GetProperty("Evidence").GetString());
        var visualBasicFactory = Assert.Single(signals, signal => signal.GetProperty("Language").GetString() == "VB.NET");
        Assert.Equal("client/FactoryReturnTypes.vb", visualBasicFactory.GetProperty("Path").GetString());
        Assert.True(visualBasicFactory.GetProperty("Line").GetInt32() > 0);
        Assert.Equal("Candidate", visualBasicFactory.GetProperty("EvidenceKind").GetString());
        Assert.Contains("BuildProduct", visualBasicFactory.GetProperty("Evidence").GetString());

        using var markdownOutput = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root }, markdownOutput, error));
        var factoryLines = markdownOutput.ToString().Split('\n')
            .Where(line => line.Contains("Factory method [Candidate]", StringComparison.Ordinal))
            .ToArray();
        Assert.DoesNotContain(factoryLines, line => line.Contains("ResolveCustomActiveWorkItemPath", StringComparison.Ordinal));
        Assert.DoesNotContain(factoryLines, line => line.Contains("ResolveOutputPath", StringComparison.Ordinal));
        Assert.DoesNotContain(factoryLines, line => line.Contains("ResolveVersionPath", StringComparison.Ordinal));
        Assert.DoesNotContain(factoryLines, line => line.Contains("CreateDisplayName", StringComparison.Ordinal));
        Assert.DoesNotContain(factoryLines, line => line.Contains("CreateLabel", StringComparison.Ordinal));
        Assert.DoesNotContain(factoryLines, line => line.Contains("BuildLabel", StringComparison.Ordinal));
        Assert.Contains(factoryLines, line => line.Contains("(C#)", StringComparison.Ordinal)
            && line.Contains("FactoryReturnTypes.cs", StringComparison.Ordinal)
            && line.Contains("BuildProduct", StringComparison.Ordinal));
        Assert.Contains(factoryLines, line => line.Contains("(VB.NET)", StringComparison.Ordinal)
            && line.Contains("FactoryReturnTypes.vb", StringComparison.Ordinal)
            && line.Contains("BuildProduct", StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_ExcludesAttributedTestMethodsAndLocatesFactorySignaturesInCSharpAndVisualBasic()
    {
        Write("client/AttributedFactories.cs", """
            public class Product { }
            public class TestMethods
            {
                [System.Obsolete]
                [Xunit.Fact]
                public Product CreateXunitFact() => new Product();
                [Xunit.Theory]
                public Product BuildXunitTheory() => new Product();
                [NUnit.Framework.Test]
                public Product ResolveNUnitTest() => new Product();
                [NUnit.Framework.TestCase("case")]
                public Product CreateNUnitCase() => new Product();
                [NUnit.Framework.TestCaseSource("Cases")]
                public Product BuildNUnitSource() => new Product();
                [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute]
                public Product ResolveMSTestMethod() => new Product();
                [Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethod]
                public Product CreateMSTestData() => new Product();

                /// <summary>Production factory.</summary>
                [System.Obsolete]
                public Product CreateProductionProduct() => new Product();
            }
            """);
        Write("client/AttributedFactories.vb", """
            Public Class Product
            End Class
            Public Class TestMethods
                <System.Obsolete, Xunit.Fact>
                Public Function CreateXunitFact() As Product
                    Return New Product()
                End Function
                <Xunit.TheoryAttribute>
                Public Function BuildXunitTheory() As Product
                    Return New Product()
                End Function
                <NUnit.Framework.Test>
                Public Function ResolveNUnitTest() As Product
                    Return New Product()
                End Function
                <NUnit.Framework.TestCase("case")>
                Public Function CreateNUnitCase() As Product
                    Return New Product()
                End Function
                <NUnit.Framework.TestCaseSourceAttribute("Cases")>
                Public Function BuildNUnitSource() As Product
                    Return New Product()
                End Function
                <Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute>
                Public Function ResolveMSTestMethod() As Product
                    Return New Product()
                End Function
                <Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethod>
                Public Function CreateMSTestData() As Product
                    Return New Product()
                End Function

                ''' <summary>Production factory.</summary>
                <System.Obsolete>
                Public Function CreateProductionProduct() As Product
                    Return New Product()
                End Function
            End Class
            """);

        using var jsonOutput = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, jsonOutput, error));
        using var report = System.Text.Json.JsonDocument.Parse(jsonOutput.ToString());
        var signals = report.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals")
            .EnumerateArray()
            .Where(signal => signal.GetProperty("Category").GetString() == "Factory method")
            .ToArray();
        var excludedNames = new[]
        {
            "CreateXunitFact", "BuildXunitTheory", "ResolveNUnitTest", "CreateNUnitCase",
            "BuildNUnitSource", "ResolveMSTestMethod", "CreateMSTestData"
        };
        foreach (var name in excludedNames)
            Assert.DoesNotContain(signals, signal => signal.GetProperty("Evidence").GetString()!.Contains(name, StringComparison.Ordinal));

        var csharpFactory = Assert.Single(signals, signal => signal.GetProperty("Path").GetString() == "client/AttributedFactories.cs");
        Assert.Equal(22, csharpFactory.GetProperty("Line").GetInt32());
        Assert.Contains("CreateProductionProduct", csharpFactory.GetProperty("Evidence").GetString());
        Assert.DoesNotContain("Obsolete", csharpFactory.GetProperty("Evidence").GetString());
        var visualBasicFactory = Assert.Single(signals, signal => signal.GetProperty("Path").GetString() == "client/AttributedFactories.vb");
        Assert.Equal(35, visualBasicFactory.GetProperty("Line").GetInt32());
        Assert.Contains("CreateProductionProduct", visualBasicFactory.GetProperty("Evidence").GetString());
        Assert.DoesNotContain("Obsolete", visualBasicFactory.GetProperty("Evidence").GetString());

        using var markdownOutput = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root }, markdownOutput, error));
        var factoryLines = markdownOutput.ToString().Split('\n')
            .Where(line => line.Contains("Factory method [Candidate]", StringComparison.Ordinal))
            .ToArray();
        foreach (var name in excludedNames)
            Assert.DoesNotContain(factoryLines, line => line.Contains(name, StringComparison.Ordinal));
        Assert.Contains(factoryLines, line => line.Contains("AttributedFactories.cs:22", StringComparison.Ordinal)
            && line.Contains("CreateProductionProduct", StringComparison.Ordinal));
        Assert.Contains(factoryLines, line => line.Contains("AttributedFactories.vb:35", StringComparison.Ordinal)
            && line.Contains("CreateProductionProduct", StringComparison.Ordinal));
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesInvocationTargetsFromCompilationSymbols()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace Legacy.App
            {
                public class Store { public void Save() { } }
                public class Controller
                {
                    public void Run() { new Store().Save(); }
                    public void Missing() { Unknown(); }
                    public string NameOfStore() { return nameof(Store); }
                }
            }
            """, path: "src/Api.cs", cancellationToken: TestContext.Current.CancellationToken);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = CSharpCompilation.Create("LegacyFixture", new[] { tree }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("LegacyFixture.csproj", (Compilation)compilation) }, 1, 10, _root);

        Assert.Equal(1, analysis.ProjectsLoaded);
        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Equal(1, analysis.UnresolvedInvocationCount);
        Assert.Contains(analysis.ResolvedCalls, call => call.Target.Contains("Legacy.App.Store.Save", StringComparison.Ordinal));
        Assert.Contains(analysis.ResolvedCalls, call => call.Caller.Contains("Legacy.App.Controller.Run", StringComparison.Ordinal));
        Assert.Contains(analysis.UnresolvedCalls, call => call.Expression == "Unknown");
    }

    [Fact]
    public void SemanticAnalyzer_ReconcilesInvocationCountsAndReportsTargetAndCompilerDiagnosticLocations()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace Legacy.App
            {
                public class Store { public void Save() { } }
                public class Controller
                {
                    public void Run(Store store) { store.Save(); }
                    public void Missing() { Unknown(); }
                    public string NameOfStore() { return nameof(Store); }
                }
            }
            """, path: "src/Api.cs", cancellationToken: TestContext.Current.CancellationToken);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = CSharpCompilation.Create("CoverageFixture", new[] { tree }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("CoverageFixture.csproj", (Compilation)compilation) }, 1, 10, _root);

        Assert.Equal(3, analysis.InvocationSyntaxCount);
        Assert.Equal(1, analysis.SkippedInvocationCount);
        Assert.Equal(2, analysis.AnalyzedInvocationCount);
        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Equal(1, analysis.UnresolvedInvocationCount);
        Assert.Equal(analysis.InvocationSyntaxCount,
            analysis.SkippedInvocationCount + analysis.AnalyzedInvocationCount);
        Assert.Equal(analysis.AnalyzedInvocationCount,
            analysis.ResolvedInvocationCount + analysis.UnresolvedInvocationCount);

        var resolved = Assert.Single(analysis.ResolvedCalls);
        Assert.Equal("src/Api.cs", resolved.TargetPath);
        Assert.True(resolved.TargetLine > 0);
        var unresolved = Assert.Single(analysis.UnresolvedCalls);
        Assert.Contains(analysis.UnresolvedReasonCounts ?? Array.Empty<SemanticReasonCount>(), reason => reason.Reason == unresolved.CandidateReason
            && reason.Count == 1);
        Assert.Equal(1, analysis.CompilerErrorCount);
        var compilerError = Assert.Single(analysis.CompilerDiagnostics ?? Array.Empty<SemanticCompilerDiagnostic>());
        Assert.Equal("src/Api.cs", compilerError.Path);
        Assert.Equal("CS0103", compilerError.Code);
        Assert.True(compilerError.Line > 0);
    }

    [Fact]
    public void SemanticAnalyzer_GroupsCompilerErrorsByProjectAndCodeIndependentOfSamplingAndInputOrder()
    {
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var firstTree = CSharpSyntaxTree.ParseText(
            "public class First { public MissingType Value; public void Run() { MissingOne(); MissingTwo(); } }",
            path: Path.Combine(_root, "first", "First.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        var secondTree = CSharpSyntaxTree.ParseText(
            "public class Second { public void Run() { MissingThree(); } }",
            path: Path.Combine(_root, "second", "Second.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        var firstCompilation = CSharpCompilation.Create("First", new[] { firstTree }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var secondCompilation = CSharpCompilation.Create("Second", new[] { secondTree }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var inputs = new (string Project, Compilation Compilation)[]
        {
            ("first/First.csproj", firstCompilation),
            ("second/Second.csproj", secondCompilation)
        };

        var complete = CSharpSemanticAnalyzer.AnalyzeCompilations(inputs, 2, 10, _root);
        var reversed = CSharpSemanticAnalyzer.AnalyzeCompilations(inputs.Reverse(), 2, 10, _root);
        var limited = CSharpSemanticAnalyzer.AnalyzeCompilations(inputs, 2, 1, _root);

        Assert.Equal(4, complete.CompilerErrorCount);
        Assert.Equal(0, complete.CompilerErrorOmittedCount);
        Assert.Equal(3, complete.CompilerDiagnosticGroupCount);
        Assert.Equal(0, complete.CompilerDiagnosticGroupOmittedCount);
        Assert.Equal(new[]
        {
            new SemanticCompilerDiagnosticGroup("first/First.csproj", "CS0103", 2),
            new SemanticCompilerDiagnosticGroup("first/First.csproj", "CS0246", 1),
            new SemanticCompilerDiagnosticGroup("second/Second.csproj", "CS0103", 1)
        }, complete.CompilerDiagnosticGroups);
        Assert.Equal(complete.CompilerDiagnosticGroups, reversed.CompilerDiagnosticGroups);

        Assert.Equal(4, limited.CompilerErrorCount);
        Assert.Equal(3, limited.CompilerErrorOmittedCount);
        Assert.Single(limited.CompilerDiagnostics!);
        Assert.Equal(3, limited.CompilerDiagnosticGroupCount);
        Assert.Equal(2, limited.CompilerDiagnosticGroupOmittedCount);
        Assert.Single(limited.CompilerDiagnosticGroups!);
    }

    [Fact]
    public void CompilerDiagnosticGroups_AreAdditiveInJsonAndVisibleAlongsideMarkdownSamples()
    {
        Directory.CreateDirectory(_root);
        var tree = CSharpSyntaxTree.ParseText(
            "public class Errors { public MissingType Value; public void Run() { MissingOne(); MissingTwo(); } }",
            path: Path.Combine(_root, "src", "Errors.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Errors", new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semantic = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("legacy/Errors.csproj", (Compilation)compilation) }, 1, 10, _root);
        var scanned = RepositoryScanner.Scan(_root, sampleLimit: 10);
        var inventory = scanned with
        {
            CSharp = scanned.CSharp with { Semantic = semantic }
        };

        using var json = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(inventory));
        var root = json.RootElement;
        var semanticJson = root.GetProperty("CSharp").GetProperty("Semantic");
        var groups = semanticJson.GetProperty("CompilerDiagnosticGroups").EnumerateArray().ToArray();
        var codeGroup = Assert.Single(groups, item => item.GetProperty("Code").GetString() == "CS0103");
        var limitedSemantic = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("legacy/Errors.csproj", (Compilation)compilation) }, 1, 1, _root);
        var limitedInventory = inventory with
        {
            CSharp = inventory.CSharp with { Semantic = limitedSemantic }
        };
        var markdown = MarkdownReport.Render(limitedInventory);

        Assert.Equal(1, root.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal(3, semanticJson.GetProperty("CompilerErrorCount").GetInt32());
        Assert.Equal(0, semanticJson.GetProperty("CompilerErrorOmittedCount").GetInt32());
        Assert.Equal(2, semanticJson.GetProperty("CompilerDiagnosticGroupCount").GetInt32());
        Assert.Equal(0, semanticJson.GetProperty("CompilerDiagnosticGroupOmittedCount").GetInt32());
        Assert.Equal("legacy/Errors.csproj", codeGroup.GetProperty("Project").GetString());
        Assert.Equal(2, codeGroup.GetProperty("Count").GetInt32());
        Assert.Equal(3, semanticJson.GetProperty("CompilerDiagnostics").GetArrayLength());
        Assert.Contains("Compiler diagnostic groups: 2 total; 1 group samples omitted", markdown, StringComparison.Ordinal);
        Assert.Contains("`legacy/Errors.csproj` `CS0103`: 2", markdown, StringComparison.Ordinal);
        Assert.Contains("Compiler error total: 3; 2 error samples omitted", markdown, StringComparison.Ordinal);
        Assert.Contains("Compiler error samples:", markdown, StringComparison.Ordinal);
        Assert.True(markdown.IndexOf("Compiler diagnostic groups", StringComparison.Ordinal)
            < markdown.IndexOf("Compiler error samples", StringComparison.Ordinal));
    }

    [Fact]
    public void SemanticAnalyzer_LoadsUnlistedProjectsOnceAndReportsFailedProjectsAndIssueTotals()
    {
        var projectGuid = Guid.Parse("12345678-1234-1234-1234-123456789012").ToString("B");
        Write("Listed.csproj", LegacyProject("Listed", "Listed.cs", projectGuid));
        Write("Listed.cs", "namespace Fixture; public class Listed { public void Run() { Helper(); } public void Helper() { } }");
        Write("Standalone.csproj", LegacyProject("Standalone", "Standalone.cs"));
        Write("Standalone.cs", "namespace Fixture; public class Standalone { public void Run() { Helper(); } public void Helper() { } }");
        Write("Broken.csproj", "<Project><broken></Project>");
        Write("AlsoBroken.csproj", "<Project>");

        var solution = string.Join(Environment.NewLine, new[]
        {
            "Microsoft Visual Studio Solution File, Format Version 12.00",
            "# Visual Studio Version 17",
            "VisualStudioVersion = 17.0.31903.59",
            "MinimumVisualStudioVersion = 10.0.40219.1",
            "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Listed\", \"Listed.csproj\", \"" + projectGuid + "\"",
            "EndProject",
            "Global",
            "EndGlobal"
        });
        Write("A.sln", solution);
        Write("B.sln", solution);

        var analysis = CSharpSemanticAnalyzer.AnalyzeRepository(
            _root,
            new[] { "AlsoBroken.csproj", "Broken.csproj", "Listed.csproj", "Standalone.csproj" },
            new[] { "B.sln", "A.sln" },
            sampleLimit: 1);

        Assert.Equal(4, analysis.ProjectsDiscovered);
        Assert.Equal(4, analysis.ProjectsLoaded);
        Assert.Equal(0, analysis.ProjectsFailed);
        Assert.False(analysis.ProjectCoverageComplete);
        Assert.Equal(2, analysis.ResolvedInvocationCount);
        Assert.True(analysis.WorkspaceIssueCount >= 2);
        Assert.Single(analysis.Issues);
        Assert.DoesNotContain(_root, analysis.Issues[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Listed.csproj", analysis.Issues[0], StringComparison.Ordinal);
        Assert.Equal(analysis.WorkspaceIssueCount - analysis.Issues.Count, analysis.WorkspaceIssueOmittedCount);
        using var semanticJson = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(analysis));
        Assert.Equal(analysis.WorkspaceIssueCount, semanticJson.RootElement.GetProperty("WorkspaceIssueCount").GetInt32());
        Assert.Equal(analysis.Issues[0], semanticJson.RootElement.GetProperty("Issues")[0].GetString());
        Assert.DoesNotContain(_root, semanticJson.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkspaceDiagnosticNormalizer_ProducesRelativePortablePathsAndRedactsExternalPaths()
    {
        var firstRoot = Path.Combine(_root, "checkout-one");
        var secondRoot = Path.Combine(_root, "checkout-two");
        var relativeProject = Path.Combine("legacy", "Framework", "Legacy.csproj");
        var externalPath = Path.Combine(_root, "sdk", "Microsoft.Common.CurrentVersion.targets");
        var firstMessage = $"Project '{Path.Combine(firstRoot, relativeProject)}' failed while reading '{externalPath}': Project does not contain 'Compile' target.";
        var secondMessage = $"Project '{Path.Combine(secondRoot, relativeProject)}' failed while reading '{externalPath}': Project does not contain 'Compile' target.";

        var first = WorkspaceDiagnosticNormalizer.Normalize(firstMessage, firstRoot);
        var second = WorkspaceDiagnosticNormalizer.Normalize(secondMessage, secondRoot);

        Assert.Equal(first, second);
        Assert.Contains("legacy/Framework/Legacy.csproj", first, StringComparison.Ordinal);
        Assert.Contains("[external-path]", first, StringComparison.Ordinal);
        Assert.Contains("Project does not contain 'Compile' target", first, StringComparison.Ordinal);
        Assert.DoesNotContain(firstRoot, first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain('\\', first);
    }

    [Fact]
    public void WorkspaceDiagnosticNormalizer_DoesNotTreatSharedRootPrefixAsContained()
    {
        var root = Path.Combine(_root, "repository");
        var siblingPath = root + "-copy" + Path.DirectorySeparatorChar + "project.csproj";
        var message = $"Failed to load '{siblingPath}'.";

        var normalized = WorkspaceDiagnosticNormalizer.Normalize(message, root);

        Assert.Contains("[external-path]", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("repository-copy", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain(root, normalized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkspaceDiagnosticNormalizer_PreservesUrlsAlongsideAbsolutePaths()
    {
        var root = Path.Combine(_root, "repository");
        var localPath = Path.Combine(root, "src", "Project.csproj");
        var message = $"See https://example.com/build/log and '{localPath}' for details.";

        var normalized = WorkspaceDiagnosticNormalizer.Normalize(message, root);

        Assert.Contains("https://example.com/build/log", normalized, StringComparison.Ordinal);
        Assert.Contains("'src/Project.csproj'", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void SemanticAnalyzer_ExcludesExternalTreesAndTargetLocationsFromRepositoryFindings()
    {
        var repositoryRoot = Path.Combine(_root, "repository");
        Write("repository/src/Entry.cs", """
            namespace Local;
            public class Entry
            {
                public void Run() { new External.Remote().Execute(); }
            }
            """);
        Write("repository/obj/Generated.cs", """
            namespace Local.Generated;
            public class Generated { public void Keep() { } }
            """);
        Write("external/Outside.cs", """
            namespace External;
            public class Remote
            {
                public void Execute() { }
                public void ExternalCaller() { Execute(); }
            }
            """);

        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(repositoryRoot, "src", "Entry.cs")),
                path: Path.Combine(repositoryRoot, "src", "Entry.cs"), cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(repositoryRoot, "obj", "Generated.cs")),
                path: Path.Combine(repositoryRoot, "obj", "Generated.cs"), cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_root, "external", "Outside.cs")),
                path: Path.Combine(_root, "external", "Outside.cs"), cancellationToken: TestContext.Current.CancellationToken)
        };
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = CSharpCompilation.Create("PathBoundaryFixture", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("Fixture.csproj", (Compilation)compilation) }, 1, 100, repositoryRoot);

        Assert.Equal(4, analysis.DeclarationCount);
        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Equal(0, analysis.UnresolvedInvocationCount);
        var declarations = Assert.IsAssignableFrom<IReadOnlyList<CSharpSemanticDeclaration>>(analysis.Declarations);
        Assert.Equal(4, declarations.Count);
        Assert.Contains(declarations, declaration => declaration.Path == "obj/Generated.cs");
        Assert.Contains(declarations, declaration => declaration.Symbol.Contains("Local.Entry..ctor()", StringComparison.Ordinal));
        Assert.Contains(declarations, declaration => declaration.Symbol.Contains("Local.Generated.Generated..ctor()", StringComparison.Ordinal));
        Assert.DoesNotContain(declarations, declaration => declaration.Symbol.Contains("External.Remote..ctor()", StringComparison.Ordinal));
        var call = Assert.Single(analysis.ResolvedCalls, item => item.RelationshipKind == "method");
        Assert.Contains("External.Remote.Execute", call.Target, StringComparison.Ordinal);
        Assert.Null(call.TargetPath);
        Assert.Null(call.TargetLine);
        var externalConstructor = Assert.Single(analysis.ResolvedCalls, item => item.RelationshipKind == "constructor");
        Assert.Contains("External.Remote..ctor()", externalConstructor.Target, StringComparison.Ordinal);
        Assert.Null(externalConstructor.TargetPath);
        Assert.Null(externalConstructor.TargetLine);
        Assert.DoesNotContain(Path.Combine(_root, "external"),
            System.Text.Json.JsonSerializer.Serialize(analysis), StringComparison.OrdinalIgnoreCase);

        var reportedPaths = declarations.Select(declaration => declaration.Project)
            .Concat(declarations.Select(declaration => declaration.Path))
            .Append(call.Project)
            .Append(call.Path)
            .Where(path => path is not null)
            .ToArray();
        Assert.All(reportedPaths, path =>
        {
            Assert.False(Path.IsPathRooted(path));
            Assert.DoesNotContain('\\', path);
            Assert.DoesNotContain(path.Split('/'), segment => segment == "..");
        });

        var externalProjectAnalysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { (Path.Combine(_root, "external", "Outside.csproj"), (Compilation)compilation) },
            1, 100, repositoryRoot);
        Assert.Equal(0, externalProjectAnalysis.ProjectsLoaded);
        Assert.Empty(externalProjectAnalysis.Declarations!);
        Assert.Empty(externalProjectAnalysis.ResolvedCalls);
    }

    [Fact]
    public void SemanticAnalyzer_FormatsNestedCallableIdentities()
    {
        var source = """
            namespace Demo;
            public class Workflow
            {
                public string Register()
                {
                    Func<string> first = () =>
                    {
                        return Create() + Create();
                    };
                    Func<string> second = () => Create();
                    Func<Func<string>> nested = () =>
                    {
                        _ = Create();
                        return () => Create();
                    };
                    Func<string> anonymous = delegate
                    {
                        return Create();
                    };
                    string Local() => Create();
                    return first() + second() + anonymous() + Local();
                }

                private string Create() => "ok";
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(source, path: "src/Workflow.cs",
            cancellationToken: TestContext.Current.CancellationToken);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = CSharpCompilation.Create("NestedCallableFixture", new[] { tree }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("NestedCallableFixture.csproj", (Compilation)compilation) }, 1, 100, _root);

        var calls = analysis.ResolvedCalls.Where(call => call.Target.Contains("Workflow.Create", StringComparison.Ordinal)).ToArray();
        Assert.Equal(7, calls.Length);
        Assert.All(calls, call => Assert.DoesNotContain(".(", call.Caller, StringComparison.Ordinal));
        var lambdas = calls.Where(call => call.Caller.Contains("::<lambda@src/Workflow.cs:", StringComparison.Ordinal))
            .GroupBy(call => call.Caller, StringComparer.Ordinal).ToArray();
        Assert.Equal(4, lambdas.Length);
        Assert.Equal(new[] { 1, 1, 1, 2 }, lambdas.Select(group => group.Count()).OrderBy(count => count));
        Assert.All(lambdas, group => Assert.Contains("Workflow.Register()", group.Key, StringComparison.Ordinal));
        Assert.Contains(lambdas, group => group.Key.Split("::<lambda@", StringSplitOptions.None).Length == 3);
        Assert.Contains(calls, call => call.Caller.Contains("::<anonymous@src/Workflow.cs:", StringComparison.Ordinal));
        var localCall = Assert.Single(calls, call => call.Caller.Contains("::<local global::Demo.Workflow.Local()>", StringComparison.Ordinal));
        var localDeclaration = Assert.Single(analysis.Declarations!, item => item.Symbol.Contains("::<local global::Demo.Workflow.Local()>", StringComparison.Ordinal));
        Assert.Equal(localDeclaration.Symbol, localCall.Caller);
        var localTarget = Assert.Single(analysis.ResolvedCalls, call => call.Target.Contains("::<local global::Demo.Workflow.Local()>", StringComparison.Ordinal));
        Assert.Equal(localDeclaration.Symbol, localTarget.Target);
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesLocalDelegateMethodGroupAndSelectedOverload()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            using System;
            namespace Demo;
            public class Workflow
            {
                public void Save(int value) { }
                public void Save(string value) { }
                public void Run()
                {
                    Action<int> callback = Save;
                    callback(1);
                    callback.Invoke(2);
                }
            }
            """, path: "src/Workflow.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("DelegateMethodGroupFixture", new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("DelegateMethodGroupFixture.csproj", (Compilation)compilation) }, 1, 100, _root);

        Assert.True(analysis.ResolvedInvocationCount == 2, System.Text.Json.JsonSerializer.Serialize(analysis));
        var delegateCalls = analysis.ResolvedCalls.Where(call => call.RelationshipKind == "delegate").ToArray();
        Assert.Equal(2, delegateCalls.Length);
        Assert.All(delegateCalls, call => Assert.Contains("Workflow.Save(int)", call.Target, StringComparison.Ordinal));
        Assert.All(delegateCalls, call => Assert.Equal("src/Workflow.cs", call.TargetPath));
        Assert.All(delegateCalls, call => Assert.Contains("Workflow.Run()", call.Caller, StringComparison.Ordinal));
        Assert.All(delegateCalls, call => Assert.True(call.Line > 0 && call.SourceColumn > 0));
        Assert.DoesNotContain(delegateCalls, call => call.Target.Contains("Save(string)", StringComparison.Ordinal));
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesLocalDelegateLambdaTargetWithoutChangingInvocationCount()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            using System;
            namespace Demo;
            public class Workflow
            {
                private int Convert(int value) => value;
                public int Run()
                {
                    Func<int, int> callback = value => Convert(value);
                    return callback(1);
                }
            }
            """, path: "src/Workflow.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("DelegateLambdaFixture", new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("DelegateLambdaFixture.csproj", (Compilation)compilation) }, 1, 100, _root);

        Assert.Equal(2, analysis.ResolvedInvocationCount);
        var lambdaBodyCall = Assert.Single(analysis.ResolvedCalls,
            call => call.Target.Contains("Workflow.Convert(int)", StringComparison.Ordinal));
        Assert.Contains("::<lambda@src/Workflow.cs:", lambdaBodyCall.Caller, StringComparison.Ordinal);
        var delegateCall = Assert.Single(analysis.ResolvedCalls, call => call.RelationshipKind == "delegate");
        Assert.Equal(lambdaBodyCall.Caller, delegateCall.Target);
        Assert.Contains("Workflow.Run()", delegateCall.Caller, StringComparison.Ordinal);
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesVisualBasicLambdaCallerIdentity()
    {
        var tree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText("""
            Namespace Legacy.App
                Public Class Workflow
                    Public Function Register() As String
                        Dim factory As Func(Of String) = Function() Create()
                        Return factory()
                    End Function
                    Private Function Create() As String
                        Return "ok"
                    End Function
                End Class
            End Namespace
            """, path: "src/Workflow.vb", cancellationToken: TestContext.Current.CancellationToken);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.Create("NestedCallableVbFixture",
            new[] { tree }, references,
            new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("NestedCallableVbFixture.vbproj", (Compilation)compilation) }, 1, 100, _root);

        var call = Assert.Single(analysis.ResolvedCalls, item => item.Target.Contains("Workflow.Create", StringComparison.Ordinal));
        Assert.Contains("Workflow.Register", call.Caller, StringComparison.Ordinal);
        Assert.Contains("::<lambda@src/Workflow.vb:", call.Caller, StringComparison.Ordinal);
        Assert.DoesNotContain(".(", call.Caller, StringComparison.Ordinal);
        Assert.Equal(1, analysis.ResolvedInvocationCount);
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesVisualBasicLocalDelegateMethodGroup()
    {
        var tree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText("""
            Imports System
            Namespace Legacy.App
                Public Class Workflow
                    Public Sub Save(value As Integer)
                    End Sub
                    Public Sub Save(value As String)
                    End Sub
                    Public Sub Run()
                        Dim callback As Action(Of Integer) = AddressOf Save
                        callback(1)
                    End Sub
                End Class
            End Namespace
            """, path: "src/Workflow.vb", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.Create("DelegateMethodGroupVbFixture",
            new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("DelegateMethodGroupVbFixture.vbproj", (Compilation)compilation) }, 1, 100, _root);

        Assert.Equal(1, analysis.ResolvedInvocationCount);
        var call = Assert.Single(analysis.ResolvedCalls, item => item.RelationshipKind == "delegate");
        Assert.Contains("Workflow.Save(Integer)", call.Target, StringComparison.Ordinal);
        Assert.Equal("src/Workflow.vb", call.TargetPath);
        Assert.Contains("Workflow.Run", call.Caller, StringComparison.Ordinal);
    }

    [Fact]
    public void SemanticAnalyzer_LeavesReassignedCapturedAndEscapedDelegateTargetsUnresolved()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            using System;
            namespace Demo;
            public class Workflow
            {
                private Action field = () => { };
                private static void Consume(Action callback) { }
                public void First() { }
                public void Second() { }
                public void Run(Action external)
                {
                    Action reassigned = First;
                    reassigned = Second;
                    reassigned();
                    Action captured = First;
                    Action nested = () => captured();
                    external();
                    field();
                    Action escaped = First;
                    Consume(escaped);
                    escaped();
                }
            }
            """, path: "src/Workflow.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("UnsafeDelegateFixture", new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("UnsafeDelegateFixture.csproj", (Compilation)compilation) }, 1, 100, _root);

        Assert.Equal(5, analysis.UnresolvedDelegateTargetCount);
        Assert.DoesNotContain(analysis.ResolvedCalls, call => call.RelationshipKind == "delegate");
        Assert.Equal(5, analysis.UnresolvedDelegateCalls.Count);
    }

    [Fact]
    public void SemanticAnalyzer_RedactsExternalDelegateTargetPathsAndIsDeterministic()
    {
        var externalPath = Path.Combine(Path.GetTempPath(), "brownpants-external-delegate-target.cs");
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace External; public class Target { public void Run() { } }",
                path: externalPath, cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("""
                using System;
                namespace Demo;
                public class Workflow
                {
                    public void Run()
                    {
                        Action callback = new External.Target().Run;
                        callback();
                    }
                }
                """, path: "src/Workflow.cs", cancellationToken: TestContext.Current.CancellationToken)
        };
        var compilation = CSharpCompilation.Create("ExternalDelegateFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var input = new[] { ("ExternalDelegateFixture.csproj", (Compilation)compilation) };
        var first = CSharpSemanticAnalyzer.AnalyzeCompilations(input, 1, 100, _root);
        var second = CSharpSemanticAnalyzer.AnalyzeCompilations(input, 1, 100, _root);
        var delegateCall = Assert.Single(first.ResolvedCalls, call => call.RelationshipKind == "delegate");
        Assert.Null(delegateCall.TargetPath);
        Assert.Null(delegateCall.TargetLine);
        var firstJson = System.Text.Json.JsonSerializer.Serialize(first);
        Assert.DoesNotContain(externalPath, firstJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(firstJson, System.Text.Json.JsonSerializer.Serialize(second));
    }

    [Fact]
    public void SemanticAnalyzer_MapsImplicitAndPrimaryConstructorsToTheirSourceTypes()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace Demo; public class ImplicitTarget { }", path: "src/ImplicitTarget.cs",
                cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class PrimaryTarget(int id) { public int Id => id; }", path: "src/PrimaryTarget.cs",
                cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class Caller { public ImplicitTarget CreateImplicit() => new ImplicitTarget(); public PrimaryTarget CreatePrimary() => new PrimaryTarget(42); }",
                path: "src/Caller.cs", cancellationToken: TestContext.Current.CancellationToken)
        };
        var compilation = CSharpCompilation.Create("SourceBackedConstructorsFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("SourceBackedConstructorsFixture.csproj", (Compilation)compilation) }, 1, 100, _root);
        var repeated = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("SourceBackedConstructorsFixture.csproj", (Compilation)compilation) }, 1, 100, _root);

        var constructors = analysis.ResolvedCalls.Where(call => call.RelationshipKind == "constructor").ToArray();
        Assert.Equal(2, constructors.Length);
        Assert.Equal(analysis.Declarations, repeated.Declarations);
        Assert.Equal(analysis.ResolvedCalls, repeated.ResolvedCalls);
        foreach (var call in constructors)
        {
            var declaration = Assert.Single(analysis.Declarations!, item => item.Symbol == call.Target);
            Assert.Equal(call.TargetPath, declaration.Path);
            Assert.Equal(call.TargetLine, declaration.Line);
        }

        var implicitConstructor = Assert.Single(constructors, call => call.Target.Contains("ImplicitTarget..ctor()", StringComparison.Ordinal));
        Assert.Equal("src/ImplicitTarget.cs", implicitConstructor.TargetPath);
        var primaryConstructor = Assert.Single(constructors, call => call.Target.Contains("PrimaryTarget..ctor(int)", StringComparison.Ordinal));
        Assert.Equal("src/PrimaryTarget.cs", primaryConstructor.TargetPath);
        Assert.DoesNotContain(analysis.Declarations!, declaration => declaration.Symbol.Contains("ImplicitTarget..ctor()", StringComparison.Ordinal)
            && declaration.Path != "src/ImplicitTarget.cs");
    }

    [Fact]
    public void SemanticAnalyzer_MapsVisualBasicImplicitConstructorToItsSourceType()
    {
        var tree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText("""
            Namespace Legacy.App
                Public Class ImplicitTarget
                End Class
                Public Class Caller
                    Public Function Create() As ImplicitTarget
                        Return New ImplicitTarget()
                    End Function
                End Class
            End Namespace
            """, path: "src/ImplicitTarget.vb", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.Create("SourceBackedVbFixture",
            new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("SourceBackedVbFixture.vbproj", (Compilation)compilation) }, 1, 100, _root);

        var call = Assert.Single(analysis.ResolvedCalls, item => item.RelationshipKind == "constructor");
        var declaration = Assert.Single(analysis.Declarations!, item => item.Symbol == call.Target);
        Assert.Equal("src/ImplicitTarget.vb", declaration.Path);
        Assert.Equal(call.TargetPath, declaration.Path);
        Assert.Equal(call.TargetLine, declaration.Line);
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesVisualBasicInvocationTargets()
    {
        var tree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText("""
            Namespace Legacy.App
                Public Class Store
                    Public Sub Save()
                    End Sub
                End Class
                Public Class Controller
                    Public Sub Run()
                        Dim store As New Store()
                        store.Save()
                    End Sub
                End Class
            End Namespace
            """, path: "src/Api.vb", cancellationToken: TestContext.Current.CancellationToken);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.Create("LegacyFixture",
            new[] { tree }, references,
            new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("LegacyFixture.vbproj", (Compilation)compilation) }, 1, 10, _root);

        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Equal(1, analysis.ResolvedConstructorCount);
        Assert.Contains(analysis.ResolvedCalls, call => call.Target.Contains("Legacy.App.Store.Save", StringComparison.Ordinal));
        Assert.Contains(analysis.ResolvedCalls, call => call.RelationshipKind == "constructor"
            && call.Target.Contains("Legacy.App.Store..ctor()", StringComparison.Ordinal));
        Assert.Contains(analysis.ResolvedCalls, call => call.Caller.Contains("Legacy.App.Controller.Run", StringComparison.Ordinal));
        Assert.Equal(2, analysis.ImpactGraphCalls!.Count);
        using var semanticJson = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(analysis));
        Assert.False(semanticJson.RootElement.TryGetProperty("ImpactGraphCalls", out _));
        Assert.False(semanticJson.RootElement.TryGetProperty("ImpactGraphOmittedCount", out _));
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesConstructorOverloadsTargetTypedCreationAndChaining()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace Legacy.App;
            public class Store
            {
                public Store(int id) { }
                public Store(string name) { }
                public Store() : this(1) { }
            }
            public class StoreFactory
            {
                public Store Create() => new Store("primary");
                public Store CreateDefault() => new();
                public Store CreateAmbiguous() => new Store(default);
            }
            """, path: "src/Store.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("ConstructorFixture", new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("ConstructorFixture.csproj", (Compilation)compilation) }, 1, 20, _root);

        Assert.Equal(4, analysis.ConstructorSyntaxCount);
        Assert.Equal(3, analysis.ResolvedConstructorCount);
        Assert.Equal(1, analysis.UnresolvedConstructorCount);
        Assert.Equal(0, analysis.ResolvedInvocationCount);
        var constructors = analysis.ResolvedCalls.Where(call => call.RelationshipKind == "constructor").ToArray();
        Assert.Equal(3, constructors.Length);
        Assert.Contains(constructors, call => call.Target.Contains("Store..ctor(string)", StringComparison.Ordinal));
        Assert.Contains(constructors, call => call.Target.Contains("Store..ctor(int)", StringComparison.Ordinal));
        Assert.All(constructors, call => Assert.Equal("src/Store.cs", call.TargetPath));
        Assert.All(constructors, call => Assert.True(call.TargetLine > 0));
        var unresolved = Assert.Single(analysis.UnresolvedCalls, call => call.RelationshipKind == "constructor");
        Assert.Contains(unresolved.CandidateReason, new[] { "OverloadResolutionFailure", "Ambiguous" });
        Assert.NotEmpty(unresolved.CandidateTargets);
    }

    [Fact]
    public void SemanticAnalyzer_LoadsClassicNetFrameworkProjectWithoutBuildingIt()
    {
        Write("Legacy.csproj", """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
              <PropertyGroup>
                <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
                <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
                <ProjectGuid>{12345678-1234-1234-1234-123456789012}</ProjectGuid>
                <OutputType>Library</OutputType>
                <RootNamespace>LegacyFixture</RootNamespace>
                <AssemblyName>LegacyFixture</AssemblyName>
                <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
                <LangVersion>7.3</LangVersion>
              </PropertyGroup>
              <ItemGroup>
                <Reference Include="System" />
                <Compile Include="Legacy.cs" />
              </ItemGroup>
            </Project>
            """);
        Write("Legacy.cs", """
            namespace LegacyFixture
            {
                public class WidgetFactory { public Widget Create() { return new Widget(); } }
                public class Widget { }
                public class Consumer { public Widget Run(WidgetFactory factory) { return factory.Create(); } }
            }
            """);

        var analysis = CSharpSemanticAnalyzer.AnalyzeRepository(
            _root, new[] { "Legacy.csproj" }, Array.Empty<string>(), 20);

        Assert.Equal(1, analysis.ProjectsDiscovered);
        Assert.Equal(1, analysis.ProjectsLoaded);
        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Contains(analysis.ResolvedCalls, call => call.Target.Contains("LegacyFixture.WidgetFactory.Create", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static string LegacyProject(string name, string sourcePath, string? projectGuid = null) => $"""
        <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
          <PropertyGroup>
            <ProjectGuid>{projectGuid ?? Guid.NewGuid().ToString("B")}</ProjectGuid>
            <OutputType>Library</OutputType>
            <RootNamespace>Fixture</RootNamespace>
            <AssemblyName>{name}</AssemblyName>
            <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
            <LangVersion>7.3</LangVersion>
          </PropertyGroup>
          <ItemGroup>
            <Reference Include="System" />
            <Compile Include="{sourcePath}" />
          </ItemGroup>
        </Project>
        """;

    private static string ToCrLf(string source)
    {
        var normalized = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return string.Join("\r\n", normalized.Split('\n'));
    }
}
