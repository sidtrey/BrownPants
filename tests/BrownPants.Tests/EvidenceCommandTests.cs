using System.Text.Json;
using BrownPants.Cli;

namespace BrownPants.Tests;

public sealed class EvidenceCommandTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-evidence-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Evidence_EmitsDeterministicCandidateFindingsAndTestPaths()
    {
        Write("src/OrderFactory.cs", "namespace Sample; public class OrderFactory { public object Create() => new(); }");
        Write("tests/OrderServiceTests.cs", "namespace Sample.Tests; public class OrderServiceTests { }");

        var first = Run("evidence", _root, "--limit", "1000");
        var second = Run("evidence", _root, "--limit", "1000");

        Assert.Equal(0, first.ExitCode);
        Assert.Empty(first.Error);
        Assert.Equal(first.Output, second.Output);
        using var json = JsonDocument.Parse(first.Output);
        var report = json.RootElement;
        Assert.Equal(1, report.GetProperty("schemaVersion").GetInt32());
        var findings = report.GetProperty("findings").EnumerateArray().ToArray();
        var factory = Assert.Single(findings, item => item.GetProperty("signal").GetString() == "Factory type");
        Assert.Equal("architecture-candidate", factory.GetProperty("category").GetString());
        Assert.Equal("candidate", factory.GetProperty("status").GetString());
        Assert.Equal("src/OrderFactory.cs", factory.GetProperty("path").GetString());
        var test = Assert.Single(findings, item => item.GetProperty("category").GetString() == "test-identification");
        Assert.Equal("tests/OrderServiceTests.cs", test.GetProperty("path").GetString());
        Assert.Equal("candidate", test.GetProperty("status").GetString());
        Assert.Contains(report.GetProperty("limitations").EnumerateArray(), item =>
            item.GetString()!.Contains("Semantic analysis was not requested", StringComparison.Ordinal));
    }

    [Fact]
    public void Evidence_RejectsMarkdownAndInvalidOptions()
    {
        var format = Run("evidence", _root, "--format", "markdown");
        var option = Run("evidence", _root, "--unknown");

        Assert.Equal(2, format.ExitCode);
        Assert.Contains("Expected json", format.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, option.ExitCode);
        Assert.Contains("Unknown evidence option", option.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Evidence_ClassifiesPatternsInLegacyFrameworkProjectAsFrameworkCandidates()
    {
        Write("legacy/Framework/OldApp.csproj", "<Project ToolsVersion=\"15.0\"><PropertyGroup><TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion></PropertyGroup></Project>");
        Write("legacy/Framework/Service.cs", "namespace Legacy; public interface IStore { } public class Service { public Service(IStore store) { } }");
        Assert.True(BrownPants.Cli.Analysis.RepositoryScanner.Scan(_root).DotNetProjects.Single().IsLegacyFramework);

        var result = Run("evidence", _root);

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var finding = Assert.Single(json.RootElement.GetProperty("findings").EnumerateArray(), item =>
            item.GetProperty("signal").GetString() == "Constructor injection");
        Assert.Equal("framework-candidate", finding.GetProperty("category").GetString());
        Assert.Equal("candidate", finding.GetProperty("status").GetString());
    }

    [Fact]
    public void Evidence_ResolvesManagedCallsOnlyWhenSemanticAnalysisIsRequested()
    {
        Write("Legacy.csproj", """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup><TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion></PropertyGroup>
              <ItemGroup><Reference Include="System" /><Compile Include="Calls.cs" /></ItemGroup>
            </Project>
            """);
        Write("Calls.cs", "namespace Sample; public class Store { public void Save() { } } public class Runner { private readonly Store _store = new(); public void Run() => _store.Save(); }");

        var staticResult = Run("evidence", _root);
        var semanticResult = Run("evidence", _root, "--semantic");

        Assert.Equal(0, staticResult.ExitCode);
        using (var staticJson = JsonDocument.Parse(staticResult.Output))
            Assert.DoesNotContain(staticJson.RootElement.GetProperty("findings").EnumerateArray(), item =>
                item.GetProperty("category").GetString() == "managed-call");
        Assert.Equal(0, semanticResult.ExitCode);
        using var semanticJson = JsonDocument.Parse(semanticResult.Output);
        var call = Assert.Single(semanticJson.RootElement.GetProperty("findings").EnumerateArray(), item =>
            item.GetProperty("category").GetString() == "managed-call");
        Assert.Equal("resolved", call.GetProperty("status").GetString());
        Assert.Equal("Calls.cs", call.GetProperty("path").GetString());
        Assert.Contains("Store.Save", call.GetProperty("targetSignature").GetString());
        Assert.True(semanticJson.RootElement.GetProperty("completeness").GetProperty("semanticAnalysisRequested").GetBoolean());
    }

    [Fact]
    public void Evidence_ReportsWhenUnifiedFindingLimitTruncatesOutput()
    {
        Write("src/OrderFactory.cs", "namespace Sample; public class AFactory { } public class BFactory { } public class CFactory { }");

        var result = Run("evidence", _root, "--limit", "1");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var completeness = json.RootElement.GetProperty("completeness");
        Assert.Equal(1, completeness.GetProperty("emittedFindingCount").GetInt32());
        Assert.True(completeness.GetProperty("findingsTruncated").GetBoolean());
        Assert.True(completeness.GetProperty("totalFindingCount").GetInt32() > 1);
    }

    [Fact]
    public void Evidence_ReportsIncompleteSemanticProjectCoverage()
    {
        Write("Broken.csproj", "<Project");
        Write("Broken.cs", "public class Broken { }");

        var result = Run("evidence", _root, "--semantic");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var completeness = json.RootElement.GetProperty("completeness");
        Assert.True(completeness.GetProperty("semanticAnalysisRequested").GetBoolean());
        Assert.False(completeness.GetProperty("semanticProjectCoverageComplete").GetBoolean());
        Assert.Contains(json.RootElement.GetProperty("limitations").EnumerateArray(), item =>
            item.GetString()!.Contains("coverage is incomplete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evidence_HelpExplainsCandidateAndSemanticBoundaries()
    {
        var result = Run("evidence", "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("candidate", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MSBuild", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    private (int ExitCode, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = Program.Run(args, output, error);
        return (exitCode, output.ToString(), error.ToString());
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
