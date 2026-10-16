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
    IReadOnlyList<string> EvidenceLimits);

public sealed record JavaScriptTypeScriptDeclaration(
    string Language, string Path, int Line, int Column, string Kind, string Name,
    string? Container, string EvidenceKind = "syntax-candidate", string? ExportKind = null);

public sealed record JavaScriptTypeScriptModuleRelationship(
    string SourcePath, int SourceLine, int SourceColumn, string Specifier, string Kind, string Status,
    string? TargetPath, IReadOnlyList<string> CandidateTargetPaths, string? Reason);

public sealed record JavaScriptTypeScriptCallRelationship(
    string SourcePath, int SourceLine, int SourceColumn, string Caller, string Expression, string Status,
    string? TargetPath, int? TargetLine, string? TargetName, IReadOnlyList<string> CandidateTargets, string? Reason);

public sealed record JavaScriptTypeScriptIssue(string Path, string Code);

internal sealed record JavaScriptTypeScriptToken(string Text, string Kind, int Offset, int Line, int Column);
