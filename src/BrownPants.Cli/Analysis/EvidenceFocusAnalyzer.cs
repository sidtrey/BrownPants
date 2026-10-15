namespace BrownPants.Cli.Analysis;

public static class EvidenceFocusAnalyzer
{
    public static EvidenceReport Select(RepositoryInventory inventory, EvidenceReport fullReport,
        IReadOnlyList<string> clues, IReadOnlyList<string> changedPaths, int sampleLimit)
    {
        var selected = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var clueCounts = new int[clues.Count];
        var clueMatchCounts = new int[clues.Count];
        var clueMatchTruncated = false;
        var clueReadIssueCount = 0;

        if (clues.Count > 0)
        {
            var investigation = RepositoryInvestigator.Investigate(inventory.Root, clues, 10_000);
            clueReadIssueCount = investigation.ReadIssueCount;
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
        var semanticImpactAnalyzed = false;
        var declarations = inventory.CSharp.Semantic?.Declarations ?? Array.Empty<CSharpSemanticDeclaration>();

        foreach (var path in normalizedChanged)
        {
            foreach (var finding in fullReport.Findings.Where(item => PathEquals(item.Path, path)))
            {
                changedCounts[path][0]++;
                Add(finding.Id, [$"changed-path:{path}:source-file"]);
            }
        }

        if (normalizedChanged.Length > 0 && inventory.CSharp.Semantic is not null)
        {
            semanticImpactAnalyzed = true;
            var impact = CallImpactAnalyzer.Analyze(inventory.Root, normalizedChanged, discovered.ToArray(),
                inventory.CSharp.Semantic, 10_000);
            impactComplete = impact.IsComplete;
            impactIssues.AddRange(impact.Issues);

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
        }
        else if (normalizedChanged.Length > 0)
        {
            impactIssues.Add("Semantic caller impact was not analyzed; pass --semantic to resolve direct and transitive C#/VB callers.");
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
        var clueFocus = clues.Select((clue, index) => new EvidenceClueFocus(
            clue, clueMatchCounts[index], clueCounts[index], clueMatchCounts[index] > 0)).ToArray();
        var pathFocus = normalizedChanged.Select(path => new EvidenceChangedPathFocus(
            path, foundPaths.Contains(path), changedCounts[path][0], changedCounts[path][1], changedCounts[path][2],
            changedCounts[path].Sum() > 0)).ToArray();
        var focus = new EvidenceFocusSummary(
            clueFocus,
            pathFocus,
            semanticImpactAnalyzed,
            normalizedChanged.Length == 0 || semanticImpactAnalyzed && impactComplete,
            impactIssues.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            clueReadIssueCount,
            clueMatchTruncated,
            fullReport.Completeness.FindingsTruncated,
            ordered.Length,
            emitted.Length,
            omitted);
        var limitations = fullReport.Limitations.ToList();
        if (normalizedChanged.Length > 0 && !semanticImpactAnalyzed)
            limitations.Add("Changed-path focus selected source-local candidates only; caller impact was not analyzed because --semantic was not requested.");
        if (clueReadIssueCount > 0)
            limitations.Add($"Clue matching skipped or could not read {clueReadIssueCount} file(s); see investigator rules for bounded reads.");
        if (clueMatchTruncated)
            limitations.Add("Some clue locations exceeded the bounded match sample; focused selection may be incomplete.");
        if (focus.SourceEvidenceTruncated)
            limitations.Add("The source evidence used for focus selection was sampled before filtering; focused selection may be incomplete.");

        return fullReport with
        {
            Findings = emitted,
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
