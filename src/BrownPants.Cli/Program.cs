using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Cli;

public static class Program
{
    private const int MaximumInvestigationClues = 20;
    private const int MaximumClueLength = 1000;

    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            output.WriteLine("BrownPants - deterministic brownfield evidence for spec-driven development");
            output.WriteLine("Usage: brownpants inspect [path] [--format markdown|json] [--limit 1..10000] [--semantic]");
            output.WriteLine("       brownpants investigate [path] --clue <text> [--clue <text> ...] [--format markdown|json] [--limit 1..10000]");
            output.WriteLine("       brownpants impact <repository> --changed <relative-source-path> [--changed <relative-source-path> ...] [--format markdown|json] [--limit 1..10000]");
            output.WriteLine("       brownpants instructions [path] [--source <relative-path>] [--section <anchor>] [--format markdown|json]");
            output.WriteLine("       brownpants frame create <repository> <draft.json> <output.md> [--overwrite]");
            output.WriteLine("       brownpants frame validate <repository> <frame.md>");
            output.WriteLine("       brownpants placement --rule <text> [--scope repository|feature|unknown] [--durability enduring|feature|unknown] [--purpose principle|outcome|agent-workflow|mixed|unknown] [--format markdown|json]");
            return args.Length == 0 ? 2 : 0;
        }

        if (string.Equals(args[0], "instructions", StringComparison.OrdinalIgnoreCase))
            return RunInstructions(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "frame", StringComparison.OrdinalIgnoreCase))
            return RunFrame(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "placement", StringComparison.OrdinalIgnoreCase))
            return RunPlacement(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "investigate", StringComparison.OrdinalIgnoreCase))
            return RunInvestigation(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "impact", StringComparison.OrdinalIgnoreCase))
            return RunImpact(args.Skip(1).ToArray(), output, error);

        if (!string.Equals(args[0], "inspect", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine($"Unknown command '{args[0]}'. Use --help for usage.");
            return 2;
        }

        var path = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sampleLimit = 200;
        var analyzeSemantics = false;
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index] == "--format")
            {
                if (++index >= args.Length)
                {
                    error.WriteLine("Missing value for --format. Expected markdown or json.");
                    return 2;
                }
                format = args[index].ToLowerInvariant();
            }
            else if (args[index] == "--limit")
            {
                if (++index >= args.Length || !int.TryParse(args[index], out sampleLimit) || sampleLimit is < 1 or > 10_000)
                {
                    error.WriteLine("Invalid --limit value. Expected an integer from 1 to 10000.");
                    return 2;
                }
            }
            else if (args[index] == "--semantic")
            {
                analyzeSemantics = true;
            }
            else if (args[index].StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown option '{args[index]}'.");
                return 2;
            }
            else if (!pathProvided)
            {
                path = args[index];
                pathProvided = true;
            }
            else
            {
                error.WriteLine("Only one repository path may be supplied.");
                return 2;
            }
        }

        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }

        try
        {
            var inventory = RepositoryScanner.Scan(path, sampleLimit, analyzeSemantics);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true })
                : MarkdownReport.Render(inventory));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to inspect repository: {exception.Message}");
            return 1;
        }
    }

    private static int RunPlacement(string[] args, TextWriter output, TextWriter error)
    {
        string? rule = null;
        var scope = "unknown";
        var durability = "unknown";
        var purpose = "unknown";
        var format = "markdown";

        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (option is not ("--rule" or "--scope" or "--durability" or "--purpose" or "--format"))
            {
                error.WriteLine($"Unknown placement option '{option}'. Use --help for usage.");
                return 2;
            }
            if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
            {
                error.WriteLine($"Missing value for {option}.");
                return 2;
            }
            var value = args[index];
            switch (option)
            {
                case "--rule": rule = value; break;
                case "--scope": scope = value; break;
                case "--durability": durability = value; break;
                case "--purpose": purpose = value; break;
                case "--format": format = value.ToLowerInvariant(); break;
            }
        }

        if (string.IsNullOrWhiteSpace(rule))
        {
            error.WriteLine("A non-empty --rule value is required.");
            return 2;
        }
        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }

        try
        {
            var report = PlacementGuidanceAnalyzer.Analyze(rule, scope, durability, purpose);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : PlacementGuidanceRenderer.Render(report));
            return 0;
        }
        catch (ArgumentException exception)
        {
            error.WriteLine($"Invalid placement input: {exception.Message}");
            return 2;
        }
    }

    private static int RunFrame(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 3 && string.Equals(args[0], "validate", StringComparison.OrdinalIgnoreCase))
        {
            var validationRoot = Path.GetFullPath(args[1]);
            var validationPath = Path.GetFullPath(Path.Combine(validationRoot, args[2]));
            var relativePath = Path.GetRelativePath(validationRoot, validationPath);
            if (relativePath == ".." || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
            {
                error.WriteLine("Frame path must be inside the repository.");
                return 2;
            }
            if (HasReparsePointAncestor(validationPath, validationRoot))
            {
                error.WriteLine("Frame path cannot pass through a symbolic link or reparse point.");
                return 2;
            }
            try
            {
                var result = InstructionContextFrameAnalyzer.Validate(validationRoot, relativePath);
                output.WriteLine(result);
                return result.StartsWith("Frame is stale", StringComparison.Ordinal) ? 1 : 0;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                error.WriteLine($"Unable to validate frame: {exception.Message}");
                return 1;
            }
        }
        if (args.Length < 4 || !string.Equals(args[0], "create", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: brownpants frame create <repository> <draft.json> <output.md> [--overwrite]");
            return 2;
        }
        var repository = Path.GetFullPath(args[1]);
        var draft = Path.GetFullPath(args[2]);
        var destination = Path.GetFullPath(Path.Combine(repository, args[3]));
        var overwrite = args.Length == 5 && args[4] == "--overwrite";
        if (args.Length > 5 || args.Length == 5 && !overwrite)
        {
            error.WriteLine("The only supported option is --overwrite.");
            return 2;
        }
        var relative = Path.GetRelativePath(repository, destination);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            error.WriteLine("Frame output must be inside the repository.");
            return 2;
        }
        if (HasReparsePointAncestor(destination, repository))
        {
            error.WriteLine("Frame output cannot pass through a symbolic link or reparse point.");
            return 2;
        }
        try
        {
            var frame = InstructionContextFrameAnalyzer.RenderDraft(repository, draft);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (overwrite) File.WriteAllText(destination, frame);
            else
            {
                using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                writer.Write(frame);
            }
            output.WriteLine($"Created frame: {relative.Replace('\\', '/')}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException)
        {
            error.WriteLine($"Unable to create frame: {exception.Message}");
            return 1;
        }
    }

    private static bool HasReparsePointAncestor(string fullPath, string root)
    {
        var current = fullPath;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        while (!string.Equals(current, root, comparison))
        {
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
            current = Path.GetDirectoryName(current) ?? root;
        }
        return false;
    }

    private static int RunInstructions(string[] args, TextWriter output, TextWriter error)
    {
        var path = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sources = new List<string>();
        string? sectionAnchor = null;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--format")
            {
                if (++index >= args.Length)
                {
                    error.WriteLine("Missing value for --format. Expected markdown or json.");
                    return 2;
                }
                format = args[index].ToLowerInvariant();
            }
            else if (args[index] == "--source")
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine("Missing value for --source. Provide a repository-relative file path.");
                    return 2;
                }
                sources.Add(args[index]);
            }
            else if (args[index] == "--section")
            {
                if (sectionAnchor is not null || ++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(args[index]))
                {
                    error.WriteLine("Missing or repeated value for --section. Provide one section anchor.");
                    return 2;
                }
                sectionAnchor = args[index];
            }
            else if (args[index].StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown or incomplete option '{args[index]}'.");
                return 2;
            }
            else if (!pathProvided) { path = args[index]; pathProvided = true; }
            else { error.WriteLine("Only one repository path may be supplied."); return 2; }
        }
        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }
        if (sectionAnchor is not null && sources.Count != 1)
        {
            error.WriteLine("A section request requires exactly one --source <relative-path>.");
            return 2;
        }
        try
        {
            if (sectionAnchor is not null)
            {
                var section = InstructionSourceAnalyzer.ReadSection(path, sources[0], sectionAnchor);
                output.WriteLine(format == "json"
                    ? JsonSerializer.Serialize(section, new JsonSerializerOptions { WriteIndented = true })
                    : InstructionSourceReportRenderer.RenderSection(section));
                return 0;
            }

            var report = InstructionSourceAnalyzer.Analyze(path, sources);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : InstructionSourceReportRenderer.Render(report));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to retrieve instruction source or section: {exception.Message}");
            return 1;
        }
    }

    private static int RunInvestigation(string[] args, TextWriter output, TextWriter error)
    {
        var path = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sampleLimit = 200;
        var clues = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--clue")
            {
                if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index])
                    || args[index] is "--clue" or "--format" or "--limit")
                {
                    error.WriteLine("Missing or empty value for --clue. Provide a literal identifier, error phrase, or path fragment.");
                    return 2;
                }
                if (args[index].Length > MaximumClueLength)
                {
                    error.WriteLine($"Clues must be {MaximumClueLength} characters or fewer.");
                    return 2;
                }
                clues.Add(args[index]);
                if (clues.Count > MaximumInvestigationClues)
                {
                    error.WriteLine($"Provide no more than {MaximumInvestigationClues} --clue values.");
                    return 2;
                }
            }
            else if (args[index] == "--format")
            {
                if (++index >= args.Length)
                {
                    error.WriteLine("Missing value for --format. Expected markdown or json.");
                    return 2;
                }
                format = args[index].ToLowerInvariant();
            }
            else if (args[index] == "--limit")
            {
                if (++index >= args.Length || !int.TryParse(args[index], out sampleLimit) || sampleLimit is < 1 or > 10_000)
                {
                    error.WriteLine("Invalid --limit value. Expected an integer from 1 to 10000.");
                    return 2;
                }
            }
            else if (args[index].StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown option '{args[index]}'.");
                return 2;
            }
            else if (!pathProvided)
            {
                path = args[index];
                pathProvided = true;
            }
            else
            {
                error.WriteLine("Only one repository path may be supplied.");
                return 2;
            }
        }

        if (clues.Count == 0)
        {
            error.WriteLine("At least one --clue value is required. Use an identifier, error phrase, or path fragment from the bug report.");
            return 2;
        }

        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }

        try
        {
            var report = RepositoryInvestigator.Investigate(path, clues, sampleLimit);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : InvestigationReportRenderer.Render(report));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to investigate repository: {exception.Message}");
            return 1;
        }
    }

    private static int RunImpact(string[] args, TextWriter output, TextWriter error)
    {
        var root = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sampleLimit = 200;
        var changedPaths = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--changed")
            {
                if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]) || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine("Missing value for --changed. Provide a repository-relative C# or Visual Basic source path.");
                    return 2;
                }
                changedPaths.Add(args[index]);
            }
            else if (args[index] == "--format")
            {
                if (++index >= args.Length)
                {
                    error.WriteLine("Missing value for --format. Expected markdown or json.");
                    return 2;
                }
                format = args[index].ToLowerInvariant();
            }
            else if (args[index] == "--limit")
            {
                if (++index >= args.Length || !int.TryParse(args[index], out sampleLimit) || sampleLimit is < 1 or > 10_000)
                {
                    error.WriteLine("Invalid --limit value. Expected an integer from 1 to 10000.");
                    return 2;
                }
            }
            else if (args[index].StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown option '{args[index]}'.");
                return 2;
            }
            else if (!pathProvided)
            {
                root = args[index];
                pathProvided = true;
            }
            else
            {
                error.WriteLine("Only one repository path may be supplied.");
                return 2;
            }
        }
        if (changedPaths.Count == 0)
        {
            error.WriteLine("At least one --changed path is required.");
            return 2;
        }
        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }
        try
        {
            var inventory = RepositoryScanner.Scan(root, sampleLimit, analyzeSemantics: true);
            var report = CallImpactAnalyzer.Analyze(inventory.Root, changedPaths, RepositoryScanner.DiscoverFiles(inventory.Root),
                inventory.CSharp.Semantic!, sampleLimit);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : CallImpactReportRenderer.Render(report));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to analyze call impact: {exception.Message}");
            return 1;
        }
    }
}




