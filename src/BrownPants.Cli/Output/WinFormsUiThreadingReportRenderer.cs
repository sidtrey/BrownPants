using System.Text.Json;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class WinFormsUiThreadingReportRenderer
{
    public static string Render(WinFormsUiThreadingReport report, string format) => format == "json"
        ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        : RenderMarkdown(report);

    private static string RenderMarkdown(WinFormsUiThreadingReport report)
    {
        var text = new System.Text.StringBuilder("# WinForms UI access candidates\n\n")
            .AppendLine("Findings are syntax-based candidates. They do not prove a runtime cross-thread defect or safe access.")
            .AppendLine().AppendLine($"- C#/VB files scanned: {report.ScannedFiles}")
            .AppendLine($"- Candidates: {report.CandidateCount}")
            .AppendLine($"- Omitted by limit: {report.OmittedCount}")
            .AppendLine($"- Parse errors: {report.ParseErrorCount}")
            .AppendLine($"- Indexed source types: {report.IndexedTypeCount}")
            .AppendLine($"- Assignments with unresolved receiver types: {report.UnresolvedReceiverCount}")
            .AppendLine("- Type matching uses source declarations and does not evaluate project conditions.").AppendLine();
        foreach (var item in report.Findings)
        {
            text.AppendLine($"## `{item.Path}:{item.Line}:{item.Column}`")
                .AppendLine($"- Target: `{item.Receiver}.{item.Property}` ({item.TypeEvidence})")
                .AppendLine($"- Execution context: {item.ExecutionContext}{(item.BackgroundCallback is null ? "" : $" (`{item.BackgroundCallback}`)")}")
                .AppendLine($"- Marshaling: {item.MarshalingObservation ?? "No recognized marshaling callback observed"}")
                .AppendLine($"- Classification: {item.Classification}").AppendLine();
        }
        if (report.Issues.Count > 0)
        {
            text.AppendLine("## Coverage issues").AppendLine();
            foreach (var issue in report.Issues) text.AppendLine($"- `{issue.Path}:{issue.Line}` {issue.Id}: {issue.Message}");
        }
        return text.ToString();
    }
}
