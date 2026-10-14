using System.Text;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class PlacementGuidanceRenderer
{
    public static string Render(PlacementGuidanceReport report)
    {
        var output = new StringBuilder();
        output.AppendLine("# Requirement placement guidance");
        output.AppendLine();
        output.AppendLine($"- Recommendation: **{Label(report.Recommendation)}**");
        output.AppendLine($"- Scope: `{report.Inputs.Scope}`");
        output.AppendLine($"- Durability: `{report.Inputs.Durability}`");
        output.AppendLine($"- Purpose: `{report.Inputs.Purpose}`");
        output.AppendLine();
        output.AppendLine("## Proposed rule");
        output.AppendLine();
        output.AppendLine($"> {Escape(report.Rule)}");
        output.AppendLine();
        output.AppendLine("## Destinations");
        output.AppendLine();
        if (report.Destinations.Count == 0) output.AppendLine("No destination selected until the open questions are resolved.");
        else foreach (var destination in report.Destinations) output.AppendLine($"- {Label(destination)}");
        output.AppendLine();
        output.AppendLine("## Rationale");
        output.AppendLine();
        foreach (var rationale in report.Rationale) output.AppendLine($"- {rationale}");
        output.AppendLine();
        output.AppendLine("## Questions");
        output.AppendLine();
        if (report.Questions.Count == 0) output.AppendLine("No clarification is required by the selected rule.");
        else foreach (var question in report.Questions) output.AppendLine($"- {question}");
        output.AppendLine();
        output.AppendLine("## Evidence limits");
        output.AppendLine();
        output.AppendLine(report.EvidenceLimits);
        output.AppendLine();
        output.AppendLine("This report is decision support. Review the recommendation before changing a constitution, feature specification, or instruction file.");
        return output.ToString().TrimEnd();
    }

    private static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);

    private static string Label(string value) => value switch
    {
        "constitution" => "Project constitution",
        "feature_spec" => "Feature specification",
        "agent_guidance" => "Agent/contributor guidance",
        "split" => "Split the rule",
        "clarify" => "Clarify the inputs",
        _ => value
    };
}
