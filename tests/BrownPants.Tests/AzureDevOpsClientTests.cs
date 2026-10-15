using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class AzureDevOpsClientTests
{
    [Fact]
    public async Task GetWorkItem_UsesVersionedConfiguredEndpointAndParsesEvidence()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, """
            {
              "id": 42,
              "url": "https://dev.azure.com/example/project/_apis/wit/workItems/42",
              "fields": {
                "System.Title": "Update billing flow",
                "System.WorkItemType": "User Story",
                "System.State": "Active",
                "System.IterationPath": "Project\\Sprint 4"
              },
              "relations": [
                { "rel": "ArtifactLink", "url": "vstfs:///Git/PullRequestId/project%2Frepo%2F7", "attributes": { "name": "Pull Request" } },
                { "rel": "Hyperlink", "url": "https://dev.azure.com/example/project/_build/results?buildId=88", "attributes": { "name": "Build 88" } }
              ]
            }
            """));
        var client = CreateClient(handler, "private-token-value");

        var item = await client.GetWorkItemAsync("42", TestContext.Current.CancellationToken);

        Assert.Equal("Update billing flow", item.Title);
        Assert.Equal("User Story", item.Type);
        Assert.Equal("Active", item.State);
        Assert.Equal("Project\\Sprint 4", item.IterationPath);
        Assert.Equal(2, item.Relations.Count);
        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Contains("/project/_apis/wit/workitems/42?", handler.Request.RequestUri!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api-version=7.1", handler.Request.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("private-token-value", handler.Request.Headers.Authorization.Parameter);
        Assert.DoesNotContain("private-token-value", handler.Request.RequestUri.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "authorization")]
    [InlineData(HttpStatusCode.Forbidden, "authorization")]
    [InlineData((HttpStatusCode)429, "throttled")]
    [InlineData(HttpStatusCode.InternalServerError, "service")]
    public async Task GetWorkItem_ReturnsBoundedSanitizedErrors(HttpStatusCode status, string expected)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent("body-with-private-token-value")
        });
        var client = CreateClient(handler, "private-token-value");

        var exception = await Assert.ThrowsAsync<AzureDevOpsApiException>(() => client.GetWorkItemAsync("42", TestContext.Current.CancellationToken));

        Assert.Equal(expected, exception.Code);
        Assert.DoesNotContain("private-token-value", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("body-with-private-token-value", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWorkItem_RejectsInvalidIdentifiersBeforeSendingRequests()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, "{}"));
        var client = CreateClient(handler, "token");

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetWorkItemAsync("../42", TestContext.Current.CancellationToken));

        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task GetWorkItem_UsesPatHeaderOnlyWhenConfigured()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, """{"id":42,"fields":{}}"""));
        var client = CreateClient(handler, "pat-secret", "pat");

        await client.GetWorkItemAsync("42", TestContext.Current.CancellationToken);

        Assert.Equal("Basic", handler.Request!.Headers.Authorization!.Scheme);
        Assert.NotEqual("pat-secret", handler.Request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetWorkItem_ReportsContinuationInsteadOfSilentlyTruncating()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = Json(HttpStatusCode.OK, """{"id":42,"fields":{}}""");
            response.Headers.TryAddWithoutValidation("x-ms-continuationtoken", "next-page");
            return response;
        });
        using var client = CreateClient(handler, "token");

        var exception = await Assert.ThrowsAsync<AzureDevOpsApiException>(() => client.GetWorkItemAsync("42", TestContext.Current.CancellationToken));

        Assert.Equal("pagination", exception.Code);
        Assert.Contains("incomplete", exception.Message, StringComparison.Ordinal);
    }

    private static AzureDevOpsClient CreateClient(RecordingHandler handler, string token, string scheme = "bearer")
    {
        var config = new AzureDevOpsConfiguration(new Uri("https://dev.azure.com/example/"), "project", "repo", scheme,
            new Dictionary<string, IReadOnlyList<string>>());
        return new AzureDevOpsClient(config, handler, token);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string content) => new(status)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
