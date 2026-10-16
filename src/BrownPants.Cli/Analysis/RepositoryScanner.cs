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
        "coverage", "dist", "target", "artifacts", ".tools", "venv", ".venv", ".tox", "__pycache__",
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
        var managedMethodIndex = csharp.ManagedMethodIndex.Concat(visualBasic.ManagedMethodIndex).ToArray();
        var architecturePatterns = ArchitecturePatternAnalyzer.Analyze(root, files, sampleLimit,
            managedMethodIndex, csharp.ManagedMethodIndexTruncated || visualBasic.ManagedMethodIndexTruncated);
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
            dotNetProjects)
        {
            JavaScriptTypeScript = JavaScriptTypeScriptAnalyzer.Analyze(root, files, sampleLimit)
        };
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
                    if (!ShouldIgnoreDirectory(root, entry, name) && (attributes & FileAttributes.ReparsePoint) == 0)
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

    private static bool ShouldIgnoreDirectory(string root, string directory, string name)
    {
        if (!name.Equals("packages", StringComparison.OrdinalIgnoreCase))
            return IgnoredDirectories.Contains(name);

        var relativeDirectory = Path.GetRelativePath(root, directory).Replace('\\', '/');
        return !ReadWorkspacePatterns(root).Any(pattern => WorkspacePatternCouldContainDirectory(pattern, relativeDirectory));
    }

    private static IReadOnlyList<string> ReadWorkspacePatterns(string root)
    {
        var patterns = new List<string>();
        var packageJson = Path.Combine(root, "package.json");
        if (File.Exists(packageJson))
        {
            try
            {
                var info = new FileInfo(packageJson);
                if (info.Length <= 1024 * 1024)
                {
                    using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(packageJson),
                        new System.Text.Json.JsonDocumentOptions { MaxDepth = 64 });
                    if (document.RootElement.TryGetProperty("workspaces", out var workspaces))
                    {
                        var entries = workspaces.ValueKind == System.Text.Json.JsonValueKind.Array ? workspaces :
                            workspaces.ValueKind == System.Text.Json.JsonValueKind.Object
                            && workspaces.TryGetProperty("packages", out var nested) ? nested : default;
                        if (entries.ValueKind == System.Text.Json.JsonValueKind.Array)
                            patterns.AddRange(entries.EnumerateArray().Where(item => item.ValueKind == System.Text.Json.JsonValueKind.String)
                                .Select(item => item.GetString()!));
                    }
                }
            }
            catch (System.Text.Json.JsonException) { }
        }

        var pnpmWorkspace = Path.Combine(root, "pnpm-workspace.yaml");
        if (File.Exists(pnpmWorkspace))
        {
            try
            {
                if (new FileInfo(pnpmWorkspace).Length <= 1024 * 1024)
                    foreach (var line in File.ReadLines(pnpmWorkspace))
                    {
                        var pattern = line.Trim().Trim('-', ' ', '\'', '"');
                        if (pattern.Length > 0 && !pattern.EndsWith(':')) patterns.Add(pattern);
                    }
            }
            catch (IOException) { }
        }
        return patterns;
    }

    private static bool WorkspacePatternCouldContainDirectory(string pattern, string directory)
    {
        if (pattern.StartsWith('!')) return false;
        var patternParts = pattern.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var directoryParts = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var index = 0;
        while (index < patternParts.Length && index < directoryParts.Length)
        {
            if (patternParts[index] == "**") return true;
            if (patternParts[index] != "*" && !patternParts[index].Equals(directoryParts[index], StringComparison.OrdinalIgnoreCase)) return false;
            index++;
        }
        return index == directoryParts.Length;
    }

    internal static bool IsTestPath(string file)
    {
        var parts = file.Split('/');
        return parts.Any(part => part.Equals("test", StringComparison.OrdinalIgnoreCase)
            || part.Equals("tests", StringComparison.OrdinalIgnoreCase))
            || IsTestFileName(Path.GetFileName(file));
    }

    private static bool IsTestFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (name.Equals("Test", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Tests", StringComparison.OrdinalIgnoreCase))
            return true;

        if (name.Length > 4 && name.StartsWith("Test", StringComparison.OrdinalIgnoreCase))
        {
            var next = name[4];
            if (char.IsUpper(next) || next is '_' or '-' or '.' || char.IsDigit(next))
                return true;
        }

        return IsTestSuffix(name, "Test") || IsTestSuffix(name, "Tests");
    }

    private static bool IsTestSuffix(string name, string suffix)
    {
        if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffixStart = name.Length - suffix.Length;
        return suffixStart > 0
            && (char.IsLower(name[suffixStart - 1]) && char.IsUpper(name[suffixStart])
                || name[suffixStart - 1] is '_' or '-' or '.');
    }

    private static bool Matches(string path, string pattern)
    {
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
            return path.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        return path.Equals(pattern, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith('/' + pattern, StringComparison.OrdinalIgnoreCase);
    }
}



