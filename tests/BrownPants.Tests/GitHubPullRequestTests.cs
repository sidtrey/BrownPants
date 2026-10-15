using System.Net;
using System.Text;
using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class GitHubPullRequestTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-github-pr", Guid.NewGuid().ToString("N"));
    private const string Feature = "specs/feature-a";
    private const string Repository = "sidtrey/BrownPants";
    private const string Source = "feature/work";
    private const string Target = "main";

    [Fact]
    public void Create_UsesOneDraftPostWithExactReviewedContentAndNoOtherFields()
    {
        WritePreview();
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.Created,
            """{"number":42,"draft":true,"html_url":"https://github.com/sidtrey/BrownPants/pull/42"}"""));
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = GitHubPullRequestCommand.Run(CreateArgs(), output, error, () => "secret-token-for-test", () => handler);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.Contains("#42", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("https://github.com/sidtrey/BrownPants/pull/42", output.ToString(), StringComparison.Ordinal);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.github.com/repos/sidtrey/BrownPants/pulls", request.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer secret-token-for-test", request.Authorization);
        Assert.Equal("2026-03-10", request.ApiVersion);
        Assert.Equal("application/vnd.github+json", request.Accept);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(5, body.RootElement.EnumerateObject().Count());
        Assert.Equal("Preview Sample", body.RootElement.GetProperty("title").GetString());
        Assert.Contains("QA knowledge transfer", body.RootElement.GetProperty("body").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Source, body.RootElement.GetProperty("head").GetString());
        Assert.Equal(Target, body.RootElement.GetProperty("base").GetString());
        Assert.True(body.RootElement.GetProperty("draft").GetBoolean());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Create_RequiresBothOnlineAndConfirmationBeforeCredentialOrNetworkAccess(bool online, bool confirm)
    {
        WritePreview();
        var factoryCalls = 0;
        string? Token() => throw new Xunit.Sdk.XunitException("Token provider must not run before both gates.");
        HttpMessageHandler Handler() { factoryCalls++; throw new Xunit.Sdk.XunitException("HTTP handler must not be created before both gates."); }
        var args = CreateArgs(online, confirm);

        var exitCode = GitHubPullRequestCommand.Run(args, TextWriter.Null, new StringWriter(), Token, Handler);

        Assert.Equal(2, exitCode);
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public void Create_RejectsTamperedPreviewBeforeCredentialOrNetworkAccess()
    {
        WritePreview();
        var path = Path.Combine(_root, "artifacts", "preview.md");
        File.WriteAllText(path, File.ReadAllText(path).Replace("QA knowledge transfer", "changed after review", StringComparison.Ordinal));
        var handlerCalls = 0;
        HttpMessageHandler Handler() { handlerCalls++; return new RecordingHandler(_ => JsonResponse(HttpStatusCode.Created, "{}")); }
        string? Token() => throw new Xunit.Sdk.XunitException("Token provider must not run for a tampered preview.");

        var exitCode = GitHubPullRequestCommand.Run(CreateArgs(), TextWriter.Null, new StringWriter(), Token, Handler);

        Assert.Equal(1, exitCode);
        Assert.Equal(0, handlerCalls);
    }

    [Fact]
    public void Create_RejectsPreviewFileSymbolicLinkWhenSupported()
    {
        WritePreview();
        var outsidePath = Path.Combine(Path.GetTempPath(), "brownpants-github-preview-" + Guid.NewGuid().ToString("N") + ".md");
        File.Copy(Path.Combine(_root, "artifacts", "preview.md"), outsidePath);
        File.Delete(Path.Combine(_root, "artifacts", "preview.md"));
        try
        {
            File.CreateSymbolicLink(Path.Combine(_root, "artifacts", "preview.md"), outsidePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException or ArgumentException)
        {
            File.Delete(outsidePath);
            return;
        }

        try
        {
            var handlerCalls = 0;
            HttpMessageHandler Handler() { handlerCalls++; return new RecordingHandler(_ => JsonResponse(HttpStatusCode.Created, "{}")); }
            string? Token() => throw new Xunit.Sdk.XunitException("Token provider must not run for a linked preview file.");
            var error = new StringWriter();

            var exitCode = GitHubPullRequestCommand.Run(CreateArgs(), TextWriter.Null, error, Token, Handler);

            Assert.Equal(1, exitCode);
            Assert.Equal(0, handlerCalls);
            Assert.Contains("symbolic link", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(outsidePath);
        }
    }

    [Theory]
    [InlineData("--repository", "other/repo")]
    [InlineData("--repository", "../outside")]
    [InlineData("--source", "feature/other")]
    [InlineData("--target", "release")]
    public void Create_RejectsRepositoryOrBranchMismatchWithoutRequest(string option, string replacement)
    {
        WritePreview();
        var handlerCalls = 0;
        HttpMessageHandler Handler() { handlerCalls++; return new RecordingHandler(_ => JsonResponse(HttpStatusCode.Created, "{}")); }
        string? Token() => "test-token";

        var args = CreateArgs();
        var optionIndex = Array.IndexOf(args, option);
        args[optionIndex + 1] = replacement;
        var exitCode = GitHubPullRequestCommand.Run(args, TextWriter.Null, new StringWriter(), Token, Handler);

        Assert.Equal(1, exitCode);
        Assert.Equal(0, handlerCalls);
    }

    [Fact]
    public void Create_RejectsNonDraftOrUntrustedResponseUrl()
    {
        WritePreview();
        foreach (var response in new[]
        {
            """{"number":42,"draft":false,"html_url":"https://github.com/sidtrey/BrownPants/pull/42"}""",
            """{"number":42,"draft":true,"html_url":"https://evil.example/pr/42"}""",
            """{"number":42,"draft":true,"html_url":"https://github.com/other/repo/pull/42"}""",
            "not-json"
        })
        {
            var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.Created, response));
            var error = new StringWriter();
            var exitCode = GitHubPullRequestCommand.Run(CreateArgs(), TextWriter.Null, error, () => "test-token", () => handler);
            Assert.Equal(1, exitCode);
            Assert.Single(handler.Requests);
            Assert.Contains("inspect GitHub", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Create_DoesNotRetryAmbiguousTransportFailureOrLeakToken()
    {
        WritePreview();
        var handler = new RecordingHandler(_ => throw new HttpRequestException("transport failure"));
        var error = new StringWriter();

        var exitCode = GitHubPullRequestCommand.Run(CreateArgs(), TextWriter.Null, error, () => "sensitive-test-token", () => handler);

        Assert.Equal(1, exitCode);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain("sensitive-test-token", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("inspect GitHub", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_RejectsRedirectWithoutFollowingIt()
    {
        WritePreview();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TemporaryRedirect)
        {
            Headers = { Location = new Uri("https://evil.example/redirect") }
        });
        var error = new StringWriter();

        var exitCode = GitHubPullRequestCommand.Run(CreateArgs(), TextWriter.Null, error, () => "test-token", () => handler);

        Assert.Equal(1, exitCode);
        Assert.Single(handler.Requests);
        Assert.Contains("redirected", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_MissingTokenDoesNotCreateAnHttpHandler()
    {
        WritePreview();
        var handlerCalls = 0;
        HttpMessageHandler Handler() { handlerCalls++; return new RecordingHandler(_ => JsonResponse(HttpStatusCode.Created, "{}")); }
        var error = new StringWriter();

        var exitCode = GitHubPullRequestCommand.Run(CreateArgs(), TextWriter.Null, error, () => null, Handler);

        Assert.Equal(1, exitCode);
        Assert.Equal(0, handlerCalls);
        Assert.Contains("BROWNPANTS_GITHUB_TOKEN", error.ToString(), StringComparison.Ordinal);
    }

    private string[] CreateArgs(bool online = true, bool confirm = true) =>
    [
        "create", _root, "--provider", "github", "--preview", "artifacts/preview.md", "--repository", Repository,
        "--source", Source, "--target", Target,
        .. (confirm ? ["--confirm"] : Array.Empty<string>()),
        .. (online ? ["--online"] : Array.Empty<string>())
    ];

    private void WritePreview()
    {
        Directory.CreateDirectory(Path.Combine(_root, "artifacts"));
        var featureDirectory = Path.Combine(_root, Feature.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(featureDirectory);
        File.WriteAllText(Path.Combine(featureDirectory, "evidence.txt"), "verified source evidence");
        var spec = """
            # Feature Specification: Preview Sample

            ## User Scenarios & Testing

            1. **AC-001** — Given a service, when called, then it returns a result.

            ## Success Criteria

            - **SC-001**: All source evidence is deterministic.
            """;
        File.WriteAllText(Path.Combine(featureDirectory, "spec.md"), spec);
        var digest = PullRequestPreviewAnalyzer.ComputeCriteriaDigest(spec);
        File.WriteAllText(Path.Combine(featureDirectory, "acceptance-criteria.sha256"), digest + "  spec.md\n");
        File.WriteAllText(Path.Combine(featureDirectory, "tasks.md"), "# Tasks\n\n- [x] T001 Verify result\n");
        File.WriteAllText(Path.Combine(featureDirectory, "acceptance-evidence.json"), """
            {"schemaVersion":1,"tasks":[{"taskId":"T001","criterionIds":["AC-001","SC-001"],"verificationCommand":"dotnet test","evidencePaths":["specs/feature-a/evidence.txt"],"result":"passed"}]}
            """);
        var preview = PullRequestPreviewAnalyzer.Analyze(_root, Feature, Source, Target, Repository);
        Assert.True(preview.IsComplete);
        File.WriteAllText(Path.Combine(_root, "artifacts", "preview.md"), PullRequestPreviewRenderer.Render(preview));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri? RequestUri, string? Authorization, string? ApiVersion, string? Accept, string? Body);

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri, request.Headers.Authorization?.ToString(),
                request.Headers.TryGetValues("X-GitHub-Api-Version", out var version) ? version.Single() : null,
                request.Headers.Accept.SingleOrDefault()?.MediaType, body));
            return responseFactory(request);
        }
    }
}
