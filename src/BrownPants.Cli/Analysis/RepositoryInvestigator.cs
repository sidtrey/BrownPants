namespace BrownPants.Cli.Analysis;

public static class RepositoryInvestigator
{
    private const int MaximumFileBytes = 10 * 1024 * 1024;

    public static InvestigationReport Investigate(string path, IReadOnlyList<string> clues, int sampleLimit = 200)
    {
        if (sampleLimit is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "Sample limit must be between 1 and 10000.");
        if (clues.Count == 0 || clues.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Provide at least one non-empty clue.", nameof(clues));

        var root = Path.GetFullPath(path);
        var files = RepositoryScanner.DiscoverFiles(root);
        var matches = clues.Select(_ => new List<InvestigationMatch>()).ToArray();
        var matchCounts = new int[clues.Count];
        var readIssues = new List<InvestigationReadIssue>();
        var readIssueCount = 0;

        foreach (var relativePath in files)
        {
            var isTestCandidate = RepositoryScanner.IsTestPath(relativePath);
            for (var clueIndex = 0; clueIndex < clues.Count; clueIndex++)
            {
                if (relativePath.Contains(clues[clueIndex], StringComparison.OrdinalIgnoreCase))
                    AddMatch(clueIndex, new InvestigationMatch("Path", relativePath, null, isTestCandidate));
            }

            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (new FileInfo(fullPath).Length > MaximumFileBytes)
                {
                    AddReadIssue(relativePath, "FileTooLarge");
                    continue;
                }

                var lines = File.ReadAllLines(fullPath);
                if (lines.Any(line => line.Contains('\0')))
                {
                    AddReadIssue(relativePath, "BinaryFileSkipped");
                    continue;
                }

                for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    for (var clueIndex = 0; clueIndex < clues.Count; clueIndex++)
                    {
                        if (lines[lineIndex].Contains(clues[clueIndex], StringComparison.OrdinalIgnoreCase))
                        {
                            AddMatch(clueIndex, new InvestigationMatch(
                                "Content", relativePath, lineIndex + 1, isTestCandidate));
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                AddReadIssue(relativePath, "ReadFailure");
            }
        }

        return new InvestigationReport(
            root,
            clues.Select((clue, index) => new InvestigationClueResult(clue, matchCounts[index], matches[index])).ToArray(),
            readIssueCount,
            readIssues,
            sampleLimit);

        void AddMatch(int clueIndex, InvestigationMatch match)
        {
            matchCounts[clueIndex]++;
            if (matches[clueIndex].Count < sampleLimit)
                matches[clueIndex].Add(match);
        }

        void AddReadIssue(string relativePath, string code)
        {
            readIssueCount++;
            if (readIssues.Count < sampleLimit)
                readIssues.Add(new InvestigationReadIssue(relativePath, code));
        }
    }
}
