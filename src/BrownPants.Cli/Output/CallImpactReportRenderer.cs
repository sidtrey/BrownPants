using System.Text;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class CallImpactReportRenderer
{
    public static string Render(CallImpactReport report)
    {
        var output = new StringBuilder();
        output.AppendLine("# Call-trace change impact");
        output.AppendLine();
        output.AppendLine($"- Repository: `{Escape(report.Root)}`");
        output.AppendLine($"- Changed paths: {string.Join(", ", report.ChangedPaths.Select(path => $"`{Escape(path)}`"))}");
        output.AppendLine($"- Analysis complete: **{(report.IsComplete ? "yes" : "no")}**");
        var hasManagedPaths = report.ChangedPaths.Any(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        if (hasManagedPaths)
        {
            output.AppendLine();
            output.AppendLine("## Managed resolved graph measures");
            output.AppendLine();
            output.AppendLine($"- Direct callers: {report.Summary.DirectCallerCount}");
            output.AppendLine($"- Transitive callers: {report.Summary.TransitiveCallerCount}");
            output.AppendLine($"- Affected files: {report.Summary.AffectedFileCount}");
            output.AppendLine($"- Affected projects: {report.Summary.AffectedProjectCount}");
            output.AppendLine($"- Test-candidate files: {report.Summary.TestCandidateCount}");
            output.AppendLine($"- Maximum observed caller depth: {report.Summary.MaximumCallerDepth}");
        }
        if (report.ChangedPaths.Any(IsNativePath) || report.NativeImpacts.Count > 0)
        {
            output.AppendLine();
            output.AppendLine("## Native candidate measures");
            output.AppendLine();
            output.AppendLine($"- Candidate relations: {report.NativeCandidateCount}");
            output.AppendLine($"- Unresolved relations: {report.NativeUnresolvedCount}");
            output.AppendLine($"- Ambiguous relations: {report.NativeAmbiguousCount}");
            output.AppendLine($"- Source files in retained relations: {report.NativeAffectedFileCount}");
        }
        if (hasManagedPaths)
        {
            output.AppendLine();
            output.AppendLine("## Managed callers");
            output.AppendLine();
            if (report.Impacts.Count == 0)
                output.AppendLine(report.IsComplete ? "No managed callers were found in the loaded graph." : "No managed callers were retained; incomplete analysis prevents a no-impact conclusion.");
            else
            {
                foreach (var impact in report.Impacts)
                {
                    var candidate = impact.IsTestCandidate ? " — test-path candidate" : string.Empty;
                    var relation = impact.RelationshipKind switch
                    {
                        "constructor" => "constructs",
                        "delegate" => "dispatches to",
                        _ => "calls"
                    };
                    output.AppendLine($"- Depth {impact.Depth}: `{Escape(impact.Caller)}` {relation} `{Escape(impact.Target)}` on the path to changed member `{Escape(impact.ChangedMethod)}` at `{Escape(impact.Path)}:{impact.Line}` (project `{Escape(impact.Project)}`){candidate}");
                }
            }
        }
        if (report.ChangedPaths.Any(IsNativePath) || report.NativeImpacts.Count > 0)
        {
            output.AppendLine();
            output.AppendLine("## Native impact candidates");
            output.AppendLine();
            if (report.NativeImpacts.Count == 0)
                output.AppendLine(report.IsComplete ? "No native-flow candidate relations were found for the changed paths." : "No native-flow candidate relations were retained; incomplete analysis prevents a no-impact conclusion.");
            foreach (var impact in report.NativeImpacts)
            {
                var command = impact.CommandId is null ? string.Empty : $" command `{Escape(impact.CommandId)}`";
                var targetLocation = impact.TargetPath is null ? "target location unresolved"
                    : $"target `{Escape(impact.TargetPath)}:{impact.TargetLine}`";
                var depthLabel = impact.ManagedImpactDepth is null ? $"Depth {impact.Depth}"
                    : $"Managed depth {impact.ManagedImpactDepth}, native depth {impact.Depth}";
                var candidateTargets = impact.CandidateTargets.Count == 0 ? string.Empty
                    : $"; possible locations: {string.Join(", ", impact.CandidateTargets.Select(target => $"`{Escape(target.Path)}:{target.Line}`"))}";
                output.AppendLine($"- {depthLabel} **{Escape(impact.Status)}** {Escape(impact.RelationshipKind)}{command}: `{Escape(impact.SourceSymbol)}` at `{Escape(impact.SourcePath)}:{impact.SourceLine}` → `{Escape(impact.TargetSymbol)}` ({targetLocation}; {impact.CandidateTargetCount} candidate target(s){candidateTargets}; changed `{Escape(impact.ChangedPath)}`)");
            }
        }
        if (report.Issues.Count > 0)
        {
            output.AppendLine();
            output.AppendLine("## Completeness limitations");
            output.AppendLine();
            foreach (var issue in report.Issues)
                output.AppendLine($"- {Escape(issue)}");
        }
        output.AppendLine();
        output.AppendLine("## Evidence limits");
        output.AppendLine();
        output.Append("Managed entries are statically resolved C# and Visual Basic method, explicit constructor, and conservative local-delegate relationships. Native entries are lexical MFC/native-flow candidates and are not compiler-resolved calls. Neither category proves runtime activation, dependency injection behavior, reflection, dynamic dispatch, or exhaustive impact. Test-path candidates do not prove test coverage. Graph counts and depth describe observed evidence; they are not effort estimates.");
        return output.ToString().TrimEnd();
    }

    private static string Escape(string value) => value.Replace("`", "\\`");

    private static bool IsNativePath(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".c" or ".cc" or ".cpp" or ".cxx" or ".h" or ".hh" or ".hpp" or ".hxx" or ".inl" or ".ipp";
}
