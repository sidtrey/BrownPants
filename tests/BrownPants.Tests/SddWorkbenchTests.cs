using BrownPants.Cli;
using System.Text.Json;

namespace BrownPants.Tests;

public sealed class SddWorkbenchTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-workbench-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void WorkbenchCommand_RendersActiveFeatureSummariesAndSafeOfflineHtml()
    {
        CreateFeature();
        var sourceSnapshot = SnapshotSourceFiles();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(new[] { "workbench", _root }, output, error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        var report = File.ReadAllText(Path.Combine(_root, "TestResults", "brownpants-workbench.html"));
        Assert.Contains("Local SDD Review Workbench", report);
        Assert.Contains("A product owner reviews the checkout flow", report);
        Assert.Contains("AC-001", report);
        Assert.Contains("SC-001", report);
        Assert.Contains("1 of 2", report);
        Assert.Contains("Tasks recorded complete", report);
        Assert.Contains("T001", report);
        Assert.Contains("Evidence entries are recorded", report);
        Assert.Contains("baseline is invalid", report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;alert", report);
        Assert.DoesNotContain("<script>alert('x')</script>", report);
        Assert.Contains("Blob", report);
        Assert.Contains("JSON.stringify", report);
        Assert.DoesNotContain("<script src=", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<link rel=\"stylesheet\" href=", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fetch(", report, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(sourceSnapshot, SnapshotSourceFiles());
    }

    [Fact]
    public void WorkbenchCommand_UsesExplicitFeatureAndProtectsExistingOutput()
    {
        CreateFeature("specs/first");
        CreateFeature("specs/second");
        var sourceSpec = Path.Combine(_root, "specs", "first", "spec.md");
        var originalSpec = File.ReadAllText(sourceSpec);
        using var sourceOutput = new StringWriter();
        using var sourceError = new StringWriter();
        var sourceOverwrite = Program.Run(new[] { "workbench", _root, "--feature", "specs/first", "--output", "specs/first/spec.md", "--overwrite" },
            sourceOutput, sourceError);
        Assert.NotEqual(0, sourceOverwrite);
        Assert.Equal(originalSpec, File.ReadAllText(sourceSpec));

        var outputPath = Path.Combine(_root, "review.html");
        File.WriteAllText(outputPath, "team-owned report");
        using var error = new StringWriter();

        var refused = Program.Run(new[] { "workbench", _root, "--feature", "specs/first", "--output", "review.html" },
            TextWriter.Null, error);

        Assert.Equal(1, refused);
        Assert.Contains("--overwrite", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("team-owned report", File.ReadAllText(outputPath));

        using var successfulOutput = new StringWriter();
        using var successfulError = new StringWriter();
        var replaced = Program.Run(new[] { "workbench", _root, "--feature", "specs/first", "--output", "review.html", "--overwrite" },
            successfulOutput, successfulError);

        Assert.Equal(0, replaced);
        Assert.Empty(successfulError.ToString());
        Assert.Contains("Checkout Feature", File.ReadAllText(outputPath));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("missing-feature")]
    public void WorkbenchCommand_RejectsInvalidFeatureWithoutCreatingReport(string featurePath)
    {
        CreateFeature();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(new[] { "workbench", _root, "--feature", featurePath }, output, error);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("feature", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(_root, "TestResults", "brownpants-workbench.html")));
    }

    [Fact]
    public void WorkbenchCommand_RejectsMissingActiveFeatureAndEscapingOutput()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, ".specify-feature.json"), "{}");
        using var noActiveOutput = new StringWriter();
        using var noActiveError = new StringWriter();
        var noActive = Program.Run(new[] { "workbench", _root }, noActiveOutput, noActiveError);
        Assert.NotEqual(0, noActive);
        Assert.Contains("active", noActiveError.ToString(), StringComparison.OrdinalIgnoreCase);

        CreateFeature();
        using var escapingOutput = new StringWriter();
        using var escapingError = new StringWriter();
        var escaping = Program.Run(new[] { "workbench", _root, "--output", "../outside.html" }, escapingOutput, escapingError);
        Assert.NotEqual(0, escaping);
        Assert.Contains("outside", escapingError.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(_root, "TestResults", "brownpants-workbench.html")));
    }

    private void CreateFeature(string featurePath = "specs/current")
    {
        var feature = Path.Combine(_root, featurePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(feature);
        Directory.CreateDirectory(Path.Combine(_root, ".specify"));
        File.WriteAllText(Path.Combine(_root, ".specify", "feature.json"),
            JsonSerializer.Serialize(new { feature_directory = featurePath.Replace('/', '\\') }));
        File.WriteAllText(Path.Combine(feature, "spec.md"), """
            # Checkout Feature

            ### User Story 1 - Review a checkout flow (Priority: P1)

            A product owner reviews the checkout flow.

            **Acceptance Scenarios**:

            1. **AC-001** Given a cart, when checkout runs, then show the total.
            2. **AC-002** Given an invalid cart, when checkout runs, then show the error.

            ## Success Criteria

            - **SC-001**: User reviews the result in under one minute.

            ## Hostile content

            <script>alert('x')</script>
            """);
        File.WriteAllText(Path.Combine(feature, "tasks.md"), """
            - [X] T001 [US1] Add checkout acceptance in tests/CheckoutTests.cs
            - [ ] T002 [US1] Implement checkout in src/Checkout.cs
            """);
        File.WriteAllText(Path.Combine(feature, "acceptance-evidence.json"), """
            {
              "schemaVersion": 1,
              "tasks": [
                { "taskId": "T001", "criterionIds": ["AC-001"], "verificationCommand": "dotnet test", "evidencePaths": ["tests/CheckoutTests.cs"], "result": "passed" }
              ]
            }
            """);
        File.WriteAllText(Path.Combine(feature, "verification.md"), "# Verification\n\nManual QA evidence pending.");
        File.WriteAllText(Path.Combine(feature, "acceptance-criteria.sha256"), "missing baseline for fixture");
    }

    private string SnapshotSourceFiles()
    {
        return string.Join("\n", Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "TestResults" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => Path.GetRelativePath(_root, path) + ":" + Convert.ToBase64String(File.ReadAllBytes(path))));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
