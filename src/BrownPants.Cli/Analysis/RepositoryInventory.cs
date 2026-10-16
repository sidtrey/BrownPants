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

    public JavaScriptTypeScriptAnalysis JavaScriptTypeScript { get; init; } = new("Bounded JavaScript/TypeScript syntax analysis", 0, 0, 0, 0,
        Array.Empty<JavaScriptTypeScriptDeclaration>(), Array.Empty<JavaScriptTypeScriptModuleRelationship>(),
        Array.Empty<JavaScriptTypeScriptCallRelationship>(), 0, 0, 0, 0, 0,
        Array.Empty<JavaScriptTypeScriptIssue>(), Array.Empty<string>());
}

public sealed record InvestigationReport(
    string Root,
    IReadOnlyList<InvestigationClueResult> Clues,
    int ReadIssueCount,
    IReadOnlyList<InvestigationReadIssue> ReadIssues,
    int SampleLimit)
{
    [JsonPropertyOrder(-1)]
    public int SchemaVersion => 1;
}

public sealed record InvestigationClueResult(
    string Clue,
    int MatchCount,
    IReadOnlyList<InvestigationMatch> Matches);

public sealed record InvestigationMatch(
    string Kind,
    string Path,
    int? Line,
    bool IsTestCandidate);

public sealed record InvestigationReadIssue(string Path, string Code);

public sealed record ExtensionCount(string Extension, int Count);

public sealed record ProjectSystemInfo(string Name, IReadOnlyList<string> ManifestPaths);

public sealed record LanguageCoverageInfo(string Language, int SourceFileCount, string AnalysisLevel);

public sealed record SddWorkflowInventory(
    string Name,
    bool IsInitialized,
    int SpecificationDirectoryCount,
    int? ActiveChangeDirectoryCount,
    int ArtifactFileCount,
    IReadOnlyList<string> ArtifactPaths,
    string? ActiveWorkItemPath = null,
    string? ActiveWorkItemEvidence = null);

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
    CSharpSemanticAnalysis? Semantic = null)
{
    internal IReadOnlyList<ManagedMethodTarget> ManagedMethodIndex { get; init; } = Array.Empty<ManagedMethodTarget>();
    internal bool ManagedMethodIndexTruncated { get; init; }
}

public sealed record CSharpTypeInfo(string Path, int Line, string Namespace, string Kind, string Name);

public sealed record CSharpMemberInfo(string Path, int Line, string ContainingType, string Kind, string Signature);

public sealed record CSharpUsingInfo(string Path, int Line, string Namespace, string? Alias, bool IsStatic, bool IsGlobal);

public sealed record ManagedMethodTarget(string Language, string Namespace, string TypeName, string MethodName, string Path, int Line,
    string DisplayTypeName = "");

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
    IReadOnlyList<VisualBasicParseIssue> Issues)
{
    internal IReadOnlyList<ManagedMethodTarget> ManagedMethodIndex { get; init; } = Array.Empty<ManagedMethodTarget>();
    internal bool ManagedMethodIndexTruncated { get; init; }
}

public sealed record VisualBasicTypeInfo(string Path, int Line, string Namespace, string Kind, string Name);
public sealed record VisualBasicMemberInfo(string Path, int Line, string ContainingType, string Kind, string Signature);
public sealed record VisualBasicInvocationInfo(string Path, int Line, string Expression);
public sealed record VisualBasicParseIssue(string Path, int Line, string Code, string Message);

public sealed record ArchitecturePatternAnalysis(int SignalCount, IReadOnlyList<ArchitecturePatternSignal> Signals,
    string EvidenceLimits = "", NativeFlowAnalysis? NativeFlow = null);
public sealed record ArchitecturePatternSignal(string Category, string EvidenceKind, string Language, string Path, int Line, string Evidence);

public sealed record NativeFlowAnalysis(
    int ScannedFileCount,
    int RelationCount,
    int UnresolvedCount,
    int AmbiguousCount,
    int OmittedCount,
    IReadOnlyList<NativeFlowRelation> Relations,
    IReadOnlyList<string> EvidenceLimits,
    bool ManagedTargetIndexTruncated = false);

public sealed record NativeFlowRelation(
    string Id,
    string Kind,
    string Status,
    string SourcePath,
    int SourceLine,
    string SourceSymbol,
    string? TargetPath,
    int? TargetLine,
    string TargetSymbol,
    string? CommandId,
    int CandidateTargetCount,
    string Evidence,
    IReadOnlyList<NativeFlowTarget>? CandidateTargets = null);

public sealed record NativeFlowTarget(string Path, int Line, string Symbol);

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
    IReadOnlyList<string> Issues,
    int DeclarationCount = 0,
    IReadOnlyList<CSharpSemanticDeclaration>? Declarations = null,
    int InvocationSyntaxCount = 0,
    int SkippedInvocationCount = 0,
    int AnalyzedInvocationCount = 0,
    IReadOnlyList<SemanticReasonCount>? UnresolvedReasonCounts = null,
    int ProjectsFailed = 0,
    bool ProjectCoverageComplete = true,
    int WorkspaceIssueCount = 0,
    int WorkspaceIssueOmittedCount = 0,
    int CompilerErrorCount = 0,
    int CompilerErrorOmittedCount = 0,
    IReadOnlyList<SemanticCompilerDiagnostic>? CompilerDiagnostics = null,
    int ConstructorSyntaxCount = 0,
    int ResolvedConstructorCount = 0,
    int UnresolvedConstructorCount = 0)
{
    public int CompilerDiagnosticGroupCount { get; init; }
    public int CompilerDiagnosticGroupOmittedCount { get; init; }
    public IReadOnlyList<SemanticCompilerDiagnosticGroup> CompilerDiagnosticGroups { get; init; }
        = Array.Empty<SemanticCompilerDiagnosticGroup>();
    internal IReadOnlyList<CSharpResolvedCall>? ImpactGraphCalls { get; init; }
    internal int ImpactGraphOmittedCount { get; init; }
    public int ResolvedDelegateTargetCount { get; init; }
    public int UnresolvedDelegateTargetCount { get; init; }
    public IReadOnlyList<CSharpUnresolvedCall> UnresolvedDelegateCalls { get; init; } = Array.Empty<CSharpUnresolvedCall>();
    internal MethodControlFlowSnapshot? RequestedControlFlow { get; init; }
}

public sealed record SemanticReasonCount(string Reason, int Count);
public sealed record SemanticCompilerDiagnostic(string Project, string? Path, int? Line, string Code, string Message);
public sealed record SemanticCompilerDiagnosticGroup(string Project, string Code, int Count);

public sealed record CSharpSemanticDeclaration(string Project, string Path, int Line, string Symbol);

public sealed record CSharpResolvedCall(string Project, string Path, int Line, string Expression, string Caller, string Target,
    string? TargetPath = null, int? TargetLine = null, string RelationshipKind = "method", string? TargetIdentity = null,
    int? SourceColumn = null);

public sealed record CSharpUnresolvedCall(
    string Project,
    string Path,
    int Line,
    string Expression,
    string CandidateReason,
    IReadOnlyList<string> CandidateTargets,
    string RelationshipKind = "method");

public sealed record DotNetProjectInfo(
    string Path,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> LanguageVersions,
    IReadOnlyList<string> ProjectReferences,
    bool IsLegacyFramework,
    string? ReadError);
