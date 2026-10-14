using System.Text;
using System.Text.Json;

namespace BrownPants.Cli.Analysis;

public static class SddWorkflowAnalyzer
{
    private const string ConfigurationPath = ".brownpants/sdd-workflows.json";
    private const string ActiveFeaturePath = ".specify/feature.json";
    private const int MaximumConfigurationBytes = 1_048_576;
    private const int MaximumWorkflowDefinitions = 100;
    private const int MaximumActiveFeatureBytes = 65_536;

    private static readonly IComparer<string> StablePathComparer = Comparer<string>.Create((left, right) =>
    {
        var result = StringComparer.OrdinalIgnoreCase.Compare(left, right);
        return result != 0 ? result : StringComparer.Ordinal.Compare(left, right);
    });

    public static SddWorkflowAnalysis Analyze(string root, IReadOnlyList<string> files, int sampleLimit)
    {
        var workflows = new List<SddWorkflowInventory>();
        var issues = new List<SddWorkflowConfigurationIssue>();
        var specKitArtifacts = files.Where(path => path.StartsWith(".specify/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("specs/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StablePathComparer)
            .ToArray();
        if (files.Any(path => path.StartsWith(".specify/", StringComparison.OrdinalIgnoreCase)))
        {
            var activeFeature = ReadActiveFeatureDirectory(root, files, issues);
            workflows.Add(new SddWorkflowInventory(
                "Spec Kit",
                true,
                FirstLevelDirectoryCount(files, "specs/"),
                null,
                specKitArtifacts.Length,
                specKitArtifacts.Take(sampleLimit).ToArray(),
                activeFeature,
                activeFeature is null ? null : "SpecKitFeaturePointer"));
        }

        var openSpecArtifacts = files.Where(path => path.StartsWith("openspec/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StablePathComparer)
            .ToArray();
        if (openSpecArtifacts.Length > 0)
        {
            var activeChangeDirectories = FirstLevelDirectories(files, "openspec/changes/")
                .Where(name => !name.Equals("archive", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StablePathComparer)
                .ToArray();
            var activeChanges = activeChangeDirectories.Length;
            var activeChangePath = activeChanges == 1
                ? $"openspec/changes/{activeChangeDirectories[0]}"
                : null;
            workflows.Add(new SddWorkflowInventory(
                "OpenSpec",
                true,
                FirstLevelDirectoryCount(files, "openspec/specs/"),
                activeChanges,
                openSpecArtifacts.Length,
                openSpecArtifacts.Take(sampleLimit).ToArray(),
                activeChangePath,
                activeChangePath is null ? null : "UniqueOpenSpecChange"));
        }

        var declarationPaths = files
            .Where(path => path.Equals(ConfigurationPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StablePathComparer)
            .ToArray();
        if (declarationPaths.Length > 1)
        {
            issues.Add(new SddWorkflowConfigurationIssue(
                ConfigurationPath,
                "DuplicateConfigurationFile",
                "More than one case-variant declaration file was found; keep only .brownpants/sdd-workflows.json."));
        }
        else if (declarationPaths.Length == 1)
        {
            var definitions = ReadDefinitions(root, declarationPaths[0], issues);
            AddCustomWorkflows(files, definitions, sampleLimit, workflows);
        }

        var customWorkflows = workflows.Skip(CountBuiltInWorkflows(workflows))
            .OrderBy(workflow => workflow.Name, StablePathComparer)
            .ToArray();
        var orderedWorkflows = workflows.Take(CountBuiltInWorkflows(workflows)).Concat(customWorkflows).ToArray();
        var orderedIssues = issues
            .OrderBy(issue => issue.Path, StablePathComparer)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray();
        return new SddWorkflowAnalysis(orderedWorkflows, orderedIssues);
    }

    private static int CountBuiltInWorkflows(IReadOnlyList<SddWorkflowInventory> workflows) =>
        workflows.Count(workflow => workflow.Name is "Spec Kit" or "OpenSpec");

    private static string? ReadActiveFeatureDirectory(
        string root,
        IReadOnlyList<string> files,
        List<SddWorkflowConfigurationIssue> issues)
    {
        var matchingPaths = files
            .Where(path => path.Equals(ActiveFeaturePath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StablePathComparer)
            .ToArray();
        if (matchingPaths.Length == 0)
            return null;
        if (matchingPaths.Length > 1)
        {
            issues.Add(ActiveFeatureIssue("DuplicateActiveFeatureFile",
                "More than one case-variant feature pointer was found; keep only .specify/feature.json."));
            return null;
        }

        var fullPath = Path.Combine(root, matchingPaths[0].Replace('/', Path.DirectorySeparatorChar));
        byte[] bytes;
        try
        {
            if ((File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
            {
                issues.Add(ActiveFeatureIssue("ActiveFeatureIsReparsePoint",
                    "The feature pointer must not be a symbolic link or reparse point."));
                return null;
            }

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumActiveFeatureBytes)
            {
                issues.Add(ActiveFeatureIssue("ActiveFeatureTooLarge",
                    $"The feature pointer exceeds the {MaximumActiveFeatureBytes} byte size limit."));
                return null;
            }

            var buffer = new byte[MaximumActiveFeatureBytes + 1];
            var length = 0;
            while (length < buffer.Length)
            {
                var read = stream.Read(buffer, length, buffer.Length - length);
                if (read == 0)
                    break;
                length += read;
            }
            if (length > MaximumActiveFeatureBytes)
            {
                issues.Add(ActiveFeatureIssue("ActiveFeatureTooLarge",
                    $"The feature pointer exceeds the {MaximumActiveFeatureBytes} byte size limit."));
                return null;
            }
            bytes = buffer[..length];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(ActiveFeatureIssue("UnableToReadActiveFeature",
                "The feature pointer could not be read."));
            return null;
        }

        string json;
        try
        {
            json = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            if (json.StartsWith('\uFEFF'))
                json = json[1..];
        }
        catch (DecoderFallbackException)
        {
            issues.Add(ActiveFeatureIssue("InvalidActiveFeatureEncoding",
                "The feature pointer must use UTF-8 encoding."));
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
        }
        catch (JsonException exception)
        {
            var line = exception.LineNumber.GetValueOrDefault();
            var bytePosition = exception.BytePositionInLine.GetValueOrDefault();
            issues.Add(ActiveFeatureIssue("InvalidActiveFeatureJson",
                $"The feature pointer contains invalid JSON at line {line + 1}, byte {bytePosition}."));
            return null;
        }

        string? selectedPath;
        using (document)
        {
            var documentRoot = document.RootElement;
            var selectedProperties = documentRoot.ValueKind == JsonValueKind.Object
                ? documentRoot.EnumerateObject()
                    .Where(property => property.NameEquals("feature_directory"))
                    .ToArray()
                : Array.Empty<JsonProperty>();
            if (selectedProperties.Length != 1
                || selectedProperties[0].Value.ValueKind != JsonValueKind.String)
            {
                issues.Add(ActiveFeatureIssue("UnsafeActiveFeaturePath",
                    "The feature pointer must contain one safe repository-relative feature_directory."));
                return null;
            }

            if (!TryNormalizeActiveFeaturePath(selectedProperties[0].Value.GetString(), out var normalizedPath))
            {
                issues.Add(ActiveFeatureIssue("UnsafeActiveFeaturePath",
                    "The feature pointer must contain one safe repository-relative feature_directory."));
                return null;
            }
            selectedPath = normalizedPath;
        }

        var featurePrefix = selectedPath + "/";
        if (!files.Any(path => path.StartsWith(featurePrefix, StringComparison.Ordinal)))
        {
            issues.Add(ActiveFeatureIssue("ActiveFeatureNotFound",
                "The selected feature directory has no files in the scanned repository inventory."));
            return null;
        }

        return selectedPath;
    }

    private static bool TryNormalizeActiveFeaturePath(string? value, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
            return false;

        var path = value.Replace('\\', '/');
        if (path.StartsWith("/", StringComparison.Ordinal)
            || path.Contains(':')
            || path.Contains('*')
            || path.Contains('?'))
            return false;

        var segments = path.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."
            || segment.Any(char.IsControl)))
            return false;

        normalizedPath = string.Join('/', segments);
        return true;
    }

    private static IReadOnlyList<CustomSddWorkflowDefinition> ReadDefinitions(
        string root,
        string relativePath,
        List<SddWorkflowConfigurationIssue> issues)
    {
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        byte[] bytes;
        try
        {
            if ((File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
            {
                issues.Add(Issue("ConfigurationIsReparsePoint", "The declaration file must not be a symbolic link or reparse point."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumConfigurationBytes)
            {
                issues.Add(Issue("ConfigurationTooLarge", "The declaration file exceeds the 1 MiB size limit."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            var buffer = new byte[MaximumConfigurationBytes + 1];
            var length = 0;
            while (length < buffer.Length)
            {
                var read = stream.Read(buffer, length, buffer.Length - length);
                if (read == 0)
                    break;
                length += read;
            }

            if (length > MaximumConfigurationBytes)
            {
                issues.Add(Issue("ConfigurationTooLarge", "The declaration file exceeds the 1 MiB size limit."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            bytes = buffer[..length];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(Issue("UnableToReadConfiguration", "The declaration file could not be read."));
            return Array.Empty<CustomSddWorkflowDefinition>();
        }

        string json;
        try
        {
            json = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            if (json.StartsWith('\uFEFF'))
                json = json[1..];
        }
        catch (DecoderFallbackException)
        {
            issues.Add(Issue("InvalidEncoding", "The declaration file must use UTF-8 encoding."));
            return Array.Empty<CustomSddWorkflowDefinition>();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (JsonException exception)
        {
            var line = exception.LineNumber.GetValueOrDefault();
            var bytePosition = exception.BytePositionInLine.GetValueOrDefault();
            issues.Add(Issue("InvalidJson", $"The declaration contains invalid JSON at line {line + 1}, byte {bytePosition}."));
            return Array.Empty<CustomSddWorkflowDefinition>();
        }

        using (document)
        {
            var documentRoot = document.RootElement;
            if (documentRoot.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Issue("InvalidConfiguration", "The declaration must be a JSON object."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            if (!HasOnlyProperties(documentRoot, new[] { "schemaVersion", "workflows" }, "The declaration", issues))
                return Array.Empty<CustomSddWorkflowDefinition>();

            if (!documentRoot.TryGetProperty("schemaVersion", out var schemaVersion)
                || schemaVersion.ValueKind != JsonValueKind.Number
                || !schemaVersion.TryGetInt32(out var version))
            {
                issues.Add(Issue("InvalidSchemaVersion", "The declaration must specify integer schemaVersion 1."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            if (version != 1)
            {
                issues.Add(Issue("UnsupportedSchemaVersion", $"Schema version {version} is unsupported; use version 1."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            if (!documentRoot.TryGetProperty("workflows", out var workflowElements)
                || workflowElements.ValueKind != JsonValueKind.Array
                || workflowElements.GetArrayLength() == 0)
            {
                issues.Add(Issue("InvalidWorkflowList", "The declaration must contain a non-empty workflows array."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            if (workflowElements.GetArrayLength() > MaximumWorkflowDefinitions)
            {
                issues.Add(Issue("TooManyWorkflowDefinitions", $"The declaration may contain at most {MaximumWorkflowDefinitions} workflows."));
                return Array.Empty<CustomSddWorkflowDefinition>();
            }

            var definitions = new List<CustomSddWorkflowDefinition>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Spec Kit", "OpenSpec" };
            var index = 0;
            foreach (var element in workflowElements.EnumerateArray())
            {
                ReadDefinition(element, index++, names, definitions, issues);
            }

            return definitions;
        }
    }

    private static void ReadDefinition(
        JsonElement element,
        int index,
        HashSet<string> names,
        List<CustomSddWorkflowDefinition> definitions,
        List<SddWorkflowConfigurationIssue> issues)
    {
        var context = $"Workflow at index {index}";
        if (element.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Issue("InvalidWorkflow", $"{context} must be a JSON object."));
            return;
        }

        if (!HasOnlyProperties(element,
                new[] { "name", "artifactRoots", "specificationRoots", "changeRoots", "archivedChangeDirectoryNames" },
                context,
                issues))
            return;

        if (!element.TryGetProperty("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(nameElement.GetString())
            || nameElement.GetString() != nameElement.GetString()!.Trim()
            || nameElement.GetString()!.Any(char.IsControl))
        {
            issues.Add(Issue("InvalidWorkflowName", $"{context} must have a non-empty name without surrounding whitespace or control characters."));
            return;
        }

        var name = nameElement.GetString()!;
        if (!names.Add(name))
        {
            issues.Add(Issue("DuplicateWorkflowName", "Workflow names must be unique and cannot collide with Spec Kit or OpenSpec."));
            return;
        }

        var valid = true;
        var artifactRoots = ReadStringArray(element, "artifactRoots", required: true, context, issues, ref valid);
        var specificationRoots = ReadStringArray(element, "specificationRoots", required: false, context, issues, ref valid);
        var changeRoots = ReadStringArray(element, "changeRoots", required: false, context, issues, ref valid);
        var archivedNames = ReadStringArray(element, "archivedChangeDirectoryNames", required: false, context, issues, ref valid);

        ValidateRoots(artifactRoots, "artifactRoots", context, required: true, issues, ref valid);
        ValidateRoots(specificationRoots, "specificationRoots", context, required: false, issues, ref valid);
        ValidateRoots(changeRoots, "changeRoots", context, required: false, issues, ref valid);
        ValidateArchiveNames(archivedNames, context, issues, ref valid);

        if (valid)
        {
            definitions.Add(new CustomSddWorkflowDefinition(
                name,
                artifactRoots.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                specificationRoots.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                changeRoots.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                archivedNames.ToHashSet(StringComparer.OrdinalIgnoreCase)));
        }
    }

    private static List<string> ReadStringArray(
        JsonElement definition,
        string propertyName,
        bool required,
        string context,
        List<SddWorkflowConfigurationIssue> issues,
        ref bool valid)
    {
        if (!definition.TryGetProperty(propertyName, out var value))
        {
            if (required)
            {
                issues.Add(Issue("MissingProperty", $"{context} is missing required property {propertyName}."));
                valid = false;
            }
            return new List<string>();
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            issues.Add(Issue("InvalidProperty", $"{context} property {propertyName} must be an array of strings."));
            valid = false;
            return new List<string>();
        }

        var result = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                issues.Add(Issue("InvalidProperty", $"{context} property {propertyName} must contain only strings."));
                valid = false;
                continue;
            }
            result.Add(item.GetString()!);
        }
        return result;
    }

    private static void ValidateRoots(
        IReadOnlyList<string> roots,
        string propertyName,
        string context,
        bool required,
        List<SddWorkflowConfigurationIssue> issues,
        ref bool valid)
    {
        if (required && roots.Count == 0)
        {
            issues.Add(Issue("InvalidRoot", $"{context} property {propertyName} must contain at least one root."));
            valid = false;
        }

        foreach (var root in roots)
        {
            if (IsValidRoot(root))
                continue;
            issues.Add(Issue("InvalidRoot", $"{context} property {propertyName} contains an unsafe repository-relative directory root."));
            valid = false;
        }
    }

    private static bool IsValidRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root)
            || root != root.Trim()
            || root.StartsWith("/", StringComparison.Ordinal)
            || root.EndsWith("/", StringComparison.Ordinal)
            || root.Contains('\\')
            || root.Contains(':')
            || root.Contains('*')
            || root.Contains('?'))
            return false;

        var segments = root.Split('/');
        return segments.All(segment => segment.Length > 0 && segment is not "." and not ".."
            && !segment.Any(char.IsControl));
    }

    private static void ValidateArchiveNames(
        IReadOnlyList<string> names,
        string context,
        List<SddWorkflowConfigurationIssue> issues,
        ref bool valid)
    {
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name)
                && name == name.Trim()
                && name is not "." and not ".."
                && !name.Contains('/')
                && !name.Contains('\\')
                && !name.Contains('*')
                && !name.Contains('?')
                && !name.Contains(':')
                && !name.Any(char.IsControl))
                continue;

            issues.Add(Issue("InvalidArchiveDirectoryName", $"{context} archive directory names must be non-empty single directory names."));
            valid = false;
        }
    }

    private static bool HasOnlyProperties(
        JsonElement element,
        IReadOnlyCollection<string> allowedProperties,
        string context,
        List<SddWorkflowConfigurationIssue> issues)
    {
        var valid = true;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                issues.Add(Issue("DuplicateProperty", $"{context} repeats a JSON property."));
                valid = false;
            }
            else if (!allowedProperties.Contains(property.Name))
            {
                issues.Add(Issue("UnknownProperty", $"{context} contains a property not recognized by schema version 1."));
                valid = false;
            }
        }
        return valid;
    }

    private static void AddCustomWorkflows(
        IReadOnlyList<string> files,
        IReadOnlyList<CustomSddWorkflowDefinition> definitions,
        int sampleLimit,
        List<SddWorkflowInventory> workflows)
    {
        if (definitions.Count == 0)
            return;

        var custom = new List<CustomWorkflowAccumulator>();
        var artifactsByRoot = new Dictionary<string, List<CustomWorkflowAccumulator>>(StringComparer.OrdinalIgnoreCase);
        var specificationsByRoot = new Dictionary<string, List<CustomWorkflowAccumulator>>(StringComparer.OrdinalIgnoreCase);
        var changesByRoot = new Dictionary<string, List<CustomWorkflowAccumulator>>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            var workflow = new CustomWorkflowAccumulator(definition);
            custom.Add(workflow);
            RegisterRoots(artifactsByRoot, definition.ArtifactRoots, workflow);
            RegisterRoots(specificationsByRoot, definition.SpecificationRoots, workflow);
            RegisterRoots(changesByRoot, definition.ChangeRoots, workflow);
        }

        var orderedFiles = files.Distinct(StringComparer.Ordinal).OrderBy(path => path, StablePathComparer);
        foreach (var path in orderedFiles)
        {
            var matchedArtifacts = new HashSet<CustomWorkflowAccumulator>();
            for (var separator = path.IndexOf('/'); separator >= 0; separator = path.IndexOf('/', separator + 1))
            {
                var directory = path[..separator];
                if (!path.Equals(ConfigurationPath, StringComparison.OrdinalIgnoreCase)
                    && artifactsByRoot.TryGetValue(directory, out var artifactWorkflows))
                {
                    foreach (var workflow in artifactWorkflows)
                    {
                        if (matchedArtifacts.Add(workflow))
                            workflow.AddArtifact(path, sampleLimit);
                    }
                }

                var nextSeparator = path.IndexOf('/', separator + 1);
                if (nextSeparator <= separator + 1)
                    continue;

                var childDirectory = path[..nextSeparator];
                if (specificationsByRoot.TryGetValue(directory, out var specificationWorkflows))
                    foreach (var workflow in specificationWorkflows)
                        workflow.AddSpecificationDirectory(childDirectory);
                if (changesByRoot.TryGetValue(directory, out var changeWorkflows))
                    foreach (var workflow in changeWorkflows)
                        workflow.AddChangeDirectory(childDirectory);
            }
        }

        workflows.AddRange(custom
            .OrderBy(workflow => workflow.Definition.Name, StablePathComparer)
            .Select(workflow => workflow.ToInventory()));
    }

    private static void RegisterRoots(
        Dictionary<string, List<CustomWorkflowAccumulator>> workflowsByRoot,
        IReadOnlyList<string> roots,
        CustomWorkflowAccumulator workflow)
    {
        foreach (var root in roots)
        {
            if (!workflowsByRoot.TryGetValue(root, out var workflows))
            {
                workflows = new List<CustomWorkflowAccumulator>();
                workflowsByRoot.Add(root, workflows);
            }
            workflows.Add(workflow);
        }
    }

    private sealed class CustomWorkflowAccumulator(CustomSddWorkflowDefinition definition)
    {
        private readonly List<string> _artifactPaths = new();
        private readonly HashSet<string> _specificationDirectories = new(StringComparer.Ordinal);
        private readonly HashSet<string> _changeDirectories = new(StringComparer.Ordinal);

        public CustomSddWorkflowDefinition Definition { get; } = definition;
        public int ArtifactFileCount { get; private set; }

        public void AddArtifact(string path, int sampleLimit)
        {
            ArtifactFileCount++;
            if (_artifactPaths.Count < sampleLimit)
                _artifactPaths.Add(path);
        }

        public void AddSpecificationDirectory(string path) => _specificationDirectories.Add(path);

        public void AddChangeDirectory(string path) => _changeDirectories.Add(path);

        public SddWorkflowInventory ToInventory()
        {
            int? activeChanges = Definition.ChangeRoots.Count == 0
                ? null
                : _changeDirectories.Count(path => !Definition.ArchivedChangeDirectoryNames.Contains(LastPathSegment(path)));
            return new SddWorkflowInventory(
                Definition.Name,
                ArtifactFileCount > 0,
                _specificationDirectories.Count,
                activeChanges,
                ArtifactFileCount,
                _artifactPaths);
        }
    }

    private static string LastPathSegment(string path) => path[(path.LastIndexOf('/') + 1)..];

    private static SddWorkflowConfigurationIssue Issue(string code, string message) =>
        new(ConfigurationPath, code, message);

    private static SddWorkflowConfigurationIssue ActiveFeatureIssue(string code, string message) =>
        new(ActiveFeaturePath, code, message);

    private static int FirstLevelDirectoryCount(IReadOnlyList<string> files, string prefix) =>
        FirstLevelDirectories(files, prefix).Count;

    private static HashSet<string> FirstLevelDirectories(IReadOnlyList<string> files, string prefix) => files
        .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .Select(path => path[prefix.Length..])
        .Where(remainder => remainder.IndexOf('/') > 0)
        .Select(remainder => remainder[..remainder.IndexOf('/')])
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public sealed record SddWorkflowAnalysis(
    IReadOnlyList<SddWorkflowInventory> Workflows,
    IReadOnlyList<SddWorkflowConfigurationIssue> Issues);

internal sealed record CustomSddWorkflowDefinition(
    string Name,
    IReadOnlyList<string> ArtifactRoots,
    IReadOnlyList<string> SpecificationRoots,
    IReadOnlyList<string> ChangeRoots,
    IReadOnlySet<string> ArchivedChangeDirectoryNames);
