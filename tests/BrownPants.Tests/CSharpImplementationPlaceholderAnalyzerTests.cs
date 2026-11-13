using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class CSharpImplementationPlaceholderAnalyzerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-noop-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Analyze_FindsWholeMethodPlaceholderBodiesAndIgnoresRealBodiesAndText()
    {
        Write("src/Repository.cs", """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            class Repository {
                public Task SaveAsync() => Task.CompletedTask;
                public Task<IEnumerable<int>> ListAsync() => Task.FromResult(Enumerable.Empty<int>());
                public Task<int> CountAsync() => Task.FromResult(0);
                public int DefaultValue() => default;
                public List<int> NewList() => new List<int>();
                public int[] ArrayValue() => Array.Empty<int>();
                public string Missing(string value = "NeverLeakParameter") => throw new NotImplementedException("NeverLeakExceptionMessage");
                public void Empty() { }
                public int Real() { return 3; }
                public string Text() => "return null; Task.CompletedTask; Enumerable.Empty<int>()";
                // public int CommentOnly() => 0;
            }
            """);

        var analysis = CSharpImplementationPlaceholderAnalyzer.Analyze(_root, ["src/Repository.cs"], sampleLimit: 20);

        Assert.Equal(8, analysis.CandidateCount);
        Assert.Equal(0, analysis.OmittedCandidateCount);
        Assert.Contains(analysis.Candidates, item => item.Kind == "Completed task" && item.Signature.Contains("SaveAsync"));
        Assert.Contains(analysis.Candidates, item => item.Kind == "Empty result" && item.Signature.Contains("ListAsync"));
        Assert.Contains(analysis.Candidates, item => item.Kind == "Default result" && item.Signature.Contains("CountAsync"));
        Assert.Contains(analysis.Candidates, item => item.Kind == "Default result" && item.Signature.Contains("DefaultValue"));
        Assert.Contains(analysis.Candidates, item => item.Kind == "Empty result" && item.Signature.Contains("NewList"));
        Assert.Contains(analysis.Candidates, item => item.Kind == "Empty result" && item.Signature.Contains("ArrayValue"));
        Assert.Contains(analysis.Candidates, item => item.Kind == "NotImplementedException" && item.Signature.Contains("Missing"));
        Assert.Contains(analysis.Candidates, item => item.Kind == "Empty body" && item.Signature.Contains("Empty"));
        Assert.DoesNotContain(analysis.Candidates, item => item.Signature.Contains("Real"));
        Assert.DoesNotContain(analysis.Candidates, item => item.Signature.Contains("Text"));
        Assert.DoesNotContain(analysis.Candidates, item => item.Signature.Contains("NeverLeak", StringComparison.Ordinal));
        Assert.DoesNotContain(analysis.Candidates, item => item.Evidence.Contains("NeverLeak", StringComparison.Ordinal));
        Assert.All(analysis.Candidates, item => Assert.Equal("production", item.Scope));
    }

    [Fact]
    public void Analyze_UsesStableSampleLimitAndReportsSkippedOversizedFiles()
    {
        Write("src/B.cs", "class B { int Run() => 0; }");
        Write("src/A.cs", "class A { int Run() => 0; }");
        Write("src/Large.cs", new string(' ', 1024 * 1024 + 1));

        var first = CSharpImplementationPlaceholderAnalyzer.Analyze(_root,
            ["src/B.cs", "src/A.cs", "src/Large.cs"], sampleLimit: 1);
        var second = CSharpImplementationPlaceholderAnalyzer.Analyze(_root,
            ["src/Large.cs", "src/A.cs", "src/B.cs"], sampleLimit: 1);

        Assert.Equal(2, first.CandidateCount);
        Assert.Equal(1, first.OmittedCandidateCount);
        Assert.Equal(1, first.FilesSkipped);
        Assert.Single(first.Candidates);
        Assert.Equal("src/A.cs", first.Candidates[0].Path);
        Assert.Equal(first.SourceFilesScanned, second.SourceFilesScanned);
        Assert.Equal(first.BytesScanned, second.BytesScanned);
        Assert.Equal(first.CandidateCount, second.CandidateCount);
        Assert.Equal(first.OmittedCandidateCount, second.OmittedCandidateCount);
        Assert.Equal(first.FilesSkipped, second.FilesSkipped);
        Assert.Equal(first.Candidates, second.Candidates);
    }

    [Fact]
    public void Analyze_LabelsDotNetTestProjectCandidatesAsTests()
    {
        Write("tests/Repository.Tests.Unit/RepositoryFake.cs", "class Fake { System.Threading.Tasks.Task Save() => System.Threading.Tasks.Task.CompletedTask; }");

        var analysis = CSharpImplementationPlaceholderAnalyzer.Analyze(_root,
            ["tests/Repository.Tests.Unit/RepositoryFake.cs"], sampleLimit: 10);

        var candidate = Assert.Single(analysis.Candidates);
        Assert.Equal("test", candidate.Scope);
    }

    [Fact]
    public void RepositoryScanner_ExposesCandidatesInJsonAndMarkdown()
    {
        Write("src/Repository.cs", "class Repository { System.Threading.Tasks.Task Save() => System.Threading.Tasks.Task.CompletedTask; private System.NotImplementedException CreateNotImplemented() => new System.NotImplementedException(); }");
        Write("Repository.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 10);
        var json = JsonSerializer.Serialize(inventory);
        var markdown = MarkdownReport.Render(inventory);

        Assert.Contains("Save", json, StringComparison.Ordinal);
        Assert.Equal(2, inventory.ImplementationPlaceholders.CandidateCount);
        Assert.Contains("CreateNotImplemented", json, StringComparison.Ordinal);
        Assert.Contains("NotImplementedException", json, StringComparison.Ordinal);
        Assert.Contains("No-op implementation candidates", markdown, StringComparison.Ordinal);
        Assert.Contains("may be intentional behavior", markdown, StringComparison.Ordinal);
        Assert.Contains("src/Repository.cs:1", markdown, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
