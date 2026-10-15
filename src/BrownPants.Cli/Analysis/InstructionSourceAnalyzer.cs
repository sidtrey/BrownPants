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
    private static readonly Regex HeadingPattern = new(@"^ {0,3}(?<marks>#{1,6})[ \t]+(?<title>.+?)[ \t]*#*[ \t]*$", RegexOptions.CultureInvariant);
    private static readonly Regex FenceStartPattern = new(@"^ {0,3}(?<fence>`{3,}|~{3,})(?<info>.*)$", RegexOptions.CultureInvariant);
    private static readonly Regex FenceEndPattern = new(@"^ {0,3}(?<fence>`{3,}|~{3,})[ \t]*$", RegexOptions.CultureInvariant);

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
            var resolved = ValidateSourcePath(root, source);
            selected[resolved.RelativePath] = "Explicit";
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

    public static InstructionSectionResult ReadSection(string repository, string sourcePath, string anchor)
    {
        if (string.IsNullOrWhiteSpace(anchor)) throw new ArgumentException("A section anchor is required.", nameof(anchor));
        var root = Path.GetFullPath(repository);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Directory '{root}' does not exist.");
        var resolved = ValidateSourcePath(root, sourcePath);
        var fullPath = resolved.FullPath;
        if (new FileInfo(fullPath).Length > MaximumSourceBytes)
            throw new IOException($"Instruction source '{sourcePath}' is not readable (TooLarge; maximum {MaximumSourceBytes} bytes).");

        string content;
        try
        {
            var bytes = File.ReadAllBytes(fullPath);
            if (bytes.Length > MaximumSourceBytes)
                throw new IOException($"Instruction source '{sourcePath}' is not readable (TooLarge; maximum {MaximumSourceBytes} bytes).");
            content = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new IOException($"Instruction source '{sourcePath}' is not readable (UnsupportedEncoding; expected UTF-8).", exception);
        }

        var sections = ParseSections(content);
        var section = sections.SingleOrDefault(item => item.Anchor.Equals(anchor, StringComparison.OrdinalIgnoreCase));
        if (section is null) throw new ArgumentException($"Section anchor '{anchor}' was not found in '{sourcePath}'.");
        var lines = NormalizeLineEndings(content).Split('\n');
        var selected = string.Join('\n', lines[(section.StartLine - 1)..section.EndLine]);
        var normalizedContent = NormalizeLineEndings(content);
        var nextHeading = sections.Any(item => item.StartLine == section.EndLine + 1 && item.Level <= section.Level);
        if (nextHeading || normalizedContent.EndsWith('\n')) selected += "\n";

        return new(1, resolved.RelativePath, section.Heading, section.Anchor, section.StartLine, section.EndLine, selected);
    }

    private static (string RelativePath, string FullPath) ValidateSourcePath(string root, string source)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, source));
        var relativePath = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        if (relativePath == ".." || relativePath.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
            throw new ArgumentException($"Instruction source '{source}' must be inside the repository.");
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Instruction source '{source}' was not found.", fullPath);
        if (HasReparsePoint(fullPath, root))
            throw new ArgumentException($"Instruction source '{source}' passes through a symbolic link or reparse point.");
        return (relativePath, fullPath);
    }

    private static IReadOnlyList<InstructionSection> ParseSections(string content)
    {
        var result = new List<InstructionSection>();
        var anchorCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var lines = NormalizeLineEndings(content).Split('\n');
        var physicalLineCount = lines.Length - (lines.Length > 1 && lines[^1].Length == 0 ? 1 : 0);
        var headings = new List<(string Heading, string Anchor, int Level, int Line)>();
        char fenceMarker = '\0';
        var fenceLength = 0;
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (fenceMarker != '\0')
            {
                var fenceEnd = FenceEndPattern.Match(line);
                if (fenceEnd.Success && fenceEnd.Groups["fence"].Value[0] == fenceMarker
                    && fenceEnd.Groups["fence"].Value.Length >= fenceLength)
                {
                    fenceMarker = '\0';
                    fenceLength = 0;
                }
                continue;
            }

            var fenceStart = FenceStartPattern.Match(line);
            if (fenceStart.Success && !(fenceStart.Groups["fence"].Value[0] == '`' && fenceStart.Groups["info"].Value.Contains('`')))
            {
                fenceMarker = fenceStart.Groups["fence"].Value[0];
                fenceLength = fenceStart.Groups["fence"].Value.Length;
                continue;
            }

            var match = HeadingPattern.Match(line);
            if (!match.Success) continue;
            var title = match.Groups["title"].Value.Trim();
            var anchor = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9\s-]", string.Empty).Trim().Replace(' ', '-');
            if (anchorCounts.TryGetValue(anchor, out var count))
            {
                anchorCounts[anchor] = count + 1;
                anchor = $"{anchor}-{count}";
            }
            else anchorCounts.Add(anchor, 1);
            headings.Add((title, anchor, match.Groups["marks"].Value.Length, index + 1));
        }

        for (var index = 0; index < headings.Count; index++)
        {
            var heading = headings[index];
            var endLine = physicalLineCount;
            for (var next = index + 1; next < headings.Count; next++)
            {
                if (headings[next].Level <= heading.Level)
                {
                    endLine = headings[next].Line - 1;
                    break;
                }
            }
            result.Add(new(heading.Heading, heading.Anchor, heading.Level, heading.Line, Math.Max(heading.Line, endLine)));
        }
        return result;
    }

    private static string NormalizeLineEndings(string content) => content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

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
}

public sealed record InstructionSourceReport(string Root, IReadOnlyList<InstructionSourceInfo> Sources)
{
    [System.Text.Json.Serialization.JsonPropertyOrder(-1)]
    public int SchemaVersion => 1;
}
public sealed record InstructionSourceInfo(string Path, string Recognition, string Scope, IReadOnlyList<InstructionSection> Sections,
    long? ByteCount, int? ApproximateTokenCount, string? ContentDigest, string Status);
public sealed record InstructionSection(string Heading, string Anchor, int Level, int StartLine, int EndLine);
public sealed record InstructionSectionResult(int SchemaVersion, string SourcePath, string Heading, string Anchor,
    int StartLine, int EndLine, string Content);
