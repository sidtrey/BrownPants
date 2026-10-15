using System.Net;
using System.Text;
using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class MultiProviderPullRequestTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-provider-pr", Guid.NewGuid().ToString("N"));
    private const string Source = "feature/work";
    private const string Target = "main";

    [Theory]
    [InlineData("gitlab", "group/project", "https://gitlab.com/group/project/-/merge_requests/7", "{\"draft\":true,\"state\":\"opened\",\"web_url\":\"https://gitlab.com/group/project/-/merge_requests/7\"}")]
    [InlineData("bitbucket", "workspace/repo", "https://bitbucket.org/workspace/repo/pull-requests/7", "{\"draft\":true,\"state\":\"OPEN\",\"links\":{\"html\":{\"href\":\"https://bitbucket.org/workspace/repo/pull-requests/7\"}}}")]
    public void Create_SendsOneDraftRequestToFixedProvider(string provider, string repository, string url, string response)
    {
        WritePreview(repository);
        var handler = new RecordingHandler(HttpStatusCode.Created, response);
        var output = new StringWriter(); var error = new StringWriter();
        var exit = MultiProviderPullRequestCommand.Run(Args(provider, repository), output, error, () => "test-secret", () => handler);
        Assert.Equal(0, exit); Assert.Empty(error.ToString()); Assert.Contains(url, output.ToString(), StringComparison.Ordinal);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(provider == "gitlab" ? "https://gitlab.com/api/v4/projects/group%2Fproject/merge_requests" :
            "https://api.bitbucket.org/2.0/repositories/workspace/repo/pullrequests", request.Uri!.AbsoluteUri);
        Assert.Contains("test-secret", request.Authorization, StringComparison.Ordinal);
        using var body = JsonDocument.Parse(request.Body!);
        if (provider == "gitlab")
        {
            Assert.Equal("Draft: Feature A", body.RootElement.GetProperty("title").GetString());
            Assert.Equal(Source, body.RootElement.GetProperty("source_branch").GetString());
            Assert.Equal(Target, body.RootElement.GetProperty("target_branch").GetString());
            Assert.Contains("QA knowledge transfer", body.RootElement.GetProperty("description").GetString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Equal("Feature A", body.RootElement.GetProperty("title").GetString());
            Assert.Equal(Source, body.RootElement.GetProperty("source").GetProperty("branch").GetProperty("name").GetString());
            Assert.Equal(Target, body.RootElement.GetProperty("destination").GetProperty("branch").GetProperty("name").GetString());
            Assert.True(body.RootElement.GetProperty("draft").GetBoolean());
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Create_RequiresBothGatesBeforeCredentialsOrRequests(bool online, bool confirm)
    {
        var handlerCalls = 0;
        var exit = MultiProviderPullRequestCommand.Run(Args("gitlab", "group/project", online, confirm), TextWriter.Null, new StringWriter(),
            () => throw new Xunit.Sdk.XunitException("Credential read before gates."),
            () => { handlerCalls++; throw new Xunit.Sdk.XunitException("Request created before gates."); });
        Assert.Equal(2, exit); Assert.Equal(0, handlerCalls);
    }

    [Fact]
    public void Create_DoesNotRetryAmbiguousNetworkFailureOrPrintCredential()
    {
        WritePreview("workspace/repo");
        var handler = new RecordingHandler(_ => throw new HttpRequestException("connection lost"));
        var error = new StringWriter();
        var exit = MultiProviderPullRequestCommand.Run(Args("bitbucket", "workspace/repo"), TextWriter.Null, error,
            () => "sensitive-token", () => handler);
        Assert.Equal(1, exit); Assert.Single(handler.Requests);
        Assert.DoesNotContain("sensitive-token", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("retry", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_RejectsNonDraftResponse()
    {
        WritePreview("group/project");
        var handler = new RecordingHandler(HttpStatusCode.Created, "{\"draft\":false,\"state\":\"opened\",\"web_url\":\"https://gitlab.com/group/project/-/merge_requests/7\"}");
        var exit = MultiProviderPullRequestCommand.Run(Args("gitlab", "group/project"), TextWriter.Null, new StringWriter(), () => "token", () => handler);
        Assert.Equal(1, exit); Assert.Single(handler.Requests);
    }

    private string[] Args(string provider, string repository, bool online = true, bool confirm = true) =>
    ["create", _root, "--provider", provider, "--preview", "artifacts/preview.md", "--repository", repository,
     "--source", Source, "--target", Target, .. (confirm ? new[] { "--confirm" } : Array.Empty<string>()), .. (online ? new[] { "--online" } : Array.Empty<string>())];

    private void WritePreview(string repository)
    {
        var feature = Path.Combine(_root, "specs", "feature-a");
        Directory.CreateDirectory(feature); Directory.CreateDirectory(Path.Combine(_root, "artifacts"));
        var spec = "# Sample\n\n## User Scenarios & Testing\n\n1. **AC-001** — Given a user, when run, then output exists.\n\n## Success Criteria\n\n- **SC-001**: Output is stable.\n";
        File.WriteAllText(Path.Combine(feature, "spec.md"), spec);
        File.WriteAllText(Path.Combine(feature, "acceptance-criteria.sha256"), PullRequestPreviewAnalyzer.ComputeCriteriaDigest(spec) + "  spec.md\n");
        File.WriteAllText(Path.Combine(feature, "tasks.md"), "# Tasks\n\n- [x] T001 Check output\n");
        File.WriteAllText(Path.Combine(feature, "acceptance-evidence.json"), "{\"schemaVersion\":1,\"tasks\":[{\"taskId\":\"T001\",\"criterionIds\":[\"AC-001\",\"SC-001\"],\"verificationCommand\":\"dotnet test\",\"evidencePaths\":[],\"result\":\"passed\"}]}");
        var preview = PullRequestPreviewAnalyzer.Analyze(_root, "specs/feature-a", Source, Target, repository);
        preview = preview with { Title = "Feature A" };
        File.WriteAllText(Path.Combine(_root, "artifacts", "preview.md"), PullRequestPreviewRenderer.Render(preview));
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _response;
        public List<(HttpMethod Method, Uri? Uri, string? Authorization, string? Body)> Requests { get; } = [];
        public RecordingHandler(HttpStatusCode status, string json) : this(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") }) { }
        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) => _response = response;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri, request.Headers.Authorization?.ToString() ??
                (request.Headers.TryGetValues("PRIVATE-TOKEN", out var token) ? token.Single() : null),
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return _response(request);
        }
    }
}
