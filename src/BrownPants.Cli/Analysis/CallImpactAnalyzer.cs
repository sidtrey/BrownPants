using System.Text.Json.Serialization;

namespace BrownPants.Cli.Analysis;

public sealed record CallImpactReport(
    string Root,
    IReadOnlyList<string> ChangedPaths,
    bool IsComplete,
    CallImpactSummary Summary,
    IReadOnlyList<CallImpact> Impacts,
    IReadOnlyList<string> Issues,
    int SampleLimit)
{
    [JsonPropertyOrder(-1)]
    public int SchemaVersion => 1;
}

public sealed record CallImpactSummary(
    int DirectCallerCount,
    int TransitiveCallerCount,
    int AffectedFileCount,
    int AffectedProjectCount,
    int TestCandidateCount,
    int MaximumCallerDepth);

public sealed record CallImpact(
    string ChangedMethod,
    string Target,
    string Caller,
    string Path,
    int Line,
    int Depth,
    string Project,
    bool IsTestCandidate,
    string RelationshipKind = "method");

public static class CallImpactAnalyzer
{
    public static CallImpactReport Analyze(string root, IReadOnlyList<string> changedPaths,
        IReadOnlyList<string> discoveredFiles, CSharpSemanticAnalysis semantic, int sampleLimit)
    {
        if (changedPaths.Count == 0)
            throw new ArgumentException("At least one --changed path is required.", nameof(changedPaths));
        if (sampleLimit is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(sampleLimit));

        root = Path.GetFullPath(root);
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var discovered = new HashSet<string>(discoveredFiles.Select(NormalizePath), pathComparer);
        var normalizedChanged = new SortedSet<string>(StringComparer.Ordinal);
        var issues = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var input in changedPaths)
        {
            if (Path.IsPathRooted(input) || input.Split('/', '\\').Any(part => part is ".." or ""))
            {
                issues.Add($"Unsupported changed path '{input}': provide a repository-relative path without parent traversal.");
                continue;
            }
            var relative = NormalizePath(input);
            if (!relative.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && !relative.EndsWith(".vb", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"Unsupported changed path '{relative}': semantic impact currently supports C# and Visual Basic source files.");
                continue;
            }
            if (!discovered.Contains(relative))
            {
                issues.Add($"Changed path '{relative}' was not found among discovered repository files.");
                continue;
            }
            normalizedChanged.Add(relative);
        }

        var declarations = semantic.Declarations ?? Array.Empty<CSharpSemanticDeclaration>();
        var declarationGroups = declarations
            .GroupBy(item => NormalizePath(item.Path), pathComparer)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Symbol).ToHashSet(StringComparer.Ordinal), pathComparer);
        foreach (var changed in normalizedChanged)
        {
            if (!declarationGroups.TryGetValue(changed, out var symbols) || symbols.Count == 0)
                issues.Add($"Changed path '{changed}' has no mapped method declarations in the loaded semantic graph.");
        }

        if (semantic.ProjectsLoaded != semantic.ProjectsDiscovered)
            issues.Add($"Only {semantic.ProjectsLoaded} of {semantic.ProjectsDiscovered} discovered managed projects loaded.");
        if (semantic.Issues.Count > 0)
            issues.Add($"Semantic workspace reported {semantic.Issues.Count} load or workspace issue(s).");
        if (semantic.UnresolvedInvocationCount > 0)
            issues.Add($"{semantic.UnresolvedInvocationCount} invocation(s) were unresolved.");
        if (semantic.UnresolvedConstructorCount > 0)
            issues.Add($"{semantic.UnresolvedConstructorCount} constructor relationship(s) were unresolved.");
        var resolvedTotal = semantic.ResolvedInvocationCount + semantic.ResolvedConstructorCount;
        var unresolvedTotal = semantic.UnresolvedInvocationCount + semantic.UnresolvedConstructorCount;
        if (semantic.ResolvedCalls.Count < resolvedTotal)
            issues.Add($"Resolved relationship edges were sampled: {semantic.ResolvedCalls.Count} of {resolvedTotal} retained.");
        if (semantic.UnresolvedCalls.Count < unresolvedTotal)
            issues.Add($"Unresolved relationship details were sampled: {semantic.UnresolvedCalls.Count} of {unresolvedTotal} retained.");
        if (declarations.Count < semantic.DeclarationCount)
            issues.Add($"Method declarations were sampled: {declarations.Count} of {semantic.DeclarationCount} retained.");

        var calls = semantic.ResolvedCalls;
        var impacts = new Dictionary<string, CallImpact>(StringComparer.Ordinal);
        foreach (var changed in normalizedChanged)
        {
            if (!declarationGroups.TryGetValue(changed, out var changedSymbols))
                continue;
            foreach (var changedSymbol in changedSymbols.OrderBy(value => value, StringComparer.Ordinal))
            {
                var visited = new Dictionary<string, int>(StringComparer.Ordinal) { [changedSymbol] = 0 };
                var queue = new Queue<string>();
                queue.Enqueue(changedSymbol);
                while (queue.Count > 0)
                {
                    var target = queue.Dequeue();
                    var depth = visited[target];
                    foreach (var call in calls.Where(item => string.Equals(item.Target, target, StringComparison.Ordinal))
                        .OrderBy(item => item.Path, StringComparer.Ordinal).ThenBy(item => item.Line).ThenBy(item => item.Caller, StringComparer.Ordinal))
                    {
                        var callerDepth = depth + 1;
                        if (callerDepth > 0)
                        {
                            var impact = new CallImpact(changedSymbol, call.Target, call.Caller, NormalizePath(call.Path), call.Line,
                                callerDepth, NormalizePath(call.Project), RepositoryScanner.IsTestPath(call.Path), call.RelationshipKind);
                            var key = string.Join("\n", impact.ChangedMethod, impact.Target, impact.Caller, impact.Path, impact.Line, impact.RelationshipKind);
                            if (!impacts.ContainsKey(key)) impacts[key] = impact;
                        }
                        if (!visited.TryGetValue(call.Caller, out var prior) || callerDepth < prior)
                        {
                            visited[call.Caller] = callerDepth;
                            queue.Enqueue(call.Caller);
                        }
                    }
                }
            }
        }

        var orderedImpacts = impacts.Values.OrderBy(item => item.Depth).ThenBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line).ThenBy(item => item.Caller, StringComparer.Ordinal)
            .ThenBy(item => item.ChangedMethod, StringComparer.Ordinal).ThenBy(item => item.Target, StringComparer.Ordinal)
            .ThenBy(item => item.RelationshipKind, StringComparer.Ordinal)
            .Take(sampleLimit).ToArray();
        if (impacts.Count > orderedImpacts.Length)
            issues.Add($"Impact findings were sampled: {orderedImpacts.Length} of {impacts.Count} retained.");
        var summary = new CallImpactSummary(
            impacts.Values.Where(item => item.Depth == 1).Select(item => item.Caller).Distinct(StringComparer.Ordinal).Count(),
            impacts.Values.Where(item => item.Depth > 1).Select(item => item.Caller).Distinct(StringComparer.Ordinal).Count(),
            impacts.Values.Select(item => item.Path).Distinct(pathComparer).Count(),
            impacts.Values.Select(item => item.Project).Distinct(pathComparer).Count(),
            impacts.Values.Where(item => item.IsTestCandidate).Select(item => item.Path).Distinct(pathComparer).Count(),
            impacts.Values.Select(item => item.Depth).DefaultIfEmpty(0).Max());
        return new CallImpactReport(root, normalizedChanged.ToArray(), issues.Count == 0, summary, orderedImpacts,
            issues.ToArray(), sampleLimit);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('.', '/');
}
