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

    public IReadOnlyList<NativeCallImpact> NativeImpacts { get; init; } = Array.Empty<NativeCallImpact>();
    public int NativeCandidateCount => NativeImpacts.Count(item => item.Status == "candidate");
    public int NativeUnresolvedCount => NativeImpacts.Count(item => item.Status == "unresolved");
    public int NativeAmbiguousCount => NativeImpacts.Count(item => item.Status == "ambiguous");
    public int NativeAffectedFileCount => NativeImpacts.Select(item => item.SourcePath).Distinct(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).Count();
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
    string RelationshipKind = "method")
{
    public string EvidenceKind => "managed-compiler-resolved";
}

public sealed record NativeCallImpact(
    string ChangedPath,
    string RelationshipId,
    string RelationshipKind,
    string Status,
    string SourceSymbol,
    string TargetSymbol,
    string SourcePath,
    int SourceLine,
    string? TargetPath,
    int? TargetLine,
    string? CommandId,
    int CandidateTargetCount,
    int Depth,
    string Evidence)
{
    public IReadOnlyList<NativeFlowTarget> CandidateTargets { get; init; } = Array.Empty<NativeFlowTarget>();
    public string EvidenceKind => "native-lexical";
}

public static class CallImpactAnalyzer
{
    public static CallImpactReport Analyze(string root, IReadOnlyList<string> changedPaths,
        IReadOnlyList<string> discoveredFiles, CSharpSemanticAnalysis semantic, int sampleLimit,
        NativeFlowAnalysis? nativeFlow = null)
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
            var isManagedSource = relative.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || relative.EndsWith(".vb", StringComparison.OrdinalIgnoreCase);
            var isNativeSource = Path.GetExtension(relative).ToLowerInvariant() is
                ".c" or ".cc" or ".cpp" or ".cxx" or ".h" or ".hh" or ".hpp" or ".hxx" or ".inl" or ".ipp";
            if (!isManagedSource && !isNativeSource)
            {
                issues.Add($"Unsupported changed path '{relative}': impact supports C#, Visual Basic, and C/C++ source files.");
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
            if (IsNativeSource(changed) && nativeFlow is null)
                issues.Add($"Native impact for '{changed}' was not analyzed because native flow evidence was unavailable.");
            if (!declarationGroups.TryGetValue(changed, out var symbols) || symbols.Count == 0)
            {
                if (IsNativeSource(changed)) continue;
                issues.Add($"Changed path '{changed}' has no mapped method declarations in the loaded semantic graph.");
            }
        }

        if (normalizedChanged.Any(path => !IsNativeSource(path)))
        {
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
            if (semantic.ImpactGraphCalls is null && semantic.ResolvedCalls.Count < resolvedTotal)
                issues.Add($"Resolved relationship edges were sampled: {semantic.ResolvedCalls.Count} of {resolvedTotal} retained.");
            if (semantic.ImpactGraphCalls is not null && semantic.ImpactGraphOmittedCount > 0)
                issues.Add($"Impact graph edges were sampled: {semantic.ImpactGraphCalls.Count} retained and {semantic.ImpactGraphOmittedCount} omitted.");
            if (semantic.UnresolvedCalls.Count < unresolvedTotal)
                issues.Add($"Unresolved relationship details were sampled: {semantic.UnresolvedCalls.Count} of {unresolvedTotal} retained.");
            if (declarations.Count < semantic.DeclarationCount)
                issues.Add($"Method declarations were sampled: {declarations.Count} of {semantic.DeclarationCount} retained.");
        }

        var calls = semantic.ImpactGraphCalls ?? semantic.ResolvedCalls;
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
                    foreach (var call in calls.Where(item => string.Equals(item.TargetIdentity ?? item.Target, target, StringComparison.Ordinal))
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

        var nativeImpacts = AnalyzeNativeImpacts(normalizedChanged, nativeFlow, sampleLimit, issues);
        var summary = new CallImpactSummary(
            impacts.Values.Where(item => item.Depth == 1).Select(item => item.Caller).Distinct(StringComparer.Ordinal).Count(),
            impacts.Values.Where(item => item.Depth > 1).Select(item => item.Caller).Distinct(StringComparer.Ordinal).Count(),
            impacts.Values.Select(item => item.Path).Distinct(pathComparer).Count(),
            impacts.Values.Select(item => item.Project).Distinct(pathComparer).Count(),
            impacts.Values.Where(item => item.IsTestCandidate).Select(item => item.Path).Distinct(pathComparer).Count(),
            impacts.Values.Select(item => item.Depth).DefaultIfEmpty(0).Max());
        return new CallImpactReport(root, normalizedChanged.ToArray(), issues.Count == 0, summary, orderedImpacts,
            issues.ToArray(), sampleLimit) { NativeImpacts = nativeImpacts };
    }

    private static NativeCallImpact[] AnalyzeNativeImpacts(IReadOnlyCollection<string> changedPaths,
        NativeFlowAnalysis? nativeFlow, int sampleLimit, ISet<string> issues)
    {
        if (changedPaths.Count == 0 || nativeFlow is null) return [];

        var managedChanged = changedPaths.Where(IsManagedSource).ToArray();
        if (nativeFlow.OmittedCount > 0)
            issues.Add($"Native flow relations were sampled: {nativeFlow.Relations.Count} retained and {nativeFlow.OmittedCount} omitted.");
        if (managedChanged.Length > 0 && nativeFlow.ManagedTargetIndexTruncated)
            issues.Add("The managed target index was truncated; additional C#/VB declarations may be missing from native impact.");

        var incomingBySymbol = nativeFlow.Relations.Where(item => item.Status is "candidate" or "ambiguous")
            .GroupBy(item => item.TargetSymbol, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.SourcePath, StringComparer.Ordinal)
                .ThenBy(item => item.SourceLine).ThenBy(item => item.Id, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var results = new Dictionary<string, NativeCallImpact>(StringComparer.Ordinal);
        foreach (var changed in changedPaths)
        {
            var incoming = nativeFlow.Relations
                .Where(relation => relation.Status is "candidate" or "ambiguous")
                .Where(relation => relation.TargetPath is not null && PathEquals(relation.TargetPath, changed)
                    || relation.CandidateTargets?.Any(target => PathEquals(target.Path, changed)) == true)
                .OrderBy(relation => relation.SourcePath, StringComparer.Ordinal)
                .ThenBy(relation => relation.SourceLine)
                .ThenBy(relation => relation.Id, StringComparer.Ordinal)
                .ToArray();
            var queue = new Queue<(string Symbol, int Depth)>();
            var visited = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var relation in incoming)
            {
                Add(relation, changed, 1);
                queue.Enqueue((relation.SourceSymbol, 1));
            }

            while (queue.Count > 0)
            {
                var (symbol, depth) = queue.Dequeue();
                if (string.IsNullOrWhiteSpace(symbol) || symbol.StartsWith("MFC message map ", StringComparison.Ordinal))
                    continue;
                if (visited.TryGetValue(symbol, out var priorDepth) && priorDepth <= depth) continue;
                visited[symbol] = depth;
                if (!incomingBySymbol.TryGetValue(symbol, out var related)) continue;
                foreach (var relation in related)
                {
                    if (relation.Status == "ambiguous" && relation.CandidateTargets is { Count: > 0 }
                        && !relation.CandidateTargets.Any(target => string.Equals(target.Symbol, symbol, StringComparison.Ordinal)))
                        continue;
                    Add(relation, changed, depth + 1);
                    queue.Enqueue((relation.SourceSymbol, depth + 1));
                }
            }

            foreach (var relation in nativeFlow.Relations.Where(item => item.Status is "unresolved" or "ambiguous"
                         && (PathEquals(item.SourcePath, changed)
                             || item.TargetPath is not null && PathEquals(item.TargetPath, changed)
                             || item.CandidateTargets?.Any(target => PathEquals(target.Path, changed)) == true))
                     .OrderBy(item => item.SourcePath, StringComparer.Ordinal).ThenBy(item => item.SourceLine)
                     .ThenBy(item => item.Id, StringComparer.Ordinal))
                Add(relation, changed, 1);

            void Add(NativeFlowRelation relation, string changedPath, int depth)
            {
                var item = new NativeCallImpact(changedPath, relation.Id, relation.Kind, relation.Status,
                    relation.SourceSymbol, relation.TargetSymbol, NormalizePath(relation.SourcePath),
                    relation.SourceLine, relation.TargetPath is null ? null : NormalizePath(relation.TargetPath),
                    relation.TargetLine, relation.CommandId, relation.CandidateTargetCount, depth, relation.Evidence)
                {
                    CandidateTargets = (relation.CandidateTargets ?? Array.Empty<NativeFlowTarget>())
                        .Select(target => target with { Path = NormalizePath(target.Path) })
                        .OrderBy(target => target.Path, StringComparer.Ordinal).ThenBy(target => target.Line)
                        .ThenBy(target => target.Symbol, StringComparer.Ordinal).ToArray()
                };
                results.TryAdd(string.Join("\n", changedPath, relation.Id), item);
            }
        }

        if (results.Values.Any(item => item.Status == "unresolved"))
            issues.Add("Related native flow contains unresolved relation(s); native impact may be incomplete.");
        if (results.Values.Any(item => item.Status == "ambiguous"))
            issues.Add("Related native flow contains ambiguous relation(s); native impact may be incomplete.");

        var ordered = results.Values.OrderBy(item => item.ChangedPath, StringComparer.Ordinal)
            .ThenBy(item => item.Depth).ThenBy(item => item.SourcePath, StringComparer.Ordinal)
            .ThenBy(item => item.SourceLine).ThenBy(item => item.RelationshipId, StringComparer.Ordinal)
            .Take(sampleLimit).ToArray();
        if (results.Count > ordered.Length)
            issues.Add($"Native impact findings were sampled: {ordered.Length} of {results.Count} retained.");
        return ordered;
    }

    private static bool IsNativeSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".c" or ".cc" or ".cpp" or ".cxx" or ".h" or ".hh" or ".hpp" or ".hxx" or ".inl" or ".ipp";

    private static bool IsManagedSource(string path) => Path.GetExtension(path).ToLowerInvariant() is ".cs" or ".vb";

    private static bool PathEquals(string left, string right) => string.Equals(NormalizePath(left), NormalizePath(right),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal)) normalized = normalized[2..];
        return normalized;
    }
}
