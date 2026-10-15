using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

public static partial class SddWorkbenchAnalyzer
{
    private const long MaximumArtifactBytes = 2 * 1024 * 1024;
    private const long MaximumTotalBytes = 8 * 1024 * 1024;
    private static readonly string[] ArtifactNames = { "spec.md", "plan.md", "tasks.md", "acceptance-evidence.json", "verification.md" };

    public static SddWorkbenchData Analyze(string repositoryRoot, string? featurePath, string engine = "speckit")
    {
        var root = Path.GetFullPath(repositoryRoot);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Repository directory was not found: '{repositoryRoot}'.");

        if (engine.Equals("openspec", StringComparison.OrdinalIgnoreCase))
            return AnalyzeOpenSpec(root, featurePath);
        if (!engine.Equals("speckit", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown workbench engine '{engine}'. Use 'speckit' or 'openspec'.");

        var relativeFeature = featurePath is null
            ? ReadActiveFeature(root)
            : NormalizeRelativePath(featurePath, "Feature");
        var featureDirectory = ResolveInside(root, relativeFeature, "Feature");
        if (!Directory.Exists(featureDirectory))
            throw new DirectoryNotFoundException($"Feature directory was not found: '{relativeFeature}'.");

        var contents = new Dictionary<string, string>(StringComparer.Ordinal);
        long totalBytes = 0;
        foreach (var name in ArtifactNames)
        {
            var relativePath = $"{relativeFeature}/{name}".Replace('\\', '/');
            var path = Path.Combine(featureDirectory, name);
            if (!File.Exists(path))
            {
                if (name == "spec.md")
                    throw new FileNotFoundException($"Required feature specification was not found: '{relativePath}'.", relativePath);
                continue;
            }

            EnsureNoReparsePoints(root, path);
            var bytes = new FileInfo(path).Length;
            if (bytes > MaximumArtifactBytes)
                throw new InvalidDataException($"Feature artifact '{relativePath}' exceeds the 2 MiB workbench limit.");
            totalBytes += bytes;
            if (totalBytes > MaximumTotalBytes)
                throw new InvalidDataException("Feature artifacts exceed the 8 MiB total workbench limit.");

            try
            {
                contents[name] = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException($"Feature artifact '{relativePath}' is not valid UTF-8.", exception);
            }
        }

        var spec = contents["spec.md"];
        var stories = ParseStories(spec);
        var criteria = ParseCriteria(spec);
        var tasks = contents.TryGetValue("tasks.md", out var taskText) ? ParseTasks(taskText, $"{relativeFeature}/tasks.md") : Array.Empty<SddWorkbenchTask>();
        var evidence = ParseEvidence(contents, relativeFeature);
        var baseline = GetBaselineStatus(root, featureDirectory, spec);
        var artifacts = contents.OrderBy(pair => Array.IndexOf(ArtifactNames, pair.Key))
            .Select(pair => new SddWorkbenchArtifact(pair.Key, $"{relativeFeature}/{pair.Key}".Replace('\\', '/'), pair.Value))
            .ToArray();

        return new SddWorkbenchData("Spec Kit", relativeFeature, FeatureTitle(spec, relativeFeature), string.Empty,
            stories, criteria, tasks, evidence.Entries, evidence.Issues, baseline, artifacts);
    }

    private static SddWorkbenchData AnalyzeOpenSpec(string root, string? featurePath)
    {
        var relativeChange = featurePath is null ? SelectOpenSpecChange(root) : NormalizeOpenSpecChange(featurePath);
        var changeDirectory = ResolveInside(root, relativeChange, "OpenSpec change");
        if (!Directory.Exists(changeDirectory))
            throw new DirectoryNotFoundException($"OpenSpec change was not found: '{relativeChange}'. Supply --feature openspec/changes/<change-name>.");
        EnsureNoReparsePoints(root, changeDirectory);
        relativeChange = Path.GetRelativePath(root, changeDirectory).Replace('\\', '/');

        var paths = new List<string>();
        foreach (var fileName in new[] { "proposal.md", "design.md", "tasks.md" })
        {
            var path = Path.Combine(changeDirectory, fileName);
            if (File.Exists(path)) paths.Add(path);
        }
        var proposalPath = Path.Combine(changeDirectory, "proposal.md");
        if (!File.Exists(proposalPath))
            throw new FileNotFoundException($"Required OpenSpec proposal was not found: '{relativeChange}/proposal.md'.", proposalPath);

        var deltaRoot = Path.Combine(changeDirectory, "specs");
        if (Directory.Exists(deltaRoot))
            CollectOpenSpecDeltaFiles(root, deltaRoot, paths);

        paths = paths.Distinct(PathComparer()).OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal).ToList();
        if (paths.Count > 1000)
            throw new InvalidDataException("OpenSpec change contains more than 1000 workbench artifacts.");

        var contents = new Dictionary<string, string>(StringComparer.Ordinal);
        long totalBytes = 0;
        foreach (var path in paths)
        {
            EnsureNoReparsePoints(root, path);
            var bytes = new FileInfo(path).Length;
            var relativePath = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (bytes > MaximumArtifactBytes)
                throw new InvalidDataException($"OpenSpec artifact '{relativePath}' exceeds the 2 MiB workbench limit.");
            totalBytes += bytes;
            if (totalBytes > MaximumTotalBytes)
                throw new InvalidDataException("OpenSpec change artifacts exceed the 8 MiB total workbench limit.");
            try
            {
                contents[relativePath] = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException($"OpenSpec artifact '{relativePath}' is not valid UTF-8.", exception);
            }
        }

        var proposal = contents[Path.Combine(relativeChange, "proposal.md").Replace('\\', '/')];
        var title = $"OpenSpec Change: {Path.GetFileName(relativeChange)}";
        var changePrefix = relativeChange.Replace('\\', '/') + "/";
        var deltaSpecs = contents.Where(pair => pair.Key.StartsWith(changePrefix + "specs/", StringComparison.Ordinal)
                && pair.Key.EndsWith("/spec.md", StringComparison.Ordinal))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray();
        var criteria = ParseOpenSpecReviewPoints(deltaSpecs);
        var tasksPath = Path.Combine(relativeChange, "tasks.md").Replace('\\', '/');
        var tasks = contents.TryGetValue(tasksPath, out var taskText) ? ParseOpenSpecTasks(taskText, tasksPath) : Array.Empty<SddWorkbenchTask>();
        var proposalSummary = string.Join("\n\n", new[] { ExtractMarkdownSection(proposal, "Why"), ExtractMarkdownSection(proposal, "What Changes") }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var artifacts = contents.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new SddWorkbenchArtifact(Path.GetFileName(pair.Key), pair.Key, pair.Value))
            .ToArray();

        return new SddWorkbenchData("OpenSpec", relativeChange.Replace('\\', '/'), title, proposalSummary,
            Array.Empty<SddWorkbenchStory>(), criteria, tasks, Array.Empty<SddWorkbenchEvidence>(), Array.Empty<string>(),
            "OpenSpec changes use delta specifications; no Spec Kit acceptance baseline is read.", artifacts);
    }

    private static string SelectOpenSpecChange(string root)
    {
        var changesRoot = Path.Combine(root, "openspec", "changes");
        if (!Directory.Exists(changesRoot))
            throw new InvalidDataException("No OpenSpec changes directory was found. Supply --feature openspec/changes/<change-name>.");
        EnsureNoReparsePoints(root, changesRoot);
        var candidates = Directory.EnumerateDirectories(changesRoot)
            .Take(1001)
            .Where(path => !Path.GetFileName(path).Equals("archive", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length > 1000)
            throw new InvalidDataException("OpenSpec has more than 1000 candidate changes; select one explicitly with --feature.");
        if (candidates.Length == 0)
            throw new InvalidDataException("No active OpenSpec change was found outside openspec/changes/archive. Supply --feature openspec/changes/<change-name>.");
        if (candidates.Length > 1)
        {
            var sample = string.Join(", ", candidates.Take(5).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')));
            throw new InvalidDataException($"Multiple OpenSpec changes are available ({sample}). Select one with --feature openspec/changes/<change-name>.");
        }
        EnsureNoReparsePoints(root, candidates[0]);
        return Path.GetRelativePath(root, candidates[0]).Replace('\\', '/');
    }

    private static string NormalizeOpenSpecChange(string path)
    {
        var normalized = NormalizeRelativePath(path, "OpenSpec change");
        const string prefix = "openspec/changes/";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("OpenSpec --feature must be openspec/changes/<change-name>.");
        var name = normalized[prefix.Length..];
        if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Equals("archive", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("OpenSpec --feature must select one non-archived directory directly under openspec/changes.");
        return normalized;
    }

    private static void CollectOpenSpecDeltaFiles(string root, string directory, List<string> paths)
    {
        var pending = new Stack<string>();
        pending.Push(directory);
        var visitedDirectories = 0;
        var visitedEntries = 0;
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (++visitedDirectories > 1000)
                throw new InvalidDataException("OpenSpec delta specs contain more than 1000 directories.");
            EnsureNoReparsePoints(root, current);
            foreach (var file in Directory.EnumerateFiles(current).OrderBy(value => value, StringComparer.Ordinal))
            {
                if (++visitedEntries > 10_000)
                    throw new InvalidDataException("OpenSpec delta spec traversal exceeds the 10000-entry limit.");
                EnsureNoReparsePoints(root, file);
                if (Path.GetFileName(file).Equals("spec.md", StringComparison.OrdinalIgnoreCase)) paths.Add(file);
            }
            foreach (var child in Directory.EnumerateDirectories(current).OrderByDescending(value => value, StringComparer.Ordinal))
            {
                if (++visitedEntries > 10_000)
                    throw new InvalidDataException("OpenSpec delta spec traversal exceeds the 10000-entry limit.");
                EnsureNoReparsePoints(root, child);
                pending.Push(child);
            }
            if (paths.Count > 1000)
                throw new InvalidDataException("OpenSpec change contains more than 1000 workbench artifacts.");
        }
    }

    private static StringComparer PathComparer() => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static SddWorkbenchCriterion[] ParseOpenSpecReviewPoints(IReadOnlyList<KeyValuePair<string, string>> deltaSpecs)
    {
        var result = new List<SddWorkbenchCriterion>();
        foreach (var (path, content) in deltaSpecs)
        {
            var requirementIndex = 0;
            var scenarioIndex = 0;
            foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                var requirement = OpenSpecRequirementRegex().Match(line);
                if (requirement.Success)
                {
                    requirementIndex++;
                    result.Add(new SddWorkbenchCriterion($"{path}#requirement-{requirementIndex:D3}", "Requirement", requirement.Groups["text"].Value.Trim(), path));
                    continue;
                }
                var scenario = OpenSpecScenarioRegex().Match(line);
                if (scenario.Success)
                {
                    scenarioIndex++;
                    result.Add(new SddWorkbenchCriterion($"{path}#scenario-{scenarioIndex:D3}", "Scenario", scenario.Groups["text"].Value.Trim(), path));
                }
            }
        }
        return result.ToArray();
    }

    private static SddWorkbenchTask[] ParseOpenSpecTasks(string text, string sourcePath)
    {
        var index = 0;
        return OpenSpecTaskRegex().Matches(text).Select(match =>
        {
            index++;
            var body = match.Groups["body"].Value.Trim();
            var numberedTask = OpenSpecTaskNumberRegex().Match(body);
            var taskId = numberedTask.Success ? numberedTask.Groups["id"].Value : $"task-{index:D3}";
            var taskText = numberedTask.Success ? numberedTask.Groups["text"].Value.Trim() : body;
            return new SddWorkbenchTask(taskId, taskText, match.Groups["checked"].Value is "x" or "X", sourcePath);
        }).ToArray();
    }

    private static string ExtractMarkdownSection(string markdown, string heading)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var start = Array.FindIndex(lines, line => line.Equals($"## {heading}", StringComparison.OrdinalIgnoreCase));
        if (start < 0) return string.Empty;
        var end = start + 1;
        while (end < lines.Length && !lines[end].StartsWith("## ", StringComparison.Ordinal)) end++;
        return string.Join("\n", lines[(start + 1)..end]).Trim();
    }

    public static string ResolveOutputPath(string repositoryRoot, string outputPath)
    {
        var root = Path.GetFullPath(repositoryRoot);
        var relative = NormalizeRelativePath(outputPath, "Output");
        var full = ResolveInside(root, relative, "Output");
        EnsureNoReparsePoints(root, full);
        return full;
    }

    private static string ReadActiveFeature(string root)
    {
        var pointerPath = Path.Combine(root, ".specify", "feature.json");
        EnsureNoReparsePoints(root, pointerPath);
        if (!File.Exists(pointerPath))
            throw new InvalidDataException("No active Spec Kit feature was found. Supply --feature <relative-feature-directory>.");
        if (new FileInfo(pointerPath).Length > 64 * 1024)
            throw new InvalidDataException("The active feature pointer exceeds the 64 KiB limit.");

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(pointerPath), new JsonDocumentOptions { MaxDepth = 16 });
            if (!document.RootElement.TryGetProperty("feature_directory", out var feature)
                || feature.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(feature.GetString()))
                throw new InvalidDataException("The active feature pointer has no feature_directory. Supply --feature <relative-feature-directory>.");
            return NormalizeRelativePath(feature.GetString()!, "Active feature");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The active Spec Kit feature pointer is invalid JSON. Supply --feature <relative-feature-directory>.", exception);
        }
    }

    private static string NormalizeRelativePath(string path, string description)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            throw new ArgumentException($"{description} path must be repository-relative.");
        var normalized = path.Replace('\\', '/').TrimEnd('/');
        if (normalized.Length == 0 || normalized == ".")
            throw new ArgumentException($"{description} path must name a repository-relative directory or file.");
        return normalized;
    }

    private static string ResolveInside(string root, string relativePath, string description)
    {
        var full = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rel = Path.GetRelativePath(root, full);
        if (Path.IsPathRooted(rel) || rel == ".." || rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException($"{description} path resolves outside the selected repository.");
        return full;
    }

    private static void EnsureNoReparsePoints(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(fullRoot, fullPath);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Workbench path resolves outside the selected repository.");

        var current = fullRoot;
        var segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current))
                break;
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException($"Workbench path contains a symbolic link or reparse point: '{Path.GetRelativePath(fullRoot, current).Replace('\\', '/')}'.");
        }
    }

    private static string FeatureTitle(string spec, string relativeFeature)
    {
        var title = FeatureHeadingRegex().Match(spec);
        return title.Success ? title.Groups["title"].Value.Trim() : Path.GetFileName(relativeFeature);
    }

    private static SddWorkbenchStory[] ParseStories(string spec)
    {
        var matches = StoryHeadingRegex().Matches(spec);
        return matches.Select(match => new SddWorkbenchStory(
            $"US{match.Groups["id"].Value}",
            match.Groups["title"].Value.Trim(),
            match.Groups["priority"].Value,
            FirstDescriptionLine(spec, match.Index + match.Length))).ToArray();
    }

    private static string FirstDescriptionLine(string text, int start)
    {
        foreach (var line in text[start..].Split('\n'))
        {
            var candidate = line.Trim();
            if (candidate.Length == 0 || candidate.StartsWith("**", StringComparison.Ordinal) || candidate.StartsWith('#'))
                continue;
            if (candidate.StartsWith("- ", StringComparison.Ordinal) || Regex.IsMatch(candidate, "^\\d+\\.", RegexOptions.CultureInvariant))
                return string.Empty;
            return candidate;
        }
        return string.Empty;
    }

    private static SddWorkbenchCriterion[] ParseCriteria(string spec)
    {
        var criteria = AcceptanceCriterionRegex().Matches(spec)
            .Select(match => new SddWorkbenchCriterion(match.Groups["id"].Value, "Acceptance criterion", match.Groups["text"].Value.Trim()))
            .Concat(SuccessCriterionRegex().Matches(spec)
                .Select(match => new SddWorkbenchCriterion(match.Groups["id"].Value, "Success criterion", match.Groups["text"].Value.Trim())))
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        return criteria;
    }

    private static SddWorkbenchTask[] ParseTasks(string text, string sourcePath)
    {
        return TaskRegex().Matches(text).Select(match => new SddWorkbenchTask(
            match.Groups["id"].Success ? match.Groups["id"].Value : $"task-{match.Index}",
            match.Groups["body"].Value.Trim(),
            match.Groups["checked"].Value is "x" or "X", sourcePath))
            .ToArray();
    }

    private static (SddWorkbenchEvidence[] Entries, string[] Issues) ParseEvidence(
        IReadOnlyDictionary<string, string> contents, string relativeFeature)
    {
        if (!contents.TryGetValue("acceptance-evidence.json", out var text))
            return (Array.Empty<SddWorkbenchEvidence>(), Array.Empty<string>());

        try
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 24 });
            if (!document.RootElement.TryGetProperty("tasks", out var tasks) || tasks.ValueKind != JsonValueKind.Array)
                return (Array.Empty<SddWorkbenchEvidence>(), new[] { "Acceptance evidence has no tasks array." });
            var entries = new List<SddWorkbenchEvidence>();
            foreach (var task in tasks.EnumerateArray())
            {
                if (task.ValueKind != JsonValueKind.Object) continue;
                entries.Add(new SddWorkbenchEvidence(
                    GetString(task, "taskId") ?? "Unknown task",
                    GetString(task, "result") ?? "unrecorded",
                    GetStringArray(task, "criterionIds"),
                    GetStringArray(task, "evidencePaths").Select(NormalizeEvidencePath).ToArray()));
            }
            return (entries.ToArray(), Array.Empty<string>());
        }
        catch (JsonException)
        {
            return (Array.Empty<SddWorkbenchEvidence>(), new[] { "Acceptance evidence JSON could not be parsed; original content is available below." });
        }
    }

    private static string NormalizeEvidencePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value)) return "(invalid path)";
        var normalized = value.Replace('\\', '/');
        return normalized.Split('/').Any(segment => segment == "..") ? "(invalid path)" : normalized;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string[] GetStringArray(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).ToArray()
            : Array.Empty<string>();

    private static string GetBaselineStatus(string root, string featureDirectory, string spec)
    {
        var path = Path.Combine(featureDirectory, "acceptance-criteria.sha256");
        if (!File.Exists(path)) return "No acceptance-criteria baseline recorded";
        EnsureNoReparsePoints(root, path);
        if (new FileInfo(path).Length > 64 * 1024) return "Acceptance-criteria baseline exceeds the 64 KiB limit";
        var expected = File.ReadAllText(path, Encoding.UTF8).Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (expected is null || !Regex.IsMatch(expected, "^[a-fA-F0-9]{64}$")) return "Acceptance-criteria baseline is invalid";
        try
        {
            var actual = PullRequestPreviewAnalyzer.ComputeCriteriaDigest(spec);
            return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)
                ? "Acceptance criteria match the recorded baseline"
                : "Acceptance criteria changed since the recorded baseline";
        }
        catch (InvalidOperationException)
        {
            return "Acceptance-criteria sections are incomplete; baseline could not be checked";
        }
    }

    [GeneratedRegex("(?m)^#\\s+Feature Specification:\\s*(?<title>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex FeatureHeadingRegex();

    [GeneratedRegex("(?m)^#{2,4}\\s+User Story\\s+(?<id>\\d+)\\s*[-—:]\\s*(?<title>.+?)(?:\\s+\\(Priority:\\s*(?<priority>P\\d)\\))?\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex StoryHeadingRegex();

    [GeneratedRegex("(?m)^\\s*\\d+\\.\\s+\\*\\*(?<id>AC-\\d{3})\\*\\*\\s*(?:[—–:-]\\s*)?(?<text>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex AcceptanceCriterionRegex();

    [GeneratedRegex("(?m)^\\s*-\\s+\\*\\*(?<id>SC-\\d{3})\\*\\*\\s*(?:[—–:-]\\s*)?(?<text>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SuccessCriterionRegex();

    [GeneratedRegex("(?m)^\\s*-\\s+\\[(?<checked>[ xX])\\]\\s+(?<body>(?<id>T\\d{3,})\\s+.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex TaskRegex();

    [GeneratedRegex("^\\s*#{3,6}\\s+Requirement:\\s*(?<text>.+?)\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex OpenSpecRequirementRegex();

    [GeneratedRegex("^\\s*#{4,6}\\s+Scenario:\\s*(?<text>.+?)\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex OpenSpecScenarioRegex();

    [GeneratedRegex("(?m)^\\s*-\\s+\\[(?<checked>[ xX])\\]\\s+(?<body>.+?)\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex OpenSpecTaskRegex();

    [GeneratedRegex("^(?<id>\\d+(?:\\.\\d+)*)(?:[.)])\\s+(?<text>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex OpenSpecTaskNumberRegex();
}

public sealed record SddWorkbenchData(
    string Engine,
    string FeaturePath,
    string FeatureTitle,
    string ProposalSummary,
    IReadOnlyList<SddWorkbenchStory> Stories,
    IReadOnlyList<SddWorkbenchCriterion> Criteria,
    IReadOnlyList<SddWorkbenchTask> Tasks,
    IReadOnlyList<SddWorkbenchEvidence> Evidence,
    IReadOnlyList<string> EvidenceIssues,
    string AcceptanceBaselineStatus,
    IReadOnlyList<SddWorkbenchArtifact> Artifacts);

public sealed record SddWorkbenchStory(string Id, string Title, string Priority, string Description);
public sealed record SddWorkbenchCriterion(string Id, string Kind, string Text, string? SourcePath = null);
public sealed record SddWorkbenchTask(string Id, string Text, bool IsComplete, string? SourcePath = null);
public sealed record SddWorkbenchEvidence(string TaskId, string Result, IReadOnlyList<string> CriterionIds, IReadOnlyList<string> EvidencePaths);
public sealed record SddWorkbenchArtifact(string Name, string Path, string Content);
