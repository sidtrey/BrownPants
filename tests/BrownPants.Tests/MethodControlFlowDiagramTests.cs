using BrownPants.Cli;
using BrownPants.Cli.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using System.Text;
using System.Xml.Linq;

namespace BrownPants.Tests;

public sealed class MethodControlFlowDiagramTests
{
    private const string CSharpSource = """
        namespace Demo;
        public sealed class Work
        {
            public int Run(int value)
            {
                var result = 0;
                try
                {
                    if (value < 0) return -1;
                    switch (value)
                    {
                        case 0: return 0;
                        case > 10 when value < 100: result = 2; break;
                        default: result = 3; break;
                    }
                    while (result < value)
                    {
                        if (result == 4) break;
                        result++;
                    }
                    if (result > 20) throw new System.Exception();
                    return result;
                }
                catch (System.Exception)
                {
                    throw;
                }
                finally
                {
                    result = 0;
                }
            }
        }
        """;

    private const string VisualBasicSource = """
        Namespace Demo
            Public NotInheritable Class Work
                Public Function Run(value As Integer) As Integer
                    Dim result = 0
                    Try
                        If value < 0 Then Return -1
                        Select Case value
                            Case 0
                                Return 0
                            Case Is > 10
                                result = 2
                            Case Else
                                result = 3
                        End Select
                        While result < value
                            If result = 4 Then Exit While
                            result += 1
                        End While
                        If result > 20 Then Throw New System.Exception()
                        Return result
                    Catch ex As System.Exception
                        Throw
                    Finally
                        result = 0
                    End Try
                End Function
            End Class
        End Namespace
        """;

    private const string LoopExitSource = """
        namespace Demo;
        public sealed class Work
        {
            public int Run(int value)
            {
                if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
                var sum = 0;
                for (var i = 0; i < value; i++)
                {
                    if (i == 3) continue;
                    sum += i;
                }
                return sum;
            }
        }
        """;

    [Fact]
    public void CSharpBehaviorChart_PreservesEveryRoslynBlockAndSuccessor()
    {
        var tree = CSharpSyntaxTree.ParseText(CSharpSource, path: "src/Work.cs",
            cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("FlowFixture", [tree], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semantic = Analyze(compilation, "src/Work.cs");
        var snapshot = Assert.IsType<MethodControlFlowSnapshot>(semantic.RequestedControlFlow);
        var result = MethodControlFlowDiagramAnalyzer.Create(snapshot);

        AssertEveryBlockAndBranchIsRepresented(snapshot, result);
        AssertDeterministicChartOutput(result.Document);
        AssertEditableXmlHasUniqueCellIdsAndValidEdgeTargets(result.Document);
        Assert.True(result.DecisionCount >= 4);
        Assert.True(result.ExplicitThrowCount >= 1);
        Assert.Contains(result.Document.Edges, edge => edge.Label == "true");
        Assert.Contains(result.Document.Edges, edge => edge.Label == "false");
        Assert.Contains(result.Document.Edges, edge => edge.Label == "return");
        Assert.Contains(result.Document.Edges, edge => edge.Label is "throw" or "rethrow");
        Assert.Contains("Implicit runtime exceptions", result.Summary, StringComparison.Ordinal);
        Assert.All(result.Document.Nodes, node => Assert.DoesNotContain("...", node.Label, StringComparison.Ordinal));
        Assert.All(result.Document.Nodes, node => Assert.All(node.Label, character => Assert.InRange(character, ' ', '~')));
        Assert.All(result.Document.Nodes, node => Assert.All(node.Label.Split(' '), word => Assert.True(word.Length <= 22)));
    }

    [Fact]
    public void BehaviorChart_RoutesContinueAndTerminalPathsWithoutConnectorCrossings()
    {
        var tree = CSharpSyntaxTree.ParseText(LoopExitSource, path: "src/Work.cs",
            cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("LoopFlowFixture", [tree], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var snapshot = Assert.IsType<MethodControlFlowSnapshot>(Analyze(compilation, "src/Work.cs").RequestedControlFlow);
        var result = MethodControlFlowDiagramAnalyzer.Create(snapshot);

        Assert.Contains(result.Document.Edges, edge => edge.Label == "true"
            && edge.Details?.Contains("branch semantics: Regular", StringComparison.Ordinal) == true);
        var layout = LocalFlowchartLayoutEngine.Create(result.Document);

        Assert.DoesNotContain(layout.Diagnostics ?? Array.Empty<LocalFlowchartLayoutDiagnostic>(),
            diagnostic => diagnostic.Code == "connector-crossings");
        AssertEditableXmlHasUniqueCellIdsAndValidEdgeTargets(result.Document);
    }

    [Fact]
    public void VisualBasicBehaviorChart_PreservesEveryRoslynBlockAndSuccessor()
    {
        var tree = VisualBasicSyntaxTree.ParseText(VisualBasicSource, path: "src/Work.vb",
            cancellationToken: TestContext.Current.CancellationToken);
        var compilation = VisualBasicCompilation.Create("FlowFixture", [tree], References(),
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semantic = Analyze(compilation, "src/Work.vb");
        var snapshot = Assert.IsType<MethodControlFlowSnapshot>(semantic.RequestedControlFlow);
        var result = MethodControlFlowDiagramAnalyzer.Create(snapshot);

        Assert.Equal(LanguageNames.VisualBasic, snapshot.Language);
        AssertEveryBlockAndBranchIsRepresented(snapshot, result);
        AssertDeterministicChartOutput(result.Document);
        AssertEditableXmlHasUniqueCellIdsAndValidEdgeTargets(result.Document);
        Assert.True(result.DecisionCount >= 4);
        Assert.True(result.ExplicitThrowCount >= 1);
        Assert.All(result.Document.Nodes, node => Assert.DoesNotContain("...", node.Label, StringComparison.Ordinal));
        Assert.Contains(result.Document.Edges, edge => edge.Label == "true");
        Assert.Contains(result.Document.Edges, edge => edge.Label == "false");
        Assert.Contains(result.Document.Edges, edge => edge.Label is "throw" or "rethrow");
    }

    [Fact]
    public void VisualBasicBehaviorChart_LoadsAnSdkProjectThroughMsBuildWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), $"BrownPants-VbFlow-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src"));
        try
        {
            File.WriteAllText(Path.Combine(root, "Demo.vbproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <RootNamespace></RootNamespace>
                  </PropertyGroup>
                </Project>
                """, Encoding.UTF8);
            File.WriteAllText(Path.Combine(root, "src", "Work.vb"), VisualBasicSource, Encoding.UTF8);

            var semantic = CSharpSemanticAnalyzer.AnalyzeRepository(root, ["Demo.vbproj"], [], 100,
                "src/Work.vb", "Global.Demo.Work.Run(Integer)");

            Assert.Equal(1, semantic.ProjectsLoaded);
            Assert.True(semantic.ProjectCoverageComplete, string.Join(Environment.NewLine, semantic.Issues));
            var snapshot = Assert.IsType<MethodControlFlowSnapshot>(semantic.RequestedControlFlow);
            Assert.Equal(LanguageNames.VisualBasic, snapshot.Language);
            AssertEveryBlockAndBranchIsRepresented(snapshot, MethodControlFlowDiagramAnalyzer.Create(snapshot));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ChartCommands_ApplyThemesToCallerAndMethodBehaviorCharts()
    {
        var root = Path.Combine(Path.GetTempPath(), $"BrownPants-ThemeCharts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src"));
        try
        {
            File.WriteAllText(Path.Combine(root, "Demo.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                </Project>
                """, Encoding.UTF8);
            File.WriteAllText(Path.Combine(root, "src", "Work.cs"), """
                namespace Demo;
                public sealed class Work
                {
                    public int Run(int value)
                    {
                        if (value > 2) return Transform(value);
                        return Transform(value + 1);
                    }
                    private int Transform(int input) => input + 1;
                }
                """, Encoding.UTF8);
            var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ChartThemes", "blue-water.json");
            Directory.CreateDirectory(Path.Combine(root, "themes"));
            File.Copy(fixture, Path.Combine(root, "themes", "team.json"));
            var declarations = CSharpSemanticAnalyzer.AnalyzeRepository(root, ["Demo.csproj"], [], 100).Declarations!;
            var target = Assert.Single(declarations, declaration => declaration.Symbol.Contains("Transform", StringComparison.Ordinal));
            var method = Assert.Single(declarations, declaration => declaration.Symbol.Contains("Run", StringComparison.Ordinal));

            var callerOutput = new StringWriter();
            var callerError = new StringWriter();
            var callerExit = Program.Run(["chart", "callers", root, "--source", "src/Work.cs", "--method", target.Symbol,
                "--semantic", "--output", "out/callers.png", "--theme", "blue-water"], callerOutput, callerError);
            Assert.True(callerExit == 0, callerError.ToString());
            var callerXml = XDocument.Load(Path.Combine(root, "out", "callers.drawio"));
            Assert.Equal("#eaf6fa", (string?)callerXml.Descendants("mxGraphModel").Single().Attribute("background"));

            var flowOutput = new StringWriter();
            var flowError = new StringWriter();
            var flowExit = Program.Run(["chart", "flow", root, "--source", "src/Work.cs", "--method", method.Symbol,
                "--semantic", "--output", "out/flow.png", "--theme-file", "themes/team.json"], flowOutput, flowError);
            Assert.True(flowExit == 0, flowError.ToString());
            var flowXml = XDocument.Load(Path.Combine(root, "out", "flow.drawio"));
            Assert.Equal("#eaf6fa", (string?)flowXml.Descendants("mxGraphModel").Single().Attribute("background"));
            Assert.Contains(flowXml.Descendants("mxCell"), cell => (string?)cell.Attribute("vertex") == "1");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BehaviorChart_RejectsUnavailableOrIncompleteGraphInsteadOfGuessing()
    {
        var failed = new MethodControlFlowSnapshot("global::Demo.Work.Run()", "src/Work.cs", 1,
            LanguageNames.CSharp, Array.Empty<MethodControlFlowBlock>(), "Roslyn CFG unavailable.");

        var exception = Assert.Throws<LocalFlowchartFormatException>(() => MethodControlFlowDiagramAnalyzer.Create(failed));

        Assert.Contains("Roslyn CFG unavailable", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BehaviorChart_CommandRequiresSemanticOptInAndRejectsSourceTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"BrownPants-FlowCommand-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var outputPath = Path.Combine(root, "out", "flow.png");
            var args = new[] { "chart", "flow", root, "--source", "src/Work.cs", "--method",
                "global::Demo.Work.Run()", "--output", "out/flow.png" };
            var optInError = new StringWriter();

            Assert.Equal(2, Program.Run(args, TextWriter.Null, optInError));
            Assert.Contains("explicit --semantic", optInError.ToString(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));

            var traversalError = new StringWriter();
            var traversalArgs = args.Concat(new[] { "--semantic" }).ToArray();
            traversalArgs[4] = "../outside.cs";
            Assert.Equal(1, Program.Run(traversalArgs, TextWriter.Null, traversalError));
            Assert.Contains("repository-relative", traversalError.ToString(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static CSharpSemanticAnalysis Analyze(Compilation compilation, string sourcePath)
    {
        var firstPass = CSharpSemanticAnalyzer.AnalyzeCompilations(
            [("fixture.csproj", compilation)], 1, 100, ".");
        var method = Assert.Single(firstPass.Declarations!, declaration => declaration.Path == sourcePath
            && declaration.Symbol.Contains("Demo.Work.Run", StringComparison.Ordinal));
        return CSharpSemanticAnalyzer.AnalyzeCompilations(
            [("fixture.csproj", compilation)], 1, 100, ".", 100_000, sourcePath, method.Symbol);
    }

    private static MetadataReference[] References() =>
        [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];

    private static int IsExplicitTerminal(MethodControlFlowBranch? branch) =>
        branch?.DestinationOrdinal is null
            && branch?.Semantics is "Throw" or "Rethrow" or "StructuredExceptionHandling" or "ProgramTermination" ? 1 : 0;

    private static void AssertEveryBlockAndBranchIsRepresented(MethodControlFlowSnapshot snapshot,
        MethodControlFlowDiagramResult result)
    {
        var expectedVisibleEdges = 0;
        foreach (var block in snapshot.Blocks)
        {
            var owner = Assert.Single(result.Document.Nodes, node =>
                node.Details?.Contains($"\nB{block.Ordinal} (", StringComparison.Ordinal) == true);
            Assert.Contains($"src/Work.{(snapshot.Language == LanguageNames.CSharp ? "cs" : "vb")}:{block.SourceLine}",
                owner.Details!, StringComparison.Ordinal);
            foreach (var (branch, name) in new[]
            {
                (block.Conditional, "Conditional successor"),
                (block.FallThrough, "Fall-through successor")
            })
            {
                if (branch is null) continue;
                var destination = branch.DestinationOrdinal is int target ? $"B{target}" : "none";
                Assert.Contains($"{name}: {destination} ({branch.Semantics})", owner.Details!, StringComparison.Ordinal);
                if (branch.DestinationOrdinal is not int targetOrdinal)
                {
                    if (IsExplicitTerminal(branch) == 1) expectedVisibleEdges++;
                    continue;
                }

                var targetNode = Assert.Single(result.Document.Nodes, node =>
                    node.Details?.Contains($"\nB{targetOrdinal} (", StringComparison.Ordinal) == true);
                if (owner.Id != targetNode.Id)
                {
                    expectedVisibleEdges++;
                    Assert.Contains(result.Document.Edges, edge => edge.FromId == owner.Id
                        && edge.ToId == targetNode.Id
                        && edge.Details?.Contains(branch.Semantics, StringComparison.Ordinal) == true);
                }
            }
        }
        Assert.Equal(expectedVisibleEdges, result.Document.Edges.Count);
    }

    private static void AssertEditableXmlHasUniqueCellIdsAndValidEdgeTargets(LocalFlowchartDocument document)
    {
        var xml = XDocument.Parse(Encoding.UTF8.GetString(LocalFlowchartDrawioWriter.Write(document)));
        var cells = xml.Descendants("mxCell")
            .Where(cell => cell.Attribute("vertex")?.Value == "1" || cell.Attribute("edge")?.Value == "1")
            .ToArray();
        var ids = cells.Select(cell => (string?)cell.Attribute("id")).ToArray();
        Assert.All(ids, id => Assert.False(string.IsNullOrWhiteSpace(id)));
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());

        var nodeIds = cells.Where(cell => cell.Attribute("vertex")?.Value == "1")
            .Select(cell => (string)cell.Attribute("id")!).ToHashSet(StringComparer.Ordinal);
        foreach (var edge in cells.Where(cell => cell.Attribute("edge")?.Value == "1"))
        {
            Assert.Contains((string)edge.Attribute("source")!, nodeIds);
            Assert.Contains((string)edge.Attribute("target")!, nodeIds);
        }
    }

    private static void AssertDeterministicChartOutput(LocalFlowchartDocument document)
    {
        Assert.Equal(LocalFlowchartPngRenderer.Render(document), LocalFlowchartPngRenderer.Render(document));
        Assert.Equal(LocalFlowchartDrawioWriter.Write(document), LocalFlowchartDrawioWriter.Write(document));
    }
}
