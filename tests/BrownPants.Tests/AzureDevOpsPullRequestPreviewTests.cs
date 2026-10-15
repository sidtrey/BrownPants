using System.Text.Json;
using BrownPants.Cli;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class AzureDevOpsPullRequestPreviewTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-ado-preview", Guid.NewGuid().ToString("N"));
    private const string Feature = "specs/feature-a";

    [Fact]
    public void Analyze_BuildsCompletePreviewAndQaHandoffFromExistingEvidence()
    {
        WriteFixture(criteriaMappingComplete: true, baselineValid: true);
        var trace = new AzureDevOpsTraceReport(1, Feature, [], new[]
        {
            new AzureDevOpsDeliveryReference("Build", "88", "Build 88", "succeeded", "https://dev.azure.com/example/build/88", "https://dev.azure.com/example/workitems/42", "direct")
        }, [], true);

        var preview = PullRequestPreviewAnalyzer.Analyze(_root, Feature, "feature/work", "main", "repo-id", trace);

        Assert.True(preview.CriteriaBaselineValid);
        Assert.True(preview.IsComplete);
        Assert.Equal("repo-id", preview.Repository);
        Assert.Equal(2, preview.QaHandoff.Count);
        Assert.All(preview.QaHandoff, item => Assert.NotEmpty(item.VerificationCommands));
        Assert.Contains("Build 88", preview.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_MarksChangedCriteriaMissingGuidanceAndIncompleteEvidence()
    {
        WriteFixture(criteriaMappingComplete: false, baselineValid: false);

        var preview = PullRequestPreviewAnalyzer.Analyze(_root, Feature, "feature/work", "main", "repo-id");

        Assert.False(preview.CriteriaBaselineValid);
        Assert.False(preview.IsComplete);
        Assert.Contains(preview.Gaps, item => item.Contains("changed", StringComparison.Ordinal));
        Assert.Contains(preview.QaHandoff, item => item.Gaps.Count > 0);
        Assert.Contains(preview.Tasks, task => task.Status == "Incomplete");
    }

    [Fact]
    public void Renderer_ProducesReviewableMarkdownAndDetectsEditsBeforeSubmission()
    {
        WriteFixture(criteriaMappingComplete: true, baselineValid: true);
        var preview = PullRequestPreviewAnalyzer.Analyze(_root, Feature, "feature/work", "main", "repo-id");

        var rendered = PullRequestPreviewRenderer.Render(preview);
        var loaded = PullRequestPreviewRenderer.Read(rendered);

        Assert.True(loaded.IntegrityValid);
        Assert.Equal("feature/work", loaded.Metadata.SourceBranch);
        Assert.Contains("QA knowledge transfer", loaded.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Documented verification commands", loaded.Body, StringComparison.Ordinal);
        Assert.False(PullRequestPreviewRenderer.Read(rendered.Replace("QA knowledge transfer", "QA guidance", StringComparison.Ordinal)).IntegrityValid);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("feature/../main")]
    public void Analyze_RejectsUnsafeFeaturePathsOrBranches(string invalidValue)
    {
        WriteFixture(criteriaMappingComplete: true, baselineValid: true);

        Assert.Throws<ArgumentException>(() => PullRequestPreviewAnalyzer.Analyze(_root, invalidValue, "feature/work", "main", "repo-id"));
    }

    [Fact]
    public void Analyze_RejectsInvalidBranchName()
    {
        WriteFixture(criteriaMappingComplete: true, baselineValid: true);
        Assert.Throws<ArgumentException>(() => PullRequestPreviewAnalyzer.Analyze(_root, Feature, "bad branch", "main", "repo-id"));
    }

    [Fact]
    public void Program_PreviewRunsOfflineAndCreatesRepositoryConfinedOutput()
    {
        WriteFixture(criteriaMappingComplete: true, baselineValid: true);
        var configDirectory = Path.Combine(_root, ".brownpants");
        Directory.CreateDirectory(configDirectory);
        File.WriteAllText(Path.Combine(configDirectory, "azure-devops.json"), """
            { "schemaVersion": 1, "organizationUrl": "https://dev.azure.com/example", "project": "p", "repository": "repo-id", "authenticationScheme": "bearer", "features": {} }
            """);
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = Program.Run(new[] { "devops", "pr", "preview", _root, "--feature", Feature, "--source", "feature/work", "--target", "main", "--output", ".brownpants/preview.md" }, output, error);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(configDirectory, "preview.md")));
        Assert.Contains("Created PR preview", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void Program_PreviewRejectsOutputOutsideRepository()
    {
        WriteFixture(criteriaMappingComplete: true, baselineValid: true);
        var configDirectory = Path.Combine(_root, ".brownpants");
        Directory.CreateDirectory(configDirectory);
        File.WriteAllText(Path.Combine(configDirectory, "azure-devops.json"), """
            { "schemaVersion": 1, "organizationUrl": "https://dev.azure.com/example", "project": "p", "repository": "repo-id", "authenticationScheme": "bearer", "features": {} }
            """);
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = Program.Run(new[] { "devops", "pr", "preview", _root, "--feature", Feature, "--source", "feature/work", "--target", "main", "--output", "../outside-preview.md" }, output, error);

        Assert.Equal(1, exitCode);
        Assert.Contains("remain inside", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    private void WriteFixture(bool criteriaMappingComplete, bool baselineValid)
    {
        var featureDirectory = Path.Combine(_root, Feature.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(featureDirectory);
        File.WriteAllText(Path.Combine(featureDirectory, "src.txt"), "source evidence");
        var spec = """
            # Feature Specification: Preview Sample

            ## User Scenarios & Testing

            ### Acceptance

            1. **AC-001** — Given a service, when called, then it returns a result.
            2. **AC-002** — Given an input, when validated, then invalid input is rejected.

            ## Success Criteria

            - **SC-001**: All source evidence is deterministic.
            """;
        File.WriteAllText(Path.Combine(featureDirectory, "spec.md"), spec);
        var digest = PullRequestPreviewAnalyzer.ComputeCriteriaDigest(spec);
        File.WriteAllText(Path.Combine(featureDirectory, "acceptance-criteria.sha256"), (baselineValid ? digest : new string('0', 64)) + "  spec.md\n");
        File.WriteAllText(Path.Combine(featureDirectory, "tasks.md"), """
            # Tasks

            - [x] T001 first verification task
            """ + (baselineValid ? "\n- [x] T002 additional verification task\n" : "\n- [ ] T002 pending validation task\n"));
        var criteria = criteriaMappingComplete ? new[] { "AC-001", "AC-002" } : new[] { "AC-001" };
        var ledger = new
        {
            schemaVersion = 1,
            tasks = new[]
            {
                new { taskId = "T001", criterionIds = criteria, verificationCommand = "dotnet test --filter PreviewTests", evidencePaths = new[] { Feature + "/src.txt" }, result = "passed" },
                new { taskId = "T002", criterionIds = new[] { "AC-001" }, verificationCommand = "dotnet test --filter InputTests", evidencePaths = new[] { baselineValid ? Feature + "/src.txt" : Feature + "/missing.txt" }, result = baselineValid ? "passed" : "planned" }
            }
        };
        File.WriteAllText(Path.Combine(featureDirectory, "acceptance-evidence.json"), JsonSerializer.Serialize(ledger));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
