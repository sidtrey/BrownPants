namespace BrownPants.Cli.Analysis;

public sealed record JavaScriptTypeScriptAnalysis(
    string EvidenceLevel,
    int ScannedFileCount,
    int DeclarationCount,
    int ModuleRelationshipCount,
    int CallRelationshipCount,
    IReadOnlyList<JavaScriptTypeScriptDeclaration> Declarations,
    IReadOnlyList<JavaScriptTypeScriptModuleRelationship> ModuleRelationships,
    IReadOnlyList<JavaScriptTypeScriptCallRelationship> CallRelationships,
    int OmittedDeclarationCount,
    int OmittedModuleRelationshipCount,
    int OmittedCallRelationshipCount,
    int IssueCount,
    int OmittedIssueCount,
    IReadOnlyList<JavaScriptTypeScriptIssue> Issues,
    IReadOnlyList<string> EvidenceLimits)
{
    public TypeScriptSyntaxVerification? SyntaxVerification { get; init; }
    public ReactSupportAnalysis ReactSupport { get; init; } = new(0, 0, Array.Empty<ReactProjectInfo>(),
        0, 0, Array.Empty<ReactSourceCandidate>(), 0, 0, Array.Empty<ReactSourceCandidate>());
}

public sealed record JavaScriptTypeScriptDeclaration(
    string Language, string Path, int Line, int Column, string Kind, string Name,
    string? Container, string EvidenceKind = "syntax-candidate", string? ExportKind = null);

public sealed record JavaScriptTypeScriptModuleRelationship(
    string SourcePath, int SourceLine, int SourceColumn, string Specifier, string Kind, string Status,
    string? TargetPath, IReadOnlyList<string> CandidateTargetPaths, string? Reason);

public sealed record JavaScriptTypeScriptCallRelationship(
    string SourcePath, int SourceLine, int SourceColumn, string Caller, string Expression, string Status,
    string? TargetPath, int? TargetLine, string? TargetName, IReadOnlyList<string> CandidateTargets, string? Reason);

public sealed record JavaScriptTypeScriptIssue(string Path, string Code)
{
    /// <summary>Distinguishes lexical parse-shape warnings from compiler, resource, and metadata diagnostics.</summary>
    public string EvidenceKind => IsHeuristicSyntaxCode(Code) ? "SyntaxHeuristic" : "Analysis";

    public static bool IsHeuristicSyntaxCode(string code) => code is "UnbalancedDelimiter" or "UnterminatedBlockComment"
        or "UnterminatedRegularExpression" or "UnterminatedStringLiteral" or "UnterminatedTemplateLiteral";
}

public sealed record TypeScriptSyntaxVerification(
    string Status,
    int FileCount,
    int OmittedFileCount,
    int DiagnosticCount,
    int OmittedDiagnosticCount,
    IReadOnlyList<SyntaxParserInfo> Parsers,
    IReadOnlyList<TypeScriptSyntaxFileResult> Files,
    IReadOnlyList<TypeScriptSyntaxDiagnostic> Diagnostics,
    int HeuristicFindingCount,
    int OmittedHeuristicFindingCount,
    IReadOnlyList<JavaScriptTypeScriptIssue> HeuristicFindings,
    string? Message)
{
    [System.Text.Json.Serialization.JsonIgnore]
    internal IReadOnlySet<string> AllVerifiedSourcePaths { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    [System.Text.Json.Serialization.JsonIgnore]
    internal IReadOnlyList<ReactSourceCandidate> AllReactComponents { get; init; } = Array.Empty<ReactSourceCandidate>();

    [System.Text.Json.Serialization.JsonIgnore]
    internal IReadOnlyList<ReactSourceCandidate> AllReactHookCalls { get; init; } = Array.Empty<ReactSourceCandidate>();
}

public sealed record SyntaxParserInfo(string Name, string Version, string Path);

public sealed record TypeScriptSyntaxFileResult(string Path, string Status, string? Message);

public sealed record TypeScriptSyntaxDiagnostic(string Path, string Code, string Message, int? Line, int? Column);

public sealed record ReactSupportAnalysis(
    int ProjectCount,
    int OmittedProjectCount,
    IReadOnlyList<ReactProjectInfo> Projects,
    int ComponentCount,
    int OmittedComponentCount,
    IReadOnlyList<ReactSourceCandidate> Components,
    int HookCallCount,
    int OmittedHookCallCount,
    IReadOnlyList<ReactSourceCandidate> HookCalls)
{
    public string CandidateAnalysisStatus { get; init; } = "not-requested";

    [System.Text.Json.Serialization.JsonIgnore]
    internal IReadOnlyList<string> AllProjectManifestPaths { get; init; } = Array.Empty<string>();
}

public sealed record ReactProjectInfo(string Name, string ManifestPath, string? Version);

public sealed record ReactSourceCandidate(string Path, string Name, int Line, int Column, string Kind);

internal sealed record JavaScriptTypeScriptToken(string Text, string Kind, int Offset, int Line, int Column);
