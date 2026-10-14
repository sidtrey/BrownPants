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
        output.AppendLine();
        output.AppendLine("## Observed graph measures");
        output.AppendLine();
        output.AppendLine($"- Direct callers: {report.Summary.DirectCallerCount}");
        output.AppendLine($"- Transitive callers: {report.Summary.TransitiveCallerCount}");
        output.AppendLine($"- Affected files: {report.Summary.AffectedFileCount}");
        output.AppendLine($"- Affected projects: {report.Summary.AffectedProjectCount}");
        output.AppendLine($"- Test-candidate files: {report.Summary.TestCandidateCount}");
        output.AppendLine($"- Maximum observed caller depth: {report.Summary.MaximumCallerDepth}");
        output.AppendLine();
        output.AppendLine("## Callers");
        output.AppendLine();
        if (report.Impacts.Count == 0)
            output.AppendLine(report.IsComplete ? "No callers were found in the loaded graph." : "No callers were found in the sampled graph; incomplete analysis prevents a no-impact conclusion.");
        else
        {
            foreach (var impact in report.Impacts)
            {
                var candidate = impact.IsTestCandidate ? " — test-path candidate" : string.Empty;
                output.AppendLine($"- Depth {impact.Depth}: `{Escape(impact.Caller)}` calls `{Escape(impact.Target)}` on the path to changed method `{Escape(impact.ChangedMethod)}` at `{Escape(impact.Path)}:{impact.Line}` (project `{Escape(impact.Project)}`){candidate}");
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
        output.Append("These are observed statically resolved C# and Visual Basic method-call relationships. Test-path candidates do not prove test coverage. Reflection, dynamic dispatch, runtime conditions, native C++/MFC calls, and constructor/object-creation relationships are not represented. Graph counts and depth describe observed evidence; they are not effort estimates.");
        return output.ToString().TrimEnd();
    }

    private static string Escape(string value) => value.Replace("`", "\\`");
}
