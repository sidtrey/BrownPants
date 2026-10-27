using System.Text.Json;
using BrownPants.Cli;
using BrownPants.Cli.Analysis;

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
        Assert.False(report.TryGetProperty("focus", out _));
        var findings = report.GetProperty("findings").EnumerateArray().ToArray();
        Assert.Equal(findings.Length, report.GetProperty("completeness").GetProperty("emittedFindingCount").GetInt32());
        Assert.Equal(findings.Length, report.GetProperty("completeness").GetProperty("totalFindingCount").GetInt32());
        var factory = Assert.Single(findings, item => item.GetProperty("signal").GetString() == "Factory type");
        Assert.Equal("architecture-candidate", factory.GetProperty("category").GetString());
        Assert.Equal("candidate", factory.GetProperty("status").GetString());
        Assert.Equal("src/OrderFactory.cs", factory.GetProperty("path").GetString());
        Assert.False(factory.TryGetProperty("matchedBy", out _));
        var test = Assert.Single(findings, item => item.GetProperty("category").GetString() == "test-identification");
        Assert.Equal("tests/OrderServiceTests.cs", test.GetProperty("path").GetString());
        Assert.Equal("candidate", test.GetProperty("status").GetString());
        Assert.Contains(report.GetProperty("limitations").EnumerateArray(), item =>
            item.GetString()!.Contains("Semantic analysis was not requested", StringComparison.Ordinal));
    }

    [Fact]
    public void Evidence_RepeatedCandidateGroupsPreserveRawFindings()
    {
        Write("src/SessionA.cs", """
            using NHibernate;
            public class SessionA
            {
                private ISession first;
                private ISession second;
                private ITransaction transaction;
            }
            """);
        Write("src/SessionB.cs", "using NHibernate; public class SessionB { private ISession other; }");

        var first = Run("evidence", _root, "--limit", "1000");
        var second = Run("evidence", _root, "--limit", "1000");

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(first.Output, second.Output);
        using var json = JsonDocument.Parse(first.Output);
        var report = json.RootElement;
        Assert.Equal(1, report.GetProperty("schemaVersion").GetInt32());
        var rawSessions = report.GetProperty("findings").EnumerateArray()
            .Where(item => item.GetProperty("category").GetString() == "framework-candidate"
                && item.GetProperty("signal").GetString() == "NHibernate session"
                && item.GetProperty("path").GetString() == "src/SessionA.cs")
            .ToArray();
        Assert.Equal(2, rawSessions.Length);
        var group = Assert.Single(report.GetProperty("repeatedCandidateGroups").EnumerateArray(), item =>
            item.GetProperty("category").GetString() == "framework-candidate"
            && item.GetProperty("signal").GetString() == "NHibernate session"
            && item.GetProperty("path").GetString() == "src/SessionA.cs");
        Assert.Equal(rawSessions.Select(item => item.GetProperty("line").GetInt32()).Order().ToArray(),
            group.GetProperty("locations").EnumerateArray().Select(item => item.GetProperty("line").GetInt32()).ToArray());
        Assert.Equal(rawSessions.Select(item => (Id: item.GetProperty("id").GetString(), Line: item.GetProperty("line").GetInt32()))
                .OrderBy(item => item.Line).ThenBy(item => item.Id, StringComparer.Ordinal).ToArray(),
            group.GetProperty("locations").EnumerateArray().Select(item =>
                (Id: item.GetProperty("findingId").GetString(), Line: item.GetProperty("line").GetInt32())).ToArray());
        Assert.DoesNotContain(report.GetProperty("repeatedCandidateGroups").EnumerateArray(), item =>
            item.GetProperty("signal").GetString() == "NHibernate session" && item.GetProperty("path").GetString() == "src/SessionB.cs");
    }

    [Fact]
    public void Evidence_FocusedRepeatedCandidateGroupsContainOnlySelectedFindingIdsAndKeepReasons()
    {
        Write("src/SessionA.cs", """
            using NHibernate;
            public class SessionA
            {
                private ISession first;
                private ISession second;
            }
            """);
        Write("src/SessionB.cs", "using NHibernate; public class SessionB { private ISession other; }");

        var result = Run("evidence", _root, "--clue", "SessionA", "--changed", "src/SessionA.cs", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var report = json.RootElement;
        var findings = report.GetProperty("findings").EnumerateArray().ToArray();
        var group = Assert.Single(report.GetProperty("repeatedCandidateGroups").EnumerateArray(), item =>
            item.GetProperty("signal").GetString() == "NHibernate session");
        var selectedIds = findings.Select(item => item.GetProperty("id").GetString()).ToHashSet(StringComparer.Ordinal);
        var locations = group.GetProperty("locations").EnumerateArray().ToArray();
        Assert.Equal(2, locations.Length);
        Assert.All(locations, location => Assert.Contains(location.GetProperty("findingId").GetString(), selectedIds));
        Assert.All(findings.Where(item => locations.Any(location =>
            location.GetProperty("findingId").GetString() == item.GetProperty("id").GetString())), item =>
        {
            var reasons = item.GetProperty("matchedBy").EnumerateArray().Select(reason => reason.GetString()!).ToArray();
            Assert.Contains(reasons, reason => reason.StartsWith("changed-path:", StringComparison.Ordinal));
            Assert.Contains(reasons, reason => reason.StartsWith("clue[0]:", StringComparison.Ordinal));
        });
        Assert.Equal(findings.Where(item => item.GetProperty("signal").GetString() == "NHibernate session")
                .Select(item => item.GetProperty("id").GetString()).Order(StringComparer.Ordinal).ToArray(),
            locations.Select(location => location.GetProperty("findingId").GetString()).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void RepeatedCandidateGroups_KeepDifferentCategoriesSignalsAndPathsSeparate()
    {
        var findings = new[]
        {
            Candidate("a1", "architecture-candidate", "Shared signal", "src/One.cs", 1),
            Candidate("a2", "architecture-candidate", "Shared signal", "src/One.cs", 2),
            Candidate("f1", "framework-candidate", "Shared signal", "src/One.cs", 3),
            Candidate("f2", "framework-candidate", "Shared signal", "src/One.cs", 4),
            Candidate("s1", "framework-candidate", "Other signal", "src/One.cs", 5),
            Candidate("s2", "framework-candidate", "Other signal", "src/One.cs", 6),
            Candidate("p1", "framework-candidate", "Other signal", "src/Two.cs", 7),
            Candidate("p2", "framework-candidate", "Other signal", "src/Two.cs", 8),
            Candidate("resolved", "framework-candidate", "Other signal", "src/Two.cs", 9, "resolved")
        };

        var groups = EvidenceReportAnalyzer.CreateRepeatedCandidateGroups(findings);

        Assert.Equal(4, groups.Count);
        Assert.Equal(4, groups.Select(group => (group.Category, group.Signal, group.Path)).Distinct().Count());
        Assert.Equal(new[]
        {
            ("architecture-candidate", "Shared signal", "src/One.cs"),
            ("framework-candidate", "Other signal", "src/One.cs"),
            ("framework-candidate", "Shared signal", "src/One.cs"),
            ("framework-candidate", "Other signal", "src/Two.cs")
        }, groups.Select(group => (group.Category, group.Signal, group.Path)));
        Assert.All(groups, group => Assert.Equal(2, group.LocationCount));
        Assert.Equal(new[] { 1, 2 }, Assert.Single(groups, group => group.Category == "architecture-candidate").Locations.Select(location => location.Line));
        Assert.Equal(new[] { 3, 4 }, Assert.Single(groups, group => group.Category == "framework-candidate" && group.Signal == "Shared signal").Locations.Select(location => location.Line));
        Assert.DoesNotContain(groups.SelectMany(group => group.Locations), location => location.FindingId == "resolved");

        static EvidenceFinding Candidate(string id, string category, string signal, string path, int line, string status = "candidate") =>
            new(id, category, status, signal, path, line, null, null, "fixture evidence");
    }

    [Fact]
    public void Evidence_FocusByClueReturnsOnlyLocatedMatchesAndUnmatchedClue()
    {
        Write("src/OrderFactory.cs", "namespace Sample; public class OrderFactory { public object Create() => new(); }");
        Write("src/UnrelatedFactory.cs", "namespace Sample; public class UnrelatedFactory { }");
        Write("tests/OrderFactoryTests.cs", "namespace Sample.Tests; public class OrderFactoryTests { }");

        var result = Run("evidence", _root, "--clue", "OrderFactory", "--clue", "missing-symbol", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var report = json.RootElement;
        var focus = report.GetProperty("focus");
        Assert.Equal(2, focus.GetProperty("clues").GetArrayLength());
        Assert.True(focus.GetProperty("clues")[0].GetProperty("matched").GetBoolean());
        Assert.False(focus.GetProperty("clues")[1].GetProperty("matched").GetBoolean());
        Assert.Equal(focus.GetProperty("totalMatchedFindingCount").GetInt32(),
            focus.GetProperty("emittedFindingCount").GetInt32() + focus.GetProperty("omittedFindingCount").GetInt32());
        var findings = report.GetProperty("findings").EnumerateArray().ToArray();
        Assert.NotEmpty(findings);
        Assert.All(findings, finding => Assert.True(finding.GetProperty("matchedBy").GetArrayLength() > 0));
        Assert.DoesNotContain(findings, finding => finding.GetProperty("path").GetString() == "src/UnrelatedFactory.cs");
        Assert.Contains(findings, finding => finding.GetProperty("path").GetString() == "src/OrderFactory.cs");
    }

    [Fact]
    public void Evidence_FocusByContentClueIncludesFileLevelTestCandidate()
    {
        Write("tests/OrderImportTests.cs", "namespace Sample.Tests; public class OrderImportTests { public void Reproduces() { const string symptom = \"UniqueImportFailure42\"; } }");

        var result = Run("evidence", _root, "--clue", "UniqueImportFailure42", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var test = Assert.Single(json.RootElement.GetProperty("findings").EnumerateArray());
        Assert.Equal("test-identification", test.GetProperty("category").GetString());
        Assert.Equal("tests/OrderImportTests.cs", test.GetProperty("path").GetString());
        Assert.Contains(test.GetProperty("matchedBy").EnumerateArray(), reason =>
            reason.GetString()!.EndsWith(":content-file", StringComparison.Ordinal));
    }

    [Fact]
    public void Evidence_FocusIncludesBoundedClueLocationsWhenNoFindingsMatch()
    {
        for (var index = 0; index < 12; index++)
            Write($"docs/lead-{index:D2}.txt", "UniqueContextMarker");
        Write("config/UniqueContextMarker.json", "{}");

        var result = Run("evidence", _root, "--clue", "UniqueContextMarker", "--limit", "2");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var report = json.RootElement;
        Assert.Empty(report.GetProperty("findings").EnumerateArray());
        var clue = Assert.Single(report.GetProperty("focus").GetProperty("clues").EnumerateArray());
        Assert.Equal(13, clue.GetProperty("matchCount").GetInt32());
        Assert.Equal(0, clue.GetProperty("findingCount").GetInt32());
        Assert.Equal(11, clue.GetProperty("omittedMatchCount").GetInt32());
        var matches = clue.GetProperty("matches").EnumerateArray().ToArray();
        Assert.Equal(2, matches.Length);
        Assert.Equal("Path", matches[0].GetProperty("kind").GetString());
        Assert.Equal("config/UniqueContextMarker.json", matches[0].GetProperty("path").GetString());
        Assert.Equal("Content", matches[1].GetProperty("kind").GetString());
        Assert.Equal("docs/lead-00.txt", matches[1].GetProperty("path").GetString());
        Assert.False(matches[0].GetProperty("isTestCandidate").GetBoolean());
        Assert.False(report.GetProperty("focus").GetProperty("clueMatchesTruncated").GetBoolean());

        var repeated = Run("evidence", _root, "--clue", "UniqueContextMarker", "--limit", "1000");
        Assert.Equal(0, repeated.ExitCode);
        var repeatedSmall = Run("evidence", _root, "--clue", "UniqueContextMarker", "--limit", "2");
        Assert.Equal(0, repeatedSmall.ExitCode);
        Assert.Equal(result.Output, repeatedSmall.Output);
        using var cappedJson = JsonDocument.Parse(repeated.Output);
        var cappedClue = Assert.Single(cappedJson.RootElement.GetProperty("focus").GetProperty("clues").EnumerateArray());
        Assert.Equal(10, cappedClue.GetProperty("matches").GetArrayLength());
        Assert.Equal(3, cappedClue.GetProperty("omittedMatchCount").GetInt32());
    }

    [Fact]
    public void Evidence_FocusedClueReportsBoundedReadIssueCodes()
    {
        Write("src/OrderFactory.cs", "namespace Sample; public class OrderFactory { }");
        var binaryPath = Path.Combine(_root, "docs", "diagram.png");
        Directory.CreateDirectory(Path.GetDirectoryName(binaryPath)!);
        File.WriteAllBytes(binaryPath, [0x89, 0x50, 0x4e, 0x47, 0x00, 0x01]);

        var result = Run("evidence", _root, "--clue", "OrderFactory", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var limitations = json.RootElement.GetProperty("limitations").EnumerateArray()
            .Select(item => item.GetString()!).ToArray();
        Assert.Contains(limitations, item => item.Contains("BinaryFileSkipped", StringComparison.Ordinal));
        Assert.Contains(limitations, item => item.Contains("1 file(s)", StringComparison.Ordinal));
    }

    [Fact]
    public void Evidence_FocusRejectsAbsoluteChangedPath()
    {
        Write("src/Store.cs", "namespace Sample; public class Store { }");

        var result = Run("evidence", _root, "--changed", Path.Combine(_root, "src", "Store.cs"));

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("repository-relative", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evidence_FocusBoundsChangedPathSelectors()
    {
        var args = new List<string> { "evidence", _root };
        for (var index = 0; index < 21; index++)
        {
            args.Add("--changed");
            args.Add($"src/Store{index}.cs");
        }

        var result = Run(args.ToArray());

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("no more than 20 --changed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evidence_CombinesCluesAndChangedPathsAsUnionWithoutDuplicatingFindings()
    {
        Write("src/OrderFactory.cs", "namespace Sample; public class OrderFactory { public object Create() => new(); }");
        Write("src/UnrelatedFactory.cs", "namespace Sample; public class UnrelatedFactory { }");
        Write("tests/OrderFactoryTests.cs", "namespace Sample.Tests; public class OrderFactoryTests { }");

        var result = Run("evidence", _root, "--clue", "OrderFactory", "--changed", "src/OrderFactory.cs", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var report = json.RootElement;
        var findings = report.GetProperty("findings").EnumerateArray().ToArray();
        Assert.Equal(findings.Length, findings.Select(item => item.GetProperty("id").GetString()).Distinct().Count());
        Assert.Contains(findings, finding =>
            finding.GetProperty("path").GetString() == "src/OrderFactory.cs"
            && finding.GetProperty("matchedBy").EnumerateArray().Any(reason => reason.GetString()!.StartsWith("clue[0]:", StringComparison.Ordinal))
            && finding.GetProperty("matchedBy").EnumerateArray().Any(reason => reason.GetString()!.StartsWith("changed-path:", StringComparison.Ordinal)));
        Assert.Contains(findings, finding => finding.GetProperty("path").GetString() == "tests/OrderFactoryTests.cs");
    }

    [Fact]
    public void Evidence_FocusedLimitReportsSelectedAndOmittedFindingCounts()
    {
        Write("src/OrderFactory.cs", "namespace Sample; public class OrderFactory { public object Create() => new(); }");

        var result = Run("evidence", _root, "--clue", "OrderFactory", "--limit", "1");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var focus = json.RootElement.GetProperty("focus");
        Assert.Equal(1, focus.GetProperty("emittedFindingCount").GetInt32());
        Assert.True(focus.GetProperty("omittedFindingCount").GetInt32() > 0);
        Assert.Equal(focus.GetProperty("totalMatchedFindingCount").GetInt32(),
            focus.GetProperty("emittedFindingCount").GetInt32() + focus.GetProperty("omittedFindingCount").GetInt32());
        Assert.True(json.RootElement.GetProperty("completeness").GetProperty("findingsTruncated").GetBoolean());
    }

    [Fact]
    public void Evidence_FocusByChangedPathIncludesTransitiveCallersAndRelatedTestCandidate()
    {
        Write("Legacy.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        Write("src/Store.cs", "namespace Sample; public class Store { public void Save() { } }");
        Write("src/Runner.cs", "namespace Sample; public class Runner { public void Run(Store store) => store.Save(); }");
        Write("tests/StoreTests.cs", "namespace Sample.Tests; public class StoreTests { public void Saves() => new Sample.Runner().Run(new Sample.Store()); }");
        Write("src/Unrelated.cs", "namespace Sample; public class Unrelated { public void Ignore() { } }");

        var result = Run("evidence", _root, "--changed", "src/Store.cs", "--semantic", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var report = json.RootElement;
        var focus = report.GetProperty("focus");
        Assert.True(focus.GetProperty("semanticImpactAnalyzed").GetBoolean());
        Assert.Equal(0, focus.GetProperty("omittedFindingCount").GetInt32());
        var findings = report.GetProperty("findings").EnumerateArray().ToArray();
        var calls = findings.Where(finding => finding.GetProperty("category").GetString() == "managed-call").ToArray();
        Assert.Contains(calls, finding => finding.GetProperty("path").GetString() == "src/Runner.cs"
            && finding.GetProperty("targetSignature").GetString()!.Contains("Store.Save", StringComparison.Ordinal));
        Assert.Contains(calls, finding => finding.GetProperty("path").GetString() == "tests/StoreTests.cs"
            && finding.GetProperty("targetSignature").GetString()!.Contains("Runner.Run", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.GetProperty("category").GetString() == "test-identification"
            && finding.GetProperty("path").GetString() == "tests/StoreTests.cs"
            && finding.GetProperty("matchedBy").EnumerateArray().Any(reason => reason.GetString()!.EndsWith(":test-impact", StringComparison.Ordinal)));
        Assert.DoesNotContain(findings, finding => finding.GetProperty("path").GetString() == "src/Unrelated.cs");
    }

    [Fact]
    public void Evidence_FocusByChangedPathWithoutSemanticModeReportsCandidatesOnly()
    {
        Write("src/OrderFactory.cs", "namespace Sample; public class OrderFactory { public object Create() => new(); }");
        Write("src/UnrelatedFactory.cs", "namespace Sample; public class UnrelatedFactory { }");

        var result = Run("evidence", _root, "--changed", "src/OrderFactory.cs", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var report = json.RootElement;
        Assert.False(report.GetProperty("focus").GetProperty("semanticImpactAnalyzed").GetBoolean());
        Assert.Contains(report.GetProperty("limitations").EnumerateArray(), item =>
            item.GetString()!.Contains("caller impact was not analyzed", StringComparison.OrdinalIgnoreCase));
        Assert.All(report.GetProperty("findings").EnumerateArray(), finding =>
            Assert.Equal("candidate", finding.GetProperty("status").GetString()));
        Assert.DoesNotContain(report.GetProperty("findings").EnumerateArray(), finding =>
            finding.GetProperty("path").GetString() == "src/UnrelatedFactory.cs");
    }

    [Fact]
    public void Evidence_FocusByChangedNativePathIncludesMfcImpactWithoutLoadingManagedProjects()
    {
        Write("legacy/Native/View.cpp", """
            void CView::OnImport() { CBridge::Import(); }
            BEGIN_MESSAGE_MAP(CView, CView)
                ON_COMMAND(ID_IMPORT, &CView::OnImport)
            END_MESSAGE_MAP()
            """);
        Write("legacy/Bridge/Bridge.cpp", "void CBridge::Import() { }");

        var result = Run("evidence", _root, "--changed", "legacy/Bridge/Bridge.cpp", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var focus = json.RootElement.GetProperty("focus");
        Assert.False(focus.GetProperty("semanticImpactAnalyzed").GetBoolean());
        Assert.True(focus.GetProperty("impactComplete").GetBoolean());
        var native = json.RootElement.GetProperty("findings").EnumerateArray()
            .Where(item => item.GetProperty("category").GetString() == "native-flow-candidate").ToArray();
        Assert.Equal(2, native.Length);
        Assert.All(native, item => Assert.Contains(item.GetProperty("matchedBy").EnumerateArray(), reason =>
            reason.GetString()!.EndsWith(":native-caller-impact", StringComparison.Ordinal)));
        Assert.Contains(native, item => item.GetProperty("relationKind").GetString() == "mfc-dispatch");
        Assert.Contains(native, item => item.GetProperty("relationKind").GetString() == "qualified-native-call");
    }

    [Fact]
    public void Evidence_FocusByChangedManagedTargetIncludesInteropAndUpstreamMfcCandidates()
    {
        Write("managed/Workflow.cs", "namespace Demo; public class Workflow { public void Run() { } }");
        Write("legacy/Bridge/Bridge.cpp", "using namespace Demo; void CBridge::Import() { Workflow^ workflow = gcnew Workflow(); workflow->Run(); }");
        Write("legacy/Native/View.cpp", "void CView::OnImport() { CBridge::Import(); }\nBEGIN_MESSAGE_MAP(CView, CView)\nON_COMMAND(ID_IMPORT, &CView::OnImport)\nEND_MESSAGE_MAP()");

        var result = Run("evidence", _root, "--changed", "managed/Workflow.cs", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var findings = json.RootElement.GetProperty("findings").EnumerateArray()
            .Where(item => item.GetProperty("category").GetString() == "native-flow-candidate").ToArray();
        Assert.Contains(findings, item => item.GetProperty("relationKind").GetString() == "cppcli-managed-call"
            && item.GetProperty("targetPath").GetString() == "managed/Workflow.cs"
            && item.GetProperty("matchedBy").EnumerateArray().Any(reason =>
                reason.GetString()!.EndsWith(":native-caller-impact", StringComparison.Ordinal)));
        Assert.Contains(findings, item => item.GetProperty("relationKind").GetString() == "qualified-native-call");
        Assert.Contains(findings, item => item.GetProperty("relationKind").GetString() == "mfc-dispatch");
    }

    [Fact]
    public void Evidence_FocusByChangedRepositoryIncludesOnlyNativeTraceForImpactedManagedCaller()
    {
        Write("Legacy.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        Write("src/Store.cs", "namespace Demo; public class Store { public void Save() { } }");
        Write("src/Workflow.cs", "namespace Demo; public class Workflow { private readonly Store _store = new(); public void Run() => _store.Save(); public void Ignore() { } }");
        Write("legacy/Bridge/Bridge.cpp", """
            using namespace Demo;
            public ref class CBridge
            {
            public:
                static void Import()
                {
                    Workflow^ workflow = gcnew Workflow();
                    workflow->Run();
                    workflow->Ignore();
                }
            };
            """);
        Write("legacy/Native/View.cpp", """
            void CView::OnImport() { CBridge::Import(); }
            BEGIN_MESSAGE_MAP(CView, CView)
                ON_COMMAND(ID_IMPORT, &CView::OnImport)
            END_MESSAGE_MAP()
            """);

        var result = Run("evidence", _root, "--changed", "src/Store.cs", "--semantic", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var findings = json.RootElement.GetProperty("findings").EnumerateArray().ToArray();
        var native = findings.Where(item => item.GetProperty("category").GetString() == "native-flow-candidate").ToArray();
        Assert.Contains(native, item => item.GetProperty("relationKind").GetString() == "cppcli-managed-call"
            && item.GetProperty("targetSymbol").GetString()!.Contains("Workflow.Run", StringComparison.Ordinal));
        Assert.DoesNotContain(native, item => item.GetProperty("targetSymbol").GetString()!.Contains("Workflow.Ignore", StringComparison.Ordinal));
        Assert.Contains(native, item => item.GetProperty("relationKind").GetString() == "qualified-native-call");
        Assert.Contains(native, item => item.GetProperty("relationKind").GetString() == "mfc-dispatch");
        Assert.All(native, item => Assert.Contains(item.GetProperty("matchedBy").EnumerateArray(), reason =>
            reason.GetString()!.EndsWith(":native-caller-impact", StringComparison.Ordinal)));
    }

    [Fact]
    public void Evidence_FocusPreservesDotPrefixedRepositoryPathSegments()
    {
        Write("Legacy.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
              <ItemGroup><Compile Include=".legacy/Store.cs" /><Compile Include="src/Runner.cs" /></ItemGroup>
            </Project>
            """);
        Write(".legacy/Store.cs", "namespace Sample; public class Store { public void Save() { } }");
        Write("src/Runner.cs", "namespace Sample; public class Runner { public void Run(Store store) => store.Save(); }");

        var result = Run("evidence", _root, "--changed", ".legacy/Store.cs", "--semantic", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal(".legacy/Store.cs", json.RootElement.GetProperty("focus").GetProperty("changedPaths")[0].GetProperty("path").GetString());
        Assert.Contains(json.RootElement.GetProperty("findings").EnumerateArray(), finding =>
            finding.GetProperty("path").GetString() == "src/Runner.cs"
            && finding.GetProperty("category").GetString() == "managed-call"
            && finding.GetProperty("targetSignature").GetString()!.Contains("Store.Save", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("--clue", " ")]
    [InlineData("--changed", "../outside.cs")]
    [InlineData("--changed", "missing.cs")]
    [InlineData("--changed", "src/image.png")]
    public void Evidence_RejectsInvalidFocusSelectors(string option, string value)
    {
        Directory.CreateDirectory(_root);
        var result = Run("evidence", _root, option, value);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("focus", result.Error, StringComparison.OrdinalIgnoreCase);
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
    public void Evidence_LabelsCompilerResolvedLocalDelegateTargets()
    {
        Write("DelegateFixture.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        Write("src/Calls.cs", "using System; namespace Sample; public class Store { public void Save() { } } public class Runner { private readonly Store _store = new(); public void Run() { Action callback = _store.Save; callback(); } }");

        var result = Run("evidence", _root, "--semantic", "--format", "json", "--limit", "1000");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var finding = Assert.Single(json.RootElement.GetProperty("findings").EnumerateArray(), item =>
            item.GetProperty("signal").GetString() == "resolved local delegate target");
        Assert.Equal("managed-call", finding.GetProperty("category").GetString());
        Assert.Equal("resolved", finding.GetProperty("status").GetString());
        Assert.Equal("src/Calls.cs", finding.GetProperty("path").GetString());
        Assert.Contains("Store.Save", finding.GetProperty("targetSignature").GetString());
        Assert.Contains("Runner.Run", finding.GetProperty("callerSignature").GetString());
    }

    [Fact]
    public void Evidence_EmitsUniqueSameLineConstructorFindings()
    {
        Write("Fixture.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        const string oneCall = "namespace Sample; public sealed class Box { public Box(int value) { } } public sealed class Item { public Item(int value) { } public void Save(int value) { } } public static class Runner { public static void Run() { _ = new Box(1); } }";
        Write("Calls.cs", oneCall);

        var singleCallResult = Run("evidence", _root, "--semantic", "--limit", "1000");

        Assert.Equal(0, singleCallResult.ExitCode);
        using var singleCallJson = JsonDocument.Parse(singleCallResult.Output);
        Assert.Equal(1, singleCallJson.RootElement.GetProperty("schemaVersion").GetInt32());
        var boxBefore = Assert.Single(singleCallJson.RootElement.GetProperty("findings").EnumerateArray(), finding =>
            finding.GetProperty("category").GetString() == "managed-call"
            && finding.GetProperty("path").GetString() == "Calls.cs"
            && finding.GetProperty("targetSignature").GetString()!.Contains("Sample.Box..ctor", StringComparison.Ordinal));
        var boxBeforeId = boxBefore.GetProperty("id").GetString();

        Write("Calls.cs", oneCall.Replace("new Box(1); }", "new Box(1); new Item(2).Save(4); new Item(2).Save(4); }", StringComparison.Ordinal));
        var fullResult = Run("evidence", _root, "--semantic", "--limit", "1000");
        var focusedResult = Run("evidence", _root, "--changed", "Calls.cs", "--semantic", "--limit", "1000");
        var repeatedFullResult = Run("evidence", _root, "--semantic", "--limit", "1000");

        Assert.Equal(0, fullResult.ExitCode);
        Assert.Equal(0, focusedResult.ExitCode);
        Assert.Equal(fullResult.Output, repeatedFullResult.Output);
        using var fullJson = JsonDocument.Parse(fullResult.Output);
        using var focusedJson = JsonDocument.Parse(focusedResult.Output);
        Assert.Equal(1, fullJson.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(1, focusedJson.RootElement.GetProperty("schemaVersion").GetInt32());
        var findings = fullJson.RootElement.GetProperty("findings").EnumerateArray()
            .Where(finding => finding.GetProperty("category").GetString() == "managed-call"
                && finding.GetProperty("path").GetString() == "Calls.cs")
            .ToArray();
        var boxAfter = Assert.Single(findings, finding =>
            finding.GetProperty("targetSignature").GetString()!.Contains("Sample.Box..ctor", StringComparison.Ordinal));
        var itemFindings = findings.Where(finding =>
            finding.GetProperty("targetSignature").GetString()!.Contains("Sample.Item..ctor", StringComparison.Ordinal)).ToArray();
        var saveFindings = findings.Where(finding =>
            finding.GetProperty("targetSignature").GetString()!.Contains("Sample.Item.Save", StringComparison.Ordinal)).ToArray();

        Assert.Equal(boxBeforeId, boxAfter.GetProperty("id").GetString());
        Assert.Equal(2, itemFindings.Length);
        Assert.Equal(2, itemFindings.Select(finding => finding.GetProperty("id").GetString()).Distinct().Count());
        Assert.Equal(2, itemFindings.Select(finding => finding.GetProperty("column").GetInt32()).Distinct().Count());
        Assert.All(itemFindings, finding => Assert.Equal("new Item(2)", finding.GetProperty("evidence").GetString()));
        Assert.Equal(2, saveFindings.Length);
        Assert.Equal(2, saveFindings.Select(finding => finding.GetProperty("id").GetString()).Distinct().Count());
        Assert.Equal(2, saveFindings.Select(finding => finding.GetProperty("column").GetInt32()).Distinct().Count());
        var completeness = fullJson.RootElement.GetProperty("completeness");
        Assert.Equal(5, completeness.GetProperty("totalFindingCount").GetInt32());
        Assert.Equal(5, completeness.GetProperty("emittedFindingCount").GetInt32());
        Assert.False(completeness.GetProperty("findingsTruncated").GetBoolean());

        var focusedFindings = focusedJson.RootElement.GetProperty("findings").EnumerateArray()
            .Where(finding => finding.GetProperty("category").GetString() == "managed-call"
                && finding.GetProperty("path").GetString() == "Calls.cs")
            .ToArray();
        Assert.Equal(5, focusedFindings.Length);
        Assert.Equal(5, focusedFindings.Select(finding => finding.GetProperty("id").GetString()).Distinct().Count());
        Assert.Equal(findings.Select(finding => finding.GetProperty("id").GetString()).Order(StringComparer.Ordinal),
            focusedFindings.Select(finding => finding.GetProperty("id").GetString()).Order(StringComparer.Ordinal));
        Assert.All(focusedFindings, finding => Assert.NotEmpty(finding.GetProperty("matchedBy").EnumerateArray()));
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
        Assert.Contains("--clue", result.Output, StringComparison.Ordinal);
        Assert.Contains("--changed", result.Output, StringComparison.Ordinal);
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
