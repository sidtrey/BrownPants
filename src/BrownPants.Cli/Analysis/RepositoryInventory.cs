using System.Text.Json.Serialization;

namespace BrownPants.Cli.Analysis;

public sealed record RepositoryInventory(
    string Root,
    int FileCount,
    IReadOnlyList<ExtensionCount> Extensions,
    IReadOnlyList<string> ProjectMarkers,
    IReadOnlyList<ProjectSystemInfo> ProjectSystems,
    IReadOnlyList<LanguageCoverageInfo> LanguageCoverage,
    IReadOnlyList<string> TestPaths,
    IReadOnlyList<string> TopLevelEntries,
    IReadOnlyList<SddWorkflowInventory> SddWorkflows,
    IReadOnlyList<SddWorkflowConfigurationIssue> SddWorkflowIssues,
    CSharpAnalysis CSharp,
    VisualBasicAnalysis VisualBasic,
    ArchitecturePatternAnalysis ArchitecturePatterns,
    IReadOnlyList<NativeProjectInfo> NativeProjects,
    IReadOnlyList<DotNetProjectInfo> DotNetProjects)
{
    [JsonPropertyOrder(-1)]
    public int SchemaVersion => 1;
}

public sealed record ExtensionCount(string Extension, int Count);

public sealed record ProjectSystemInfo(string Name, IReadOnlyList<string> ManifestPaths);

public sealed record LanguageCoverageInfo(string Language, int SourceFileCount, string AnalysisLevel);

public sealed record SddWorkflowInventory(
    string Name,
    bool IsInitialized,
    int SpecificationDirectoryCount,
    int? ActiveChangeDirectoryCount,
    int ArtifactFileCount,
    IReadOnlyList<string> ArtifactPaths);

public sealed record SddWorkflowConfigurationIssue(string Path, string Code, string Message);

public sealed record CSharpAnalysis(
    int SourceFileCount,
    int TypeCount,
    int MemberCount,
    int InvocationCount,
    int ParseErrorCount,
    int InactiveCodeRegionCount,
    IReadOnlyList<CSharpTypeInfo> Types,
    IReadOnlyList<CSharpMemberInfo> Members,
    IReadOnlyList<CSharpUsingInfo> Usings,
    IReadOnlyList<CSharpInvocationInfo> Invocations,
    IReadOnlyList<CSharpParseIssue> Issues,
    CSharpSemanticAnalysis? Semantic = null);

public sealed record CSharpTypeInfo(string Path, int Line, string Namespace, string Kind, string Name);

public sealed record CSharpMemberInfo(string Path, int Line, string ContainingType, string Kind, string Signature);

public sealed record CSharpUsingInfo(string Path, int Line, string Namespace, string? Alias, bool IsStatic, bool IsGlobal);

public sealed record CSharpInvocationInfo(string Path, int Line, string Expression);

public sealed record CSharpParseIssue(string Path, int Line, string Code, string Message);

public sealed record VisualBasicAnalysis(
    int SourceFileCount,
    int TypeCount,
    int MemberCount,
    int InvocationCount,
    int ParseErrorCount,
    IReadOnlyList<VisualBasicTypeInfo> Types,
    IReadOnlyList<VisualBasicMemberInfo> Members,
    IReadOnlyList<VisualBasicInvocationInfo> Invocations,
    IReadOnlyList<VisualBasicParseIssue> Issues);

public sealed record VisualBasicTypeInfo(string Path, int Line, string Namespace, string Kind, string Name);
public sealed record VisualBasicMemberInfo(string Path, int Line, string ContainingType, string Kind, string Signature);
public sealed record VisualBasicInvocationInfo(string Path, int Line, string Expression);
public sealed record VisualBasicParseIssue(string Path, int Line, string Code, string Message);

public sealed record ArchitecturePatternAnalysis(int SignalCount, IReadOnlyList<ArchitecturePatternSignal> Signals);
public sealed record ArchitecturePatternSignal(string Category, string EvidenceKind, string Language, string Path, int Line, string Evidence);

public sealed record NativeProjectInfo(
    string Path,
    bool HasMfcSignal,
    bool HasClrSupport,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> PlatformToolsets,
    IReadOnlyList<string> ProjectReferences,
    string? ReadError);

public sealed record CSharpSemanticAnalysis(
    int ProjectsDiscovered,
    int ProjectsLoaded,
    int ResolvedInvocationCount,
    int UnresolvedInvocationCount,
    IReadOnlyList<CSharpResolvedCall> ResolvedCalls,
    IReadOnlyList<CSharpUnresolvedCall> UnresolvedCalls,
    IReadOnlyList<string> Issues);

public sealed record CSharpResolvedCall(string Project, string Path, int Line, string Expression, string Caller, string Target);

public sealed record CSharpUnresolvedCall(
    string Project,
    string Path,
    int Line,
    string Expression,
    string CandidateReason,
    IReadOnlyList<string> CandidateTargets);

public sealed record DotNetProjectInfo(
    string Path,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> LanguageVersions,
    IReadOnlyList<string> ProjectReferences,
    bool IsLegacyFramework,
    string? ReadError);
