namespace BrownPants.Cli.Analysis;

internal static class CallTreeDiagramCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length > 0 && args[0].Equals("flow", StringComparison.OrdinalIgnoreCase))
            return MethodControlFlowDiagramCommand.Run(args.Skip(1).ToArray(), output, error);
        if (args.Length == 0 || args[0] is "--help" or "-h") return WriteHelp(output);
        if (!args[0].Equals("callers", StringComparison.OrdinalIgnoreCase))
            return Invalid(error, "Usage: brownpants chart callers <repository> --source <relative-path> --method <exact-symbol> --semantic --output <path>");

        string? repository = null;
        string? source = null;
        string? method = null;
        string? destination = null;
        var semanticEnabled = false;
        var overwrite = false;
        var format = "png";
        var direction = LocalFlowchartDirection.TopDown;
        var depth = 8;
        var sampleLimit = 1000;
        int? tileSize = null;
        var overlap = 64;
        string? themeName = null;
        string? themeFile = null;
        for (var index = 1; index < args.Length; index++)
        {
            var option = args[index];
            if (option is "--semantic")
            {
                if (semanticEnabled) return Invalid(error, "--semantic may be supplied only once.");
                semanticEnabled = true;
            }
            else if (option is "--overwrite")
            {
                if (overwrite) return Invalid(error, "--overwrite may be supplied only once.");
                overwrite = true;
            }
            else if (option is "--source" or "--method" or "--output" or "--format" or "--direction"
                or "--depth" or "--limit" or "--tile-size" or "--overlap" or "--theme" or "--theme-file")
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                    return Invalid(error, $"{option} requires a value.");
                var value = args[index];
                switch (option)
                {
                    case "--source":
                        if (source is not null) return Invalid(error, "--source may be supplied only once.");
                        source = value;
                        break;
                    case "--method":
                        if (method is not null) return Invalid(error, "--method may be supplied only once.");
                        method = value;
                        break;
                    case "--output":
                        if (destination is not null) return Invalid(error, "--output may be supplied only once.");
                        destination = value;
                        break;
                    case "--theme":
                        if (themeName is not null) return Invalid(error, "--theme may be supplied only once.");
                        themeName = value;
                        break;
                    case "--theme-file":
                        if (themeFile is not null) return Invalid(error, "--theme-file may be supplied only once.");
                        themeFile = value;
                        break;
                    case "--format": format = value.ToLowerInvariant(); break;
                    case "--direction":
                        direction = value.ToLowerInvariant() switch
                        {
                            "top-down" => LocalFlowchartDirection.TopDown,
                            "left-to-right" => LocalFlowchartDirection.LeftToRight,
                            _ => (LocalFlowchartDirection)(-1)
                        };
                        if ((int)direction < 0) return Invalid(error, "--direction must be top-down or left-to-right.");
                        break;
                    case "--depth":
                        if (!int.TryParse(value, out depth) || depth is < 1 or > 100)
                            return Invalid(error, "--depth must be an integer from 1 to 100.");
                        break;
                    case "--limit":
                        if (!int.TryParse(value, out sampleLimit) || sampleLimit is < 1 or > 10_000)
                            return Invalid(error, "--limit must be an integer from 1 to 10000.");
                        break;
                    case "--tile-size":
                        if (!int.TryParse(value, out var parsedTileSize) || parsedTileSize is < 256 or > 4000)
                            return Invalid(error, "--tile-size must be an integer from 256 to 4000.");
                        tileSize = parsedTileSize;
                        break;
                    case "--overlap":
                        if (!int.TryParse(value, out overlap) || overlap < 0)
                            return Invalid(error, "--overlap must be a non-negative integer.");
                        break;
                }
            }
            else if (option.StartsWith("-", StringComparison.Ordinal)) return Invalid(error, $"Unknown chart option '{option}'.");
            else if (repository is null) repository = option;
            else return Invalid(error, "Provide one repository path; source, method, and output use named options.");
        }

        if (repository is null || source is null || method is null || destination is null)
            return Invalid(error, "Repository, --source, --method, and --output are required.");
        if (themeName is not null && themeFile is not null) return Invalid(error, "Choose either --theme or --theme-file, not both.");
        if (!semanticEnabled)
            return Invalid(error, "Caller charts require explicit --semantic because project evaluation can execute imported MSBuild targets.");
        if (!Path.GetExtension(source).Equals(".cs", StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(source).Equals(".vb", StringComparison.OrdinalIgnoreCase))
            return Invalid(error, "--source must identify a C# or Visual Basic source file.");
        if (format is not ("png" or "drawio")) return Invalid(error, "--format must be png or drawio.");
        if (tileSize is not null && format != "png") return Invalid(error, "--tile-size requires PNG output.");
        if (tileSize is null && overlap != 64) return Invalid(error, "--overlap requires --tile-size.");
        if (tileSize is not null && overlap >= tileSize.Value / 2)
            return Invalid(error, "--overlap must be less than half the tile size.");

        try
        {
            var root = Path.GetFullPath(repository);
            if (!Directory.Exists(root)) throw new ArgumentException("The repository path must name an existing directory.");
            if (HasReparsePointAncestor(root, root))
                throw new ArgumentException("The repository root cannot be a symbolic link or reparse point.");
            var theme = ChartThemeParser.Select(root, themeName, themeFile);
            var relativeSource = source.Replace('\\', '/');
            if (Path.IsPathRooted(source) || source.Split('/', '\\').Any(part => part is ".." or ""))
                throw new ArgumentException("--source must be a repository-relative path without parent traversal.");
            var sourceFullPath = Path.GetFullPath(Path.Combine(root, source));
            var rootPrefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!sourceFullPath.StartsWith(rootPrefix, comparison))
                throw new ArgumentException("--source must remain inside the selected repository.");
            if (HasReparsePointAncestor(root, root) || HasReparsePointAncestor(sourceFullPath, root))
                throw new ArgumentException("The repository root and source path cannot pass through symbolic links or reparse points.");
            if (!File.Exists(sourceFullPath)) throw new FileNotFoundException($"Source '{relativeSource}' was not found.");

            output.WriteLine("Running local Roslyn semantic analysis. Project evaluation may execute imported MSBuild targets.");
            var inventory = RepositoryScanner.Scan(root, sampleLimit: 10_000);
            var files = RepositoryScanner.DiscoverFiles(root);
            var semantic = CSharpSemanticAnalyzer.AnalyzeRepository(root,
                inventory.DotNetProjects.Select(project => project.Path).ToArray(),
                files.Where(path => path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).ToArray(), 10_000);
            var result = CallTreeDiagramAnalyzer.Analyze(semantic, relativeSource, method, depth, sampleLimit);
            var rootNode = result.Document.Nodes.Single(node => node.Label.StartsWith("Target:", StringComparison.Ordinal));
            var summary = $"This chart traces callers of {rootNode.Label}: {result.DirectCallerCount} direct and {result.TransitiveCallerCount} transitive callers through depth {depth}."
                + (result.TestCandidateCount > 0
                    ? $" It marks {result.TestCandidateCount} test-path candidate(s); these paths do not prove runtime coverage."
                    : " Test coverage is not inferred from source paths.");
            if (!result.IsComplete)
                summary += " Analysis is partial: " + string.Join(" ", result.Limitations.Take(2));
            return LocalFlowchartCommand.WriteGeneratedDocument(root, destination, format, result.Document,
                direction, tileSize, overlap, overwrite, summary, output, error, theme);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException
            or LocalFlowchartFormatException or System.Security.SecurityException)
        {
            error.WriteLine($"Unable to create caller chart: {exception.Message}");
            return 1;
        }
    }

    private static int WriteHelp(TextWriter output)
    {
        output.WriteLine("Usage: brownpants chart <callers|flow> ...");
        output.WriteLine("  callers: show compiler-resolved methods that reach the selected target.");
        output.WriteLine("  flow: show Roslyn basic blocks and decisions inside one selected C# or Visual Basic method.");
        output.WriteLine("Usage: brownpants chart callers <repository> --source <relative.cs|vb> --method <exact-symbol> --semantic --output <relative.png|drawio> [--depth 1..100] [--limit 1..10000] [--direction top-down|left-to-right] [--theme default|blue-water|high-contrast | --theme-file <relative.json>] [--tile-size 256..4000] [--overlap pixels] [--overwrite]");
        output.WriteLine("  --method must exactly match the fully qualified compiler symbol reported by `brownpants inspect <repository> --semantic --format markdown`.");
        output.WriteLine("  Only compiler-resolved C#/VB relationships are edges. Analysis limitations are shown in the chart and report.");
        output.WriteLine("  Test-path nodes are candidates only; source paths do not establish executed test coverage.");
        output.WriteLine("  --semantic is required because Roslyn evaluates projects and imported MSBuild targets may execute.");
        output.WriteLine("  --theme selects a built-in palette; --theme-file reads a validated repository-local JSON palette. They are mutually exclusive.");
        MethodControlFlowDiagramCommand.WriteHelp(output);
        return 0;
    }

    private static int Invalid(TextWriter error, string message)
    {
        error.WriteLine(message);
        return 2;
    }

    private static bool HasReparsePointAncestor(string path, string root)
    {
        var current = path;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        while (true)
        {
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            if (string.Equals(current, root, comparison)) return false;
            current = Path.GetDirectoryName(current) ?? root;
        }
    }
}
