namespace BrownPants.Cli.Analysis;

public sealed record WinFormsUiThreadingFinding(string Path, int Line, int Column, string Receiver,
    string Property, string TypeEvidence, string ExecutionContext, string? BackgroundCallback,
    string? MarshalingObservation, string Classification = "Candidate");

public sealed record WinFormsUiThreadingIssue(string Path, int Line, string Id, string Message);

public sealed record WinFormsUiThreadingReport(int SchemaVersion, int ScannedFiles, int CandidateCount,
    int OmittedCount, int ParseErrorCount, IReadOnlyList<WinFormsUiThreadingFinding> Findings,
    IReadOnlyList<WinFormsUiThreadingIssue> Issues);
