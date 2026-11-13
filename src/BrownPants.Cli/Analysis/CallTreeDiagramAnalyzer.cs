namespace BrownPants.Cli.Analysis;

internal sealed record CallTreeDiagramResult(LocalFlowchartDocument Document, bool IsComplete,
    IReadOnlyList<string> Limitations, int DirectCallerCount, int TransitiveCallerCount,
    int TestCandidateCount, int OmittedRelationshipCount);

internal static class CallTreeDiagramAnalyzer
{
    internal static CallTreeDiagramResult Analyze(CSharpSemanticAnalysis semantic, string sourcePath,
        string exactMethod, int maximumDepth, int sampleLimit)
    {
        if (maximumDepth is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maximumDepth), "Depth must be between 1 and 100.");
        if (sampleLimit is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "Sample limit must be between 1 and 10000.");

        sourcePath = NormalizePath(sourcePath);
        var declarations = semantic.Declarations ?? Array.Empty<CSharpSemanticDeclaration>();
        var rootDeclaration = declarations.Where(item => string.Equals(NormalizePath(item.Path), sourcePath,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                && string.Equals(item.Symbol, exactMethod, StringComparison.Ordinal))
            .OrderBy(item => item.Line).FirstOrDefault();
        if (rootDeclaration is null)
            throw new LocalFlowchartFormatException(
                $"No compiler-resolved declaration exactly matching '{exactMethod}' was found in '{sourcePath}'. Check the fully qualified signature and semantic project coverage.");

        var calls = semantic.ImpactGraphCalls ?? semantic.ResolvedCalls;
        var callsByTarget = calls.GroupBy(TargetIdentity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(call => call.Path, StringComparer.Ordinal)
                .ThenBy(call => call.Line).ThenBy(call => call.SourceColumn ?? 0)
                .ThenBy(call => call.Caller, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var declarationBySymbol = declarations.GroupBy(item => item.Symbol, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Path, StringComparer.Ordinal)
                .ThenBy(item => item.Line).First(), StringComparer.Ordinal);
        var depths = new Dictionary<string, int>(StringComparer.Ordinal) { [rootDeclaration.Symbol] = 0 };
        var pending = new Queue<string>();
        pending.Enqueue(rootDeclaration.Symbol);
        var retainedEdges = new List<(CSharpResolvedCall Call, int Depth)>();
        var omitted = 0;
        var depthLimited = false;

        while (pending.Count > 0)
        {
            var target = pending.Dequeue();
            var targetDepth = depths[target];
            if (targetDepth >= maximumDepth)
            {
                depthLimited |= callsByTarget.ContainsKey(target);
                continue;
            }
            if (!callsByTarget.TryGetValue(target, out var incoming)) continue;

            foreach (var call in incoming)
            {
                var caller = call.Caller;
                if (string.IsNullOrWhiteSpace(caller) || caller == "<unknown>")
                {
                    omitted++;
                    continue;
                }
                if (retainedEdges.Count >= sampleLimit)
                {
                    omitted++;
                    continue;
                }
                var callerDepth = targetDepth + 1;
                retainedEdges.Add((call, callerDepth));
                if (depths.TryGetValue(caller, out var priorDepth) && priorDepth <= callerDepth) continue;
                if (depths.Count >= sampleLimit + 1)
                {
                    omitted++;
                    retainedEdges.RemoveAt(retainedEdges.Count - 1);
                    continue;
                }
                depths[caller] = callerDepth;
                pending.Enqueue(caller);
            }
        }

        var limitations = new SortedSet<string>(StringComparer.Ordinal);
        if (!semantic.ProjectCoverageComplete || semantic.ProjectsLoaded < semantic.ProjectsDiscovered)
            limitations.Add($"Only {semantic.ProjectsLoaded} of {semantic.ProjectsDiscovered} managed projects loaded successfully.");
        var workspaceIssues = Math.Max(semantic.WorkspaceIssueCount, semantic.Issues.Count);
        if (workspaceIssues > 0)
            limitations.Add($"Semantic workspace reported {workspaceIssues} issue(s).");
        var unresolvedCalls = semantic.UnresolvedInvocationCount + semantic.UnresolvedConstructorCount;
        if (unresolvedCalls > 0)
            limitations.Add($"{unresolvedCalls} invocation or constructor relationship(s) could not be resolved.");
        if (semantic.UnresolvedDelegateTargetCount > 0)
            limitations.Add($"{semantic.UnresolvedDelegateTargetCount} delegate target(s) could not be resolved; delegate-based callers may be missing.");
        if (semantic.ImpactGraphOmittedCount > 0)
            limitations.Add($"The semantic call graph omitted {semantic.ImpactGraphOmittedCount} relationship(s) at its 100,000-edge safety limit.");
        else if (semantic.ImpactGraphCalls is null
            && semantic.ResolvedCalls.Count < semantic.ResolvedInvocationCount + semantic.ResolvedConstructorCount
                + semantic.ResolvedDelegateTargetCount)
            limitations.Add("Resolved call relationships were sampled in the semantic report.");
        if (semantic.DeclarationCount > declarations.Count)
            limitations.Add($"Method declarations were sampled: {declarations.Count} of {semantic.DeclarationCount} retained.");
        if (omitted > 0)
            limitations.Add($"The requested diagram omitted {omitted} relationship(s) at the sample limit of {sampleLimit}.");
        if (depthLimited)
            limitations.Add($"Caller traversal stopped at depth {maximumDepth}; additional callers may exist beyond that depth.");

        var nodes = new List<LocalFlowchartNode>();
        var nodeIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var nodeDepths = new Dictionary<string, int>(StringComparer.Ordinal);
        var callSiteIds = new HashSet<string>(StringComparer.Ordinal);
        var order = 0;
        string AddMethodNode(string symbol, CSharpSemanticDeclaration? declaration, bool isRoot)
        {
            if (nodeIds.TryGetValue(symbol, out var existing)) return existing;
            var id = $"m{nodeIds.Count:D5}";
            nodeIds.Add(symbol, id);
            nodeDepths[id] = depths.GetValueOrDefault(symbol, int.MaxValue);
            var isTest = declaration is not null && RepositoryScanner.IsTestPath(declaration.Path);
            var shortName = ShortName(symbol);
            var label = isRoot ? $"Target: {shortName}"
                : isTest ? $"Test candidate: {shortName}"
                : shortName;
            nodes.Add(new LocalFlowchartNode(id,
                isRoot ? LocalFlowchartNodeShape.End : isTest ? LocalFlowchartNodeShape.InputOutput : LocalFlowchartNodeShape.Process,
                label, declaration?.Line ?? 1, order++,
                declaration is null ? $"Compiler symbol: {symbol}" : $"Compiler symbol: {symbol}\nDeclared at: {declaration.Path}:{declaration.Line}"));
            return id;
        }

        AddMethodNode(rootDeclaration.Symbol, rootDeclaration, true);
        foreach (var caller in retainedEdges.Select(item => item.Call.Caller).Distinct(StringComparer.Ordinal)
            .OrderBy(symbol => depths.GetValueOrDefault(symbol, int.MaxValue))
            .ThenBy(symbol => symbol, StringComparer.Ordinal))
            AddMethodNode(caller, declarationBySymbol.GetValueOrDefault(caller), false);

        var edges = new List<LocalFlowchartEdge>(retainedEdges.Count * 2);
        foreach (var (item, edgeOrder) in retainedEdges.Select((item, edgeOrder) => (item, edgeOrder)))
        {
            var callsiteId = $"c{edgeOrder:D5}";
            callSiteIds.Add(callsiteId);
            nodeDepths[callsiteId] = item.Depth;
            var callsiteLabel = $"Call site: {ShortLocation(item.Call.Path)}:{item.Call.Line} (hop {item.Depth})";
            nodes.Add(new LocalFlowchartNode(callsiteId, LocalFlowchartNodeShape.Process,
                callsiteLabel, item.Call.Line, order++,
                $"Resolved {item.Call.RelationshipKind} call\nCaller: {item.Call.Caller}\nSource: {item.Call.Path}:{item.Call.Line}\nTarget: {TargetIdentity(item.Call)}\nHop depth: {item.Depth}"));
            edges.Add(new LocalFlowchartEdge(nodeIds[item.Call.Caller], callsiteId,
                null, item.Call.Line, edges.Count));
            edges.Add(new LocalFlowchartEdge(callsiteId, nodeIds[TargetIdentity(item.Call)],
                null, item.Call.Line, edges.Count));
        }
        if (limitations.Count > 0)
        {
            var warningId = $"coverage{order}";
            nodes.Add(new LocalFlowchartNode(warningId, LocalFlowchartNodeShape.Decision,
                "Analysis is partial; see the command report for gaps", 1, order));
            nodeDepths[warningId] = int.MinValue;
        }

        var directCallers = depths.Count(pair => pair.Value == 1);
        var transitiveCallers = depths.Count(pair => pair.Value > 1);
        var testCandidates = retainedEdges.Select(item => item.Call.Caller).Distinct(StringComparer.Ordinal)
            .Count(symbol => declarationBySymbol.TryGetValue(symbol, out var declaration)
                && RepositoryScanner.IsTestPath(declaration.Path));
        var orderedNodes = nodes.OrderByDescending(node => nodeDepths.GetValueOrDefault(node.Id, int.MinValue))
            .ThenBy(node => callSiteIds.Contains(node.Id) ? 1 : 0)
            .ThenBy(node => node.Order)
            .Select((node, index) => node with { Order = index })
            .ToArray();
        var document = new LocalFlowchartDocument("callers", sourcePath, orderedNodes, edges);
        return new CallTreeDiagramResult(document, limitations.Count == 0, limitations.ToArray(),
            directCallers, transitiveCallers, testCandidates, omitted);
    }

    private static string TargetIdentity(CSharpResolvedCall call) => call.TargetIdentity ?? call.Target;

    private static string ShortName(string symbol)
    {
        var openParameters = symbol.IndexOf('(');
        var head = openParameters < 0 ? symbol : symbol[..openParameters];
        var separator = head.LastIndexOf('.');
        if (separator < 0) return head;
        var typeSeparator = head.LastIndexOf('.', separator - 1);
        return head[(typeSeparator < 0 ? separator : typeSeparator + 1)..];
    }

    private static string ShortLocation(string path)
    {
        var segments = NormalizePath(path).Split('/');
        return segments.Length < 3 ? string.Join('/', segments)
            : ".../" + string.Join('/', segments[^2..]);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

}
