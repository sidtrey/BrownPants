using System.Text.Json;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli;

internal static class EvidenceCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length > 0 && (args[0] is "--help" or "-h"))
        {
            WriteHelp(output);
            return 0;
        }

        var repository = Directory.GetCurrentDirectory();
        var repositoryProvided = false;
        var format = "json";
        var limit = 200;
        var semantic = false;
        var clues = new List<string>();
        var changedPaths = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--format":
                    if (++index >= args.Length) { error.WriteLine("Missing value for --format. Expected json."); return 2; }
                    format = args[index].ToLowerInvariant();
                    break;
                case "--limit":
                    if (++index >= args.Length || !int.TryParse(args[index], out limit) || limit is < 1 or > 10_000)
                    {
                        error.WriteLine("Invalid --limit value. Expected an integer from 1 to 10000.");
                        return 2;
                    }
                    break;
                case "--semantic":
                    semantic = true;
                    break;
                case "--clue":
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]) || args[index].StartsWith("--", StringComparison.Ordinal))
                    {
                        error.WriteLine("Missing or empty value for focus option --clue.");
                        return 2;
                    }
                    if (args[index].Length > 1000)
                    {
                        error.WriteLine("Focus clues must be 1000 characters or fewer.");
                        return 2;
                    }
                    clues.Add(args[index]);
                    if (clues.Count > 20)
                    {
                        error.WriteLine("Provide no more than 20 --clue values for focused evidence.");
                        return 2;
                    }
                    break;
                case "--changed":
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]) || args[index].StartsWith("--", StringComparison.Ordinal))
                    {
                        error.WriteLine("Missing or empty value for focus option --changed. Provide a repository-relative C#, Visual Basic, or C/C++ source path.");
                        return 2;
                    }
                    changedPaths.Add(args[index]);
                    if (changedPaths.Count > 20)
                    {
                        error.WriteLine("Provide no more than 20 --changed values for focused evidence.");
                        return 2;
                    }
                    break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal))
                    {
                        error.WriteLine($"Unknown evidence option '{args[index]}'.");
                        return 2;
                    }
                    if (repositoryProvided)
                    {
                        error.WriteLine("Only one repository path may be supplied.");
                        return 2;
                    }
                    repository = args[index];
                    repositoryProvided = true;
                    break;
            }
        }

        if (format != "json")
        {
            error.WriteLine("Unsupported evidence format. Expected json.");
            return 2;
        }

        try
        {
            var focused = clues.Count > 0 || changedPaths.Count > 0;
            var scanLimit = focused ? 10_000 : limit;
            var normalizedChanged = focused
                ? ValidateChangedPaths(repository, changedPaths)
                : Array.Empty<string>();
            var inventory = RepositoryScanner.Scan(repository, scanLimit, semantic);
            var report = EvidenceReportAnalyzer.Create(inventory, semantic, scanLimit);
            if (focused)
                report = EvidenceFocusAnalyzer.Select(inventory, report, clues, normalizedChanged, limit);
            output.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (exception is ArgumentException && (clues.Count > 0 || changedPaths.Count > 0))
            {
                error.WriteLine($"Invalid focused evidence input: {exception.Message}");
                return 2;
            }
            error.WriteLine($"Unable to produce evidence report: {exception.Message}");
            return 1;
        }
    }

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine("Usage: brownpants evidence [repository] [--format json] [--limit 1..10000] [--semantic] [--clue <literal> ...] [--changed <relative-source-path> ...]");
        output.WriteLine("Produces a structured, source-located evidence handoff for any SDD engine.");
        output.WriteLine("--clue and --changed select task-focused findings; selectors combine as a union and findings explain why they matched.");
        output.WriteLine("--changed without --semantic selects source-local candidates and native-flow impact; add --semantic to include resolved managed callers.");
        output.WriteLine("Architecture, framework, and test-path findings are candidates; only Roslyn-resolved calls are resolved.");
        output.WriteLine("--semantic loads MSBuild projects; imported build targets may execute.");
    }

    private static string[] ValidateChangedPaths(string repository, IReadOnlyList<string> changedPaths)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repository));
        var discovered = new HashSet<string>(RepositoryScanner.DiscoverFiles(root),
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var result = new SortedSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var input in changedPaths)
        {
            if (Path.IsPathRooted(input))
                throw new ArgumentException($"Invalid focus path '{input}': use a repository-relative path.");
            var slashPath = input.Replace('\\', '/');
            if (slashPath.Split('/').Any(segment => segment == ".."))
                throw new ArgumentException($"Invalid focus path '{input}': parent traversal and empty path segments are not allowed.");
            var normalized = slashPath;
            while (normalized.StartsWith("./", StringComparison.Ordinal)) normalized = normalized[2..];
            var segments = normalized.Split('/');
            if (segments.Any(segment => segment is ".." or ""))
                throw new ArgumentException($"Invalid focus path '{input}': parent traversal and empty path segments are not allowed.");
            var extension = Path.GetExtension(normalized).ToLowerInvariant();
            if (!normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && !normalized.EndsWith(".vb", StringComparison.OrdinalIgnoreCase)
                && extension is not (".c" or ".cc" or ".cpp" or ".cxx" or ".h" or ".hh" or ".hpp" or ".hxx" or ".inl" or ".ipp"))
                throw new ArgumentException($"Invalid focus path '{normalized}': changed paths must be C#, Visual Basic, or C/C++ source files.");
            var absolute = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
            var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(rootPrefix, OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal)
                || !discovered.Contains(normalized))
                throw new ArgumentException($"Invalid focus path '{input}': file is missing or outside the scanned repository.");
            result.Add(normalized);
        }
        return result.ToArray();
    }
}
