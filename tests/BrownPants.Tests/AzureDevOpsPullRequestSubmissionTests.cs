using System.Net;
using System.Text;
using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class AzureDevOpsPullRequestSubmissionTests
{
    [Fact]
    public async Task CreateDraftAsync_SendsOneDraftCreateRequestWithExplicitWorkItems()
    {
        var handler = new RecordingHandler();
        using var submitter = new AzureDevOpsPullRequestSubmitter(Configuration(), handler, "test-token");

        var result = await submitter.CreateDraftAsync(SavedPreview());

        Assert.Equal(1, handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://dev.azure.com/example/Project%20One/_apis/git/repositories/repo-id/pullrequests?api-version=7.1", handler.RequestUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        using var request = JsonDocument.Parse(handler.RequestBody!);
        var root = request.RootElement;
        Assert.True(root.GetProperty("isDraft").GetBoolean());
        Assert.Equal("refs/heads/feature/work", root.GetProperty("sourceRefName").GetString());
        Assert.Equal("refs/heads/main", root.GetProperty("targetRefName").GetString());
        Assert.Equal("42", root.GetProperty("workItemRefs")[0].GetProperty("id").GetString());
        Assert.False(root.TryGetProperty("completionOptions", out _));
        Assert.False(root.TryGetProperty("reviewers", out _));
        Assert.Equal(123, result.Id);
        Assert.True(result.IsDraft);
        Assert.Contains("pullrequest/123", result.Url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateDraftAsync_RejectsEditedPreviewWithoutNetworkRequest()
    {
        var handler = new RecordingHandler();
        using var submitter = new AzureDevOpsPullRequestSubmitter(Configuration(), handler, "test-token");

        await Assert.ThrowsAsync<InvalidDataException>(() => submitter.CreateDraftAsync(SavedPreview().Replace("Task evidence", "Edited evidence", StringComparison.Ordinal)));

        Assert.Equal(0, handler.Requests);
    }

    private static AzureDevOpsConfiguration Configuration() => new(
        new Uri("https://dev.azure.com/example/"), "Project One", "repo-id", "entra-browser",
        new Dictionary<string, IReadOnlyList<string>>(), Guid.NewGuid().ToString(), "tenant-id");

    private static string SavedPreview() => PullRequestPreviewRenderer.Render(new PullRequestPreview(
        1, "specs/feature-a", "Trace feature work", "BrownPants feature specs/feature-a. Acceptance criteria SHA-256: `digest`.",
        "repo-id", "feature/work", "main", ["42"], new string('a', 64), true, false, [], [], [], []));

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }
        public string? AuthorizationScheme { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Method = request.Method;
            RequestUri = request.RequestUri!.AbsoluteUri;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"pullRequestId":123,"title":"Trace feature work","status":"active","isDraft":true}""", Encoding.UTF8, "application/json")
            };
        }
    }
}
