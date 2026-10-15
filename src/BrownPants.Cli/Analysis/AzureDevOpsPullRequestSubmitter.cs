using System.Net;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BrownPants.Cli.Output;

namespace BrownPants.Cli.Analysis;

public sealed record AzureDevOpsPullRequestResult(int Id, string Title, string Status, bool IsDraft, string Url);

public sealed class AzureDevOpsPullRequestSubmitter : IDisposable
{
    private const string ApiVersion = "7.1";
    private static readonly Regex WorkItemTransition = new(@"\b(?:fix(?:es|ed)?|close[sd]?|resolve[sd]?)\s+#\s*\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly AzureDevOpsConfiguration _configuration;
    private readonly HttpClient _http;

    public AzureDevOpsPullRequestSubmitter(AzureDevOpsConfiguration configuration, HttpMessageHandler handler, string token)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _http = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(30) };
        var pat = configuration.AuthenticationScheme == "pat";
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(pat ? "Basic" : "Bearer",
            pat ? Convert.ToBase64String(Encoding.UTF8.GetBytes($":{token}")) : token);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<AzureDevOpsPullRequestResult> CreateDraftAsync(string savedPreview, CancellationToken cancellationToken = default)
    {
        var preview = PullRequestPreviewRenderer.Read(savedPreview);
        var metadata = preview.Metadata;
        if (!preview.IntegrityValid) throw new InvalidDataException("The saved PR preview was edited after it was generated.");
        if (metadata.Repository != _configuration.Repository) throw new InvalidDataException("The saved PR preview targets a different configured repository.");
        if (string.IsNullOrWhiteSpace(metadata.Title) || string.IsNullOrWhiteSpace(metadata.Description)
            || string.IsNullOrWhiteSpace(metadata.SourceBranch) || string.IsNullOrWhiteSpace(metadata.TargetBranch))
            throw new InvalidDataException("The saved PR preview is missing required PR fields.");
        PullRequestPreviewAnalyzer.ValidateBranch(metadata.SourceBranch, nameof(metadata.SourceBranch));
        PullRequestPreviewAnalyzer.ValidateBranch(metadata.TargetBranch, nameof(metadata.TargetBranch));
        if (WorkItemTransition.IsMatch(metadata.Title) || WorkItemTransition.IsMatch(metadata.Description))
            throw new InvalidDataException("The saved PR preview contains an Azure Boards work-item transition phrase.");
        if (metadata.WorkItemIds is null || metadata.WorkItemIds.Any(id => !int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 1))
            throw new InvalidDataException("The saved PR preview contains an invalid work-item ID.");

        var endpoint = new Uri(_configuration.OrganizationUrl,
            $"{Uri.EscapeDataString(_configuration.Project)}/_apis/git/repositories/{Uri.EscapeDataString(_configuration.Repository)}/pullrequests?api-version={ApiVersion}");
        var payload = JsonSerializer.Serialize(new
        {
            sourceRefName = "refs/heads/" + metadata.SourceBranch,
            targetRefName = "refs/heads/" + metadata.TargetBranch,
            title = metadata.Title,
            description = metadata.Description,
            isDraft = true,
            workItemRefs = metadata.WorkItemIds.Select(id => new { id }).ToArray()
        });
        HttpResponseMessage response;
        try
        {
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            response = await _http.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AzureDevOpsApiException("timeout", "Azure DevOps draft PR creation timed out; check Azure DevOps before retrying.");
        }
        catch (HttpRequestException)
        {
            throw new AzureDevOpsApiException("network", "Azure DevOps could not be reached; check Azure DevOps before retrying.");
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new AzureDevOpsApiException("authorization", "Azure DevOps denied draft PR creation; check the user's code write permission.");
            if (!response.IsSuccessStatusCode)
                throw new AzureDevOpsApiException("service", $"Azure DevOps returned HTTP {(int)response.StatusCode} while creating the draft PR.");
            try
            {
                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new AzureDevOpsApiException("invalid-response", "Azure DevOps returned an unsupported PR response; inspect Azure DevOps before retrying.");
                if (!root.TryGetProperty("pullRequestId", out var idNode) || !idNode.TryGetInt32(out var id) || id < 1
                    || !root.TryGetProperty("isDraft", out var draftNode) || draftNode.ValueKind != JsonValueKind.True)
                    throw new AzureDevOpsApiException("invalid-response", "Azure DevOps did not confirm a draft PR; inspect Azure DevOps before retrying.");
                var title = root.TryGetProperty("title", out var titleNode) && titleNode.ValueKind == JsonValueKind.String ? titleNode.GetString()! : metadata.Title;
                var status = root.TryGetProperty("status", out var statusNode) && statusNode.ValueKind == JsonValueKind.String ? statusNode.GetString()! : "unknown";
                var url = new Uri(_configuration.OrganizationUrl,
                    $"{Uri.EscapeDataString(_configuration.Project)}/_git/{Uri.EscapeDataString(_configuration.Repository)}/pullrequest/{id}").AbsoluteUri;
                return new AzureDevOpsPullRequestResult(id, title, status, true, url);
            }
            catch (JsonException)
            {
                throw new AzureDevOpsApiException("invalid-response", "Azure DevOps returned an unsupported PR response; inspect Azure DevOps before retrying.");
            }
        }
    }

    public void Dispose() => _http.Dispose();
}
