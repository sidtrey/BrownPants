using System.Text;
using System.Text.RegularExpressions;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class InstructionSourceReportRenderer
{
    public static string RenderSection(InstructionSectionResult section)
    {
        var longestFence = Regex.Matches(section.Content, @"`+")
            .Select(match => match.Length)
            .DefaultIfEmpty(0)
            .Max();
        var fence = new string('`', Math.Max(3, longestFence + 1));
        var text = new StringBuilder();
        text.AppendLine($"# Instruction section: {section.Heading}");
        text.AppendLine();
        text.AppendLine($"- Source: `{section.SourcePath}`");
        text.AppendLine($"- Anchor: `{section.Anchor}`");
        text.AppendLine($"- Lines: {section.StartLine}-{section.EndLine}");
        text.AppendLine();
        text.AppendLine(fence);
        text.Append(section.Content);
        if (!section.Content.EndsWith('\n')) text.AppendLine();
        text.AppendLine(fence);
        text.AppendLine();
        text.AppendLine("The host may continue loading other instruction files; this result does not guarantee context-window or token-cost savings.");
        return text.ToString().TrimEnd();
    }

    public static string Render(InstructionSourceReport report)
    {
        var text = new StringBuilder();
        text.AppendLine("# Repository instruction sources");
        text.AppendLine();
        text.AppendLine("Scope labels are convention-based estimates, not proof of host loading or precedence.");
        text.AppendLine("Token estimates use roughly four characters per token and are approximate.");
        text.AppendLine();
        if (report.Sources.Count == 0) text.AppendLine("No supported instruction sources found.");
        foreach (var source in report.Sources)
        {
            text.AppendLine($"## `{source.Path}`");
            text.AppendLine();
            text.AppendLine($"- Recognition: {source.Recognition}");
            text.AppendLine($"- Scope: {source.Scope}");
            text.AppendLine($"- Status: {source.Status}");
            text.AppendLine($"- Size: {(source.ByteCount?.ToString() ?? "unknown")} bytes; {(source.ApproximateTokenCount?.ToString() ?? "unknown")} approximate tokens");
            if (source.ContentDigest is not null) text.AppendLine($"- SHA-256: `{source.ContentDigest}`");
            if (source.Sections.Count > 0)
            {
                text.AppendLine("- Sections:");
                foreach (var section in source.Sections)
                    text.AppendLine($"  - {new string('#', section.Level)} {section.Heading} (`{section.Anchor}`)");
            }
            text.AppendLine();
        }
        text.AppendLine("A compact frame does not guarantee token savings: configure the agent host to load the frame instead of duplicated source files.");
        return text.ToString().TrimEnd();
    }
}
