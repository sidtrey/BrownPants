using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BrownPants.Cli.Output;

namespace BrownPants.Cli.Analysis;

public sealed record GitHubPullRequestResult(int Number, bool IsDraft, string Url);

public sealed class GitHubPullRequestException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}

public sealed class GitHubPullRequestClient : IDisposable
{
    private const string ApiVersion = "2026-03-10";
    private const string ApiRoot = "https://api.github.com/";
    private static readonly Regex RepositoryPattern = new("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant);
    private readonly HttpClient _http;

    public GitHubPullRequestClient(string token, HttpMessageHandler? handler = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (handler is null) handler = new HttpClientHandler { AllowAutoRedirect = false };
        _http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(ApiRoot, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", ApiVersion);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("BrownPants/0.1.0");
    }

    public async Task<GitHubPullRequestResult> CreateDraftAsync(
        SavedPullRequestPreview savedPreview,
        string repository,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(savedPreview);
        var metadata = savedPreview.Metadata;
        if (!savedPreview.IntegrityValid)
            throw new GitHubPullRequestException("invalid-preview", "The saved PR preview was edited after it was generated.");
        if (!IsValidRepository(repository))
            throw new GitHubPullRequestException("invalid-repository", "GitHub repository must use owner/name format with valid path segments.");
        if (!string.Equals(metadata.Repository, repository, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(metadata.SourceBranch, sourceBranch, StringComparison.Ordinal)
            || !string.Equals(metadata.TargetBranch, targetBranch, StringComparison.Ordinal))
            throw new GitHubPullRequestException("preview-mismatch", "The explicit GitHub repository and branches must match the saved preview.");
        if (string.Equals(sourceBranch, targetBranch, StringComparison.Ordinal))
            throw new GitHubPullRequestException("invalid-branches", "Source and target branches must be different.");
        if (string.IsNullOrWhiteSpace(metadata.Title) || string.IsNullOrWhiteSpace(metadata.SourceBranch)
            || string.IsNullOrWhiteSpace(metadata.TargetBranch) || string.IsNullOrWhiteSpace(savedPreview.Body))
            throw new GitHubPullRequestException("invalid-preview", "The saved PR preview is missing required title, body, or branch data.");
        PullRequestPreviewAnalyzer.ValidateBranch(metadata.SourceBranch, nameof(metadata.SourceBranch));
        PullRequestPreviewAnalyzer.ValidateBranch(metadata.TargetBranch, nameof(metadata.TargetBranch));

        var segments = repository.Split('/');
        var endpoint = $"repos/{Uri.EscapeDataString(segments[0])}/{Uri.EscapeDataString(segments[1])}/pulls";
        var payload = JsonSerializer.Serialize(new
        {
            title = metadata.Title,
            body = savedPreview.Body,
            head = metadata.SourceBranch,
            @base = metadata.TargetBranch,
            draft = true
        });

        HttpResponseMessage response;
        try
        {
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            response = await _http.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            throw new GitHubPullRequestException("timeout", "GitHub draft PR creation timed out; inspect GitHub before retrying.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new GitHubPullRequestException("network", "GitHub could not confirm draft PR creation; inspect GitHub before retrying.", exception);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.MovedPermanently || (int)response.StatusCode is >= 300 and < 400)
                throw new GitHubPullRequestException("redirect", "GitHub redirected the draft PR request; inspect GitHub and verify the configured endpoint.");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new GitHubPullRequestException("authorization", "GitHub denied draft PR creation; check repository Pull requests write permission.");
            if (response.StatusCode != HttpStatusCode.Created)
                throw new GitHubPullRequestException("service", $"GitHub returned HTTP {(int)response.StatusCode} while creating the draft PR; inspect GitHub before retrying.");
            try
            {
                string responseBody;
                try
                {
                    responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                {
                    throw new GitHubPullRequestException("timeout", "GitHub returned an incomplete creation response; inspect GitHub before retrying.", exception);
                }
                catch (HttpRequestException exception)
                {
                    throw new GitHubPullRequestException("network", "GitHub returned an incomplete creation response; inspect GitHub before retrying.", exception);
                }
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("number", out var numberNode) || !numberNode.TryGetInt32(out var number) || number < 1
                    || !root.TryGetProperty("draft", out var draftNode) || draftNode.ValueKind != JsonValueKind.True
                    || !root.TryGetProperty("html_url", out var urlNode) || urlNode.ValueKind != JsonValueKind.String
                    || !Uri.TryCreate(urlNode.GetString(), UriKind.Absolute, out var url)
                    || url.Scheme != Uri.UriSchemeHttps || !string.Equals(url.IdnHost, "github.com", StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment)
                    || !string.Equals(url.AbsolutePath,
                        $"/" + string.Join('/', repository.Split('/').Select(Uri.EscapeDataString)) + $"/pull/{number}", StringComparison.OrdinalIgnoreCase))
                    throw new GitHubPullRequestException("invalid-response", "GitHub did not confirm a draft PR with a valid github.com URL; inspect GitHub before retrying.");
                return new GitHubPullRequestResult(number, true, url.AbsoluteUri);
            }
            catch (JsonException exception)
            {
                throw new GitHubPullRequestException("invalid-response", "GitHub returned an unsupported PR response; inspect GitHub before retrying.", exception);
            }
        }
    }

    internal static bool IsValidRepository(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository) || !RepositoryPattern.IsMatch(repository)) return false;
        var segments = repository.Split('/');
        return segments.Length == 2 && segments.All(segment => segment is not ("." or ".."));
    }

    public void Dispose() => _http.Dispose();
}
