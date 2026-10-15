using System.Text;
using System.Text.Json;

namespace BrownPants.Cli.Analysis;

public sealed record ModelRoutingProfile(
    string Id,
    IReadOnlyList<string> Categories,
    string QualityTier,
    string Rationale,
    IReadOnlyDictionary<string, string> Models,
    string Instructions);

public sealed record ModelRoutingPolicy(string RelativePath, IReadOnlyList<ModelRoutingProfile> Profiles);

public static class ModelRoutingPolicyReader
{
    public const string DefaultPolicyPath = ".brownpants/models.json";
    public const int MaximumPolicyBytes = 128 * 1024;
    private const int MaximumProfiles = 64;
    private const int MaximumCategories = 32;
    private const int MaximumStringLength = 4000;

    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal)
    {
        "analysis", "complex-debugging", "specification", "planning", "task-breakdown",
        "implementation", "testing", "review", "documentation", "routine-maintenance"
    };

    private static readonly HashSet<string> Tiers = new(StringComparer.Ordinal)
    {
        "economy", "balanced", "high"
    };

    public static ModelRoutingPolicy Load(string repositoryRoot, string? policyPath = null)
    {
        var root = Path.GetFullPath(repositoryRoot);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Repository directory '{repositoryRoot}' was not found.");
        if (IsReparsePoint(root)) throw new ArgumentException("The selected repository root cannot be a symbolic link or reparse point.");

        var suppliedPath = policyPath ?? DefaultPolicyPath;
        var fullPath = ResolveInside(root, suppliedPath, "Policy");
        RejectReparseAncestors(root, fullPath, "Policy");
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Model policy '{NormalizeRelativePath(root, fullPath)}' was not found.", fullPath);

        byte[] bytes;
        using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length > MaximumPolicyBytes)
                throw new InvalidDataException($"{NormalizeRelativePath(root, fullPath)}: policy exceeds the {MaximumPolicyBytes}-byte limit.");
            var buffer = new byte[MaximumPolicyBytes + 1];
            var total = 0;
            while (total < buffer.Length)
            {
                var count = stream.Read(buffer, total, buffer.Length - total);
                if (count == 0) break;
                total += count;
            }
            if (total > MaximumPolicyBytes)
                throw new InvalidDataException($"{NormalizeRelativePath(root, fullPath)}: policy exceeds the {MaximumPolicyBytes}-byte limit.");
            bytes = buffer[..total];
        }

        string json;
        try { json = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"{NormalizeRelativePath(root, fullPath)}: policy is not valid UTF-8.", exception);
        }

        JsonDocument document;
        try { document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 }); }
        catch (JsonException exception)
        {
            var line = exception.LineNumber.GetValueOrDefault() + 1;
            throw new InvalidDataException($"{NormalizeRelativePath(root, fullPath)}:{line}: invalid JSON: {exception.Message}", exception);
        }

        using (document)
        {
            var relativePath = NormalizeRelativePath(root, fullPath);
            var rootElement = document.RootElement;
            RequireKind(rootElement, JsonValueKind.Object, relativePath, "$", "must be an object");
            EnsureProperties(rootElement, relativePath, "$", "schemaVersion", "profiles");

            var version = RequiredInt(rootElement, "schemaVersion", relativePath, "$");
            if (version != 1) throw PolicyError(relativePath, "$.schemaVersion", "must equal 1");
            var profilesElement = RequiredProperty(rootElement, "profiles", relativePath, "$", JsonValueKind.Array);
            if (profilesElement.GetArrayLength() is < 1 or > MaximumProfiles)
                throw PolicyError(relativePath, "$.profiles", $"must contain from 1 to {MaximumProfiles} profiles");

            var profiles = new List<ModelRoutingProfile>();
            var profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assignedCategories = new HashSet<string>(StringComparer.Ordinal);
            var totalCategories = 0;
            var index = 0;
            foreach (var profileElement in profilesElement.EnumerateArray())
            {
                var path = $"$.profiles[{index}]";
                RequireKind(profileElement, JsonValueKind.Object, relativePath, path, "must be an object");
                EnsureProperties(profileElement, relativePath, path, "id", "categories", "qualityTier", "rationale", "models", "instructions");

                var id = RequiredString(profileElement, "id", relativePath, path, 80);
                if (!IsSlug(id)) throw PolicyError(relativePath, $"{path}.id", "must use lowercase letters, digits, and single hyphens");
                if (!profileIds.Add(id)) throw PolicyError(relativePath, $"{path}.id", $"duplicates profile id '{id}'");

                var categoriesElement = RequiredProperty(profileElement, "categories", relativePath, path, JsonValueKind.Array);
                if (categoriesElement.GetArrayLength() is < 1 or > MaximumCategories)
                    throw PolicyError(relativePath, $"{path}.categories", $"must contain from 1 to {MaximumCategories} categories");
                var profileCategories = new List<string>();
                var localCategories = new HashSet<string>(StringComparer.Ordinal);
                foreach (var categoryElement in categoriesElement.EnumerateArray())
                {
                    if (categoryElement.ValueKind != JsonValueKind.String)
                        throw PolicyError(relativePath, $"{path}.categories[{profileCategories.Count}]", "must be a string");
                    var category = categoryElement.GetString()!;
                    if (!Categories.Contains(category))
                        throw PolicyError(relativePath, $"{path}.categories[{profileCategories.Count}]", $"unknown category '{category}'");
                    if (!localCategories.Add(category) || !assignedCategories.Add(category))
                        throw PolicyError(relativePath, $"{path}.categories[{profileCategories.Count}]", $"category '{category}' is assigned more than once");
                    profileCategories.Add(category);
                    totalCategories++;
                }
                if (totalCategories > MaximumCategories)
                    throw PolicyError(relativePath, "$.profiles", $"may assign no more than {MaximumCategories} total categories");

                var tier = RequiredString(profileElement, "qualityTier", relativePath, path, 16);
                if (!Tiers.Contains(tier)) throw PolicyError(relativePath, $"{path}.qualityTier", "must be economy, balanced, or high");
                var rationale = RequiredString(profileElement, "rationale", relativePath, path, 1000);
                var instructions = RequiredString(profileElement, "instructions", relativePath, path, MaximumStringLength);

                var modelsElement = RequiredProperty(profileElement, "models", relativePath, path, JsonValueKind.Object);
                if (!modelsElement.EnumerateObject().Any()) throw PolicyError(relativePath, $"{path}.models", "must include at least one host model identifier");
                var models = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var property in modelsElement.EnumerateObject())
                {
                    if (!IsHostSlug(property.Name)) throw PolicyError(relativePath, $"{path}.models.{property.Name}", "host key must be a lowercase slug");
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw PolicyError(relativePath, $"{path}.models.{property.Name}", "must be a model identifier string");
                    var model = property.Value.GetString()!;
                    if (string.IsNullOrWhiteSpace(model) || model.Length > 120)
                        throw PolicyError(relativePath, $"{path}.models.{property.Name}", "must be a non-empty model identifier of at most 120 characters");
                    if (!models.TryAdd(property.Name, model))
                        throw PolicyError(relativePath, $"{path}.models.{property.Name}", "is duplicated");
                }

                profiles.Add(new ModelRoutingProfile(id, profileCategories, tier, rationale, models, instructions));
                index++;
            }

            return new ModelRoutingPolicy(relativePath, profiles);
        }
    }

    public static string ResolveInside(string root, string relativePath, string description)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new ArgumentException($"{description} path must be repository-relative.");
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootPrefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new ArgumentException($"{description} path must remain inside the selected repository.");
        return fullPath;
    }

    public static void RejectReparseAncestors(string root, string fullPath, string description)
    {
        var current = fullPath;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        while (!string.Equals(current, root, comparison))
        {
            if ((File.Exists(current) || Directory.Exists(current)) && IsReparsePoint(current))
                throw new ArgumentException($"{description} path passes through a symbolic link or reparse point: '{NormalizeRelativePath(root, current)}'.");
            current = Path.GetDirectoryName(current) ?? root;
        }
        if (IsReparsePoint(root)) throw new ArgumentException($"{description} path passes through a symbolic link or reparse point at the repository root.");
    }

    public static string NormalizeRelativePath(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    private static bool IsReparsePoint(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsSlug(string value)
    {
        if (value.Length is < 1 or > 80 || value[0] is not (>= 'a' and <= 'z' or >= '0' and <= '9')) return false;
        var previousHyphen = false;
        foreach (var character in value)
        {
            if (character == '-')
            {
                if (previousHyphen) return false;
                previousHyphen = true;
            }
            else
            {
                if (character is not (>= 'a' and <= 'z' or >= '0' and <= '9')) return false;
                previousHyphen = false;
            }
        }
        return !previousHyphen;
    }

    private static bool IsHostSlug(string value)
    {
        if (value.Length is < 1 or > 64 || value[0] is < 'a' or > 'z') return false;
        return value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
    }

    private static void EnsureProperties(JsonElement element, string file, string path, params string[] allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Add(property.Name)) throw PolicyError(file, $"{path}.{property.Name}", "is duplicated");
            if (!allowedSet.Contains(property.Name)) throw PolicyError(file, $"{path}.{property.Name}", "is not a supported property");
        }
    }

    private static JsonElement RequiredProperty(JsonElement element, string name, string file, string path, JsonValueKind kind)
    {
        if (!element.TryGetProperty(name, out var property)) throw PolicyError(file, $"{path}.{name}", "is required");
        RequireKind(property, kind, file, $"{path}.{name}", $"must be {KindName(kind)}");
        return property;
    }

    private static int RequiredInt(JsonElement element, string name, string file, string path)
    {
        var value = RequiredProperty(element, name, file, path, JsonValueKind.Number);
        if (!value.TryGetInt32(out var number)) throw PolicyError(file, $"{path}.{name}", "must be an integer");
        return number;
    }

    private static string RequiredString(JsonElement element, string name, string file, string path, int maxLength)
    {
        var property = RequiredProperty(element, name, file, path, JsonValueKind.String);
        var value = property.GetString()!;
        if (string.IsNullOrWhiteSpace(value)) throw PolicyError(file, $"{path}.{name}", "must not be empty");
        if (value.Length > maxLength) throw PolicyError(file, $"{path}.{name}", $"must be no more than {maxLength} characters");
        return value;
    }

    private static void RequireKind(JsonElement element, JsonValueKind kind, string file, string path, string reason)
    {
        if (element.ValueKind != kind) throw PolicyError(file, path, reason);
    }

    private static string KindName(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        JsonValueKind.String => "a string",
        JsonValueKind.Number => "a number",
        _ => kind.ToString()
    };

    private static InvalidDataException PolicyError(string file, string path, string message) => new($"{file}:{path}: {message}.");
}
