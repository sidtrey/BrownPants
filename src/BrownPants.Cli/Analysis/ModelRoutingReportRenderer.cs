using System.Text.Json;

namespace BrownPants.Cli.Analysis;

public sealed record ModelHostCapability(string Host, string ExportStatus, string RuntimeLimit);
public sealed record ModelHostAssignment(string Host, string Model, string SupportStatus, string Availability);
public sealed record ModelProfileReport(string Id, IReadOnlyList<string> Categories, string QualityTier,
    string Rationale, IReadOnlyList<ModelHostAssignment> Assignments);
public sealed record ModelRoutingReport(int SchemaVersion, string PolicyPath,
    IReadOnlyList<ModelHostCapability> HostCapabilities, IReadOnlyList<ModelProfileReport> Profiles);

public static class ModelRoutingReportRenderer
{
    private static readonly ModelHostCapability[] KnownHosts =
    [
        new("vscode", "VS Code/Copilot project agent profile can be generated; model account access is not checked.", "The host or user still decides whether the agent profile is selected or invoked."),
        new("visual-studio", "Visual Studio 18.4+ project agent profile can be generated; model account access is not checked. Older versions are unverified.", "The host or user still decides whether the agent profile is selected or invoked; available models depend on Visual Studio and account access."),
        new("claude", "Claude Code project subagent can be generated; model account access is not checked.", "The host or user still decides whether the subagent is selected or invoked."),
        new("codex", "Codex project custom agent can be generated; model account access is not checked.", "The host or user still decides whether the agent is selected or invoked.")
    ];

    public static ModelRoutingReport Create(ModelRoutingPolicy policy)
    {
        var profiles = policy.Profiles
            .OrderBy(profile => profile.Id, StringComparer.Ordinal)
            .Select(profile => new ModelProfileReport(
                profile.Id,
                profile.Categories.OrderBy(category => category, StringComparer.Ordinal).ToArray(),
                profile.QualityTier,
                profile.Rationale,
                profile.Models.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new ModelHostAssignment(
                        pair.Key,
                        pair.Value,
                        KnownHosts.Any(host => host.Host == pair.Key)
                            ? "Native profile export supported; automatic task routing is not guaranteed."
                            : "No native exporter; manual host selection only.",
                        "Not checked; model access and current availability depend on the user's host and account."))
                    .ToArray()))
            .ToArray();

        return new ModelRoutingReport(1, policy.RelativePath, KnownHosts.ToArray(), profiles);
    }

    public static string RenderJson(ModelRoutingPolicy policy) => JsonSerializer.Serialize(Create(policy), new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    public static string RenderMarkdown(ModelRoutingPolicy policy)
    {
        var report = Create(policy);
        var output = new System.Text.StringBuilder();
        output.AppendLine("# Task-Based Model Guidance");
        output.AppendLine();
        output.AppendLine($"- Policy: `{EscapeInline(report.PolicyPath)}`");
        output.AppendLine("- Model availability: not checked; BrownPants does not contact providers or inspect account access.");
        output.AppendLine("- Invocation: generated profiles set a model when selected or invoked; hosts do not have to route matching tasks to them.");
        output.AppendLine();
        output.AppendLine("## Host Support");
        output.AppendLine();
        output.AppendLine("| Host | Export support | Runtime limit |");
        output.AppendLine("| --- | --- | --- |");
        foreach (var host in report.HostCapabilities)
            output.AppendLine($"| `{host.Host}` | {EscapeCell(host.ExportStatus)} | {EscapeCell(host.RuntimeLimit)} |");

        if (report.Profiles.Count == 0)
        {
            output.AppendLine();
            output.AppendLine("No model assignments are configured.");
            return output.ToString();
        }

        output.AppendLine();
        output.AppendLine("## Task Profiles");
        output.AppendLine();
        output.AppendLine("| Profile | Categories | Tier | Model preferences | Rationale |");
        output.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var profile in report.Profiles)
        {
            var categories = string.Join(", ", profile.Categories.Select(category => $"`{EscapeInline(category)}`"));
            var assignments = string.Join("<br>", profile.Assignments.Select(assignment =>
                $"`{EscapeInline(assignment.Host)}`: {EscapeCell(assignment.Model)} ({EscapeCell(assignment.SupportStatus)}; {EscapeCell(assignment.Availability)})"));
            output.AppendLine($"| `{EscapeInline(profile.Id)}` | {categories} | `{profile.QualityTier}` | {assignments} | {EscapeCell(profile.Rationale)} |");
        }
        return output.ToString();
    }

    private static string EscapeCell(string value) => value
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r\n", "<br>", StringComparison.Ordinal)
        .Replace("\n", "<br>", StringComparison.Ordinal)
        .Replace("\r", "<br>", StringComparison.Ordinal);

    private static string EscapeInline(string value) => value.Replace("`", "\\`", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal);
}
