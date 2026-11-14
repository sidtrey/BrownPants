using System.Text.Json;

namespace BrownPants.Cli.Analysis;

internal static class ReactProjectAnalyzer
{
    private const int MaximumManifestCount = 20_000;
    private const int MaximumManifestBytes = 1_048_576;
    private const long MaximumTotalManifestBytes = 32L * 1024 * 1024;

    public static ReactSupportAnalysis Analyze(string root, IReadOnlyList<string> paths, int sampleLimit)
    {
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var manifests = paths.Where(path => Path.GetFileName(path).Equals("package.json", StringComparison.OrdinalIgnoreCase))
            .Distinct(pathComparer).OrderBy(path => path, RepositoryPathComparer.Instance).ToArray();
        var projects = new List<ReactProjectInfo>();
        var sourcePaths = new List<string>();
        long bytesRead = 0;
        foreach (var relativePath in manifests.Take(MaximumManifestCount))
        {
            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
                if (!IsContained(root, fullPath)) continue;
                var length = new FileInfo(fullPath).Length;
                if (length > MaximumManifestBytes || length > MaximumTotalManifestBytes - bytesRead) continue;
                var text = File.ReadAllText(fullPath);
                bytesRead += length;
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 });
                var package = document.RootElement;
                var reactVersion = FindReactVersion(package);
                if (reactVersion is null) continue;
                var projectName = package.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                    ? nameElement.GetString() : null;
                projectName ??= Path.GetFileName(Path.GetDirectoryName(fullPath));
                projects.Add(new ReactProjectInfo(projectName ?? "(unnamed React package)",
                    relativePath.Replace('\\', '/'), reactVersion));
                sourcePaths.Add(relativePath.Replace('\\', '/'));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
                or ArgumentException or NotSupportedException)
            {
                // An unreadable or malformed manifest is not evidence of a React project.
            }
        }

        var ordered = projects.OrderBy(project => project.ManifestPath, RepositoryPathComparer.Instance)
            .ThenBy(project => project.ManifestPath, StringComparer.Ordinal).ToArray();
        var selected = ordered.Take(sampleLimit).ToArray();
        return new ReactSupportAnalysis(ordered.Length, Math.Max(0, ordered.Length - selected.Length), selected,
            0, 0, Array.Empty<ReactSourceCandidate>(), 0, 0, Array.Empty<ReactSourceCandidate>())
        {
            AllProjectManifestPaths = sourcePaths.OrderBy(path => path, RepositoryPathComparer.Instance).ToArray()
        };
    }

    private static string? FindReactVersion(JsonElement package)
    {
        foreach (var sectionName in new[] { "dependencies", "devDependencies", "peerDependencies", "optionalDependencies" })
        {
            if (!package.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Object) continue;
            foreach (var dependency in section.EnumerateObject())
            {
                if (dependency.Name.Equals("react", StringComparison.OrdinalIgnoreCase)
                    && dependency.Value.ValueKind == JsonValueKind.String)
                    return dependency.Value.GetString();
            }
        }
        return null;
    }

    private static bool IsContained(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar,
                   OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }
}
