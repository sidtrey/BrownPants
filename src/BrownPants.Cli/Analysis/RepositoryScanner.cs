namespace BrownPants.Cli.Analysis;

public static class RepositoryScanner
{
    private static readonly IComparer<string> StablePathComparer = Comparer<string>.Create((left, right) =>
    {
        var result = StringComparer.OrdinalIgnoreCase.Compare(left, right);
        return result != 0 ? result : StringComparer.Ordinal.Compare(left, right);
    });

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".hg", ".svn", ".vs", ".idea", "bin", "obj", "TestResults", "test-results", "testresults", "node_modules",
        "packages", "coverage", "dist", "target", "artifacts", ".tools", "venv", ".venv", ".tox", "__pycache__",
        ".gradle", ".next", ".nuxt", ".terraform", ".dart_tool", ".build", "Pods", "__pypackages__",
        ".pytest_cache", ".mypy_cache", ".ruff_cache", ".cache", "bower_components", "jspm_packages",
        ".angular", ".svelte-kit", ".vite", ".turbo", ".vercel"
    };

    private static readonly string[] KnownMarkers =
    {
        "*.sln", "*.slnx", "global.json", "Directory.Build.props", "*.csproj", "*.vbproj", "*.vcxproj", "*.vcproj",
        "pyproject.toml", "requirements.txt", "Pipfile", "package.json", "pnpm-workspace.yaml",
        "go.mod", "Cargo.toml", "pom.xml", "build.gradle", "build.gradle.kts",
        "Gemfile", "composer.json"
    };

    public static RepositoryInventory Scan(string path, int sampleLimit = 200, bool analyzeSemantics = false)
    {
        if (sampleLimit is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "Sample limit must be between 1 and 10000.");
        var root = Path.GetFullPath(path);
        var files = DiscoverFiles(root);
        var extensions = files
            .Select(file => Path.GetExtension(file).ToLowerInvariant() is { Length: > 0 } extension
                ? extension : "[no extension]")
            .GroupBy(extension => extension, StringComparer.Ordinal)
            .Select(group => new ExtensionCount(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Extension, StringComparer.Ordinal)
            .ToArray();

        var projectSystems = ProjectSystemAnalyzer.Analyze(files);
        var markers = files.Where(file => KnownMarkers.Any(pattern => Matches(file, pattern)))
            .Concat(projectSystems.SelectMany(system => system.ManifestPaths))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(file => file, StablePathComparer).ToArray();
        var languageCoverage = SourceLanguageCoverageAnalyzer.Analyze(files);
        var tests = files.Where(IsTestPath).OrderBy(file => file, StablePathComparer).ToArray();
        var topLevel = Directory.EnumerateFileSystemEntries(root)
            .Select(entry => Path.GetFileName(entry)!)
            .Where(name => !string.IsNullOrEmpty(name) && !IgnoredDirectories.Contains(name))
            .OrderBy(name => name, StablePathComparer)
            .ToArray()!;

        var sddAnalysis = SddWorkflowAnalyzer.Analyze(root, files, sampleLimit);
        var dotNetProjects = DotNetProjectAnalyzer.Analyze(root,
            files.Where(file => file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)));
        var nativeProjects = NativeProjectAnalyzer.Analyze(root,
            files.Where(file => file.EndsWith(".vcxproj", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".vcproj", StringComparison.OrdinalIgnoreCase)));
        var csharp = CSharpAnalyzer.Analyze(root,
            files.Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)), dotNetProjects, sampleLimit);
        var visualBasic = VisualBasicAnalyzer.Analyze(root,
            files.Where(file => file.EndsWith(".vb", StringComparison.OrdinalIgnoreCase)), sampleLimit);
        var architecturePatterns = ArchitecturePatternAnalyzer.Analyze(root, files, sampleLimit);
        if (analyzeSemantics)
        {
            var semantic = CSharpSemanticAnalyzer.AnalyzeRepository(
                root,
                dotNetProjects.Select(project => project.Path).ToArray(),
                files.Where(file => file.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).ToArray(),
                sampleLimit);
            csharp = csharp with { Semantic = semantic };
        }

        return new RepositoryInventory(
            root,
            files.Count,
            extensions,
            markers,
            projectSystems,
            languageCoverage,
            tests,
            topLevel,
            sddAnalysis.Workflows,
            sddAnalysis.Issues,
            csharp,
            visualBasic,
            architecturePatterns,
            nativeProjects,
            dotNetProjects);
    }

    public static IReadOnlyList<string> DiscoverFiles(string path)
    {
        var root = Path.GetFullPath(path);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Directory '{root}' does not exist.");

        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory).OrderBy(value => value, StablePathComparer))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    var name = Path.GetFileName(entry);
                    if (!IgnoredDirectories.Contains(name) && (attributes & FileAttributes.ReparsePoint) == 0)
                        pending.Push(entry);
                }
                else if ((attributes & FileAttributes.ReparsePoint) == 0)
                {
                    files.Add(Path.GetRelativePath(root, entry).Replace('\\', '/'));
                }
            }
        }

        files.Sort(StablePathComparer);
        return files;
    }

    internal static bool IsTestPath(string file)
    {
        var parts = file.Split('/');
        return parts.Any(part => part.Equals("test", StringComparison.OrdinalIgnoreCase)
            || part.Equals("tests", StringComparison.OrdinalIgnoreCase))
            || Path.GetFileName(file).Contains("test", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(string path, string pattern)
    {
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
            return path.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        return path.Equals(pattern, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith('/' + pattern, StringComparison.OrdinalIgnoreCase);
    }
}



