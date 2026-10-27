namespace BrownPants.Cli.Analysis;

public static class EvidenceFocusAnalyzer
{
    private const int MaximumClueLocationSample = 10;

    public static EvidenceReport Select(RepositoryInventory inventory, EvidenceReport fullReport,
        IReadOnlyList<string> clues, IReadOnlyList<string> changedPaths, int sampleLimit)
    {
        var selected = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var clueCounts = new int[clues.Count];
        var clueMatchCounts = new int[clues.Count];
        var clueMatchTruncated = false;
        var clueReadIssueCount = 0;
        IReadOnlyList<InvestigationClueResult> clueResults = Array.Empty<InvestigationClueResult>();
        IReadOnlyList<InvestigationReadIssue> clueReadIssues = Array.Empty<InvestigationReadIssue>();

        if (clues.Count > 0)
        {
            var investigation = RepositoryInvestigator.Investigate(inventory.Root, clues, 10_000);
            clueResults = investigation.Clues;
            clueReadIssueCount = investigation.ReadIssueCount;
            clueReadIssues = investigation.ReadIssues;
            foreach (var issue in investigation.Clues)
                if (issue.MatchCount > issue.Matches.Count) clueMatchTruncated = true;

            for (var clueIndex = 0; clueIndex < investigation.Clues.Count; clueIndex++)
            {
                var clue = clues[clueIndex];
                var clueResult = investigation.Clues[clueIndex];
                clueMatchCounts[clueIndex] = clueResult.MatchCount;
                foreach (var finding in fullReport.Findings)
                {
                    var reasons = MatchClue(finding, clue, clueResult.Matches);
                    if (reasons.Count == 0) continue;
                    clueCounts[clueIndex]++;
                    Add(finding.Id, reasons.Select(reason => $"clue[{clueIndex}]:{reason}"));
                }
            }
        }

        var normalizedChanged = changedPaths.Select(NormalizePath).Distinct(PathComparer())
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var changedCounts = normalizedChanged.ToDictionary(path => path, _ => new int[3], PathComparer());
        var discovered = new HashSet<string>(RepositoryScanner.DiscoverFiles(inventory.Root), PathComparer());
        var foundPaths = new HashSet<string>(changedCounts.Keys.Where(discovered.Contains), PathComparer());
        var impactIssues = new List<string>();
        var impactComplete = false;
        var impactAnalyzed = false;
        var semanticImpactAnalyzed = false;
        var semantic = inventory.CSharp.Semantic;
        var declarations = semantic?.Declarations ?? Array.Empty<CSharpSemanticDeclaration>();
        var managedChanged = normalizedChanged.Any(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));

        foreach (var path in normalizedChanged)
        {
            foreach (var finding in fullReport.Findings.Where(item => PathEquals(item.Path, path)))
            {
                changedCounts[path][0]++;
                Add(finding.Id, [$"changed-path:{path}:source-file"]);
            }
        }

        if (normalizedChanged.Length > 0)
        {
            semanticImpactAnalyzed = semantic is not null;
            impactAnalyzed = true;
            var semanticForImpact = semantic ?? new CSharpSemanticAnalysis(0, 0, 0, 0,
                Array.Empty<CSharpResolvedCall>(), Array.Empty<CSharpUnresolvedCall>(), Array.Empty<string>());
            var impact = CallImpactAnalyzer.Analyze(inventory.Root, normalizedChanged, discovered.ToArray(),
                semanticForImpact, 10_000, inventory.ArchitecturePatterns.NativeFlow);
            impactComplete = impact.IsComplete && (!managedChanged || semantic is not null);
            impactIssues.AddRange(impact.Issues);
            if (managedChanged && semantic is null)
                impactIssues.Add("Managed caller impact was not analyzed; pass --semantic to resolve direct and transitive C#/VB callers.");

            foreach (var item in impact.Impacts)
            {
                var finding = fullReport.Findings.FirstOrDefault(candidate =>
                    candidate.Category == "managed-call"
                    && candidate.Path == item.Path
                    && candidate.Line == item.Line
                    && candidate.CallerSignature == item.Caller
                    && candidate.TargetSignature == item.Target);
                if (finding is not null)
                {
                    var declaration = declarations.FirstOrDefault(value => value.Symbol == item.ChangedMethod);
                    var changedPath = declaration is null ? null : normalizedChanged.FirstOrDefault(path =>
                        PathEquals(path, declaration.Path));
                    changedPath ??= normalizedChanged[0];
                    if (changedCounts.TryGetValue(changedPath, out var counts)) counts[1]++;
                    Add(finding.Id, [$"changed-path:{changedPath}:caller-impact"]);
                }

                if (item.IsTestCandidate)
                {
                    var testFinding = fullReport.Findings.FirstOrDefault(candidate =>
                        candidate.Category == "test-identification" && PathEquals(candidate.Path, item.Path));
                    if (testFinding is not null)
                    {
                        var declaration = declarations.FirstOrDefault(value => value.Symbol == item.ChangedMethod);
                        var changedPath = declaration is null ? normalizedChanged[0] : normalizedChanged.FirstOrDefault(path =>
                            PathEquals(path, declaration.Path)) ?? normalizedChanged[0];
                        if (changedCounts.TryGetValue(changedPath, out var counts)) counts[2]++;
                        Add(testFinding.Id, [$"changed-path:{changedPath}:test-impact"]);
                    }
                }
            }

            foreach (var item in impact.NativeImpacts)
            {
                var finding = fullReport.Findings.FirstOrDefault(candidate =>
                    candidate.Category == "native-flow-candidate"
                    && candidate.RelationKind == item.RelationshipKind
                    && PathEquals(candidate.Path, item.SourcePath)
                    && candidate.Line == item.SourceLine
                    && candidate.SourceSymbol == item.SourceSymbol
                    && candidate.TargetSymbol == item.TargetSymbol);
                if (finding is null) continue;
                if (changedCounts.TryGetValue(item.ChangedPath, out var counts)) counts[1]++;
                Add(finding.Id, [$"changed-path:{item.ChangedPath}:native-caller-impact"]);
            }
        }

        var findingById = fullReport.Findings.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var ordered = selected.Keys.Where(findingById.ContainsKey).Select(id => findingById[id])
            .OrderBy(item => item.Category, StringComparer.Ordinal)
            .ThenBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .ThenBy(item => item.Signal, StringComparer.Ordinal)
            .ThenBy(item => item.CallerSignature, StringComparer.Ordinal)
            .ThenBy(item => item.TargetSignature, StringComparer.Ordinal)
            .ToArray();
        var emitted = ordered.Take(sampleLimit)
            .Select(item => item with { MatchedBy = selected[item.Id].OrderBy(value => value, StringComparer.Ordinal).ToArray() })
            .ToArray();
        var omitted = Math.Max(0, ordered.Length - emitted.Length);
        var clueLocationLimit = Math.Min(sampleLimit, MaximumClueLocationSample);
        var clueFocus = clues.Select((clue, index) =>
        {
            var matches = clueResults[index].Matches
                .OrderBy(match => match.Kind == "Path" ? 0 : 1)
                .ThenBy(match => match.Path, StringComparer.Ordinal)
                .ThenBy(match => match.Line)
                .Take(clueLocationLimit)
                .ToArray();
            return new EvidenceClueFocus(clue, clueMatchCounts[index], clueCounts[index],
                clueMatchCounts[index] > 0, matches, Math.Max(0, clueMatchCounts[index] - matches.Length));
        }).ToArray();
        var pathFocus = normalizedChanged.Select(path => new EvidenceChangedPathFocus(
            path, foundPaths.Contains(path), changedCounts[path][0], changedCounts[path][1], changedCounts[path][2],
            changedCounts[path].Sum() > 0)).ToArray();
        var focus = new EvidenceFocusSummary(
            clueFocus,
            pathFocus,
            semanticImpactAnalyzed,
            normalizedChanged.Length == 0 || impactAnalyzed && impactComplete,
            impactIssues.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            clueReadIssueCount,
            clueMatchTruncated,
            fullReport.Completeness.FindingsTruncated,
            ordered.Length,
            emitted.Length,
            omitted);
        var limitations = fullReport.Limitations.ToList();
        if (normalizedChanged.Length > 0 && managedChanged && !semanticImpactAnalyzed)
            limitations.Add("Caller impact was not analyzed for managed changed paths; pass --semantic to resolve managed callers. Native candidate impact may still be included.");
        if (clueReadIssueCount > 0)
        {
            var issueSummary = string.Join(", ", clueReadIssues
                .GroupBy(issue => issue.Code, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key}={group.Count()}"));
            var omittedIssueCount = clueReadIssueCount - clueReadIssues.Count;
            var summaryLabel = omittedIssueCount > 0 ? "sampled issue codes" : "issue codes";
            var omittedText = omittedIssueCount > 0 ? $"; {omittedIssueCount} additional diagnostic(s) omitted" : string.Empty;
            limitations.Add($"Clue matching skipped or could not read {clueReadIssueCount} file(s); {summaryLabel}: {issueSummary}{omittedText}.");
        }
        if (clueMatchTruncated)
            limitations.Add("Some clue locations exceeded the bounded match sample; focused selection may be incomplete.");
        if (focus.SourceEvidenceTruncated)
            limitations.Add("The source evidence used for focus selection was sampled before filtering; focused selection may be incomplete.");

        return fullReport with
        {
            Findings = emitted,
            RepeatedCandidateGroups = EvidenceReportAnalyzer.CreateRepeatedCandidateGroups(emitted),
            Focus = focus,
            Completeness = fullReport.Completeness with
            {
                TotalFindingCount = ordered.Length,
                EmittedFindingCount = emitted.Length,
                FindingsTruncated = omitted > 0 || focus.SourceEvidenceTruncated || clueMatchTruncated
            },
            Limitations = limitations.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray()
        };

        void Add(string id, IEnumerable<string> reasons)
        {
            if (!selected.TryGetValue(id, out var set)) selected[id] = set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reason in reasons) set.Add(reason);
        }
    }

    private static List<string> MatchClue(EvidenceFinding finding, string clue,
        IReadOnlyList<InvestigationMatch> matches)
    {
        var reasons = new List<string>();
        if (finding.Path.Contains(clue, StringComparison.OrdinalIgnoreCase)) reasons.Add("path-text");
        if (finding.Evidence.Contains(clue, StringComparison.OrdinalIgnoreCase)
            || finding.Signal.Contains(clue, StringComparison.OrdinalIgnoreCase)
            || (finding.CallerSignature?.Contains(clue, StringComparison.OrdinalIgnoreCase) ?? false)
            || (finding.TargetSignature?.Contains(clue, StringComparison.OrdinalIgnoreCase) ?? false))
            reasons.Add("evidence-text");

        foreach (var match in matches)
        {
            if (!PathEquals(finding.Path, match.Path)) continue;
            if (match.Kind == "Path") reasons.Add("path-match");
            else if (match.Line is not null && match.Line == finding.Line) reasons.Add("content-line");
            else if (match.Kind == "Content" && finding.Category == "test-identification" && finding.Line is null)
                reasons.Add("content-file");
        }
        return reasons.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal)) normalized = normalized[2..];
        return normalized;
    }
    private static bool PathEquals(string left, string right) => PathComparer().Equals(NormalizePath(left), NormalizePath(right));
    private static StringComparer PathComparer() => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
