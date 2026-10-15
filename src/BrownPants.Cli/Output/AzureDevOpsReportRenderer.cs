using System.Text.Json;
using System.Text.Json.Serialization;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class AzureDevOpsReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static string RenderJson(AzureDevOpsTraceReport report) => JsonSerializer.Serialize(report, JsonOptions);

    public static string RenderMarkdown(AzureDevOpsTraceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"# Azure DevOps Trace: `{EscapeCode(report.FeaturePath)}`");
        builder.AppendLine();
        builder.AppendLine($"**Completeness:** {(report.IsComplete ? "Complete" : "Incomplete; see diagnostics")}");
        builder.AppendLine();
        builder.AppendLine("## Work items");
        builder.AppendLine();
        builder.AppendLine("| ID | Title | Type | State | Iteration | Status | Source |");
        builder.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var item in report.WorkItems)
        {
            builder.Append('|').Append(EscapeCell(item.Id)).Append('|')
                .Append(EscapeCell(item.Title)).Append('|').Append(EscapeCell(item.Type)).Append('|')
                .Append(EscapeCell(item.State)).Append('|').Append(EscapeCell(item.IterationPath)).Append('|')
                .Append(EscapeCell(item.Status)).Append('|').Append(Link(item.SourceUrl, "work item")).AppendLine("|");
        }

        builder.AppendLine();
        builder.AppendLine("## Delivery references");
        builder.AppendLine();
        if (report.DeliveryReferences.Count == 0)
            builder.AppendLine("No supported delivery relationships were returned; unavailable relationships remain unknown.");
        else
        {
            builder.AppendLine("| Type | ID | Title | Status | Evidence |");
            builder.AppendLine("|---|---|---|---|---|");
            foreach (var item in report.DeliveryReferences)
            {
                var title = item.TargetUrl is null ? EscapeCell(item.Title) : Link(item.TargetUrl, item.Title ?? item.Identifier);
                builder.Append('|').Append(EscapeCell(item.ArtifactType)).Append('|').Append(EscapeCell(item.Identifier)).Append('|')
                    .Append(title).Append('|').Append(EscapeCell(item.Status)).Append('|')
                    .Append(Link(item.EvidenceSourceUrl, "work-item relation")).AppendLine("|");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Diagnostics");
        builder.AppendLine();
        if (report.Diagnostics.Count == 0) builder.AppendLine("No known gaps.");
        else foreach (var diagnostic in report.Diagnostics) builder.Append("- ").AppendLine(EscapeText(diagnostic));
        builder.AppendLine();
        builder.AppendLine("Relationships are limited to evidence returned by Azure DevOps and visible to the configured identity.");
        return builder.ToString();
    }

    private static string Link(string? url, string label)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            return EscapeCell(label);
        return $"[{EscapeText(label)}]({uri.AbsoluteUri.Replace(")", "%29", StringComparison.Ordinal)})";
    }

    private static string EscapeCell(string? value) => EscapeText(value ?? "Unknown").Replace("|", "\\|", StringComparison.Ordinal);
    private static string EscapeCode(string value) => value.Replace("`", "", StringComparison.Ordinal);
    private static string EscapeText(string? value) => (value ?? "Unknown")
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);
}
