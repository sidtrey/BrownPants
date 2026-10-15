using System.Text;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class MarkdownReport
{
    public static string Render(RepositoryInventory inventory)
    {
        var report = new StringBuilder();
        report.AppendLine("# BrownPants repository context");
        report.AppendLine();
        report.AppendLine($"- Repository: `{Escape(inventory.Root)}`");
        report.AppendLine($"- Files discovered: {inventory.FileCount}");
        report.AppendLine("- Method: deterministic filesystem inventory; generated files and VCS internals are excluded.");
        report.AppendLine();
        report.AppendLine("## Project signals");
        report.AppendLine();
        AppendList(report, inventory.ProjectMarkers, "No known project markers found.");
        report.AppendLine();
        AppendProjectSystems(report, inventory.ProjectSystems);
        AppendLanguageCoverage(report, inventory.LanguageCoverage);
        report.AppendLine("## File types");
        report.AppendLine();
        if (inventory.Extensions.Count == 0)
            report.AppendLine("No files with extensions found.");
        else
            foreach (var extension in inventory.Extensions)
                report.AppendLine($"- `{extension.Extension}`: {extension.Count}");
        report.AppendLine();
        report.AppendLine("## Test signals");
        report.AppendLine();
        AppendList(report, inventory.TestPaths, "No test paths or test-named files detected.");
        report.AppendLine();
        report.AppendLine("## Top-level entries");
        report.AppendLine();
        AppendList(report, inventory.TopLevelEntries, "No visible entries.");
        report.AppendLine();
        AppendSddWorkflows(report, inventory.SddWorkflows, inventory.SddWorkflowIssues);
        AppendDotNetProjects(report, inventory.DotNetProjects);
        AppendNativeProjects(report, inventory.NativeProjects);
        AppendCSharpAnalysis(report, inventory.CSharp);
        AppendVisualBasicAnalysis(report, inventory.VisualBasic);
        AppendArchitecturePatterns(report, inventory.ArchitecturePatterns);
        report.AppendLine("## Evidence limits");
        report.AppendLine();
        report.AppendLine("Default Roslyn analysis provides syntax-level evidence without evaluating MSBuild conditions, imports, or project preprocessor symbols. Semantic mode loads projects separately when requested. Neither parse diagnostics nor unresolved symbols alone prove that a project fails to build. Review source, project files, and workspace issues before planning changes.");
        return report.ToString().TrimEnd();
    }

    private static void AppendProjectSystems(StringBuilder report, IReadOnlyList<ProjectSystemInfo> systems)
    {
        report.AppendLine("## Project ecosystems");
        report.AppendLine();
        if (systems.Count == 0)
        {
            report.AppendLine("No known project ecosystems detected.");
            report.AppendLine();
            return;
        }

        foreach (var system in systems)
        {
            report.AppendLine($"- `{Escape(system.Name)}`:");
            foreach (var path in system.ManifestPaths)
                report.AppendLine($"  - `{Escape(path)}`");
        }
        report.AppendLine();
    }

    private static void AppendSddWorkflows(
        StringBuilder report,
        IReadOnlyList<SddWorkflowInventory> workflows,
        IReadOnlyList<SddWorkflowConfigurationIssue> issues)
    {
        report.AppendLine("## SDD workflows");
        report.AppendLine();
        if (workflows.Count == 0)
        {
            report.AppendLine("No supported SDD workflow artifacts detected.");
            report.AppendLine();
        }
        else
        {
            foreach (var workflow in workflows)
            {
                report.AppendLine($"### {Escape(workflow.Name)}");
                report.AppendLine();
                report.AppendLine($"- Initialized: {YesNo(workflow.IsInitialized)}");
                report.AppendLine($"- Specification or feature directories: {workflow.SpecificationDirectoryCount}");
                if (workflow.ActiveChangeDirectoryCount.HasValue)
                    report.AppendLine($"- Active change directories: {workflow.ActiveChangeDirectoryCount.Value}");
                if (workflow.ActiveWorkItemPath is not null)
                    report.AppendLine($"- Active work item: `{Escape(workflow.ActiveWorkItemPath)}` ({Escape(workflow.ActiveWorkItemEvidence!)})");
                report.AppendLine($"- Artifact files: {workflow.ArtifactFileCount}");
                if (workflow.ArtifactPaths.Count > 0)
                {
                    report.AppendLine("- Artifact path samples:");
                    foreach (var artifact in workflow.ArtifactPaths)
                        report.AppendLine($"  - `{Escape(artifact)}`");
                    if (workflow.ArtifactFileCount > workflow.ArtifactPaths.Count)
                        report.AppendLine($"  - {workflow.ArtifactFileCount - workflow.ArtifactPaths.Count} additional artifacts omitted by the sample limit.");
                }
                report.AppendLine();
            }
        }

        if (issues.Count > 0)
        {
            report.AppendLine("### Configuration diagnostics");
            report.AppendLine();
            foreach (var issue in issues)
                report.AppendLine($"- `{Escape(issue.Path)}` **{Escape(issue.Code)}**: {Escape(issue.Message)}");
            report.AppendLine();
        }
    }

    private static void AppendLanguageCoverage(StringBuilder report, IReadOnlyList<LanguageCoverageInfo> languages)
    {
        report.AppendLine("## Language coverage");
        report.AppendLine();
        if (languages.Count == 0)
        {
            report.AppendLine("No recognized source-language extensions found; file suffixes remain in the file inventory.");
            report.AppendLine();
            return;
        }

        foreach (var language in languages)
        {
            var fileLabel = language.SourceFileCount == 1 ? "source file" : "source files";
            report.AppendLine($"- `{Escape(language.Language)}` — {language.SourceFileCount} {fileLabel} — {Escape(language.AnalysisLevel)}");
        }
        report.AppendLine();
    }

    private static void AppendDotNetProjects(StringBuilder report, IReadOnlyList<DotNetProjectInfo> projects)
    {
        report.AppendLine("## .NET projects");
        report.AppendLine();
        if (projects.Count == 0)
        {
            report.AppendLine("No `.csproj` files detected.");
            report.AppendLine();
            return;
        }

        var legacyCount = projects.Count(project => project.IsLegacyFramework);
        report.AppendLine($"- Project files: {projects.Count}");
        report.AppendLine($"- .NET Framework 4.x signals: {legacyCount}");
        foreach (var project in projects)
        {
            var frameworks = project.TargetFrameworks.Count == 0
                ? "target framework not found"
                : string.Join(", ", project.TargetFrameworks.Select(Escape));
            var languages = project.LanguageVersions.Count == 0
                ? string.Empty
                : $"; C# language version: {string.Join(", ", project.LanguageVersions.Select(Escape))}";
            report.AppendLine($"- `{Escape(project.Path)}` — {frameworks}; legacy .NET Framework: {YesNo(project.IsLegacyFramework)}{languages}");
            foreach (var reference in project.ProjectReferences)
                report.AppendLine($"  - ProjectReference: `{Escape(reference)}`");
            if (project.ReadError is not null)
                report.AppendLine($"  - Project XML could not be read: {Escape(project.ReadError)}");
        }
        report.AppendLine();
    }

    private static void AppendNativeProjects(StringBuilder report, IReadOnlyList<NativeProjectInfo> projects)
    {
        report.AppendLine("## Native C++ projects");
        report.AppendLine();
        if (projects.Count == 0)
        {
            report.AppendLine("No `.vcxproj` or `.vcproj` files detected.");
            report.AppendLine();
            return;
        }

        report.AppendLine($"- Project files: {projects.Count}");
        foreach (var project in projects)
        {
            report.AppendLine($"- `{Escape(project.Path)}`; MFC setting detected: {YesNo(project.HasMfcSignal)}; CLR support detected: {YesNo(project.HasClrSupport)}");
            if (project.TargetFrameworks.Count > 0)
                report.AppendLine($"  - Target/platform versions: {string.Join(", ", project.TargetFrameworks.Select(Escape))}");
            if (project.PlatformToolsets.Count > 0)
                report.AppendLine($"  - Platform toolsets: {string.Join(", ", project.PlatformToolsets.Select(Escape))}");
            foreach (var reference in project.ProjectReferences)
                report.AppendLine($"  - Native project reference: `{Escape(reference)}`");
            if (project.ReadError is not null)
                report.AppendLine($"  - Project XML could not be read: {Escape(project.ReadError)}");
        }
        report.AppendLine();
    }

    private static void AppendCSharpAnalysis(StringBuilder report, CSharpAnalysis analysis)
    {
        report.AppendLine("## Roslyn C# structure");
        report.AppendLine();
        report.AppendLine($"- Source files parsed: {analysis.SourceFileCount}");
        report.AppendLine($"- Type declarations: {analysis.TypeCount}");
        report.AppendLine($"- Member declarations: {analysis.MemberCount}");
        report.AppendLine($"- Invocation expressions: {analysis.InvocationCount} (syntax names only; targets are not resolved)");
        report.AppendLine($"- Parse errors: {analysis.ParseErrorCount}");
        report.AppendLine($"- Inactive preprocessor text regions: {analysis.InactiveCodeRegionCount}");

        AppendSamples(report, "Type declarations", analysis.Types
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` — {Escape(item.Namespace)} {item.Kind} `{Escape(item.Name)}`")
            .ToArray(), analysis.TypeCount, "No C# type declarations found.");
        AppendSamples(report, "Member declarations", analysis.Members
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` — {item.Kind} `{Escape(item.ContainingType)}.{Escape(item.Signature)}`")
            .ToArray(), analysis.MemberCount, "No C# members found.");
        AppendSamples(report, "Using directives", analysis.Usings
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` — `{(item.IsGlobal ? "global " : string.Empty)}{(item.Alias is null ? string.Empty : Escape(item.Alias) + " = ")}{(item.IsStatic ? "static " : string.Empty)}{Escape(item.Namespace)}`")
            .ToArray(), null, "No using directives found.");
        AppendSamples(report, "Invocation expressions (unresolved)", analysis.Invocations
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` — `{Escape(item.Expression)}`")
            .ToArray(), analysis.InvocationCount, "No invocation expressions found.");
        AppendSamples(report, "Roslyn parse issues", analysis.Issues
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` {item.Code}: {Escape(item.Message)}")
            .ToArray(), analysis.ParseErrorCount, "No Roslyn parse errors found.");

        if (analysis.Semantic is not null)
            AppendSemanticAnalysis(report, analysis.Semantic);
    }

    private static void AppendSemanticAnalysis(StringBuilder report, CSharpSemanticAnalysis semantic)
    {
        report.AppendLine();
        report.AppendLine("### .NET C# and Visual Basic semantic method resolution");
        report.AppendLine();
        report.AppendLine($"- Projects discovered / loaded by MSBuild: {semantic.ProjectsDiscovered} / {semantic.ProjectsLoaded}");
        report.AppendLine($"- Projects not loaded: {semantic.ProjectsFailed}; project coverage complete: {semantic.ProjectCoverageComplete.ToString().ToLowerInvariant()}");
        report.AppendLine($"- Invocation syntax nodes seen / skipped / analyzed: {semantic.InvocationSyntaxCount} / {semantic.SkippedInvocationCount} / {semantic.AnalyzedInvocationCount}");
        report.AppendLine($"- Invocation expressions resolved to methods: {semantic.ResolvedInvocationCount}");
        report.AppendLine($"- Invocation expressions without a resolved method: {semantic.UnresolvedInvocationCount}");
        report.AppendLine($"- Explicit constructor syntax / resolved / unresolved: {semantic.ConstructorSyntaxCount} / {semantic.ResolvedConstructorCount} / {semantic.UnresolvedConstructorCount}");
        report.AppendLine($"- Local delegate targets resolved / unresolved: {semantic.ResolvedDelegateTargetCount} / {semantic.UnresolvedDelegateTargetCount} (only safe direct local bindings are resolved)");
        if (semantic.UnresolvedReasonCounts is { Count: > 0 })
            report.AppendLine($"- Unresolved relationship counts by Roslyn candidate reason: {string.Join(", ", semantic.UnresolvedReasonCounts.Select(item => $"{Escape(item.Reason)}={item.Count}"))}");
        if (semantic.ResolvedCalls.Count > 0)
        {
            var shownCallCount = semantic.ResolvedCalls.Count;
            report.AppendLine("- Resolved call samples:");
            foreach (var call in semantic.ResolvedCalls)
            {
                var targetLocation = call.TargetPath is null ? string.Empty : $" declared at `{Escape(call.TargetPath)}:{call.TargetLine}`";
                var relation = call.RelationshipKind switch { "constructor" => "constructs", "delegate" => "dispatches to", _ => "calls" };
                report.AppendLine($"  - `{Escape(call.Path)}:{call.Line}` `{Escape(call.Caller)}` {relation} `{Escape(call.Target)}`{targetLocation} via `{Escape(call.Expression)}` (project `{Escape(call.Project)}`)");
            }
            var resolvedTotal = semantic.ResolvedInvocationCount + semantic.ResolvedConstructorCount + semantic.ResolvedDelegateTargetCount;
            if (resolvedTotal > shownCallCount)
                report.AppendLine($"  - {resolvedTotal - shownCallCount} additional resolved relationships omitted from this report.");
        }
        if (semantic.UnresolvedCalls.Count > 0)
        {
            report.AppendLine("- Unresolved call samples:");
            foreach (var call in semantic.UnresolvedCalls)
            {
                var candidates = call.CandidateTargets.Count == 0
                    ? string.Empty
                    : $"; candidates: {string.Join(", ", call.CandidateTargets.Select(Escape))}";
                report.AppendLine($"  - `{Escape(call.Path)}:{call.Line}` `{Escape(call.RelationshipKind)}` `{Escape(call.Expression)}` ({Escape(call.CandidateReason)}{candidates})");
            }
            var unresolvedTotal = semantic.UnresolvedInvocationCount + semantic.UnresolvedConstructorCount
                + semantic.UnresolvedDelegateTargetCount;
            var retainedUnresolved = semantic.UnresolvedCalls.Count + semantic.UnresolvedDelegateCalls.Count;
            if (unresolvedTotal > retainedUnresolved)
                report.AppendLine($"  - {unresolvedTotal - retainedUnresolved} additional unresolved relationships omitted from samples.");
        }
        if (semantic.UnresolvedDelegateCalls.Count > 0)
        {
            report.AppendLine("- Unresolved delegate target samples:");
            foreach (var call in semantic.UnresolvedDelegateCalls)
                report.AppendLine($"  - `{Escape(call.Path)}:{call.Line}` `{Escape(call.Expression)}` ({Escape(call.CandidateReason)})");
        }
        if (semantic.Issues.Count > 0)
        {
            report.AppendLine("- MSBuild / workspace issues:");
            foreach (var issue in semantic.Issues)
                report.AppendLine($"  - {Escape(issue)}");
        }
        report.AppendLine($"- MSBuild / workspace issue total: {semantic.WorkspaceIssueCount}; {semantic.WorkspaceIssueOmittedCount} issue samples omitted.");
        report.AppendLine($"- Compiler error total: {semantic.CompilerErrorCount}; {semantic.CompilerErrorOmittedCount} error samples omitted.");
        if (semantic.CompilerDiagnosticGroupCount > 0 || semantic.CompilerDiagnosticGroups is { Count: > 0 })
        {
            report.AppendLine($"- Compiler diagnostic groups: {semantic.CompilerDiagnosticGroupCount} total; {semantic.CompilerDiagnosticGroupOmittedCount} group samples omitted (project/code counts; not causal explanations).");
            foreach (var group in semantic.CompilerDiagnosticGroups)
                report.AppendLine($"  - `{Escape(group.Project)}` `{Escape(group.Code)}`: {group.Count}");
            if (semantic.CompilerDiagnosticGroupOmittedCount > 0)
                report.AppendLine($"  - {semantic.CompilerDiagnosticGroupOmittedCount} additional groups omitted by the sample limit.");
        }
        if (semantic.CompilerDiagnostics is { Count: > 0 })
        {
            report.AppendLine("- Compiler error samples:");
            foreach (var diagnostic in semantic.CompilerDiagnostics)
            {
                var location = diagnostic.Path is null ? string.Empty : $" `{Escape(diagnostic.Path)}:{diagnostic.Line}`";
                report.AppendLine($"  - `{Escape(diagnostic.Project)}`{location} {Escape(diagnostic.Code)}: {Escape(diagnostic.Message)}");
            }
        }
        report.AppendLine("- Resolution uses the loaded project configuration and may be partial when references or MSBuild imports are unavailable.");
    }

    private static void AppendVisualBasicAnalysis(StringBuilder report, VisualBasicAnalysis analysis)
    {
        report.AppendLine("## Roslyn Visual Basic structure");
        report.AppendLine();
        report.AppendLine($"- Source files parsed: {analysis.SourceFileCount}");
        report.AppendLine($"- Type declarations: {analysis.TypeCount}");
        report.AppendLine($"- Member declarations: {analysis.MemberCount}");
        report.AppendLine($"- Invocation expressions: {analysis.InvocationCount} (syntax names only; targets are not resolved)");
        report.AppendLine($"- Parse errors: {analysis.ParseErrorCount}");
        AppendSamples(report, "Visual Basic types", analysis.Types
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` — {Escape(item.Namespace)} {item.Kind} `{Escape(item.Name)}`")
            .ToArray(), analysis.TypeCount, "No Visual Basic type declarations found.");
        AppendSamples(report, "Visual Basic members", analysis.Members
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` — {item.Kind} `{Escape(item.ContainingType)}.{Escape(item.Signature)}`")
            .ToArray(), analysis.MemberCount, "No Visual Basic members found.");
        AppendSamples(report, "Visual Basic invocation expressions (unresolved)", analysis.Invocations
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` — `{Escape(item.Expression)}`")
            .ToArray(), analysis.InvocationCount, "No Visual Basic invocation expressions found.");
        AppendSamples(report, "Visual Basic parse issues", analysis.Issues
            .Select(item => $"`{Escape(item.Path)}:{item.Line}` {item.Code}: {Escape(item.Message)}")
            .ToArray(), analysis.ParseErrorCount, "No Visual Basic parse errors found.");
        report.AppendLine();
    }

    private static void AppendArchitecturePatterns(StringBuilder report, ArchitecturePatternAnalysis analysis)
    {
        report.AppendLine("## Architecture pattern signals");
        report.AppendLine();
        report.AppendLine($"- Candidate source signals: {analysis.SignalCount}");
        report.AppendLine("- These are source-local candidate signals, not resolved relationships; verify context in the cited source.");
        if (!string.IsNullOrEmpty(analysis.EvidenceLimits))
            report.AppendLine($"- Evidence limit: {Escape(analysis.EvidenceLimits)}");
        foreach (var signal in analysis.Signals)
            report.AppendLine($"- {signal.Category} [{signal.EvidenceKind}] ({signal.Language}): `{Escape(signal.Path)}:{signal.Line}` — `{Escape(signal.Evidence)}`");
        if (analysis.SignalCount == 0)
            report.AppendLine("No matching pattern signals found.");
        else if (analysis.SignalCount > analysis.Signals.Count)
            report.AppendLine($"- {analysis.SignalCount - analysis.Signals.Count} additional matches omitted by the sample limit.");
        if (analysis.NativeFlow is { ScannedFileCount: > 0 } nativeFlow)
        {
            report.AppendLine();
            report.AppendLine("### Native flow candidates");
            report.AppendLine();
            report.AppendLine($"- Relations: {nativeFlow.RelationCount}; unresolved: {nativeFlow.UnresolvedCount}; ambiguous: {nativeFlow.AmbiguousCount}; omitted: {nativeFlow.OmittedCount}");
            foreach (var limit in nativeFlow.EvidenceLimits)
                report.AppendLine($"- Evidence limit: {Escape(limit)}");
            foreach (var relation in nativeFlow.Relations)
            {
                var target = relation.TargetPath is null
                    ? $"`{Escape(relation.TargetSymbol)}` ({relation.Status}, {relation.CandidateTargetCount} definitions)"
                    : $"`{Escape(relation.TargetPath)}:{relation.TargetLine}` `{Escape(relation.TargetSymbol)}`";
                var command = relation.CommandId is null ? string.Empty : $" command `{Escape(relation.CommandId)}`";
                var possibleTargets = relation.CandidateTargets is not { Count: > 0 } || relation.Status == "candidate"
                    ? string.Empty
                    : "; possible target locations: " + string.Join(", ", relation.CandidateTargets.Select(item =>
                        Escape(item.Path) + ":" + item.Line));
                report.AppendLine($"- {Escape(relation.Kind)} [{Escape(relation.Status)}]{command}: `{Escape(relation.SourcePath)}:{relation.SourceLine}` `{Escape(relation.SourceSymbol)}` → {target}{possibleTargets}");
            }
            if (nativeFlow.RelationCount == 0)
                report.AppendLine("No supported native or managed interop paths found.");
            else if (nativeFlow.OmittedCount > 0)
                report.AppendLine($"- {nativeFlow.OmittedCount} additional native flow relations omitted by the sample limit.");
        }
        report.AppendLine();
    }

    private static void AppendSamples(StringBuilder report, string heading, IReadOnlyList<string> samples,
        int? total, string emptyMessage)
    {
        report.AppendLine();
        report.AppendLine($"### {heading}");
        report.AppendLine();
        if (samples.Count == 0)
        {
            report.AppendLine(emptyMessage);
            return;
        }
        foreach (var sample in samples)
            report.AppendLine($"- {sample}");
        if (total.HasValue && total.Value > samples.Count)
            report.AppendLine($"- {total.Value - samples.Count} additional entries omitted.");
    }

    private static void AppendList(StringBuilder builder, IReadOnlyList<string> values, string emptyMessage)
    {
        if (values.Count == 0)
        {
            builder.AppendLine(emptyMessage);
            return;
        }
        foreach (var value in values)
            builder.AppendLine($"- `{Escape(value)}`");
    }

    private static string Escape(string value) => value.Replace("`", "\\`");
    private static string YesNo(bool value) => value ? "yes" : "no";
}

