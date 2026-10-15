using System.Text.Json;

namespace BrownPants.Cli.Analysis;

public sealed record AzureDevOpsConfiguration(
    Uri OrganizationUrl,
    string Project,
    string Repository,
    string AuthenticationScheme,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Features,
    string? EntraClientId = null,
    string? EntraTenantId = null)
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "schemaVersion", "organizationUrl", "project", "repository", "authenticationScheme", "features", "entraClientId", "entraTenantId"
    };

    public static AzureDevOpsConfiguration Load(string repositoryRoot, string? configPath = null, bool allowCustomHost = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var root = Path.GetFullPath(repositoryRoot);
        var path = configPath is null
            ? Path.Combine(root, ".brownpants", "azure-devops.json")
            : Path.GetFullPath(Path.IsPathRooted(configPath) ? configPath : Path.Combine(root, configPath));

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidDataException("Azure DevOps configuration could not be read as valid JSON.", exception);
        }

        using (document)
        {
            var json = document.RootElement;
            if (json.ValueKind != JsonValueKind.Object) throw Invalid("The configuration root must be an object.");
            foreach (var property in json.EnumerateObject())
                if (!AllowedProperties.Contains(property.Name)) throw Invalid($"Unsupported configuration property '{property.Name}'.");

            if (!json.TryGetProperty("schemaVersion", out var version) || !version.TryGetInt32(out var schemaVersion) || schemaVersion != 1)
                throw Invalid("schemaVersion must be 1.");
            var organizationText = RequiredString(json, "organizationUrl");
            if (!Uri.TryCreate(organizationText, UriKind.Absolute, out var organization)
                || organization.Scheme != Uri.UriSchemeHttps
                || !string.IsNullOrEmpty(organization.UserInfo)
                || !string.IsNullOrEmpty(organization.Query)
                || !string.IsNullOrEmpty(organization.Fragment))
                throw Invalid("organizationUrl must be an HTTPS URL without credentials, query, or fragment.");

            var host = organization.IdnHost;
            var servicesHost = string.Equals(host, "dev.azure.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase);
            if (!servicesHost && !allowCustomHost)
                throw Invalid("Custom Azure DevOps hosts require explicit --allow-custom-host.");

            var project = RequiredString(json, "project");
            var repository = RequiredString(json, "repository");
            var authenticationScheme = RequiredString(json, "authenticationScheme").ToLowerInvariant();
            if (authenticationScheme is not ("bearer" or "pat" or "entra-browser"))
                throw Invalid("authenticationScheme must be 'bearer', 'pat', or 'entra-browser'.");
            var entraClientId = OptionalString(json, "entraClientId");
            var entraTenantId = OptionalString(json, "entraTenantId");
            if (authenticationScheme == "entra-browser")
            {
                if (!Guid.TryParse(entraClientId, out _)) throw Invalid("entraClientId must be a public-client application ID in GUID format.");
                if (string.IsNullOrWhiteSpace(entraTenantId) || entraTenantId.Contains('/') || entraTenantId.Contains('@'))
                    throw Invalid("entraTenantId must be an explicit tenant ID or tenant domain.");
                if (!servicesHost) throw Invalid("Microsoft Entra browser authentication is supported only for Azure DevOps Services hosts.");
            }

            var mappings = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            if (!json.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Object)
                throw Invalid("features must be an object mapping feature paths to work-item ID arrays.");
            foreach (var feature in features.EnumerateObject())
            {
                var featurePath = NormalizeFeaturePath(feature.Name);
                if (mappings.ContainsKey(featurePath)) throw Invalid($"Duplicate feature mapping '{featurePath}'.");
                if (feature.Value.ValueKind != JsonValueKind.Array) throw Invalid($"Work-item mapping for '{featurePath}' must be an array.");
                var ids = new List<string>();
                var unique = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in feature.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                        throw Invalid($"Work-item IDs for '{featurePath}' must be non-empty strings.");
                    var id = item.GetString()!.Trim();
                    if (!unique.Add(id)) throw Invalid($"Duplicate work-item ID in mapping '{featurePath}'.");
                    ids.Add(id);
                }
                mappings.Add(featurePath, ids.AsReadOnly());
            }

            return new AzureDevOpsConfiguration(new Uri(organization.AbsoluteUri.TrimEnd('/') + "/"), project, repository,
                authenticationScheme, mappings, entraClientId, entraTenantId);
        }
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
            throw Invalid($"{propertyName} must be a non-empty string.");
        return value.GetString()!.Trim();
    }

    private static string? OptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)) return null;
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw Invalid($"{propertyName} must be a non-empty string when supplied.");
        return value.GetString()!.Trim();
    }

    private static string NormalizeFeaturePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\\') || value.StartsWith('/') || Path.IsPathRooted(value))
            throw Invalid("Feature mapping paths must be repository-relative paths using '/' separators.");
        var segments = value.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw Invalid("Feature mapping paths must be normalized repository-relative paths.");
        return string.Join('/', segments);
    }

    private static InvalidDataException Invalid(string message) => new(message);
}
