using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

public static partial class InstructionSourceAnalyzer
{
    private const int MaximumSourceBytes = 1_048_576;
    private static readonly string[] ExactNames =
    {
        "AGENTS.md", "CLAUDE.md", "GEMINI.md", "copilot-instructions.md", "CONTRIBUTING.md"
    };

    public static InstructionSourceReport Analyze(string repository, IEnumerable<string>? explicitSources = null)
    {
        var root = Path.GetFullPath(repository);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Directory '{root}' does not exist.");
        var discovered = RepositoryScanner.DiscoverFiles(root);
        var selected = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in discovered)
        {
            var normalized = path.Replace('\\', '/');
            var name = Path.GetFileName(normalized);
            var recognized = ExactNames.Contains(name, StringComparer.OrdinalIgnoreCase)
                || normalized.Equals(".github/copilot-instructions.md", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(".github/instructions/", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(".cursor/rules/", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(".claude/", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
            if (recognized) selected[normalized] = "Convention";
        }

        foreach (var source in explicitSources ?? Array.Empty<string>())
        {
            var full = Path.GetFullPath(Path.Combine(root, source));
            var relative = Path.GetRelativePath(root, full).Replace('\\', '/');
            if (relative == ".." || relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                throw new ArgumentException($"Instruction source '{source}' must be inside the repository.");
            if (!File.Exists(full)) throw new FileNotFoundException($"Instruction source '{source}' was not found.", full);
            if (HasReparsePoint(full, root))
                throw new ArgumentException($"Instruction source '{source}' passes through a symbolic link or reparse point.");
            selected[relative] = "Explicit";
        }

        var sources = new List<InstructionSourceInfo>();
        foreach (var item in selected)
        {
            var full = Path.Combine(root, item.Key.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                var info = new FileInfo(full);
                if (info.Length > MaximumSourceBytes)
                {
                    sources.Add(new(item.Key, item.Value, InferScope(item.Key, string.Empty), Array.Empty<InstructionSection>(), info.Length, null, null, "TooLarge"));
                    continue;
                }
                var bytes = File.ReadAllBytes(full);
                var content = new UTF8Encoding(false, true).GetString(bytes);
                var sections = ParseSections(content);
                sources.Add(new(item.Key, item.Value, InferScope(item.Key, content), sections, bytes.Length,
                    Math.Max(1, (int)Math.Ceiling(content.Length / 4.0)), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), "Readable"));
            }
            catch (DecoderFallbackException)
            {
                sources.Add(new(item.Key, item.Value, InferScope(item.Key, string.Empty), Array.Empty<InstructionSection>(), null, null, null, "UnsupportedEncoding"));
            }
            catch (IOException)
            {
                sources.Add(new(item.Key, item.Value, InferScope(item.Key, string.Empty), Array.Empty<InstructionSection>(), null, null, null, "ReadError"));
            }
        }
        return new(root, sources);
    }

    private static IReadOnlyList<InstructionSection> ParseSections(string content)
    {
        var result = new List<InstructionSection>();
        var anchorCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in HeadingRegex().Matches(content))
        {
            var title = match.Groups[2].Value.Trim();
            var anchor = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9\s-]", string.Empty).Trim().Replace(' ', '-');
            if (anchorCounts.TryGetValue(anchor, out var count))
            {
                anchorCounts[anchor] = count + 1;
                anchor = $"{anchor}-{count}";
            }
            else anchorCounts.Add(anchor, 1);
            result.Add(new(title, anchor, match.Groups[1].Value.Length));
        }
        return result;
    }

    private static string InferScope(string path, string content)
    {
        var appliesTo = Regex.Match(content, @"(?im)^\s*applyTo\s*:\s*[""']?(?<pattern>[^\r\n""']+)", RegexOptions.CultureInvariant);
        if (appliesTo.Success) return $"Path-scoped by declared applyTo: {appliesTo.Groups["pattern"].Value.Trim()}";
        return path.Contains('/') ? "Path-scoped (convention heuristic; file patterns unavailable)" : "Repository-wide (convention heuristic)";
    }

    public static bool HasReparsePoint(string fullPath, string root)
    {
        var current = fullPath;
        while (!string.Equals(current, root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            current = Path.GetDirectoryName(current) ?? root;
        }
        return false;
    }

    [GeneratedRegex("^(#{1,6})\\s+(.+?)\\s*#*\\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingRegex();
}

public sealed record InstructionSourceReport(string Root, IReadOnlyList<InstructionSourceInfo> Sources)
{
    [System.Text.Json.Serialization.JsonPropertyOrder(-1)]
    public int SchemaVersion => 1;
}
public sealed record InstructionSourceInfo(string Path, string Recognition, string Scope, IReadOnlyList<InstructionSection> Sections,
    long? ByteCount, int? ApproximateTokenCount, string? ContentDigest, string Status);
public sealed record InstructionSection(string Heading, string Anchor, int Level);
