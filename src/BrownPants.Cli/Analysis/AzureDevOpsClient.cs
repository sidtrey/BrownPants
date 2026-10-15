using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BrownPants.Cli.Analysis;

public sealed record AzureDevOpsRelation(string RelationType, string Url, string? Name);

public sealed record AzureDevOpsWorkItem(
    string Id,
    string Title,
    string Type,
    string State,
    string IterationPath,
    string SourceUrl,
    IReadOnlyList<AzureDevOpsRelation> Relations);

public sealed class AzureDevOpsApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class AzureDevOpsClient : IDisposable
{
    private const string ApiVersion = "7.1";
    private readonly AzureDevOpsConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AzureDevOpsClient(AzureDevOpsConfiguration configuration, HttpMessageHandler handler, string token)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _configuration = configuration;
        _httpClient = new HttpClient(handler, disposeHandler: true);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        var credential = configuration.AuthenticationScheme == "pat"
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes($":{token}"))
            : token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            configuration.AuthenticationScheme == "pat" ? "Basic" : "Bearer", credential);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<AzureDevOpsWorkItem> GetWorkItemAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var numericId) || numericId < 1)
            throw new ArgumentException("A positive numeric Azure DevOps work-item ID is required.", nameof(id));

        var project = Uri.EscapeDataString(_configuration.Project);
        var relative = $"{project}/_apis/wit/workitems/{numericId}?$expand=relations&api-version={ApiVersion}";
        var endpoint = new Uri(_configuration.OrganizationUrl, relative);
        if (!string.Equals(endpoint.Host, _configuration.OrganizationUrl.Host, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
            throw new InvalidOperationException("The Azure DevOps endpoint escaped the configured HTTPS host.");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AzureDevOpsApiException("timeout", "Azure DevOps request timed out.");
        }
        catch (HttpRequestException)
        {
            throw new AzureDevOpsApiException("network", "Azure DevOps could not be reached.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw CreateHttpException(response.StatusCode, response.Headers.RetryAfter?.Delta);
            if (response.Headers.TryGetValues("x-ms-continuationtoken", out var continuationTokens)
                && continuationTokens.Any(value => !string.IsNullOrWhiteSpace(value)))
                throw new AzureDevOpsApiException("pagination", "Azure DevOps indicated additional pages; this bounded report is incomplete.");

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 64 }, cancellationToken).ConfigureAwait(false);
                return ParseWorkItem(document.RootElement, numericId);
            }
            catch (JsonException)
            {
                throw new AzureDevOpsApiException("invalid-response", "Azure DevOps returned an unsupported work-item response.");
            }
        }
    }

    private AzureDevOpsWorkItem ParseWorkItem(JsonElement root, int requestedId)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new AzureDevOpsApiException("invalid-response", "Azure DevOps returned an unsupported work-item response.");
        var id = root.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var responseId)
            ? responseId.ToString(CultureInfo.InvariantCulture)
            : requestedId.ToString(CultureInfo.InvariantCulture);
        var fields = root.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Object
            ? fieldsElement : default;
        var relations = new List<AzureDevOpsRelation>();
        if (root.TryGetProperty("relations", out var relationArray) && relationArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var relation in relationArray.EnumerateArray())
            {
                if (relation.ValueKind != JsonValueKind.Object) continue;
                var rel = GetString(relation, "rel");
                var url = GetString(relation, "url");
                if (string.IsNullOrWhiteSpace(rel) || string.IsNullOrWhiteSpace(url)) continue;
                string? name = null;
                if (relation.TryGetProperty("attributes", out var attributes) && attributes.ValueKind == JsonValueKind.Object)
                    name = GetString(attributes, "name");
                relations.Add(new AzureDevOpsRelation(rel, url, name));
            }
        }

        var workItemUrl = new Uri(_configuration.OrganizationUrl,
            $"{Uri.EscapeDataString(_configuration.Project)}/_apis/wit/workItems/{id}").AbsoluteUri;
        return new AzureDevOpsWorkItem(id,
            GetString(fields, "System.Title") ?? "Unknown title",
            GetString(fields, "System.WorkItemType") ?? "Unknown type",
            GetString(fields, "System.State") ?? "Unknown state",
            GetString(fields, "System.IterationPath") ?? "Unknown iteration",
            workItemUrl,
            relations.AsReadOnly());
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    private static AzureDevOpsApiException CreateHttpException(HttpStatusCode status, TimeSpan? retryAfter)
    {
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return new AzureDevOpsApiException("authorization", "Azure DevOps denied access; check token validity and read permissions.");
        if ((int)status == 429)
        {
            var suffix = retryAfter is { } delay ? $" Retry after about {Math.Ceiling(delay.TotalSeconds).ToString(CultureInfo.InvariantCulture)} seconds." : string.Empty;
            return new AzureDevOpsApiException("throttled", "Azure DevOps throttled the request." + suffix);
        }
        if (status == HttpStatusCode.NotFound)
            return new AzureDevOpsApiException("not-found", "The mapped work item was not found or is not visible to this identity.");
        return new AzureDevOpsApiException("service", $"Azure DevOps returned HTTP {(int)status}.");
    }

    public void Dispose() => _httpClient.Dispose();
}
