using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

public static class InstructionContextFrameAnalyzer
{
    public static string Validate(string repository, string framePath)
    {
        var root = Path.GetFullPath(repository);
        var content = File.ReadAllText(Path.Combine(root, framePath));
        var matches = Regex.Matches(content, @"^- `(?<path>[^`]+)` — SHA-256 `(?<digest>[a-fA-F0-9]{64})`$", RegexOptions.Multiline);
        if (matches.Count == 0) throw new ArgumentException("Frame contains no source identity records.");
        var paths = matches.Select(match => match.Groups["path"].Value).ToArray();
        var inventory = InstructionSourceAnalyzer.Analyze(root, paths).Sources.ToDictionary(source => source.Path, StringComparer.OrdinalIgnoreCase);
        var stale = new List<string>();
        foreach (Match match in matches)
        {
            var path = match.Groups["path"].Value;
            var digest = match.Groups["digest"].Value;
            if (!inventory.TryGetValue(path, out var source) || !string.Equals(source.ContentDigest, digest, StringComparison.OrdinalIgnoreCase))
                stale.Add(path);
        }
        return stale.Count == 0
            ? $"Frame sources are current ({matches.Count} checked)."
            : $"Frame is stale; changed or missing sources: {string.Join(", ", stale)}";
    }

    public static string RenderDraft(string repository, string draftJson)
    {
        using var draft = JsonDocument.Parse(File.ReadAllText(draftJson));
        var root = draft.RootElement;
        var customPaths = root.GetProperty("points").EnumerateArray()
            .SelectMany(point => point.GetProperty("sources").EnumerateArray())
            .Select(reference => reference.GetProperty("path").GetString() ?? string.Empty)
            .ToArray();
        var inventory = InstructionSourceAnalyzer.Analyze(repository, customPaths);
        var byPath = inventory.Sources.ToDictionary(source => source.Path, StringComparer.OrdinalIgnoreCase);
        var budget = root.GetProperty("budget");
        var unit = budget.GetProperty("unit").GetString()?.ToLowerInvariant();
        var requested = budget.GetProperty("requested").GetInt32();
        if (unit is not ("words" or "tokens") || requested < 1)
            throw new ArgumentException("Draft budget requires unit 'words' or 'tokens' and a positive requested value.");

        var text = new StringBuilder();
        text.AppendLine("# Response context frame");
        text.AppendLine();
        text.AppendLine($"Budget: {requested} {unit} (approximate for tokens)");
        text.AppendLine();
        var count = 0;
        foreach (var point in root.GetProperty("points").EnumerateArray())
        {
            var pointText = point.GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(pointText)) throw new ArgumentException("Frame point text cannot be empty.");
            pointText = string.Join(' ', pointText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            text.Append("- ").Append(pointText);
            var references = point.GetProperty("sources").EnumerateArray().ToArray();
            if (references.Length == 0) throw new ArgumentException("Every frame point must cite at least one source.");
            foreach (var reference in references)
            {
                var path = reference.GetProperty("path").GetString() ?? string.Empty;
                var anchor = reference.TryGetProperty("anchor", out var anchorValue) ? anchorValue.GetString() : null;
                if (!byPath.TryGetValue(path, out var source) || source.Status != "Readable")
                    throw new ArgumentException($"Frame source '{path}' is missing or unreadable.");
                if (anchor is not null && !source.Sections.Any(section => section.Anchor.Equals(anchor, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"Section '{anchor}' was not found in '{path}'.");
                text.Append(" [").Append(path);
                if (!string.IsNullOrWhiteSpace(anchor)) text.Append('#').Append(anchor);
                text.Append("](").Append(string.Join('/', path.Split('/').Select(Uri.EscapeDataString)));
                if (!string.IsNullOrWhiteSpace(anchor)) text.Append('#').Append(Uri.EscapeDataString(anchor));
                text.Append(")");
            }
            text.AppendLine();
            count += unit == "words" ? pointText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length
                : Math.Max(1, (int)Math.Ceiling(pointText.Length / 4.0));
        }
        text.AppendLine();
        text.AppendLine("## Review items");
        text.AppendLine();
        if (root.TryGetProperty("reviewItems", out var reviewItems) && reviewItems.ValueKind == JsonValueKind.Array && reviewItems.GetArrayLength() > 0)
        {
            foreach (var item in reviewItems.EnumerateArray()) text.AppendLine($"- {item.GetString()}");
        }
        else text.AppendLine("- Review source conflicts and confirm this frame covers required constraints.");
        if (count > requested) text.AppendLine($"- Budget exceeded: {count} {unit} counted against {requested} requested.");
        text.AppendLine();
        text.AppendLine("When a task depends on detailed rules or exceptions, open the cited source section. The frame does not guarantee model memory, compliance, or token savings; host loading behavior determines whether source files are still included.");
        text.AppendLine();
        text.AppendLine("## Source identities");
        text.AppendLine();
        foreach (var source in inventory.Sources.Where(source => source.ContentDigest is not null))
            text.AppendLine($"- `{source.Path}` — SHA-256 `{source.ContentDigest}`");
        return text.ToString().TrimEnd();
    }
}
