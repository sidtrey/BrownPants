using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

public sealed record PullRequestPreviewTask(
    string TaskId,
    string Status,
    IReadOnlyList<string> CriterionIds,
    string? VerificationCommand,
    IReadOnlyList<string> EvidencePaths,
    IReadOnlyList<string> Gaps);

public sealed record PullRequestQaCriterion(
    string CriterionId,
    string Text,
    IReadOnlyList<string> VerificationCommands,
    IReadOnlyList<string> EvidencePaths,
    IReadOnlyList<string> Gaps);

public sealed record PullRequestPreview(
    int SchemaVersion,
    string FeaturePath,
    string Title,
    string Description,
    string Repository,
    string SourceBranch,
    string TargetBranch,
    IReadOnlyList<string> WorkItemIds,
    string CriteriaDigest,
    bool CriteriaBaselineValid,
    bool IsComplete,
    IReadOnlyList<PullRequestPreviewTask> Tasks,
    IReadOnlyList<PullRequestQaCriterion> QaHandoff,
    IReadOnlyList<AzureDevOpsDeliveryReference> DeliveryReferences,
    IReadOnlyList<string> Gaps);

public static class PullRequestPreviewAnalyzer
{
    private static readonly Regex CriterionPattern = new("(?m)^\\s*\\d+\\.\\s+\\*\\*(?<id>AC-\\d{3})\\*\\*\\s+—\\s+(?<text>.+)$", RegexOptions.CultureInvariant);
    private static readonly Regex TaskPattern = new("(?m)^\\s*-\\s+\\[(?<state> |x|X)\\]\\s+(?<id>T\\d{3})\\b(?<text>.*)$", RegexOptions.CultureInvariant);

    public static PullRequestPreview Analyze(string repositoryRoot, string featurePath, string sourceBranch, string targetBranch,
        string repositoryId, AzureDevOpsTraceReport? traceReport = null)
    {
        var root = Path.GetFullPath(repositoryRoot);
        ValidateBranch(sourceBranch, nameof(sourceBranch));
        ValidateBranch(targetBranch, nameof(targetBranch));
        var normalizedFeature = NormalizeFeaturePath(featurePath);
        var featureDirectory = ResolveInside(root, normalizedFeature);
        var specPath = Path.Combine(featureDirectory, "spec.md");
        var tasksPath = Path.Combine(featureDirectory, "tasks.md");
        var evidencePath = Path.Combine(featureDirectory, "acceptance-evidence.json");
        var baselinePath = Path.Combine(featureDirectory, "acceptance-criteria.sha256");
        var spec = File.ReadAllText(specPath);
        var taskText = File.ReadAllText(tasksPath);
        var gaps = new List<string>();

        var criteria = CriterionPattern.Matches(spec).Select(match => new Criterion(
            match.Groups["id"].Value, match.Groups["text"].Value.Trim())).ToArray();
        if (criteria.Length == 0) gaps.Add("No acceptance criteria were found in the feature specification.");
        var actualDigest = ComputeCriteriaDigest(spec);
        var expectedDigest = ReadDigest(baselinePath);
        var baselineValid = string.Equals(actualDigest, expectedDigest, StringComparison.OrdinalIgnoreCase);
        if (!baselineValid) gaps.Add("Acceptance criteria are missing a baseline or changed since the baseline was recorded.");

        var tasks = ParseTasks(taskText);
        var ledger = ReadEvidenceLedger(evidencePath, gaps);
        var previewTasks = new List<PullRequestPreviewTask>();
        foreach (var task in tasks)
        {
            var taskGaps = new List<string>();
            if (!ledger.TryGetValue(task.Id, out var evidence))
            {
                taskGaps.Add("No verification evidence ledger entry exists.");
                previewTasks.Add(new PullRequestPreviewTask(task.Id, task.Completed ? "Task complete; evidence missing" : "Incomplete", [], null, [], taskGaps));
                continue;
            }
            var evidencePaths = new List<string>();
            foreach (var path in evidence.EvidencePaths)
            {
                try
                {
                    var fullPath = ResolveInside(root, path);
                    if (!File.Exists(fullPath)) taskGaps.Add($"Evidence file is missing: {path}");
                    else evidencePaths.Add(path.Replace('\\', '/'));
                }
                catch (ArgumentException) { taskGaps.Add("An evidence path escapes the repository."); }
            }
            var verified = task.Completed && evidence.Result == "passed" && taskGaps.Count == 0 && evidencePaths.Count > 0;
            if (task.Completed && !verified) taskGaps.Add("Task is marked complete without passing evidence for every cited file.");
            if (!task.Completed) taskGaps.Add("Task is not marked complete.");
            previewTasks.Add(new PullRequestPreviewTask(task.Id, verified ? "Verified" : task.Completed ? "Incomplete evidence" : "Incomplete",
                evidence.CriterionIds, evidence.VerificationCommand, evidencePaths, taskGaps));
        }
        foreach (var taskId in ledger.Keys.Except(tasks.Select(task => task.Id), StringComparer.Ordinal))
            gaps.Add($"Evidence ledger references task {taskId}, which is not in tasks.md.");

        var qaHandoff = criteria.Select(criterion =>
        {
            var matching = previewTasks.Where(task => task.CriterionIds.Contains(criterion.Id, StringComparer.Ordinal)).ToArray();
            var commands = matching.Where(task => !string.IsNullOrWhiteSpace(task.VerificationCommand))
                .Select(task => task.VerificationCommand!).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var evidenceFiles = matching.SelectMany(task => task.EvidencePaths).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var criterionGaps = new List<string>();
            if (matching.Length == 0) criterionGaps.Add("No task provides verification guidance for this criterion.");
            if (commands.Length == 0) criterionGaps.Add("No verification command is documented for this criterion.");
            if (evidenceFiles.Length == 0) criterionGaps.Add("No existing verification evidence is linked to this criterion.");
            return new PullRequestQaCriterion(criterion.Id, criterion.Text, commands, evidenceFiles, criterionGaps);
        }).ToArray();
        foreach (var qa in qaHandoff) gaps.AddRange(qa.Gaps.Select(gap => $"{qa.CriterionId}: {gap}"));

        var selectedTrace = traceReport is not null && traceReport.FeaturePath == normalizedFeature ? traceReport : null;
        if (traceReport is not null && selectedTrace is null) gaps.Add("Traceability data belongs to a different feature and was omitted.");
        if (selectedTrace is { IsComplete: false }) gaps.AddRange(selectedTrace.Diagnostics);
        var workItemIds = selectedTrace?.WorkItems.Select(item => item.Id).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray() ?? [];
        var deliveryReferences = selectedTrace?.DeliveryReferences ?? [];

        var title = ReadTitle(spec, normalizedFeature);
        var description = BuildDescription(normalizedFeature, actualDigest, previewTasks, qaHandoff, deliveryReferences, gaps);
        return new PullRequestPreview(1, normalizedFeature, title, description, repositoryId,
            sourceBranch, targetBranch, workItemIds, actualDigest, baselineValid,
            baselineValid && gaps.Count == 0 && previewTasks.All(task => task.Status == "Verified"),
            previewTasks, qaHandoff, deliveryReferences, gaps.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public static string ComputeCriteriaDigest(string spec)
    {
        var lines = spec.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        string GetSection(string heading)
        {
            var start = Array.IndexOf(lines, heading);
            if (start < 0) throw new InvalidDataException($"Feature specification is missing '{heading}'.");
            var end = start + 1;
            while (end < lines.Length && !lines[end].StartsWith("## ", StringComparison.Ordinal)) end++;
            return string.Join('\n', lines[start..end]).Trim();
        }
        var canonical = GetSection("## User Scenarios & Testing") + "\n\n" + GetSection("## Success Criteria");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static Dictionary<string, Evidence> ReadEvidenceLedger(string path, List<string> gaps)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (!root.TryGetProperty("schemaVersion", out var version) || version.GetInt32() != 1 || !root.TryGetProperty("tasks", out var entries))
                throw new InvalidDataException("Evidence ledger schema is unsupported.");
            var result = new Dictionary<string, Evidence>(StringComparer.Ordinal);
            foreach (var entry in entries.EnumerateArray())
            {
                var id = entry.GetProperty("taskId").GetString()!;
                result[id] = new Evidence(
                    entry.GetProperty("criterionIds").EnumerateArray().Select(item => item.GetString()!).ToArray(),
                    entry.GetProperty("verificationCommand").GetString()!,
                    entry.GetProperty("evidencePaths").EnumerateArray().Select(item => item.GetString()!).ToArray(),
                    entry.GetProperty("result").GetString()!);
            }
            return result;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or KeyNotFoundException or InvalidDataException)
        {
            gaps.Add("The acceptance evidence ledger is missing or invalid.");
            return new Dictionary<string, Evidence>(StringComparer.Ordinal);
        }
    }

    private static string ReadDigest(string path)
    {
        if (!File.Exists(path)) return string.Empty;
        var value = File.ReadAllText(path).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return value is { Length: 64 } && value.All(Uri.IsHexDigit) ? value : string.Empty;
    }

    private static string ReadTitle(string spec, string featurePath)
    {
        var line = spec.Split('\n').FirstOrDefault(value => value.StartsWith("# Feature Specification:", StringComparison.Ordinal));
        return line is null ? $"Feature: {featurePath}" : line["# Feature Specification:".Length..].Trim();
    }

    private static List<TaskInfo> ParseTasks(string taskText) => TaskPattern.Matches(taskText)
        .Select(match => new TaskInfo(match.Groups["id"].Value, match.Groups["state"].Value.Equals("x", StringComparison.OrdinalIgnoreCase)))
        .ToList();

    private static string BuildDescription(string featurePath, string digest, IReadOnlyList<PullRequestPreviewTask> tasks,
        IReadOnlyList<PullRequestQaCriterion> qa, IReadOnlyList<AzureDevOpsDeliveryReference> refs, IReadOnlyList<string> gaps)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"BrownPants feature `{featurePath}`. Acceptance criteria SHA-256: `{digest}`.");
        builder.AppendLine($"Task evidence: {tasks.Count(task => task.Status == "Verified")}/{tasks.Count} verified.");
        builder.AppendLine($"QA handoff: {qa.Count} acceptance criteria, {qa.Count(item => item.Gaps.Count > 0)} with guidance gaps.");
        if (refs.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Related delivery evidence:");
            foreach (var reference in refs.OrderBy(item => item.ArtifactType, StringComparer.Ordinal).ThenBy(item => item.Identifier, StringComparer.Ordinal))
                builder.AppendLine($"- {reference.ArtifactType} {reference.Identifier}: {reference.TargetUrl ?? reference.EvidenceSourceUrl}");
        }
        if (gaps.Count > 0) builder.AppendLine($"\nVerification gaps: {gaps.Count}. See the attached BrownPants preview and QA handoff.");
        return builder.ToString().TrimEnd();
    }

    private static string NormalizeFeaturePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.StartsWith('/') || Path.IsPathRooted(path)
            || path.Split('/').Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new ArgumentException("Feature path must be a normalized repository-relative path.", nameof(path));
        return path;
    }

    private static string ResolveInside(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath) || relativePath.Contains('\\') && OperatingSystem.IsWindows())
            throw new ArgumentException("Path must be repository-relative.");
        var path = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(root, path);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new ArgumentException("Path must remain inside the repository.");
        return path;
    }

    internal static void ValidateBranch(string branch, string parameterName)
    {
        var invalid = string.IsNullOrWhiteSpace(branch) || branch.StartsWith("-", StringComparison.Ordinal)
            || branch.StartsWith("refs/heads/", StringComparison.Ordinal) || branch.Contains("..", StringComparison.Ordinal)
            || branch.Contains("@{", StringComparison.Ordinal) || branch.StartsWith('/') || branch.EndsWith('/')
            || branch == "@" || branch.Any(character => character <= 0x20 || character == 0x7f || "~^:?*[\\<>|\"".Contains(character));
        if (!invalid)
        {
            invalid = branch.Split('/').Any(segment => segment.Length == 0 || segment[0] == '.'
                || segment[^1] == '.' || segment.EndsWith(".lock", StringComparison.OrdinalIgnoreCase));
        }
        if (invalid)
            throw new ArgumentException("Branch must be a valid branch name without refs/heads/ prefix.", parameterName);
    }

    private sealed record Criterion(string Id, string Text);
    private sealed record TaskInfo(string Id, bool Completed);
    private sealed record Evidence(IReadOnlyList<string> CriterionIds, string VerificationCommand, IReadOnlyList<string> EvidencePaths, string Result);
}
