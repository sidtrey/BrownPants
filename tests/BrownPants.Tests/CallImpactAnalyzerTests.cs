using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BrownPants.Tests;

public sealed class CallImpactAnalyzerTests
{
    [Fact]
    public void Analyze_ReportsDirectTransitiveAndTestPathCallers()
    {
        var semantic = CompileFixtureGraph();

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs" },
            new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs" }, semantic, 100);

        Assert.True(report.IsComplete);
        Assert.Equal(1, report.Summary.DirectCallerCount);
        Assert.Equal(1, report.Summary.TransitiveCallerCount);
        Assert.Equal(2, report.Summary.AffectedFileCount);
        Assert.Equal(1, report.Summary.TestCandidateCount);
        Assert.Equal(2, report.Summary.MaximumCallerDepth);
        Assert.Contains(report.Impacts, item => item.Path == "src/Service.cs" && item.Depth == 1
            && item.Target == item.ChangedMethod && !item.IsTestCandidate);
        Assert.Contains(report.Impacts, item => item.Path == "tests/CoreTests.cs" && item.Depth == 2
            && item.Target.Contains("Service.Run", StringComparison.Ordinal) && item.IsTestCandidate);
    }

    [Fact]
    public void SemanticAnalyzer_MapsOverloadsAndLeavesMetadataTargetLocationsEmpty()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace Demo; public class Core { public void Save(int value) { } public void Save(string value) { } }", path: "src/Core.cs", cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class Service { public void Run(Core core) { core.Save(1); core.Save(\"x\"); core.ToString(); } }", path: "src/Service.cs", cancellationToken: TestContext.Current.CancellationToken)
        };
        var compilation = CSharpCompilation.Create("OverloadFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semantic = CSharpSemanticAnalyzer.AnalyzeCompilations(new[] { ("fixture.csproj", (Compilation)compilation) }, 1, 100, ".");

        var overloadCalls = semantic.ResolvedCalls.Where(call => call.Expression.StartsWith("core.Save", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, overloadCalls.Length);
        Assert.All(overloadCalls, call =>
        {
            Assert.Equal("src/Core.cs", call.TargetPath);
            Assert.NotNull(call.TargetLine);
        });
        Assert.Equal(2, overloadCalls.Select(call => call.Target).Distinct(StringComparer.Ordinal).Count());
        var metadataCall = Assert.Single(semantic.ResolvedCalls, call => call.Expression == "core.ToString");
        Assert.Null(metadataCall.TargetPath);
        Assert.Null(metadataCall.TargetLine);
    }

    [Fact]
    public void Analyze_MarksUnmappedAndUnsupportedPathsIncomplete()
    {
        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Missing.cs", "native/file.cpp", "../outside.cs" },
            new[] { "src/Core.cs" }, CompileFixtureGraph(), 100);

        Assert.False(report.IsComplete);
        Assert.Contains(report.Issues, issue => issue.Contains("not found", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("supports C# and Visual Basic", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("repository-relative", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_HandlesCyclesDuplicateEdgesAndMultipleChangedPaths()
    {
        var semantic = CompileFixtureGraph();
        var declarations = semantic.Declarations!;
        var core = declarations.Single(item => item.Path == "src/Core.cs").Symbol;
        var service = declarations.Single(item => item.Path == "src/Service.cs").Symbol;
        var originalEdge = semantic.ResolvedCalls.Single(item => item.Target == core);
        semantic = semantic with
        {
            ResolvedCalls = semantic.ResolvedCalls.Concat(new[]
            {
                originalEdge,
                new CSharpResolvedCall("fixture.csproj", "src/Core.cs", 1, "recursive", core, service)
            }).ToArray(),
            ResolvedInvocationCount = semantic.ResolvedInvocationCount + 2
        };

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs", "src/Service.cs" },
            new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs" }, semantic, 100);

        Assert.True(report.IsComplete);
        Assert.Equal(2, report.ChangedPaths.Count);
        Assert.Equal(2, report.Summary.MaximumCallerDepth);
        Assert.Single(report.Impacts, item => item.ChangedMethod == core && item.Caller == core && item.Depth == 2);
        Assert.Equal(report.Impacts.Count, report.Impacts.Select(item =>
            string.Join("|", item.ChangedMethod, item.Target, item.Caller, item.Path, item.Line)).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Analyze_MarksPartialLoadsUnresolvedEdgesAndTruncationIncomplete()
    {
        var semantic = CompileFixtureGraph() with
        {
            ProjectsDiscovered = 2,
            UnresolvedInvocationCount = 3,
            UnresolvedCalls = Array.Empty<CSharpUnresolvedCall>(),
            DeclarationCount = 20
        };

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs" },
            new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs" }, semantic, 100);

        Assert.False(report.IsComplete);
        Assert.Contains(report.Issues, issue => issue.Contains("projects loaded", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("unresolved", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("declarations were sampled", StringComparison.Ordinal));
        var rendered = CallImpactReportRenderer.Render(report);
        Assert.True(rendered.Contains("## Completeness limitations", StringComparison.Ordinal), rendered);
    }

    [Fact]
    public void Renderers_ExposeEquivalentOrderedFindingsAndEvidenceLimits()
    {
        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs" },
            new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs" }, CompileFixtureGraph(), 100);
        var json = JsonSerializer.Serialize(report);
        var markdown = CallImpactReportRenderer.Render(report);
        var repeated = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs" },
            new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs" }, CompileFixtureGraph(), 100);

        using var parsed = JsonDocument.Parse(json);
        Assert.Equal(1, parsed.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.StartsWith("{\"SchemaVersion\":1,", json, StringComparison.Ordinal);
        Assert.Equal(2, parsed.RootElement.GetProperty("Impacts").GetArrayLength());
        Assert.Equal(json, JsonSerializer.Serialize(repeated));
        Assert.Equal(markdown, CallImpactReportRenderer.Render(repeated));
        Assert.Contains("Depth 1", markdown, StringComparison.Ordinal);
        Assert.Contains("Depth 2", markdown, StringComparison.Ordinal);
        Assert.Contains("do not prove test coverage", markdown, StringComparison.Ordinal);
        Assert.Contains("not effort estimates", markdown, StringComparison.Ordinal);
    }

    private static CSharpSemanticAnalysis CompileFixtureGraph()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace Demo; public class Core { public void Save() { } }", path: "src/Core.cs"),
            CSharpSyntaxTree.ParseText("namespace Demo; public class Service { public void Run(Core core) { core.Save(); } }", path: "src/Service.cs"),
            CSharpSyntaxTree.ParseText("namespace Demo; public class CoreTests { public void Saves(Core core, Service service) { service.Run(core); } }", path: "tests/CoreTests.cs")
        };
        var compilation = CSharpCompilation.Create("ImpactFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return CSharpSemanticAnalyzer.AnalyzeCompilations(new[] { ("fixture.csproj", (Compilation)compilation) }, 1, 100, ".");
    }
}
