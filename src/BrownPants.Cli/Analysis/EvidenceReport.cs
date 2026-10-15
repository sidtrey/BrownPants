using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace BrownPants.Cli.Analysis;

public sealed record EvidenceReport(
    int SchemaVersion,
    string Root,
    EvidenceContext Context,
    IReadOnlyList<EvidenceFinding> Findings,
    EvidenceCompleteness Completeness,
    IReadOnlyList<string> Limitations)
{
    public IReadOnlyList<EvidenceCandidateGroup> RepeatedCandidateGroups { get; init; } = Array.Empty<EvidenceCandidateGroup>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EvidenceFocusSummary? Focus { get; init; }
}

public sealed record EvidenceCandidateGroup(
    string Category,
    string Signal,
    string Path,
    int LocationCount,
    IReadOnlyList<EvidenceCandidateLocation> Locations);

public sealed record EvidenceCandidateLocation(string FindingId, int Line);

public sealed record EvidenceContext(
    IReadOnlyList<string> ProjectSystems,
    IReadOnlyList<EvidenceLanguage> Languages);

public sealed record EvidenceLanguage(string Name, int SourceFileCount, string AnalysisLevel);

public sealed record EvidenceFinding(
    string Id,
    string Category,
    string Status,
    string Signal,
    string Path,
    int? Line,
    string? CallerSignature,
    string? TargetSignature,
    string Evidence,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TargetPath = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? TargetLine = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RelationKind = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SourceSymbol = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TargetSymbol = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CommandId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? CandidateTargetCount = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<NativeFlowTarget>? CandidateTargets = null)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Column { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? MatchedBy { get; init; }
}

public sealed record EvidenceFocusSummary(
    IReadOnlyList<EvidenceClueFocus> Clues,
    IReadOnlyList<EvidenceChangedPathFocus> ChangedPaths,
    bool SemanticImpactAnalyzed,
    bool ImpactComplete,
    IReadOnlyList<string> ImpactIssues,
    int ClueReadIssueCount,
    bool ClueMatchesTruncated,
    bool SourceEvidenceTruncated,
    int TotalMatchedFindingCount,
    int EmittedFindingCount,
    int OmittedFindingCount);

public sealed record EvidenceClueFocus(string Clue, int MatchCount, int FindingCount, bool Matched);

public sealed record EvidenceChangedPathFocus(
    string Path,
    bool Found,
    int SourceFindingCount,
    int CallerImpactCount,
    int TestImpactCount,
    bool Matched);

public sealed record EvidenceCompleteness(
    int TotalFindingCount,
    int EmittedFindingCount,
    bool FindingsTruncated,
    bool SemanticAnalysisRequested,
    bool? SemanticProjectCoverageComplete,
    int UnresolvedCallCount);

public static class EvidenceReportAnalyzer
{
    public static EvidenceReport Create(RepositoryInventory inventory, bool semanticRequested, int sampleLimit)
    {
        var findings = new List<EvidenceFinding>();
        var semantic = inventory.CSharp.Semantic;

        foreach (var signal in inventory.ArchitecturePatterns.Signals)
        {
            var category = IsFrameworkSignal(signal) || IsInsideLegacyFrameworkProject(inventory, signal.Path)
                ? "framework-candidate"
                : "architecture-candidate";
            findings.Add(CreateFinding(category, "candidate", signal.Category, signal.Path, signal.Line,
                null, null, signal.Evidence));
        }

        var nativeFlow = inventory.ArchitecturePatterns.NativeFlow;
        if (nativeFlow is { ScannedFileCount: > 0 })
        {
            foreach (var relation in nativeFlow.Relations)
            {
                var signal = relation.Kind switch
                {
                    "mfc-dispatch" => "MFC message dispatch",
                    "cppcli-managed-call" => "C++/CLI managed method call",
                    _ => "Qualified native call"
                };
                findings.Add(CreateFinding("native-flow-candidate", relation.Status, signal,
                    relation.SourcePath, relation.SourceLine, null, null, relation.Evidence,
                    relation.TargetPath, relation.TargetLine, relation.Kind, relation.SourceSymbol,
                    relation.TargetSymbol, relation.CommandId, relation.CandidateTargetCount, relation.CandidateTargets));
            }
        }

        if (semanticRequested && semantic is not null)
        {
            foreach (var call in semantic.ResolvedCalls)
            {
                var signal = call.RelationshipKind switch
                {
                    "constructor" => "resolved constructor",
                    "delegate" => "resolved local delegate target",
                    _ => "resolved call"
                };
                findings.Add(CreateFinding("managed-call", "resolved", signal, call.Path, call.Line,
                    call.Caller, call.Target, call.Expression, column: call.SourceColumn));
            }
        }

        foreach (var path in inventory.TestPaths.Where(IsSourceTestPath))
        {
            findings.Add(CreateFinding("test-identification", "candidate", "test path", path, null,
                null, null, "Repository test-path heuristic; inspect the file to confirm relevance."));
        }

        var allOrdered = findings
            .OrderBy(item => item.Category, StringComparer.Ordinal)
            .ThenBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .ThenBy(item => item.Signal, StringComparer.Ordinal)
            .ThenBy(item => item.CallerSignature, StringComparer.Ordinal)
            .ThenBy(item => item.TargetSignature, StringComparer.Ordinal)
            .ThenBy(item => item.Evidence, StringComparer.Ordinal)
            .ThenBy(item => item.Column)
            .ToArray();
        var uniqueOrdered = EnsureUniqueIds(allOrdered);
        var ordered = uniqueOrdered
            .Take(sampleLimit).ToArray();
        var semanticTotal = semanticRequested && semantic is not null
            ? semantic.ResolvedInvocationCount + semantic.ResolvedConstructorCount + semantic.ResolvedDelegateTargetCount
            : 0;
        var knownTotal = inventory.ArchitecturePatterns.SignalCount + semanticTotal
            + inventory.TestPaths.Count(IsSourceTestPath) + (nativeFlow?.RelationCount ?? 0);

        var limitations = new SortedSet<string>(StringComparer.Ordinal)
        {
            "Architecture, framework, and test-path findings are candidates and require source review.",
            "Native, ORM, reflection, and runtime relationships are not inferred as managed edges; constructor and local-delegate edges require explicit compiler-bound source evidence."
        };
        if (!semanticRequested)
            limitations.Add("Semantic analysis was not requested; no resolved managed call findings are included.");
        if (semantic is not null)
        {
            if (!semantic.ProjectCoverageComplete)
                limitations.Add($"Semantic project coverage is incomplete: {semantic.ProjectsLoaded} of {semantic.ProjectsDiscovered} projects loaded.");
            var unresolvedTotal = semantic.UnresolvedInvocationCount + semantic.UnresolvedConstructorCount
                + semantic.UnresolvedDelegateTargetCount;
            if (unresolvedTotal > 0)
                limitations.Add($"{semantic.UnresolvedInvocationCount} managed invocation(s), {semantic.UnresolvedConstructorCount} constructor relationship(s), and {semantic.UnresolvedDelegateTargetCount} delegate target(s) remain unresolved.");
            foreach (var issue in semantic.Issues.OrderBy(value => value, StringComparer.Ordinal))
                limitations.Add(issue);
        }
        if (inventory.ArchitecturePatterns.SignalCount > inventory.ArchitecturePatterns.Signals.Count)
            limitations.Add($"Architecture signals were sampled: {inventory.ArchitecturePatterns.Signals.Count} of {inventory.ArchitecturePatterns.SignalCount} retained.");
        if (nativeFlow is not null)
            foreach (var limit in nativeFlow.EvidenceLimits) limitations.Add(limit);
        if (nativeFlow is { OmittedCount: > 0 })
            limitations.Add($"Native-flow relations were sampled: {nativeFlow.Relations.Count} of {nativeFlow.RelationCount} retained.");

        var report = new EvidenceReport(
            1,
            inventory.Root,
            new EvidenceContext(
                inventory.ProjectSystems.Select(item => item.Name).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                inventory.LanguageCoverage.Select(item => new EvidenceLanguage(item.Language, item.SourceFileCount, item.AnalysisLevel))
                    .OrderBy(item => item.Name, StringComparer.Ordinal).ToArray()),
            ordered,
            new EvidenceCompleteness(
                Math.Max(knownTotal, allOrdered.Length),
                ordered.Length,
                ordered.Length < Math.Max(knownTotal, allOrdered.Length)
                    || inventory.ArchitecturePatterns.SignalCount > inventory.ArchitecturePatterns.Signals.Count
                    || nativeFlow is { OmittedCount: > 0 }
                    || (semanticRequested && semantic is not null
                        && semantic.ResolvedInvocationCount + semantic.ResolvedConstructorCount + semantic.ResolvedDelegateTargetCount > semantic.ResolvedCalls.Count),
                semanticRequested,
                semanticRequested ? semantic?.ProjectCoverageComplete : null,
                semanticRequested && semantic is not null
                    ? semantic.UnresolvedInvocationCount + semantic.UnresolvedConstructorCount + semantic.UnresolvedDelegateTargetCount
                    : 0),
            limitations.ToArray());
        return report with { RepeatedCandidateGroups = CreateRepeatedCandidateGroups(ordered) };
    }

    public static IReadOnlyList<EvidenceCandidateGroup> CreateRepeatedCandidateGroups(
        IReadOnlyList<EvidenceFinding> emittedFindings) => emittedFindings
        .Where(item => (item.Category == "architecture-candidate" || item.Category == "framework-candidate")
            && item.Status == "candidate" && item.Line is not null)
        .GroupBy(item => (item.Category, item.Signal, item.Path))
        .Where(group => group.Count() >= 2)
        .OrderBy(group => group.Key.Category, StringComparer.Ordinal)
        .ThenBy(group => group.Key.Path, StringComparer.Ordinal)
        .ThenBy(group => group.Key.Signal, StringComparer.Ordinal)
        .Select(group =>
        {
            var locations = group.Select(item => new EvidenceCandidateLocation(item.Id, item.Line!.Value))
                .OrderBy(item => item.Line)
                .ThenBy(item => item.FindingId, StringComparer.Ordinal)
                .ToArray();
            return new EvidenceCandidateGroup(group.Key.Category, group.Key.Signal, group.Key.Path,
                locations.Length, locations);
        }).ToArray();

    private static IReadOnlyList<EvidenceFinding> EnsureUniqueIds(IReadOnlyList<EvidenceFinding> findings)
    {
        var originalIds = findings.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        var collisionOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
        var results = new EvidenceFinding[findings.Count];

        for (var index = 0; index < findings.Count; index++)
        {
            var finding = findings[index];
            if (usedIds.Add(finding.Id))
            {
                results[index] = finding;
                continue;
            }

            var ordinal = collisionOrdinals.GetValueOrDefault(finding.Id) + 1;
            string uniqueId;
            do
            {
                var identity = string.Join("\n", finding.Id,
                    finding.Column?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                    finding.Evidence, ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                uniqueId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16].ToLowerInvariant();
                ordinal++;
            }
            while (originalIds.Contains(uniqueId) || !usedIds.Add(uniqueId));

            collisionOrdinals[finding.Id] = ordinal - 1;
            results[index] = finding with { Id = uniqueId };
        }

        return results;
    }

    private static EvidenceFinding CreateFinding(string category, string status, string signal, string path, int? line,
        string? caller, string? target, string evidence, string? targetPath = null, int? targetLine = null,
        string? relationKind = null, string? sourceSymbol = null, string? targetSymbol = null,
        string? commandId = null, int? candidateTargetCount = null, IReadOnlyList<NativeFlowTarget>? candidateTargets = null,
        int? column = null)
    {
        var normalizedPath = path.Replace('\\', '/');
        var identity = relationKind is null
            ? string.Join("\n", category, status, signal, normalizedPath, line?.ToString() ?? "",
                caller ?? "", target ?? "")
            : string.Join("\n", category, status, signal, normalizedPath, line?.ToString() ?? "",
                relationKind, sourceSymbol ?? "", targetSymbol ?? "", targetPath ?? "", targetLine?.ToString() ?? "",
                commandId ?? "", candidateTargetCount?.ToString() ?? "",
                string.Join("|", (candidateTargets ?? Array.Empty<NativeFlowTarget>())
                    .Select(item => $"{item.Path}:{item.Line}:{item.Symbol}")));
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16].ToLowerInvariant();
        return new EvidenceFinding(id, category, status, signal, normalizedPath, line, caller, target, evidence,
            targetPath?.Replace('\\', '/'), targetLine, relationKind, sourceSymbol, targetSymbol, commandId,
            candidateTargetCount, candidateTargets) { Column = column };
    }

    private static bool IsSourceTestPath(string path) =>
        path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".vb", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".c", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".h", StringComparison.OrdinalIgnoreCase);

    private static bool IsFrameworkSignal(ArchitecturePatternSignal signal)
    {
        var text = signal.Category + " " + signal.Evidence;
        return text.Contains("MFC", StringComparison.OrdinalIgnoreCase)
            || text.Contains("C++/CLI", StringComparison.OrdinalIgnoreCase)
            || text.Contains("COM", StringComparison.OrdinalIgnoreCase)
            || text.Contains("NHibernate", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInsideLegacyFrameworkProject(RepositoryInventory inventory, string sourcePath)
    {
        var normalizedSource = sourcePath.Replace('\\', '/');
        foreach (var project in inventory.DotNetProjects.Where(item => item.IsLegacyFramework && string.IsNullOrWhiteSpace(item.ReadError)))
        {
            var projectDirectory = Path.GetDirectoryName(project.Path.Replace('\\', '/'))?.Replace('\\', '/')?.TrimEnd('/');
            if (string.IsNullOrEmpty(projectDirectory)) continue;
            var prefix = projectDirectory + "/";
            if (normalizedSource.StartsWith(prefix, OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
