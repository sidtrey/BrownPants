using System.Text;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class InvestigationReportRenderer
{
    public static string Render(InvestigationReport report)
    {
        var output = new StringBuilder();
        output.AppendLine("# Bug investigation context");
        output.AppendLine();
        output.AppendLine($"- Repository: `{Escape(report.Root)}`");
        output.AppendLine($"- Match sample limit per clue: {report.SampleLimit}");
        output.AppendLine("- Method: case-insensitive literal matching in discovered paths and file lines.");
        output.AppendLine();

        foreach (var clue in report.Clues)
        {
            output.AppendLine($"## Clue: `{Escape(clue.Clue)}`");
            output.AppendLine();
            output.AppendLine($"- Matches: {clue.MatchCount}");
            if (clue.Matches.Count == 0)
            {
                output.AppendLine("No repository matches found.");
            }
            else
            {
                foreach (var match in clue.Matches)
                {
                    var location = match.Line.HasValue
                        ? $"{Escape(match.Path)}:{match.Line.Value}"
                        : Escape(match.Path);
                    var testCandidate = match.IsTestCandidate ? " — test-path candidate" : string.Empty;
                    output.AppendLine($"- {match.Kind}: `{location}`{testCandidate}");
                }
            }

            var omitted = clue.MatchCount - clue.Matches.Count;
            if (omitted > 0)
                output.AppendLine($"- {omitted} additional matches omitted by the sample limit.");
            output.AppendLine();
        }

        if (report.ReadIssueCount > 0)
        {
            output.AppendLine("## Files not searched");
            output.AppendLine();
            foreach (var issue in report.ReadIssues)
                output.AppendLine($"- `{Escape(issue.Path)}` — {Escape(issue.Code)}");
            var omitted = report.ReadIssueCount - report.ReadIssues.Count;
            if (omitted > 0)
                output.AppendLine($"- {omitted} additional files omitted by the sample limit.");
            output.AppendLine();
        }

        output.AppendLine("## Evidence limits");
        output.AppendLine();
        output.AppendLine("Literal path and text matches are investigation leads. They do not establish reachability, runtime behavior, a bug's cause, or that a test reproduces or covers the bug. Test-path labels follow repository naming conventions. Matched source-line contents are not included.");
        return output.ToString().TrimEnd();
    }

    private static string Escape(string value) => value.Replace("`", "\\`");
}
