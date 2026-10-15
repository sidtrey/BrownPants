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

    [Fact]
    public void WorkbenchCommand_RendersUniqueOpenSpecChangeAndPreservesInputs()
    {
        CreateOpenSpecChange("openspec/changes/add-checkout", "checkout");
        var sourceSnapshot = SnapshotSourceFiles();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(new[] { "workbench", _root, "--engine", "openspec" }, output, error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        var report = File.ReadAllText(Path.Combine(_root, "TestResults", "brownpants-workbench.html"));
        Assert.Contains("OpenSpec Change: add-checkout", report);
        Assert.Contains("Why this is changing", report);
        Assert.Contains("Source: <code>openspec/changes/add-checkout/proposal.md</code>", report);
        Assert.Contains("Refund checkout", report);
        Assert.Contains("Successful refund", report);
        Assert.Contains("openspec/changes/add-checkout/specs/checkout/spec.md#requirement-001", report);
        Assert.Contains("openspec/changes/add-checkout/specs/shared/spec.md#requirement-001", report);
        Assert.Contains("data-note-kind=\"requirement\" data-note-id=\"openspec/changes/add-checkout/specs/shared/spec.md#requirement-001\"", report);
        var noteIds = report.Split("data-note-id=\"", StringSplitOptions.None).Skip(1).Select(value => value.Split('"')[0]).ToArray();
        Assert.Equal(noteIds.Length, noteIds.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("1 of 3", report);
        Assert.Contains("1.1", report);
        Assert.Contains("Source: <code>openspec/changes/add-checkout/tasks.md</code>", report);
        Assert.Contains("openspec/changes/add-checkout/design.md", report);
        Assert.Contains("&lt;script&gt;alert", report);
        Assert.DoesNotContain("<script>alert('x')</script>", report);
        Assert.DoesNotContain("<script src=", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fetch(", report, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(sourceSnapshot, SnapshotSourceFiles());
    }

    [Fact]
    public void WorkbenchCommand_RequiresSelectionForMultipleOpenSpecChangesAndIgnoresArchive()
    {
        CreateOpenSpecChange("openspec/changes/add-checkout", "checkout");
        CreateOpenSpecChange("openspec/changes/change-tax", "tax");
        CreateOpenSpecChange("openspec/changes/archive/2026-10-01-old-change", "old");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(new[] { "workbench", _root, "--engine", "openspec" }, output, error);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("--feature", error.ToString());
        Assert.False(File.Exists(Path.Combine(_root, "TestResults", "brownpants-workbench.html")));
    }

    [Fact]
    public void WorkbenchCommand_ExplicitOpenSpecChangeHasUniquePathQualifiedReviewReferences()
    {
        CreateOpenSpecChange("openspec/changes/add-checkout", "checkout");
        CreateOpenSpecChange("openspec/changes/change-tax", "tax");
        var proposalPath = Path.Combine(_root, "openspec", "changes", "change-tax", "proposal.md");
        var originalProposal = File.ReadAllText(proposalPath);
        using var protectedOutput = new StringWriter();
        using var protectedError = new StringWriter();
        var sourceOverwrite = Program.Run(new[] { "workbench", _root, "--engine", "openspec", "--feature", "openspec/changes/change-tax", "--output", "openspec/changes/change-tax/proposal.md", "--overwrite" }, protectedOutput, protectedError);
        Assert.NotEqual(0, sourceOverwrite);
        Assert.Equal(originalProposal, File.ReadAllText(proposalPath));
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(new[] { "workbench", _root, "--engine", "openspec", "--feature", "openspec/changes/change-tax", "--output", "review.html" }, output, error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        var report = File.ReadAllText(Path.Combine(_root, "review.html"));
        Assert.Contains("OpenSpec Change: change-tax", report);
        Assert.Contains("openspec/changes/change-tax/specs/tax/spec.md#requirement-001", report);
        Assert.DoesNotContain("openspec/changes/add-checkout/specs/checkout/spec.md", report);
    }

    [Fact]
    public void WorkbenchCommand_RejectsMissingArchivedAndOutsideOpenSpecChanges()
    {
        using var malformedOutput = new StringWriter();
        using var malformedError = new StringWriter();
        var malformed = Program.Run(new[] { "workbench", _root, "--engine", "open-spec" }, malformedOutput, malformedError);
        Assert.Equal(2, malformed);
        Assert.Contains("speckit", malformedError.ToString(), StringComparison.OrdinalIgnoreCase);

        CreateOpenSpecChange("openspec/changes/archive/2026-10-01-old-change", "old");
        using var archivedOutput = new StringWriter();
        using var archivedError = new StringWriter();
        var archivedOnly = Program.Run(new[] { "workbench", _root, "--engine", "openspec" }, archivedOutput, archivedError);
        Assert.NotEqual(0, archivedOnly);
        Assert.Contains("active", archivedError.ToString(), StringComparison.OrdinalIgnoreCase);

        using var missingOutput = new StringWriter();
        using var missingError = new StringWriter();
        var missing = Program.Run(new[] { "workbench", _root, "--engine", "openspec", "--feature", "openspec/changes/missing" }, missingOutput, missingError);
        Assert.NotEqual(0, missing);
        Assert.Contains("change", missingError.ToString(), StringComparison.OrdinalIgnoreCase);

        Directory.CreateDirectory(Path.Combine(_root, "openspec", "changes", "missing-proposal"));
        using var noProposalOutput = new StringWriter();
        using var noProposalError = new StringWriter();
        var noProposal = Program.Run(new[] { "workbench", _root, "--engine", "openspec", "--feature", "openspec/changes/missing-proposal" }, noProposalOutput, noProposalError);
        Assert.NotEqual(0, noProposal);
        Assert.Contains("proposal.md", noProposalError.ToString(), StringComparison.OrdinalIgnoreCase);

        using var outsideOutput = new StringWriter();
        using var outsideError = new StringWriter();
        var outside = Program.Run(new[] { "workbench", _root, "--engine", "openspec", "--feature", "../outside" }, outsideOutput, outsideError);
        Assert.NotEqual(0, outside);
        Assert.Contains("openspec/changes", outsideError.ToString(), StringComparison.OrdinalIgnoreCase);
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

    private void CreateOpenSpecChange(string changePath, string capability)
    {
        var change = Path.Combine(_root, changePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(change);
        Directory.CreateDirectory(Path.Combine(change, "specs", capability));
        File.WriteAllText(Path.Combine(change, "proposal.md"), """
            # Proposal

            ## Why
            This improves checkout reliability.

            ## What Changes
            Refunds can be retried safely.
            """);
        File.WriteAllText(Path.Combine(change, "design.md"), "# Design\nUse an idempotency key.");
        File.WriteAllText(Path.Combine(change, "tasks.md"), "- [x] 1. Add retry handling\n  - [ ] 1.1 Add an edge-case test\n- [ ] 2. Verify recovery\n");
        File.WriteAllText(Path.Combine(change, "specs", capability, "spec.md"), """
            # Payments

            ### Requirement: Refund checkout

            #### Scenario: Successful refund
            * WHEN a refund is retried
            * THEN charge the customer once

            <script>alert('x')</script>
            """);
        Directory.CreateDirectory(Path.Combine(change, "specs", "shared"));
        File.WriteAllText(Path.Combine(change, "specs", "shared", "spec.md"), """
            ### Requirement: Refund checkout

            #### Scenario: Shared capability review
            * WHEN a shared policy is loaded
            * THEN apply it to this capability
            """);
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
