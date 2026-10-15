using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BrownPants.Tests;

public sealed class CallImpactAnalyzerTests
{
    [Fact]
    public void ImpactRetainsRelevantEdgesBeyondPublicSemanticSample()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace Demo; public class Changed { public void Save() { } }",
                path: "src/Changed.cs", cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class Noise { public void Ping() { } } public class NoiseCaller { public void Run(Noise noise) => noise.Ping(); }",
                path: "src/AUnrelatedCaller.cs", cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class ChangedCaller { public void Run(Changed changed) => changed.Save(); }",
                path: "src/ZChangedCaller.cs", cancellationToken: TestContext.Current.CancellationToken)
        };
        var compilation = CSharpCompilation.Create("ImpactSampleFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var project = ("fixture.csproj", (Compilation)compilation);
        var semantic = CSharpSemanticAnalyzer.AnalyzeCompilations(new[] { project }, 1, 1, ".");
        var allDeclarations = CSharpSemanticAnalyzer.AnalyzeCompilations(new[] { project }, 1, 100, ".").Declarations;
        semantic = semantic with { Declarations = allDeclarations };

        Assert.Single(semantic.ResolvedCalls);
        Assert.DoesNotContain(semantic.ResolvedCalls, call => call.TargetPath == "src/Changed.cs");
        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Changed.cs" },
            new[] { "src/Changed.cs", "src/AUnrelatedCaller.cs", "src/ZChangedCaller.cs" }, semantic, 100);

        Assert.True(report.IsComplete);
        var impact = Assert.Single(report.Impacts);
        Assert.Equal("src/ZChangedCaller.cs", impact.Path);
        Assert.Equal(1, impact.Line);
        Assert.Equal(1, impact.Depth);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(semantic));
        Assert.False(json.RootElement.TryGetProperty("ImpactGraphCalls", out _));
        Assert.False(json.RootElement.TryGetProperty("ImpactGraphOmittedCount", out _));
    }

    [Fact]
    public void ImpactReportsExactOmissionWhenInternalGraphReachesItsBound()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace Demo; public class Changed { public void Save() { } }",
                path: "src/Changed.cs", cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class Noise { public void Ping() { } } public class NoiseCaller { public void Run(Noise noise) => noise.Ping(); }",
                path: "src/AUnrelatedCaller.cs", cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class ChangedCaller { public void Run(Changed changed) => changed.Save(); }",
                path: "src/ZChangedCaller.cs", cancellationToken: TestContext.Current.CancellationToken)
        };
        var compilation = CSharpCompilation.Create("ImpactGraphBoundFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semantic = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("fixture.csproj", (Compilation)compilation) }, 1, 1, ".", 1);
        semantic = semantic with
        {
            Declarations = CSharpSemanticAnalyzer.AnalyzeCompilations(
                new[] { ("fixture.csproj", (Compilation)compilation) }, 1, 100, ".").Declarations
        };

        Assert.Single(semantic.ImpactGraphCalls!);
        Assert.Equal(1, semantic.ImpactGraphOmittedCount);
        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Changed.cs" },
            new[] { "src/Changed.cs", "src/AUnrelatedCaller.cs", "src/ZChangedCaller.cs" }, semantic, 100);

        Assert.False(report.IsComplete);
        Assert.Empty(report.Impacts);
        Assert.Contains(report.Issues, issue => issue.Contains("1 retained and 1 omitted", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_TracesChangedNativeBridgeBackToMfcDispatchAsCandidates()
    {
        var semantic = new CSharpSemanticAnalysis(1, 0, 5, 3, Array.Empty<CSharpResolvedCall>(),
            Array.Empty<CSharpUnresolvedCall>(), new[] { "unrelated managed workspace failure" });
        var native = new NativeFlowAnalysis(2, 2, 0, 0, 0, new[]
        {
            new NativeFlowRelation("dispatch", "mfc-dispatch", "candidate", "legacy/Native/View.cpp", 6,
                "MFC message map CView", "legacy/Native/View.cpp", 9, "CView::OnImport", "ID_IMPORT", 1, "ON_COMMAND"),
            new NativeFlowRelation("bridge", "qualified-native-call", "candidate", "legacy/Native/View.cpp", 11,
                "CView::OnImport", "legacy/Bridge/Bridge.cpp", 4, "CBridge::Import", null, 1, "CBridge::Import()")
        }, new[] { "Native flow links are lexical candidates, not compiler-resolved calls." });

        var report = CallImpactAnalyzer.Analyze(".", new[] { "legacy/Bridge/Bridge.cpp" },
            new[] { "legacy/Native/View.cpp", "legacy/Bridge/Bridge.cpp" }, semantic, 100, native);

        Assert.True(report.IsComplete);
        Assert.Equal(2, report.NativeImpacts.Count);
        Assert.Contains(report.NativeImpacts, item => item.RelationshipKind == "qualified-native-call"
            && item.SourcePath == "legacy/Native/View.cpp" && item.Depth == 1 && item.Status == "candidate");
        Assert.Contains(report.NativeImpacts, item => item.RelationshipKind == "mfc-dispatch"
            && item.TargetSymbol == "CView::OnImport" && item.Depth == 2 && item.Status == "candidate");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        Assert.Equal("native-lexical", json.RootElement.GetProperty("NativeImpacts")[0].GetProperty("EvidenceKind").GetString());
        Assert.Contains("Native impact candidates", CallImpactReportRenderer.Render(report), StringComparison.Ordinal);
        Assert.Equal(report.NativeImpacts, CallImpactAnalyzer.Analyze(".", new[] { "legacy/Bridge/Bridge.cpp" },
            new[] { "legacy/Native/View.cpp", "legacy/Bridge/Bridge.cpp" }, semantic, 100, native).NativeImpacts);
    }

    [Fact]
    public void Analyze_TracesChangedManagedTargetBackThroughCppCliAndMfcCandidates()
    {
        const string targetSymbol = "Demo.App.OrderImportWorkflow.ImportBatch";
        var semantic = new CSharpSemanticAnalysis(1, 0, 1, 0, Array.Empty<CSharpResolvedCall>(),
            Array.Empty<CSharpUnresolvedCall>(), Array.Empty<string>())
        {
            Declarations = new[] { new CSharpSemanticDeclaration("fixture.csproj", "managed/Workflow.cs", 6, targetSymbol) }
        };
        var native = new NativeFlowAnalysis(3, 4, 0, 1, 0, new[]
        {
            new NativeFlowRelation("interop", "cppcli-managed-call", "candidate", "legacy/Bridge/Bridge.cpp", 9,
                "CBridge::Import", "managed/Workflow.cs", 6, targetSymbol, null, 1,
                "OrderImportWorkflow<Order^>^ workflow; workflow->ImportBatch(...)",
                new[] { new NativeFlowTarget("managed/Workflow.cs", 6, targetSymbol) }),
            new NativeFlowRelation("interop-ambiguous", "cppcli-managed-call", "ambiguous", "legacy/Bridge/Bridge.cpp", 10,
                "CBridge::Import", null, null, "Demo.App.OrderImportWorkflow.ImportMapped", null, 2,
                "workflow->ImportMapped(...)", new[]
                {
                    new NativeFlowTarget("managed/Workflow.cs", 7, "Demo.App.OrderImportWorkflow.ImportMapped"),
                    new NativeFlowTarget("managed/OtherWorkflow.cs", 12, "Demo.App.OtherWorkflow.ImportMapped")
                }),
            new NativeFlowRelation("native-call", "qualified-native-call", "candidate", "legacy/Native/View.cpp", 11,
                "CView::OnImport", "legacy/Bridge/Bridge.cpp", 4, "CBridge::Import", null, 1, "CBridge::Import()"),
            new NativeFlowRelation("dispatch", "mfc-dispatch", "candidate", "legacy/Native/View.cpp", 6,
                "MFC message map CView", "legacy/Native/View.cpp", 9, "CView::OnImport", "ID_IMPORT", 1, "ON_COMMAND")
        }, Array.Empty<string>());

        var report = CallImpactAnalyzer.Analyze(".", new[] { "managed/Workflow.cs" },
            new[] { "managed/Workflow.cs", "managed/OtherWorkflow.cs", "legacy/Native/View.cpp", "legacy/Bridge/Bridge.cpp" },
            semantic, 100, native);

        Assert.False(report.IsComplete);
        Assert.Equal(4, report.NativeImpacts.Count);
        Assert.Contains(report.NativeImpacts, item => item.RelationshipKind == "cppcli-managed-call"
            && item.Status == "candidate" && item.Depth == 1 && item.TargetPath == "managed/Workflow.cs");
        Assert.Contains(report.NativeImpacts, item => item.RelationshipKind == "cppcli-managed-call"
            && item.Status == "ambiguous" && item.CandidateTargets.Count == 2
            && item.CandidateTargets.Any(target => target.Path == "managed/Workflow.cs"));
        Assert.Contains(report.NativeImpacts, item => item.RelationshipKind == "qualified-native-call" && item.Depth == 2);
        Assert.Contains(report.NativeImpacts, item => item.RelationshipKind == "mfc-dispatch" && item.Depth == 3);
        var rendered = CallImpactReportRenderer.Render(report);
        Assert.Contains("Native impact candidates", rendered, StringComparison.Ordinal);
        Assert.Contains("possible locations:", rendered, StringComparison.Ordinal);
        Assert.Contains("`managed/Workflow.cs:7`", rendered, StringComparison.Ordinal);
        Assert.Contains("`managed/OtherWorkflow.cs:12`", rendered, StringComparison.Ordinal);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        Assert.Equal("managed/Workflow.cs", json.RootElement.GetProperty("NativeImpacts")[0]
            .GetProperty("ChangedPath").GetString());
        Assert.Equal(report.NativeImpacts.Select(item => item.RelationshipId), CallImpactAnalyzer.Analyze(".", new[] { "managed/Workflow.cs" },
            new[] { "managed/Workflow.cs", "managed/OtherWorkflow.cs", "legacy/Native/View.cpp", "legacy/Bridge/Bridge.cpp" },
            semantic, 100, native).NativeImpacts.Select(item => item.RelationshipId));
    }

    [Fact]
    public void Analyze_TracesManagedCallerDeclarationToNativeAndMfcCandidates()
    {
        var semantic = CompileFixtureGraph();
        var declarations = semantic.Declarations!;
        var managedCaller = declarations.Single(item => item.Symbol.Contains("Service.Run", StringComparison.Ordinal));
        var changedSave = declarations.Single(item => item.Symbol.Contains("Core.Save", StringComparison.Ordinal)).Symbol;
        const string changedDelete = "global::Demo.Core.Delete()";
        var addedCall = new CSharpResolvedCall("fixture.csproj", "src/Core.cs", 1, "Delete()", changedSave, changedDelete);
        semantic = semantic with
        {
            Declarations = declarations.Append(new CSharpSemanticDeclaration("fixture.csproj", "src/Core.cs", 1, changedDelete)).ToArray(),
            ResolvedCalls = semantic.ResolvedCalls.Append(addedCall).ToArray(),
            ImpactGraphCalls = semantic.ImpactGraphCalls!.Append(addedCall).ToArray(),
            ResolvedInvocationCount = semantic.ResolvedInvocationCount + 1
        };
        var native = new NativeFlowAnalysis(6, 6, 1, 1, 0, new[]
        {
            new NativeFlowRelation("interop", "cppcli-managed-call", "candidate", "legacy/Bridge/Bridge.cpp", 9,
                "CBridge::Import", managedCaller.Path, managedCaller.Line, managedCaller.Symbol, null, 1,
                "workflow->Run(...)", new[] { new NativeFlowTarget(managedCaller.Path, managedCaller.Line, managedCaller.Symbol) }),
            new NativeFlowRelation("same-location-decoy", "cppcli-managed-call", "candidate", "legacy/Bridge/Bridge.cpp", 15,
                "CBridge::Other", managedCaller.Path, managedCaller.Line, "Demo.Other.Run", null, 1,
                "other->Run(...)", new[] { new NativeFlowTarget(managedCaller.Path, managedCaller.Line, "Demo.Other.Run") }),
            new NativeFlowRelation("same-file-decoy", "cppcli-managed-call", "candidate", "legacy/Bridge/Bridge.cpp", 16,
                "CBridge::Other", managedCaller.Path, managedCaller.Line + 20, "Demo.Other.Other", null, 1,
                "other->Other(...)", new[] { new NativeFlowTarget(managedCaller.Path, managedCaller.Line + 20, "Demo.Other.Other") }),
            new NativeFlowRelation("ambiguous-interop", "cppcli-managed-call", "ambiguous", "legacy/Bridge/Bridge.cpp", 17,
                "CBridge::ImportAmbiguous", null, null, "Demo.Service.Run", null, 2,
                "other->Run(...)", new[]
                {
                    new NativeFlowTarget(managedCaller.Path, managedCaller.Line, managedCaller.Symbol),
                    new NativeFlowTarget("src/Other.cs", 4, "Demo.Other.Run")
                }),
            new NativeFlowRelation("unresolved-interop", "cppcli-managed-call", "unresolved", "legacy/Bridge/Bridge.cpp", 18,
                "CBridge::ImportUnresolved", null, null, "Demo.Service.Run", null, 0, "workflow->Run(...)"),
            new NativeFlowRelation("native-call", "qualified-native-call", "candidate", "legacy/Native/View.cpp", 11,
                "CView::OnImport", "legacy/Bridge/Bridge.cpp", 9, "CBridge::Import", null, 1, "CBridge::Import()"),
            new NativeFlowRelation("dispatch", "mfc-dispatch", "candidate", "legacy/Native/View.cpp", 6,
                "MFC message map CView", "legacy/Native/View.cpp", 10, "CView::OnImport", "ID_IMPORT", 1, "ON_COMMAND")
        }, Array.Empty<string>());

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs" },
            new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs", "legacy/Bridge/Bridge.cpp", "legacy/Native/View.cpp" },
            semantic, 100, native);

        Assert.Equal(new[] { "interop", "ambiguous-interop", "unresolved-interop", "native-call", "dispatch" },
            report.NativeImpacts.Select(item => item.RelationshipId));
        Assert.False(report.IsComplete);
        Assert.Contains(report.Issues, issue => issue.Contains("ambiguous relation", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("unresolved relation", StringComparison.Ordinal));
        Assert.All(report.NativeImpacts, item => Assert.Equal(1, item.ManagedImpactDepth));
        Assert.Equal(new[] { 1, 1, 1, 2, 3 }, report.NativeImpacts.Select(item => item.Depth));
        Assert.All(report.NativeImpacts, item => Assert.Equal("native-lexical", item.EvidenceKind));
        Assert.Contains("Managed depth 1", CallImpactReportRenderer.Render(report), StringComparison.Ordinal);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        Assert.All(json.RootElement.GetProperty("NativeImpacts").EnumerateArray(), item =>
            Assert.Equal(1, item.GetProperty("ManagedImpactDepth").GetInt32()));
        var repeated = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs" },
            new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs", "legacy/Bridge/Bridge.cpp", "legacy/Native/View.cpp" },
            semantic, 100, native);
        Assert.Equal(JsonSerializer.Serialize(report), JsonSerializer.Serialize(repeated));
        Assert.Equal(CallImpactReportRenderer.Render(report), CallImpactReportRenderer.Render(repeated));
    }

    [Fact]
    public void Analyze_TracesCallsFromDistinctLambdasWithoutMergingCallerIdentities()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace Demo;
            public class Registration
            {
                public void Register()
                {
                    System.Action first = () => new Changed().Save();
                    System.Action second = () => new Changed().Save();
                }
            }
            """, path: "src/Registration.cs", cancellationToken: TestContext.Current.CancellationToken);
        var changedTree = CSharpSyntaxTree.ParseText("namespace Demo; public class Changed { public Changed() { } public void Save() { } }",
            path: "src/Changed.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("LambdaImpactFixture", new[] { tree, changedTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semantic = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("fixture.csproj", (Compilation)compilation) }, 1, 100, ".");
        Assert.Contains(semantic.ResolvedCalls, call => call.Target.Contains("Changed.Save", StringComparison.Ordinal));
        Assert.Contains(semantic.Declarations!, declaration => declaration.Path == "src/Changed.cs"
            && declaration.Symbol.Contains("Changed.Save", StringComparison.Ordinal));

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Changed.cs" },
            new[] { "src/Changed.cs", "src/Registration.cs" }, semantic, 100);

        var callers = report.Impacts.Where(item => item.Target.Contains("Changed.Save", StringComparison.Ordinal))
            .Select(item => item.Caller).Distinct(StringComparer.Ordinal).ToArray();
        Assert.Equal(2, callers.Length);
        Assert.All(callers, caller => Assert.Contains("::<lambda@src/Registration.cs:", caller, StringComparison.Ordinal));
        Assert.Equal(2, report.Impacts.Where(item => item.Target.Contains("Changed.Save", StringComparison.Ordinal)).Count());
        var repeated = CallImpactAnalyzer.Analyze(".", new[] { "src/Changed.cs" },
            new[] { "src/Changed.cs", "src/Registration.cs" }, semantic, 100);
        Assert.Equal(report.Impacts, repeated.Impacts);
        Assert.Equal(JsonSerializer.Serialize(report), JsonSerializer.Serialize(repeated));
        Assert.Equal(CallImpactReportRenderer.Render(report), CallImpactReportRenderer.Render(repeated));
    }

    [Fact]
    public void Analyze_RetainsAmbiguousAndUnresolvedNativeEvidenceAsIncomplete()
    {
        var semantic = new CSharpSemanticAnalysis(0, 0, 0, 0, Array.Empty<CSharpResolvedCall>(),
            Array.Empty<CSharpUnresolvedCall>(), Array.Empty<string>());
        var native = new NativeFlowAnalysis(1, 2, 1, 1, 0, new[]
        {
            new NativeFlowRelation("unresolved", "qualified-native-call", "unresolved", "native/View.cpp", 10,
                "CView::Run", null, null, "CBridge::Missing", null, 0, "CBridge::Missing()"),
            new NativeFlowRelation("ambiguous", "mfc-dispatch", "ambiguous", "native/View.cpp", 4,
                "MFC message map CView", null, null, "CView::OnRun", "ID_RUN", 2, "ON_COMMAND(ID_RUN, CView::OnRun)")
        }, Array.Empty<string>());

        var report = CallImpactAnalyzer.Analyze(".", new[] { "native/View.cpp" },
            new[] { "native/View.cpp" }, semantic, 100, native);

        Assert.False(report.IsComplete);
        Assert.Equal(2, report.NativeImpacts.Count);
        Assert.Equal(1, report.NativeUnresolvedCount);
        Assert.Equal(1, report.NativeAmbiguousCount);
        Assert.All(report.NativeImpacts, item => Assert.Null(item.TargetPath));
        Assert.Contains(report.Issues, issue => issue.Contains("unresolved relation", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("ambiguous relation", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_SeparatesManagedResolvedCallsFromNativeCandidatesInMixedRequest()
    {
        var native = new NativeFlowAnalysis(2, 2, 0, 0, 0, new[]
        {
            new NativeFlowRelation("dispatch", "mfc-dispatch", "candidate", "legacy/View.cpp", 4,
                "MFC message map CView", "native/View.cpp", 8, "CView::OnImport", "ID_IMPORT", 1, "ON_COMMAND"),
            new NativeFlowRelation("bridge", "qualified-native-call", "candidate", "native/View.cpp", 10,
                "CView::OnImport", "legacy/Bridge.cpp", 3, "CBridge::Import", null, 1, "CBridge::Import()")
        }, Array.Empty<string>());
        var discovered = new[] { "src/Core.cs", "src/Service.cs", "tests/CoreTests.cs", "legacy/View.cpp", "native/View.cpp", "legacy/Bridge.cpp" };

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs", "legacy/Bridge.cpp" }, discovered,
            CompileFixtureGraph(), 100, native);

        Assert.NotEmpty(report.Impacts);
        Assert.NotEmpty(report.NativeImpacts);
        Assert.All(report.Impacts, item => Assert.Contains(item.Path, discovered));
        Assert.All(report.NativeImpacts, item => Assert.Equal("candidate", item.Status));
    }

    [Fact]
    public void Analyze_StopsNativeCyclesAndDeduplicatesRelations()
    {
        var semantic = new CSharpSemanticAnalysis(0, 0, 0, 0, Array.Empty<CSharpResolvedCall>(),
            Array.Empty<CSharpUnresolvedCall>(), Array.Empty<string>());
        var native = new NativeFlowAnalysis(2, 3, 0, 0, 0, new[]
        {
            new NativeFlowRelation("direct", "qualified-native-call", "candidate", "native/Caller.cpp", 4,
                "CNative::Caller", "native/Changed.cpp", 2, "CBridge::Changed", null, 1, "CBridge::Changed()"),
            new NativeFlowRelation("cycle-a", "qualified-native-call", "candidate", "native/Caller.cpp", 5,
                "CNative::Caller", "native/Caller.cpp", 1, "CNative::Other", null, 1, "CNative::Other()"),
            new NativeFlowRelation("cycle-b", "qualified-native-call", "candidate", "native/Caller.cpp", 6,
                "CNative::Other", "native/Caller.cpp", 1, "CNative::Caller", null, 1, "CNative::Caller()")
        }, Array.Empty<string>());

        var report = CallImpactAnalyzer.Analyze(".", new[] { "native/Changed.cpp" },
            new[] { "native/Changed.cpp", "native/Caller.cpp" }, semantic, 100, native);

        Assert.True(report.IsComplete);
        Assert.Equal(3, report.NativeImpacts.Count);
        Assert.Equal(3, report.NativeImpacts.Select(item => item.RelationshipId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3, report.NativeImpacts.Max(item => item.Depth));
    }

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
    public void Analyze_TracesConstructorRelationshipsThroughFactoryAndTestCallers()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace Demo; public class Core<T> { public Core(int value) { } }", path: "src/Core.cs", cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class Factory { public Core<int> Create() => new Core<int>(1); }", path: "src/Factory.cs", cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class CoreTests { public Core<int> Create(Factory factory) => factory.Create(); }", path: "tests/CoreTests.cs", cancellationToken: TestContext.Current.CancellationToken)
        };
        var compilation = CSharpCompilation.Create("ConstructorImpactFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semantic = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("fixture.csproj", (Compilation)compilation) }, 1, 100, ".");

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/Core.cs" },
            new[] { "src/Core.cs", "src/Factory.cs", "tests/CoreTests.cs" }, semantic, 100);

        Assert.True(report.IsComplete);
        Assert.Contains(report.Impacts, item => item.Path == "src/Factory.cs" && item.Depth == 1
            && item.RelationshipKind == "constructor");
        Assert.Contains(report.Impacts, item => item.Path == "tests/CoreTests.cs" && item.Depth == 2
            && item.RelationshipKind == "method");
        Assert.Contains("constructs", CallImpactReportRenderer.Render(report), StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_TracesConstructionFromImplicitAndPrimaryConstructorTypeFiles()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("namespace Demo; public class ImplicitTarget { }", path: "src/ImplicitTarget.cs",
                cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class PrimaryTarget(int id) { }", path: "src/PrimaryTarget.cs",
                cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText("namespace Demo; public class Caller { public ImplicitTarget CreateImplicit() => new ImplicitTarget(); public PrimaryTarget CreatePrimary() => new PrimaryTarget(42); }",
                path: "src/Caller.cs", cancellationToken: TestContext.Current.CancellationToken)
        };
        var compilation = CSharpCompilation.Create("ConstructorTypeImpactFixture", trees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semantic = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("ConstructorTypeImpactFixture.csproj", (Compilation)compilation) }, 1, 100, ".");

        var report = CallImpactAnalyzer.Analyze(".", new[] { "src/ImplicitTarget.cs", "src/PrimaryTarget.cs" },
            new[] { "src/ImplicitTarget.cs", "src/PrimaryTarget.cs", "src/Caller.cs" }, semantic, 100);

        Assert.True(report.IsComplete);
        Assert.Equal(2, report.Impacts.Count);
        Assert.Contains(report.Impacts, item => item.Path == "src/Caller.cs" && item.Caller.Contains("CreateImplicit", StringComparison.Ordinal)
            && item.Depth == 1 && item.RelationshipKind == "constructor");
        Assert.Contains(report.Impacts, item => item.Path == "src/Caller.cs" && item.Caller.Contains("CreatePrimary", StringComparison.Ordinal)
            && item.Depth == 1 && item.RelationshipKind == "constructor");
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
            new[] { "src/Core.cs", "native/file.cpp" }, CompileFixtureGraph(), 100);

        Assert.False(report.IsComplete);
        Assert.Contains(report.Issues, issue => issue.Contains("not found", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("native flow evidence was unavailable", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Contains("repository-relative", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_HandlesCyclesDuplicateEdgesAndMultipleChangedPaths()
    {
        var semantic = CompileFixtureGraph();
        var declarations = semantic.Declarations!;
        var core = declarations.Single(item => item.Path == "src/Core.cs"
            && item.Symbol.Contains("Core.Save()", StringComparison.Ordinal)).Symbol;
        var service = declarations.Single(item => item.Path == "src/Service.cs"
            && item.Symbol.Contains("Service.Run", StringComparison.Ordinal)).Symbol;
        var originalEdge = semantic.ResolvedCalls.Single(item => item.Target == core);
        semantic = semantic with
        {
            ResolvedCalls = semantic.ResolvedCalls.Concat(new[]
            {
                originalEdge,
                new CSharpResolvedCall("fixture.csproj", "src/Core.cs", 1, "recursive", core, service)
            }).ToArray(),
            ImpactGraphCalls = semantic.ImpactGraphCalls!.Concat(new[]
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
        Assert.Equal("method", parsed.RootElement.GetProperty("Impacts")[0].GetProperty("RelationshipKind").GetString());
        Assert.Equal("managed-compiler-resolved", parsed.RootElement.GetProperty("Impacts")[0].GetProperty("EvidenceKind").GetString());
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
