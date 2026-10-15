using System.Text.Json;
using BrownPants.Cli;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class PullRequestPreviewCommandTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-generic-pr", Guid.NewGuid().ToString("N"));
    private const string Feature = "specs/feature-a";

    [Fact]
    public void Program_PreviewWorksWithoutAzureConfigurationOrCredentials()
    {
        WriteFixture();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = Program.Run(["pr", "preview", _root, "--feature", Feature, "--repository", "sidtrey/BrownPants",
            "--source", "feature/work", "--target", "main", "--output", "artifacts/pr-preview.md"], output, error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.True(File.Exists(Path.Combine(_root, "artifacts", "pr-preview.md")));
        var saved = PullRequestPreviewRenderer.Read(File.ReadAllText(Path.Combine(_root, "artifacts", "pr-preview.md")));
        Assert.True(saved.IntegrityValid);
        Assert.Equal("sidtrey/BrownPants", saved.Metadata.Repository);
        Assert.False(File.Exists(Path.Combine(_root, ".brownpants", "azure-devops.json")));
    }

    [Fact]
    public void Program_PreviewPreservesEvidenceGaps()
    {
        WriteFixture(baselineValid: false, taskComplete: false);
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = Program.Run(["pr", "preview", _root, "--feature", Feature, "--repository", "sidtrey/BrownPants",
            "--source", "feature/work", "--target", "main"], output, error);

        Assert.Equal(1, exitCode);
        Assert.Contains("Incomplete", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void Program_PreviewRejectsOutputOutsideRepository()
    {
        WriteFixture();
        var error = new StringWriter();
        var exitCode = Program.Run(["pr", "preview", _root, "--feature", Feature, "--repository", "sidtrey/BrownPants",
            "--source", "feature/work", "--target", "main", "--output", "../outside.md"], TextWriter.Null, error);

        Assert.Equal(1, exitCode);
        Assert.Contains("inside", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Program_PreviewRejectsOutputFileSymbolicLinkWhenSupported()
    {
        WriteFixture();
        Directory.CreateDirectory(Path.Combine(_root, "artifacts"));
        var outsidePath = Path.Combine(Path.GetTempPath(), "brownpants-preview-target-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(outsidePath, "preserve this content");
        try
        {
            File.CreateSymbolicLink(Path.Combine(_root, "artifacts", "linked.md"), outsidePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException or ArgumentException)
        {
            File.Delete(outsidePath);
            return;
        }

        try
        {
            var error = new StringWriter();
            var exitCode = Program.Run(["pr", "preview", _root, "--feature", Feature, "--repository", "sidtrey/BrownPants",
                "--source", "feature/work", "--target", "main", "--output", "artifacts/linked.md", "--overwrite"], TextWriter.Null, error);

            Assert.Equal(1, exitCode);
            Assert.Equal("preserve this content", File.ReadAllText(outsidePath));
            Assert.Contains("symbolic link", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(outsidePath);
        }
    }

    [Fact]
    public void Program_PreviewRejectsDanglingOutputSymbolicLinkWhenSupported()
    {
        WriteFixture();
        Directory.CreateDirectory(Path.Combine(_root, "artifacts"));
        var outsidePath = Path.Combine(Path.GetTempPath(), "brownpants-dangling-target-" + Guid.NewGuid().ToString("N") + ".md");
        var linkPath = Path.Combine(_root, "artifacts", "linked.md");
        try
        {
            File.CreateSymbolicLink(linkPath, outsidePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException or ArgumentException)
        {
            return;
        }

        var error = new StringWriter();
        var exitCode = Program.Run(["pr", "preview", _root, "--feature", Feature, "--repository", "sidtrey/BrownPants",
            "--source", "feature/work", "--target", "main", "--output", "artifacts/linked.md", "--overwrite"], TextWriter.Null, error);

        Assert.Equal(1, exitCode);
        Assert.False(File.Exists(outsidePath));
        Assert.Contains("symbolic link", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private void WriteFixture(bool baselineValid = true, bool taskComplete = true)
    {
        var featureDirectory = Path.Combine(_root, Feature.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(featureDirectory);
        File.WriteAllText(Path.Combine(featureDirectory, "evidence.txt"), "verified source evidence");
        var spec = """
            # Feature Specification: Preview Sample

            ## User Scenarios & Testing

            1. **AC-001** — Given a service, when called, then it returns a result.

            ## Success Criteria

            - **SC-001**: All source evidence is deterministic.
            """;
        File.WriteAllText(Path.Combine(featureDirectory, "spec.md"), spec);
        var digest = PullRequestPreviewAnalyzer.ComputeCriteriaDigest(spec);
        File.WriteAllText(Path.Combine(featureDirectory, "acceptance-criteria.sha256"), (baselineValid ? digest : new string('0', 64)) + "  spec.md\n");
        File.WriteAllText(Path.Combine(featureDirectory, "tasks.md"), "# Tasks\n\n- [" + (taskComplete ? "x" : " ") + "] T001 Verify result\n");
        var ledger = new
        {
            schemaVersion = 1,
            tasks = new[]
            {
                new { taskId = "T001", criterionIds = new[] { "AC-001", "SC-001" }, verificationCommand = "dotnet test", evidencePaths = new[] { Feature + "/evidence.txt" }, result = taskComplete ? "passed" : "planned" }
            }
        };
        File.WriteAllText(Path.Combine(featureDirectory, "acceptance-evidence.json"), JsonSerializer.Serialize(ledger));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
