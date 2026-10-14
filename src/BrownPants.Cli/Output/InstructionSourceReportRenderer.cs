using System.Text;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class InstructionSourceReportRenderer
{
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
