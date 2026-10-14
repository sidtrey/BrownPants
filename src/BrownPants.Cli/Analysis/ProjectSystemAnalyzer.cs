namespace BrownPants.Cli.Analysis;

public static class ProjectSystemAnalyzer
{
    private static readonly (string Name, Func<string, bool> Matches)[] Rules =
    {
        (".NET", path => HasExtension(path, ".sln", ".slnx", ".csproj", ".vbproj", ".fsproj")
            || HasName(path, "global.json", "Directory.Build.props", "Directory.Build.targets", "packages.config")),
        ("Apple", path => HasName(path, "Package.swift", "Package.resolved", "Podfile", "Podfile.lock", "project.pbxproj")
            || path.EndsWith(".xcworkspace/contents.xcworkspacedata", StringComparison.OrdinalIgnoreCase)),
        ("C/C++", path => HasExtension(path, ".vcxproj", ".vcproj")
            || HasName(path, "CMakeLists.txt", "CMakePresets.json", "meson.build")),
        ("Containers", path => HasName(path, "docker-compose.yml", "docker-compose.yaml", "compose.yml", "compose.yaml")
            || Path.GetFileName(path).StartsWith("Dockerfile", StringComparison.OrdinalIgnoreCase)),
        ("Dart", path => HasName(path, "pubspec.yaml", "pubspec.lock")),
        ("Elixir", path => HasName(path, "mix.exs", "mix.lock")),
        ("Go", path => HasName(path, "go.mod", "go.work")),
        ("Infrastructure", path => HasExtension(path, ".tf", ".tfvars")
            || HasName(path, "Pulumi.yaml", "Pulumi.yml", "Chart.yaml", "kustomization.yaml", "kustomization.yml")),
        ("JavaScript/TypeScript", path => HasName(path, "package.json", "package-lock.json", "npm-shrinkwrap.json",
            "pnpm-lock.yaml", "pnpm-workspace.yaml", "yarn.lock", "bun.lock", "bun.lockb", "deno.json", "deno.jsonc")),
        ("JVM", path => HasName(path, "pom.xml", "build.gradle", "build.gradle.kts", "settings.gradle",
            "settings.gradle.kts", "gradlew", "build.xml")),
        ("Make", path => HasName(path, "Makefile", "GNUmakefile")),
        ("PHP", path => HasName(path, "composer.json", "composer.lock")),
        ("Python", IsPythonManifest),
        ("Ruby", path => HasName(path, "Gemfile", "Gemfile.lock") || HasExtension(path, ".gemspec")),
        ("Rust", path => HasName(path, "Cargo.toml", "Cargo.lock")),
        ("Scala", path => HasName(path, "build.sbt", "build.sc"))
    };

    public static IReadOnlyList<ProjectSystemInfo> Analyze(IEnumerable<string> paths)
    {
        var files = paths.ToArray();
        return Rules
            .Select(rule => new ProjectSystemInfo(rule.Name, files.Where(rule.Matches)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, RepositoryPathComparer.Instance)
                .ToArray()))
            .Where(system => system.ManifestPaths.Count > 0)
            .OrderBy(system => system.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsPythonManifest(string path)
    {
        var name = Path.GetFileName(path);
        return HasName(path, "pyproject.toml", "Pipfile", "Pipfile.lock", "poetry.lock", "uv.lock", "setup.py",
                "setup.cfg", "tox.ini", "environment.yml", "environment.yaml")
            || (name.StartsWith("requirements", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasName(string path, params string[] names) =>
        names.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

    private static bool HasExtension(string path, params string[] extensions) =>
        extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
