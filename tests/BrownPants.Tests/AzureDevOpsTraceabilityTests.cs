using System.Net;
using System.Text;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class AzureDevOpsTraceabilityTests
{
    [Fact]
    public async Task Analyze_ReturnsMappedWorkItemsAndSupportedDeliveryLinksInStableOrder()
    {
        var config = Configuration(new Dictionary<string, IReadOnlyList<string>>
        {
            ["specs/feature-a"] = new[] { "43", "42" }
        });
        var handler = new FixtureHandler(request =>
        {
            var id = request.RequestUri!.AbsolutePath.EndsWith("/42", StringComparison.Ordinal) ? "42" : "43";
            return Json(HttpStatusCode.OK, WorkItemJson(id));
        });
        using var client = new AzureDevOpsClient(config, handler, "fixture-token");

        var report = await AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(config, "specs/feature-a", client);

        Assert.True(report.IsComplete);
        Assert.Equal(new[] { "42", "43" }, report.WorkItems.Select(item => item.Id));
        Assert.Contains(report.DeliveryReferences, item => item.ArtifactType == "PullRequest" && item.Identifier == "7");
        Assert.Contains(report.DeliveryReferences, item => item.ArtifactType == "Build" && item.Identifier == "88"
            && item.TargetUrl == "https://dev.azure.com/example/project/_build/results?buildId=88");
        Assert.Contains(report.DeliveryReferences, item => item.ArtifactType == "Release" && item.Identifier == "12");
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Analyze_MarksMissingMappingsAndPartialFailuresIncomplete()
    {
        var noMapping = Configuration(new Dictionary<string, IReadOnlyList<string>>());
        using var emptyClient = new AzureDevOpsClient(noMapping, new FixtureHandler(_ => Json(HttpStatusCode.OK, "{}")), "token");
        var missing = await AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(noMapping, "specs/none", emptyClient);
        Assert.False(missing.IsComplete);
        Assert.Contains(missing.Diagnostics, value => value.Contains("No Azure DevOps work items are mapped", StringComparison.Ordinal));

        var mapped = Configuration(new Dictionary<string, IReadOnlyList<string>> { ["specs/f"] = new[] { "42", "43" } });
        var handler = new FixtureHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/42", StringComparison.Ordinal)
            ? Json(HttpStatusCode.OK, WorkItemJson("42")) : new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var client = new AzureDevOpsClient(mapped, handler, "token");
        var partial = await AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(mapped, "specs/f", client);
        Assert.False(partial.IsComplete);
        Assert.Equal(new[] { "Observed", "Unavailable" }, partial.WorkItems.Select(item => item.Status));
        Assert.Contains(partial.Diagnostics, value => value.Contains("43", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Analyze_RejectsInvalidFeaturePathsAndDoesNotTrustCrossHostResponseLinks()
    {
        var config = Configuration(new Dictionary<string, IReadOnlyList<string>> { ["specs/f"] = new[] { "42" } });
        var handler = new FixtureHandler(_ => Json(HttpStatusCode.OK, WorkItemJson("42", includeUntrustedLink: true)));
        using var client = new AzureDevOpsClient(config, handler, "token");

        await Assert.ThrowsAsync<ArgumentException>(() => AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(config, "../outside", client));
        var report = await AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(config, "specs/f", client);

        Assert.DoesNotContain(report.DeliveryReferences, reference => reference.TargetUrl?.Contains("evil.example", StringComparison.Ordinal) == true);
        Assert.Contains(report.Diagnostics, value => value.Contains("validated clickable URL", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Renderers_AreDeterministicAndEscapeServerProvidedText()
    {
        var config = Configuration(new Dictionary<string, IReadOnlyList<string>> { ["specs/f"] = new[] { "42" } });
        var handler = new FixtureHandler(_ => Json(HttpStatusCode.OK, WorkItemJson("42", title: "Fix | billing <script>")));
        using var client = new AzureDevOpsClient(config, handler, "token");
        var report = await AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(config, "specs/f", client);

        var markdown = AzureDevOpsReportRenderer.RenderMarkdown(report);
        Assert.Equal(markdown, AzureDevOpsReportRenderer.RenderMarkdown(report));
        Assert.Contains("Fix \\| billing &lt;script&gt;", markdown, StringComparison.Ordinal);
        Assert.Equal(AzureDevOpsReportRenderer.RenderJson(report), AzureDevOpsReportRenderer.RenderJson(report));
        Assert.Contains("\"schemaVersion\": 1", AzureDevOpsReportRenderer.RenderJson(report), StringComparison.Ordinal);
    }

    private static AzureDevOpsConfiguration Configuration(IReadOnlyDictionary<string, IReadOnlyList<string>> mappings) =>
        new(new Uri("https://dev.azure.com/example/"), "project", "repo", "bearer", mappings);

    private static string WorkItemJson(string id, bool includeUntrustedLink = false, string title = "Story") => $$"""
        {
          "id": {{id}},
          "fields": { "System.Title": "{{title}}", "System.WorkItemType": "User Story", "System.State": "Active", "System.IterationPath": "Project\\Sprint 4" },
          "relations": [
            { "rel": "ArtifactLink", "url": "vstfs:///Git/PullRequestId/project%2Frepo%2F7", "attributes": { "name": "Pull Request" } },
            { "rel": "Hyperlink", "url": "https://dev.azure.com/example/project/_build/results?buildId=88", "attributes": { "name": "Build 88" } },
            { "rel": "Hyperlink", "url": "https://dev.azure.com/example/project/_releaseProgress?releaseId=12", "attributes": { "name": "Release 12" } }
            {{(includeUntrustedLink ? ", { \"rel\": \"Hyperlink\", \"url\": \"https://evil.example/build/99\", \"attributes\": { \"name\": \"Build 99\" } }" : "")}}
          ]
        }
        """;

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class FixtureHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }
}
