using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class CSharpDroppedCollectionMappingAnalyzerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-dropped-mapping-", Guid.NewGuid().ToString("N"));

    public CSharpDroppedCollectionMappingAnalyzerTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Analyze_FindsEmptyCollectionArgumentBesidePropertiesFromSameSource()
    {
        Write("src/Mapper.cs", """
            using System.Collections.Generic;
            class Mapper {
                void Map(PageResult ocrPage) {
                    target.WithOcrData(ocrPage.Text, new List<Word>(), ocrPage.Confidence);
                }
            }
            """);

        var analysis = CSharpDroppedCollectionMappingAnalyzer.Analyze(_root, ["src/Mapper.cs"], sampleLimit: 10);

        var candidate = Assert.Single(analysis.Candidates);
        Assert.Equal(1, analysis.CandidateCount);
        Assert.Equal("ocrPage", candidate.SourceObject);
        Assert.Equal("2", candidate.EmptyArgumentIndexes);
        Assert.Equal("WithOcrData", candidate.TargetMethod);
        Assert.Contains("new List<Word>()", candidate.Evidence);
    }

    [Fact]
    public void Analyze_ExcludesNonEmptyMappingsAndCommentOrStringMentions()
    {
        Write("src/Mapper.cs", """
            using System.Collections.Generic;
            class Mapper {
                void Map(PageResult page) {
                    target.WithData(page.Text, page.Words);
                    // target.WithOcrData(page.Text, new List<Word>());
                    var note = "WithOcrData(page.Text, new List<Word>())";
                    target.WithTitle(page.Text, string.Empty);
                    target.WithEmpty(new List<Word>());
                    target.WithTrimmed(extractedText.Trim(), new List<Word>());
                }
            }
            """);

        var analysis = CSharpDroppedCollectionMappingAnalyzer.Analyze(_root, ["src/Mapper.cs"], sampleLimit: 10);

        Assert.Empty(analysis.Candidates);
        Assert.Equal(0, analysis.CandidateCount);
    }

    [Fact]
    public void Analyze_RecognizesCollectionExpressionsAndEmptyHelpers()
    {
        Write("src/Mapper.cs", """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Mapper {
                void Map(PageResult page) {
                    target.One(page.Text, []);
                    target.Two(page.Text, Array.Empty<Word>());
                    target.Three(page.Text, Enumerable.Empty<Word>());
                }
            }
            """);

        var analysis = CSharpDroppedCollectionMappingAnalyzer.Analyze(_root, ["src/Mapper.cs"], sampleLimit: 10);

        Assert.Equal(3, analysis.CandidateCount);
        Assert.Equal(new[] { "One", "Three", "Two" }, analysis.Candidates.Select(candidate => candidate.TargetMethod).OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void Analyze_OrdersCandidatesAndReportsOmissionsAndOversizedFiles()
    {
        Write("src/B.cs", "class B { void Map(P p) { target.Apply(p.Text, new System.Collections.Generic.List<W>()); } }");
        Write("src/A.cs", "class A { void Map(P p) { target.Apply(p.Text, new System.Collections.Generic.List<W>()); } }");
        Write("src/Large.cs", new string(' ', 1024 * 1024 + 1));

        var first = CSharpDroppedCollectionMappingAnalyzer.Analyze(_root,
            ["src/B.cs", "src/A.cs", "src/Large.cs"], sampleLimit: 1);
        var second = CSharpDroppedCollectionMappingAnalyzer.Analyze(_root,
            ["src/Large.cs", "src/A.cs", "src/B.cs"], sampleLimit: 1);

        Assert.Equal(2, first.CandidateCount);
        Assert.Equal(1, first.OmittedCandidateCount);
        Assert.Equal(1, first.FilesSkipped);
        Assert.Equal("src/A.cs", Assert.Single(first.Candidates).Path);
        Assert.Equal(first.Candidates, second.Candidates);
        Assert.Equal(first.CandidateCount, second.CandidateCount);
        Assert.Equal(first.OmittedCandidateCount, second.OmittedCandidateCount);
    }

    [Fact]
    public void RepositoryScanner_ExposesDroppedCollectionCandidatesInJsonAndMarkdown()
    {
        Write("src/Mapper.cs", "class Mapper { void Map(PageResult page) { target.WithData(page.Text, new System.Collections.Generic.List<Word>()); } }");
        Write("Repository.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 10);
        var json = JsonSerializer.Serialize(inventory);
        var markdown = MarkdownReport.Render(inventory);

        Assert.Equal(1, inventory.DroppedCollectionMappings.CandidateCount);
        Assert.Contains("DroppedCollectionMappings", json, StringComparison.Ordinal);
        Assert.Contains("Possible dropped collection mappings", markdown, StringComparison.Ordinal);
        Assert.Contains("do not prove the source data should be copied", markdown, StringComparison.Ordinal);
        Assert.Contains("src/Mapper.cs:1", markdown, StringComparison.Ordinal);
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
