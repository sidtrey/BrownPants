namespace BrownPants.Cli.Analysis;

public static class SddWorkflowAnalyzer
{
    private static readonly IComparer<string> StablePathComparer = Comparer<string>.Create((left, right) =>
    {
        var result = StringComparer.OrdinalIgnoreCase.Compare(left, right);
        return result != 0 ? result : StringComparer.Ordinal.Compare(left, right);
    });

    public static IReadOnlyList<SddWorkflowInventory> Analyze(IReadOnlyList<string> files, int sampleLimit)
    {
        var workflows = new List<SddWorkflowInventory>();
        var specKitArtifacts = files.Where(path => path.StartsWith(".specify/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("specs/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StablePathComparer)
            .ToArray();
        if (files.Any(path => path.StartsWith(".specify/", StringComparison.OrdinalIgnoreCase)))
        {
            workflows.Add(new SddWorkflowInventory(
                "Spec Kit",
                true,
                FirstLevelDirectoryCount(files, "specs/"),
                null,
                specKitArtifacts.Length,
                specKitArtifacts.Take(sampleLimit).ToArray()));
        }

        var openSpecArtifacts = files.Where(path => path.StartsWith("openspec/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StablePathComparer)
            .ToArray();
        if (openSpecArtifacts.Length > 0)
        {
            var activeChanges = FirstLevelDirectories(files, "openspec/changes/")
                .Count(name => !name.Equals("archive", StringComparison.OrdinalIgnoreCase));
            workflows.Add(new SddWorkflowInventory(
                "OpenSpec",
                true,
                FirstLevelDirectoryCount(files, "openspec/specs/"),
                activeChanges,
                openSpecArtifacts.Length,
                openSpecArtifacts.Take(sampleLimit).ToArray()));
        }

        return workflows;
    }

    private static int FirstLevelDirectoryCount(IReadOnlyList<string> files, string prefix) =>
        FirstLevelDirectories(files, prefix).Count;

    private static HashSet<string> FirstLevelDirectories(IReadOnlyList<string> files, string prefix) => files
        .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .Select(path => path[prefix.Length..])
        .Where(remainder => remainder.IndexOf('/') > 0)
        .Select(remainder => remainder[..remainder.IndexOf('/')])
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
