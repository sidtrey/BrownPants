using System.Security.Cryptography;
using System.Text;

namespace BrownPants.Cli.Analysis;

public sealed record EvidenceReport(
    int SchemaVersion,
    string Root,
    EvidenceContext Context,
    IReadOnlyList<EvidenceFinding> Findings,
    EvidenceCompleteness Completeness,
    IReadOnlyList<string> Limitations);

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
    string Evidence);

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

        if (semanticRequested && semantic is not null)
        {
            foreach (var call in semantic.ResolvedCalls)
            {
                var signal = call.RelationshipKind == "constructor" ? "resolved constructor" : "resolved call";
                findings.Add(CreateFinding("managed-call", "resolved", signal, call.Path, call.Line,
                    call.Caller, call.Target, call.Expression));
            }
        }

        foreach (var path in inventory.TestPaths.Where(IsSourceTestPath))
        {
            findings.Add(CreateFinding("test-identification", "candidate", "test path", path, null,
                null, null, "Repository test-path heuristic; inspect the file to confirm relevance."));
        }

        var allOrdered = findings
            .DistinctBy(item => item.Id, StringComparer.Ordinal)
            .OrderBy(item => item.Category, StringComparer.Ordinal)
            .ThenBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .ThenBy(item => item.Signal, StringComparer.Ordinal)
            .ThenBy(item => item.CallerSignature, StringComparer.Ordinal)
            .ThenBy(item => item.TargetSignature, StringComparer.Ordinal)
            .ToArray();
        var ordered = allOrdered.Take(sampleLimit).ToArray();
        var semanticTotal = semanticRequested && semantic is not null
            ? semantic.ResolvedInvocationCount + semantic.ResolvedConstructorCount
            : 0;
        var knownTotal = inventory.ArchitecturePatterns.SignalCount + semanticTotal
            + inventory.TestPaths.Count(IsSourceTestPath);

        var limitations = new SortedSet<string>(StringComparer.Ordinal)
        {
            "Architecture, framework, and test-path findings are candidates and require source review.",
            "Native, ORM, reflection, and runtime relationships are not inferred as managed edges; constructor edges require explicit compiler-bound source construction."
        };
        if (!semanticRequested)
            limitations.Add("Semantic analysis was not requested; no resolved managed call findings are included.");
        if (semantic is not null)
        {
            if (!semantic.ProjectCoverageComplete)
                limitations.Add($"Semantic project coverage is incomplete: {semantic.ProjectsLoaded} of {semantic.ProjectsDiscovered} projects loaded.");
            var unresolvedTotal = semantic.UnresolvedInvocationCount + semantic.UnresolvedConstructorCount;
            if (unresolvedTotal > 0)
                limitations.Add($"{semantic.UnresolvedInvocationCount} managed invocation(s) and {semantic.UnresolvedConstructorCount} constructor relationship(s) remain unresolved.");
            foreach (var issue in semantic.Issues.OrderBy(value => value, StringComparer.Ordinal))
                limitations.Add(issue);
        }
        if (inventory.ArchitecturePatterns.SignalCount > inventory.ArchitecturePatterns.Signals.Count)
            limitations.Add($"Architecture signals were sampled: {inventory.ArchitecturePatterns.Signals.Count} of {inventory.ArchitecturePatterns.SignalCount} retained.");

        return new EvidenceReport(
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
                    || (semanticRequested && semantic is not null
                        && semantic.ResolvedInvocationCount + semantic.ResolvedConstructorCount > semantic.ResolvedCalls.Count),
                semanticRequested,
                semanticRequested ? semantic?.ProjectCoverageComplete : null,
                semanticRequested && semantic is not null
                    ? semantic.UnresolvedInvocationCount + semantic.UnresolvedConstructorCount
                    : 0),
            limitations.ToArray());
    }

    private static EvidenceFinding CreateFinding(string category, string status, string signal, string path, int? line,
        string? caller, string? target, string evidence)
    {
        var normalizedPath = path.Replace('\\', '/');
        var identity = string.Join("\n", category, status, signal, normalizedPath, line?.ToString() ?? "",
            caller ?? "", target ?? "");
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16].ToLowerInvariant();
        return new EvidenceFinding(id, category, status, signal, normalizedPath, line, caller, target, evidence);
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
