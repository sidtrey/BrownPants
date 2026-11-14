using System.Text.Json;
using BrownPants.Cli;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Tests;

public sealed class TypeScriptSyntaxVerifierTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-typescript-tests", Guid.NewGuid().ToString("N"));

    public TypeScriptSyntaxVerifierTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Analyze_DefaultDoesNotRequestCompilerVerification()
    {
        Write("package.json", "{\"dependencies\":{\"react\":\"19.0.0\"}}");
        Write("src/View.tsx", "export const View = () => <section />;\n");

        var scan = RepositoryScanner.Scan(_root);
        var analysis = scan.JavaScriptTypeScript;

        Assert.Null(analysis.SyntaxVerification);
        Assert.Equal("not-requested", analysis.ReactSupport.CandidateAnalysisStatus);
        var markdown = MarkdownReport.Render(scan);
        Assert.Contains("candidate analysis was not requested", markdown);
        Assert.Contains("Run `inspect --typescript-syntax`", markdown);
        Assert.DoesNotContain("Function component candidates: 0; hook-call candidates: 0", markdown);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(analysis.ReactSupport));
        Assert.Equal("not-requested", json.RootElement.GetProperty("CandidateAnalysisStatus").GetString());
    }

    [Fact]
    public void Analyze_SeparatesVerifiedSyntaxHeuristicsFromParserDiagnostics()
    {
        Write("src/broken.ts", "export function incomplete(value: string\n");
        var verifier = new FixedVerifier("src/broken.ts");

        var analysis = JavaScriptTypeScriptAnalyzer.Analyze(_root,
            new[] { "src/broken.ts" }, sampleLimit: 10, verifier);
        var verification = Assert.IsType<TypeScriptSyntaxVerification>(analysis.SyntaxVerification);

        Assert.Empty(analysis.Issues);
        Assert.Equal(1, verification.HeuristicFindingCount);
        Assert.Contains(verification.HeuristicFindings, item => item.Path == "src/broken.ts" && item.Code == "UnbalancedDelimiter");
        var diagnostic = Assert.Single(verification.Diagnostics);
        Assert.Equal("1005", diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(39, diagnostic.Column);

        var markdown = MarkdownReport.Render(RepositoryScanner.Scan(_root) with
        {
            JavaScriptTypeScript = analysis
        });
        Assert.Contains("Parser diagnostic `1005` `src/broken.ts:1:39`", markdown);
        Assert.Contains("Heuristic finding (not a compiler diagnostic)", markdown);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(analysis));
        Assert.Equal("verified", json.RootElement.GetProperty("SyntaxVerification").GetProperty("Status").GetString());
    }

    [Fact]
    public void Verify_ReportsUnavailableWhenNoLocalCompilerExists()
    {
        Write("package.json", "{\"dependencies\":{\"react\":\"19.0.0\"}}");
        Write("src/View.tsx", "export const View = () => <section />;\n");

        var analysis = RepositoryScanner.Scan(_root, verifyTypeScriptSyntax: true).JavaScriptTypeScript;

        Assert.Equal("unavailable", analysis.SyntaxVerification?.Status);
        Assert.Equal("unavailable", analysis.ReactSupport.CandidateAnalysisStatus);
        Assert.Single(analysis.SyntaxVerification!.Files);
        Assert.Equal("unavailable", analysis.SyntaxVerification.Files[0].Status);
        Assert.Contains("No repository-local TypeScript compiler", analysis.SyntaxVerification.Message);
        var markdown = MarkdownReport.Render(RepositoryScanner.Scan(_root) with
        {
            JavaScriptTypeScript = analysis
        });
        Assert.Contains("React candidate analysis status: unavailable; function component and hook-call candidate counts are unavailable.", markdown);
    }

    [Fact]
    public void Inspect_ExplicitOptionReportsUnavailableWithoutInstallingACompiler()
    {
        Write("src/View.tsx", "export const View = () => <section />;\n");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Program.Run(new[] { "inspect", _root, "--typescript-syntax", "--format", "json" }, output, error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        using var json = JsonDocument.Parse(output.ToString());
        var verification = json.RootElement.GetProperty("JavaScriptTypeScript").GetProperty("SyntaxVerification");
        Assert.Equal("unavailable", verification.GetProperty("Status").GetString());
        Assert.Contains("No repository-local TypeScript compiler", verification.GetProperty("Message").GetString());
        var react = json.RootElement.GetProperty("JavaScriptTypeScript").GetProperty("ReactSupport");
        Assert.Equal("unavailable", react.GetProperty("CandidateAnalysisStatus").GetString());
    }

    [Fact]
    public void FindNearestCompiler_UsesClosestWorkspacePackageAndRejectsEscapes()
    {
        Write("package.json", "{}");
        Write("apps/one/package.json", "{}");
        Write("apps/two/package.json", "{}");
        Write("node_modules/typescript/package.json", "{\"version\":\"5.1.0\"}");
        Write("node_modules/typescript/lib/typescript.js", "// fixture parser\n");
        Write("apps/one/node_modules/typescript/package.json", "{\"version\":\"5.8.0\"}");
        Write("apps/one/node_modules/typescript/lib/typescript.js", "// fixture parser\n");
        Write("apps/two/node_modules/typescript/package.json", "{\"version\":\"5.9.0\"}");
        Write("apps/two/node_modules/typescript/lib/typescript.js", "// fixture parser\n");
        var messages = new SortedSet<string>(StringComparer.Ordinal);
        var candidates = TypeScriptSyntaxVerifier.FindCompilerCandidates(_root,
            new[] { "package.json", "apps/one/package.json", "apps/two/package.json" }, messages);

        var nearest = TypeScriptSyntaxVerifier.FindNearestCompiler(_root, "apps/one/src/View.tsx", candidates);
        var escaped = TypeScriptSyntaxVerifier.FindNearestCompiler(_root, "../outside.ts", candidates);

        Assert.Equal("5.8.0", nearest.Version);
        Assert.EndsWith(Path.Combine("apps", "one", "node_modules", "typescript", "lib", "typescript.js"), nearest.CompilerPath);
        Assert.Null(escaped.CompilerPath);
        Assert.Contains("escapes", escaped.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_ReportsReactProjectAndSourceCandidates()
    {
        Write("FullMetalFile.Client/package.json", """
            {"name":"fullmetalfile-client","devDependencies":{"react":"^19.2.7"}}
            """);
        Write("FullMetalFile.Client/src/App.tsx", "export function App() { return <main />; }\n");
        var verifier = new ReactVerifier("FullMetalFile.Client/src/App.tsx");

        var analysis = JavaScriptTypeScriptAnalyzer.Analyze(_root,
            new[] { "FullMetalFile.Client/package.json", "FullMetalFile.Client/src/App.tsx" }, sampleLimit: 10, verifier);

        var project = Assert.Single(analysis.ReactSupport.Projects);
        Assert.Equal("fullmetalfile-client", project.Name);
        Assert.Equal("FullMetalFile.Client/package.json", project.ManifestPath);
        var component = Assert.Single(analysis.ReactSupport.Components);
        Assert.Equal("App", component.Name);
        Assert.Equal(1, component.Line);
        var hook = Assert.Single(analysis.ReactSupport.HookCalls);
        Assert.Equal("useState", hook.Name);
        Assert.Equal("hook-call-candidate", hook.Kind);
        Assert.Equal("verified", analysis.ReactSupport.CandidateAnalysisStatus);
        var markdown = MarkdownReport.Render(RepositoryScanner.Scan(_root) with { JavaScriptTypeScript = analysis });
        Assert.Contains("React candidate analysis status: verified; function component candidates: 1; hook-call candidates: 1", markdown);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(analysis.ReactSupport));
        Assert.Equal("verified", json.RootElement.GetProperty("CandidateAnalysisStatus").GetString());
    }

    [Fact]
    public void Analyze_ReportsFailedCandidateCountsWithoutCallingThemZero()
    {
        Write("package.json", "{\"dependencies\":{\"react\":\"19.0.0\"}}");
        Write("src/View.tsx", "export const View = () => <section />;\n");
        var analysis = JavaScriptTypeScriptAnalyzer.Analyze(_root,
            new[] { "package.json", "src/View.tsx" }, sampleLimit: 10, new StatusVerifier("failed"));

        Assert.Equal("failed", analysis.ReactSupport.CandidateAnalysisStatus);
        var markdown = MarkdownReport.Render(RepositoryScanner.Scan(_root) with { JavaScriptTypeScript = analysis });
        Assert.Contains("React candidate analysis status: failed; function component and hook-call candidate counts are unavailable.", markdown);
        Assert.DoesNotContain("function component candidates: 0; hook-call candidates: 0", markdown);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(analysis.ReactSupport));
        Assert.Equal("failed", json.RootElement.GetProperty("CandidateAnalysisStatus").GetString());
    }

    [Fact]
    public void Analyze_ReportsZeroCandidatesWhenVerifiedScanFindsNone()
    {
        Write("package.json", "{\"dependencies\":{\"react\":\"19.0.0\"}}");
        Write("src/View.tsx", "export const value = 1;\n");
        var analysis = JavaScriptTypeScriptAnalyzer.Analyze(_root,
            new[] { "package.json", "src/View.tsx" }, sampleLimit: 10, new StatusVerifier("verified"));

        Assert.Equal("verified", analysis.ReactSupport.CandidateAnalysisStatus);
        Assert.Equal(0, analysis.ReactSupport.ComponentCount);
        Assert.Equal(0, analysis.ReactSupport.HookCallCount);
        var markdown = MarkdownReport.Render(RepositoryScanner.Scan(_root) with { JavaScriptTypeScript = analysis });
        Assert.Contains("React candidate analysis status: verified; function component candidates: 0; hook-call candidates: 0", markdown);
    }

    [Fact]
    public void Analyze_PreservesPartialCandidateAnalysisStatus()
    {
        Write("package.json", "{\"dependencies\":{\"react\":\"19.0.0\"}}");
        Write("src/View.tsx", "export const value = 1;\n");
        var analysis = JavaScriptTypeScriptAnalyzer.Analyze(_root,
            new[] { "package.json", "src/View.tsx" }, sampleLimit: 10, new StatusVerifier("partial"));

        Assert.Equal("partial", analysis.ReactSupport.CandidateAnalysisStatus);
        var markdown = MarkdownReport.Render(RepositoryScanner.Scan(_root) with { JavaScriptTypeScript = analysis });
        Assert.Contains("React candidate analysis status: partial; function component candidates: 0; hook-call candidates: 0", markdown);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(analysis.ReactSupport));
        Assert.Equal("partial", json.RootElement.GetProperty("CandidateAnalysisStatus").GetString());
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

    private sealed class FixedVerifier(string verifiedPath) : ITypeScriptSyntaxVerifier
    {
        public TypeScriptSyntaxVerification Verify(string root, IReadOnlyList<string> sourcePaths,
            IReadOnlyList<string> packageManifestPaths, IReadOnlyList<string> reactProjectManifestPaths, int sampleLimit)
        {
            Assert.Contains(verifiedPath, sourcePaths);
            return new TypeScriptSyntaxVerification("verified", 1, 0, 1, 0,
                new[] { new SyntaxParserInfo("Test parser", "1.0", "test/parser") },
                new[] { new TypeScriptSyntaxFileResult(verifiedPath, "verified", null) },
                new[] { new TypeScriptSyntaxDiagnostic(verifiedPath, "1005", "'...' expected.", 1, 39) },
                0, 0, Array.Empty<JavaScriptTypeScriptIssue>(), null)
            {
                AllVerifiedSourcePaths = new HashSet<string>(StringComparer.Ordinal) { verifiedPath }
            };
        }
    }

    private sealed class ReactVerifier(string verifiedPath) : ITypeScriptSyntaxVerifier
    {
        public TypeScriptSyntaxVerification Verify(string root, IReadOnlyList<string> sourcePaths,
            IReadOnlyList<string> packageManifestPaths, IReadOnlyList<string> reactProjectManifestPaths, int sampleLimit)
        {
            Assert.Contains(verifiedPath, sourcePaths);
            Assert.Contains("FullMetalFile.Client/package.json", reactProjectManifestPaths);
            var component = new ReactSourceCandidate(verifiedPath, "App", 1, 17, "function-component-candidate");
            var hook = new ReactSourceCandidate(verifiedPath, "useState", 2, 18, "hook-call-candidate");
            return new TypeScriptSyntaxVerification("verified", 1, 0, 0, 0,
                new[] { new SyntaxParserInfo("Test parser", "1.0", "test/parser") },
                new[] { new TypeScriptSyntaxFileResult(verifiedPath, "verified", null) },
                Array.Empty<TypeScriptSyntaxDiagnostic>(), 0, 0, Array.Empty<JavaScriptTypeScriptIssue>(), null)
            {
                AllVerifiedSourcePaths = new HashSet<string>(StringComparer.Ordinal) { verifiedPath },
                AllReactComponents = new[] { component },
                AllReactHookCalls = new[] { hook }
            };
        }
    }

    private sealed class StatusVerifier(string status) : ITypeScriptSyntaxVerifier
    {
        public TypeScriptSyntaxVerification Verify(string root, IReadOnlyList<string> sourcePaths,
            IReadOnlyList<string> packageManifestPaths, IReadOnlyList<string> reactProjectManifestPaths, int sampleLimit) =>
            new(status, 1, 0, 0, 0, Array.Empty<SyntaxParserInfo>(),
                new[] { new TypeScriptSyntaxFileResult("src/View.tsx", status, null) },
                Array.Empty<TypeScriptSyntaxDiagnostic>(), 0, 0, Array.Empty<JavaScriptTypeScriptIssue>(), null);
    }
}
