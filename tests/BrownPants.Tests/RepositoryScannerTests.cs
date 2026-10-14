using BrownPants.Cli;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BrownPants.Tests;

public sealed class RepositoryScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Help_DescribesWorkflowNeutralPurpose()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "--help" }, output, error));
        Assert.Contains("spec-driven development", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("for Spec Kit", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Scan_CollectsProjectSignalsAndExcludesGeneratedDirectories()
    {
        Write("src/App.csproj", "<Project />");
        Write("src/Program.cs", "class Program {}");
        Write("tests/AppTests.cs", "test");
        Write(".specify/memory/constitution.md", "principles");
        Write("specs/001-feature/spec.md", "feature");
        Write("openspec/config.yaml", "schema: spec-driven");
        Write("openspec/project.md", "project context");
        Write("openspec/specs/auth/spec.md", "current behavior");
        Write("openspec/changes/add-login/proposal.md", "new behavior");
        Write("openspec/changes/add-login/tasks.md", "- [ ] implement");
        Write("openspec/changes/archive/old-change/proposal.md", "archived");
        Write("obj/Generated.cs", "generated");
        Write(".git/config", "internal");

        var inventory = RepositoryScanner.Scan(_root, sampleLimit: 2);

        Assert.Equal(11, inventory.FileCount);
        Assert.Contains("src/App.csproj", inventory.ProjectMarkers);
        Assert.Contains("tests/AppTests.cs", inventory.TestPaths);
        Assert.DoesNotContain(inventory.TestPaths, path => path.Contains("Generated", StringComparison.Ordinal));
        var specKit = Assert.Single(inventory.SddWorkflows, workflow => workflow.Name == "Spec Kit");
        Assert.True(specKit.IsInitialized);
        Assert.Equal(1, specKit.SpecificationDirectoryCount);
        Assert.Null(specKit.ActiveChangeDirectoryCount);
        Assert.Contains(".specify/memory/constitution.md", specKit.ArtifactPaths);
        var openSpec = Assert.Single(inventory.SddWorkflows, workflow => workflow.Name == "OpenSpec");
        Assert.Equal(1, openSpec.SpecificationDirectoryCount);
        Assert.Equal(1, openSpec.ActiveChangeDirectoryCount);
        Assert.Equal(6, openSpec.ArtifactFileCount);
        Assert.Equal(2, openSpec.ArtifactPaths.Count);
        Assert.Contains("openspec/changes/add-login/proposal.md", openSpec.ArtifactPaths);
        Assert.Contains("## SDD workflows", MarkdownReport.Render(inventory));
        Assert.Contains("### OpenSpec", MarkdownReport.Render(inventory));
        Assert.Contains("4 additional artifacts omitted by the sample limit", MarkdownReport.Render(inventory));
    }

    [Fact]
    public void Scan_DetectsOpenSpecWithoutSpecKit()
    {
        Write("openspec/config.yaml", "schema: spec-driven");
        Write("openspec/specs/catalog/spec.md", "current behavior");

        var inventory = RepositoryScanner.Scan(_root);

        var workflow = Assert.Single(inventory.SddWorkflows);
        Assert.Equal("OpenSpec", workflow.Name);
        Assert.True(workflow.IsInitialized);
        Assert.Equal(1, workflow.SpecificationDirectoryCount);
        Assert.Equal(0, workflow.ActiveChangeDirectoryCount);
        Assert.DoesNotContain(MarkdownReport.Render(inventory), "## Spec Kit state", StringComparison.Ordinal);

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var jsonWorkflow = Assert.Single(json.RootElement.GetProperty("SddWorkflows").EnumerateArray());
        Assert.Equal("OpenSpec", jsonWorkflow.GetProperty("Name").GetString());
        Assert.Equal(1, jsonWorkflow.GetProperty("SpecificationDirectoryCount").GetInt32());
        Assert.Equal(0, jsonWorkflow.GetProperty("ActiveChangeDirectoryCount").GetInt32());
    }

    [Fact]
    public void Scan_ReportsCrossEcosystemMarkersAndHonestLanguageCoverage()
    {
        Write("dotnet/App.csproj", "<Project />");
        Write("dotnet/Directory.Build.props", "<Project />");
        Write("native/Legacy.vcxproj", "<Project />");
        Write("native/CMakeLists.txt", "project(native)");
        Write("native/Makefile", "all:");
        Write("python/pyproject.toml", "[project]");
        Write("web/package.json", "{}");
        Write("web/pnpm-lock.yaml", "lockfileVersion: 9");
        Write("web/main.js", "export const active = true;");
        Write("web/main.ts", "export const ready = true;");
        Write("service/go.mod", "module example");
        Write("service/main.go", "package main");
        Write("rust/Cargo.toml", "[package]");
        Write("java/pom.xml", "<project />");
        Write("java/build.gradle.kts", "plugins {}");
        Write("kotlin/src/Module.kt", "fun sample() = Unit");
        Write("ruby/Gemfile", "source 'https://example.invalid'");
        Write("php/composer.json", "{}");
        Write("apple/Package.swift", "// swift-tools-version: 5.9");
        Write("flutter/pubspec.yaml", "name: sample");
        Write("flutter/main.dart", "void main() {}");
        Write("elixir/mix.exs", "defmodule Sample.MixProject do end");
        Write("scala/build.sbt", "name := \"sample\"");
        Write("containers/Dockerfile", "FROM scratch");
        Write("infra/main.tf", "terraform {}");
        Write("src/Program.cs", "class Program {}");
        Write("scripts/task.py", "print('ok')");
        Write("lib/module.rs", "fn main() {}");
        Write("README", "extensionless");
        Write("assets/blob.oddext", "unknown extension");
        Write(".gradle/caches/generated.kt", "generated");
        Write(".next/server/generated.js", "generated");
        Write(".terraform/providers/generated.go", "generated");
        Write(".dart_tool/package_config.json", "generated");
        Write("Pods/Vendor/Generated.swift", "third-party");

        var inventory = RepositoryScanner.Scan(_root);
        var systems = inventory.ProjectSystems.ToDictionary(system => system.Name, system => system.ManifestPaths);
        var languages = inventory.LanguageCoverage.ToDictionary(language => language.Language);

        Assert.Equal(30, inventory.FileCount);
        Assert.DoesNotContain(inventory.ProjectMarkers, path => path.Contains("generated", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { ".NET", "Apple", "C/C++", "Containers", "Dart", "Elixir", "Go", "Infrastructure",
                "JVM", "JavaScript/TypeScript", "Make", "PHP", "Python", "Ruby", "Rust", "Scala" },
            systems.Keys.Order(StringComparer.Ordinal));
        Assert.Contains("dotnet/App.csproj", systems[".NET"]);
        Assert.Contains("native/Legacy.vcxproj", systems["C/C++"]);
        Assert.Contains("native/CMakeLists.txt", systems["C/C++"]);
        Assert.Contains("native/Makefile", systems["Make"]);
        Assert.Contains("web/pnpm-lock.yaml", systems["JavaScript/TypeScript"]);
        Assert.Contains("Cargo.toml", systems["Rust"].Select(Path.GetFileName));
        Assert.Contains("main.tf", systems["Infrastructure"].Select(Path.GetFileName));
        Assert.Equal(1, languages["C#"].SourceFileCount);
        Assert.Equal("Roslyn syntax and candidate patterns", languages["C#"].AnalysisLevel);
        Assert.Equal("Inventory only", languages["Python"].AnalysisLevel);
        Assert.Equal(1, languages["TypeScript"].SourceFileCount);
        Assert.Equal("Inventory only", languages["TypeScript"].AnalysisLevel);
        Assert.Equal(1, languages["Kotlin"].SourceFileCount);
        Assert.Equal("Inventory only", languages["Kotlin"].AnalysisLevel);
        Assert.Equal(1, languages["JavaScript"].SourceFileCount);
        Assert.Equal(1, languages["Go"].SourceFileCount);
        Assert.Equal(1, languages["Swift"].SourceFileCount);
        Assert.Equal(1, languages["Dart"].SourceFileCount);
        Assert.Equal(4, inventory.Extensions.Single(extension => extension.Extension == "[no extension]").Count);
        Assert.Equal(inventory.FileCount, inventory.Extensions.Sum(extension => extension.Count));
        Assert.Contains(inventory.Extensions, extension => extension.Extension == ".oddext");

        var markdown = MarkdownReport.Render(inventory);
        Assert.Contains("## Project ecosystems", markdown);
        Assert.Contains("## Language coverage", markdown);
        Assert.Contains("`Python` — 1 source file — Inventory only", markdown);
        Assert.Contains("`native/Legacy.vcxproj`", markdown);
        Assert.All(inventory.ProjectSystems.SelectMany(system => system.ManifestPaths), path =>
            Assert.Contains($"`{path}`", markdown));
        Assert.All(inventory.LanguageCoverage, language =>
            Assert.Contains($"`{language.Language}` — {language.SourceFileCount}", markdown));

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var jsonSystems = json.RootElement.GetProperty("ProjectSystems").EnumerateArray().ToArray();
        var jsonLanguages = json.RootElement.GetProperty("LanguageCoverage").EnumerateArray().ToArray();
        Assert.Equal(systems.Count, jsonSystems.Length);
        Assert.Equal(languages.Count, jsonLanguages.Length);
        foreach (var system in inventory.ProjectSystems)
        {
            var serialized = Assert.Single(jsonSystems, item => item.GetProperty("Name").GetString() == system.Name);
            Assert.Equal(system.ManifestPaths, serialized.GetProperty("ManifestPaths").EnumerateArray()
                .Select(path => path.GetString()!).ToArray());
        }
        foreach (var language in inventory.LanguageCoverage)
        {
            var serialized = Assert.Single(jsonLanguages, item => item.GetProperty("Language").GetString() == language.Language);
            Assert.Equal(language.SourceFileCount, serialized.GetProperty("SourceFileCount").GetInt32());
            Assert.Equal(language.AnalysisLevel, serialized.GetProperty("AnalysisLevel").GetString());
        }
        Assert.Contains(jsonLanguages, language => language.GetProperty("Language").GetString() == "Python"
            && language.GetProperty("AnalysisLevel").GetString() == "Inventory only");
    }

    [Fact]
    public void Scan_IsStableAcrossRepeatedRuns()
    {
        Write("z/Readme.md", "hello");
        Write("a/a.csproj", "lower project");
        Write("a/app.py", "print('hello')");
        Write("openspec/config.yaml", "schema: spec-driven");
        Write("openspec/specs/catalog/spec.md", "current behavior");
        Write("openspec/changes/update-catalog/proposal.md", "change behavior");

        var first = RepositoryScanner.Scan(_root);
        var second = RepositoryScanner.Scan(_root);

        Assert.Equal(MarkdownReport.Render(first), MarkdownReport.Render(second));

        using var firstJson = new StringWriter();
        using var secondJson = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, firstJson, error));
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, secondJson, error));
        Assert.Equal(firstJson.ToString(), secondJson.ToString());
    }

    [Fact]
    public void Run_EmitsAllConfiguredArchitectureSamplesInMarkdownAndJson()
    {
        var factoryTypes = Enumerable.Range(0, 60)
            .Select(index => $"public class WidgetFactory{index} {{ }}");
        Write("src/Factories.cs", string.Join(Environment.NewLine, factoryTypes));
        var projectReferences = Enumerable.Range(0, 25)
            .Select(index => $"<ProjectReference Include=\"Project{index}.csproj\" />");
        Write("src/Root.csproj", $"<Project><ItemGroup>{string.Join(string.Empty, projectReferences)}</ItemGroup></Project>");

        using var markdown = new StringWriter();
        using var json = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--limit", "100" }, markdown, error));
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "100" }, json, error));

        using var document = System.Text.Json.JsonDocument.Parse(json.ToString());
        var signals = document.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals").EnumerateArray().ToArray();
        var jsonReferences = document.RootElement.GetProperty("DotNetProjects")[0]
            .GetProperty("ProjectReferences").EnumerateArray().Count();
        var markdownSignals = markdown.ToString().Split(Environment.NewLine)
            .Count(line => line.StartsWith("- Factory type [Candidate] (C#):", StringComparison.Ordinal));

        Assert.Equal(60, signals.Length);
        Assert.Equal(signals.Length, markdownSignals);
        Assert.Equal(25, jsonReferences);
        Assert.Contains("ProjectReference: `Project24.csproj`", markdown.ToString());
    }

    [Fact]
    public void Run_EmitsJsonAndRejectsUnknownFormat()
    {
        Write("app.py", "print('hello')");
        Write("src/App.cs", "class First {} class Second {}");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var jsonExit = Program.Run(new[] { "inspect", _root, "--format", "json", "--limit", "1" }, output, error);
        Assert.Equal(0, jsonExit);
        using (var document = System.Text.Json.JsonDocument.Parse(output.ToString()))
        {
            Assert.Equal(2, document.RootElement.GetProperty("FileCount").GetInt32());
            var csharp = document.RootElement.GetProperty("CSharp");
            Assert.Equal(2, csharp.GetProperty("TypeCount").GetInt32());
            Assert.Equal(1, csharp.GetProperty("Types").GetArrayLength());
        }

        output.GetStringBuilder().Clear();
        var invalidExit = Program.Run(new[] { "inspect", _root, "--format", "xml" }, output, error);
        Assert.Equal(2, invalidExit);
        Assert.Contains("Expected markdown or json", error.ToString());
    }

    [Fact]
    public void Scan_ParsesLegacyFrameworkProjectsAndRoslynSyntaxWithoutBuildingThem()
    {
        Write("Legacy.App.csproj", """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup>
                <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
                <LangVersion>5</LangVersion>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Legacy.Data\Legacy.Data.csproj" />
              </ItemGroup>
            </Project>
            """);
        Write("src/OrderService.cs", """
            using System;
            using DataAccess = Legacy.Data.Repository;
            namespace Legacy.App
            {
                public class OrderService
                {
                    public string Load(int id)
                    {
                        return DataAccess.Fetch(id).ToString();
                    }

                    public int Count { get; set; }

                    public string UnsupportedForCSharp5() => $"{Count}";
                }
            }
            #if NET45
            public class FrameworkSpecificService { }
            #endif
            """);
        Write("src/Broken.cs", "namespace Legacy.App { public class BrokenType {");

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Single(inventory.DotNetProjects);
        Assert.True(inventory.DotNetProjects[0].IsLegacyFramework);
        Assert.Contains("v4.8", inventory.DotNetProjects[0].TargetFrameworks);
        Assert.Contains("5", inventory.DotNetProjects[0].LanguageVersions);
        Assert.Contains("..\\Legacy.Data\\Legacy.Data.csproj", inventory.DotNetProjects[0].ProjectReferences);
        Assert.Equal(2, inventory.CSharp.SourceFileCount);
        Assert.Contains(inventory.CSharp.Types, item => item.Name == "OrderService" && item.Namespace == "Legacy.App");
        Assert.DoesNotContain(inventory.CSharp.Types, item => item.Name == "FrameworkSpecificService");
        Assert.Contains(inventory.CSharp.Members, item => item.Signature.StartsWith("string Load", StringComparison.Ordinal));
        Assert.Contains(inventory.CSharp.Usings, item => item.Alias == "DataAccess");
        Assert.Contains(inventory.CSharp.Invocations, item => item.Expression == "DataAccess.Fetch");
        Assert.Contains(inventory.CSharp.Issues, issue => issue.Code == "CS1513");
        Assert.True(inventory.CSharp.InactiveCodeRegionCount > 0);
        Assert.True(inventory.CSharp.ParseErrorCount > 0);
    }

    [Fact]
    public void Scan_ParsesVisualBasicLegacySourceWithRoslyn()
    {
        Write("LegacyVB/App.vbproj", "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><PropertyGroup><TargetFrameworkVersion>v4.8</TargetFrameworkVersion></PropertyGroup></Project>");
        Write("LegacyVB/Services/OrderService.vb", """
            Namespace Legacy.Services
                Public Class OrderService
                    Public Function Load(id As Integer) As String
                        Return Repository.Fetch(id).ToString()
                    End Function

                    Public Property Count As Integer
                End Class
            End Namespace
            """);

        var inventory = RepositoryScanner.Scan(_root);

        Assert.Equal(1, inventory.VisualBasic.SourceFileCount);
        Assert.Single(inventory.DotNetProjects);
        Assert.True(inventory.DotNetProjects[0].IsLegacyFramework);
        Assert.Contains("LegacyVB/App.vbproj", inventory.ProjectMarkers);
        Assert.Contains(inventory.VisualBasic.Types, type => type.Name == "OrderService"
            && type.Namespace == "Legacy.Services" && type.Kind == "Class");
        Assert.Contains(inventory.VisualBasic.Members, member => member.ContainingType == "Legacy.Services.OrderService"
            && member.Signature.Contains("Load", StringComparison.Ordinal));
        Assert.Contains(inventory.VisualBasic.Invocations, invocation => invocation.Expression == "Repository.Fetch");
        Assert.Equal(0, inventory.VisualBasic.ParseErrorCount);
        Assert.Contains("Roslyn Visual Basic structure", MarkdownReport.Render(inventory));
    }

    [Fact]
    public void Scan_ReportsFactoryDependencyInjectionAndMfcInteropSignals()
    {
        Write("client/Composition.cs", """
            public interface IWidgetFactory { }
            public class WidgetFactory : IWidgetFactory { }
            services.AddTransient<IWidget, Widget>();
            [DllImport("legacy.dll")]
            public static extern int LoadWidget();
            """);
        Write("client/Noise.cs", """
            // public class CommentOnlyFactory { }
            var example = "services.AddTransient<IClock, Clock>(); [DllImport(\"old.dll\")]";
            """);
        Write("native/MainFrame.cpp", """
            IMPLEMENT_DYNCREATE(CMainFrame, CFrameWnd)
            BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
            auto widget = CObject::CreateObject(RUNTIME_CLASS(CWidget));
            CoCreateInstance(clsid, nullptr, CLSCTX_INPROC_SERVER, iid, result);
            public ref class ManagedWidget {};
            auto bridge = gcnew ManagedWidget();
            """);
        Write("native/Noise.cpp", """
            // IMPLEMENT_DYNCREATE(CNoise, CObject)
            const char* sample = "BEGIN_MESSAGE_MAP(CNoise, CObject)";
            /* CObject::CreateObject(RUNTIME_CLASS(CNoise)) */
            const char* raw = R"fixture(
            IMPLEMENT_DYNCREATE(CRawNoise, CObject)
            )fixture";
            """);
        Write("native/Native.vcxproj", """
            <Project>
              <PropertyGroup>
                <UseOfMfc>Dynamic</UseOfMfc>
                <CLRSupport>true</CLRSupport>
                <PlatformToolset>v143</PlatformToolset>
                <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>
              </PropertyGroup>
            </Project>
            """);

        var inventory = RepositoryScanner.Scan(_root);
        var categories = inventory.ArchitecturePatterns.Signals.Select(signal => signal.Category).ToArray();

        Assert.Contains("Factory type", categories);
        Assert.Contains("DI registration", categories);
        Assert.Contains("Native interop", categories);
        Assert.Contains("MFC runtime type", categories);
        Assert.Contains("MFC message map", categories);
        Assert.Contains("MFC object factory", categories);
        Assert.Contains("COM activation", categories);
        Assert.Contains("C++/CLI bridge", categories);
        Assert.DoesNotContain(inventory.ArchitecturePatterns.Signals, signal => signal.Path.EndsWith("Noise.cs", StringComparison.Ordinal)
            || signal.Path.EndsWith("Noise.cpp", StringComparison.Ordinal));
        Assert.Contains("native/Native.vcxproj", inventory.ProjectMarkers);
        Assert.Single(inventory.NativeProjects);
        Assert.True(inventory.NativeProjects[0].HasMfcSignal);
        Assert.True(inventory.NativeProjects[0].HasClrSupport);
        Assert.Contains("v143", inventory.NativeProjects[0].PlatformToolsets);
        Assert.Contains("Architecture pattern signals", MarkdownReport.Render(inventory));
    }

    [Fact]
    public void Scan_RecognizesLegacyContainerResolutionAndFiltersGenericGetCalls()
    {
        Write("client/LegacyComposition.cs", """
            class Composition
            {
                void Configure(IUnityContainer container, ContainerBuilder builder, IKernel kernel, IServiceCollection services)
                {
                    container.RegisterType<IClock, SystemClock>();
                    container.RegisterInstance<IClock>(new SystemClock());
                    builder.RegisterType<IClock>().As<IClock>();
                    kernel.Bind<IClock>().To<SystemClock>();
                    services.AddTransient<IClock, SystemClock>();
                }

                void Resolve(IUnityContainer container, IKernel kernel, ILifetimeScope scope, ResourceManager resources, Cache cache)
                {
                    var unity = container.Resolve<IClock>();
                    var ninject = kernel.Get<IClock>();
                    var autofac = scope.Resolve<IClock>();
                    var resource = resources.GetObject("title");
                    var cached = cache.Get("clock");
                }
            }
            """);
        Write("client/LegacyComposition.vb", """
            Public Class Composition
                Public Sub Resolve(container As IUnityContainer, kernel As IKernel, resources As ResourceManager)
                    Dim unity = container.Resolve(Of IClock)()
                    Dim ninject = kernel.Get(Of IClock)()
                    Dim resource = resources.GetObject("title")
                End Sub
            End Class
            """);

        var signals = RepositoryScanner.Scan(_root).ArchitecturePatterns.Signals;
        var csharpResolution = signals.Where(signal => signal.Language == "C#" && signal.Category == "DI resolution").ToArray();
        var visualBasicResolution = signals.Where(signal => signal.Language == "VB.NET" && signal.Category == "DI resolution").ToArray();

        Assert.Equal(3, csharpResolution.Length);
        Assert.Contains(csharpResolution, signal => signal.Evidence.Contains("container.Resolve", StringComparison.Ordinal));
        Assert.Contains(csharpResolution, signal => signal.Evidence.Contains("kernel.Get", StringComparison.Ordinal));
        Assert.Contains(csharpResolution, signal => signal.Evidence.Contains("scope.Resolve", StringComparison.Ordinal));
        Assert.Equal(2, visualBasicResolution.Length);
        Assert.Contains(visualBasicResolution, signal => signal.Evidence.Contains("container.Resolve", StringComparison.Ordinal));
        Assert.Contains(visualBasicResolution, signal => signal.Evidence.Contains("kernel.Get", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Category == "DI resolution"
            && (signal.Evidence.Contains("resources.GetObject", StringComparison.OrdinalIgnoreCase)
                || signal.Evidence.Contains("cache.Get", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(signals, signal => signal.Category == "DI registration" && signal.Evidence.Contains("RegisterType", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Category == "DI registration" && signal.Evidence.Contains("RegisterInstance", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Category == "DI registration" && signal.Evidence.Contains("Bind", StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_ReportsVisualBasicInteropFactoryAndDependencySignalsWithEvidenceKinds()
    {
        Write("client/Composition.vb", """
            Imports System.Runtime.InteropServices

            Public Class widgetfactory
                Public Function createwidget() As Object
                    Return New Object()
                End Function
            End Class

            Public Class Composition
                Public Sub Configure(services As IServiceCollection, provider As IServiceProvider)
                    services.addtransient(Of IWidget, Widget)()
                    Dim widget = provider.getservice(GetType(IWidget))
                    Dim created = widgetfactory.createwidget()
                    Dim legacy = createobject("Legacy.Widget")
                End Sub

                <dllimport("legacy.dll")>
                Public Shared Function LoadWidget() As Integer
                End Function
            End Class
            """);
        Write("client/Ordinary.cs", """
            class FolderLoader
            {
                void Load() { Directory.CreateDirectory("cache"); }
            }
            """);
        Write("client/Injected.cs", """
            public class InjectedConsumer
            {
                public InjectedConsumer(IRepository repository, Func<IWidget> widgetFactory) { }
            }

            public class PlainConsumer
            {
                public PlainConsumer(string name, int retryCount) { }
            }
            """);
        Write("client/Injected.vb", """
            Public Class InjectedConsumer
                Public Sub New(repository As IRepository, widgetFactory As Func(Of IWidget))
                End Sub
            End Class

            Public Class PlainConsumer
                Public Sub New(name As String, retryCount As Integer)
                End Sub
            End Class
            """);
        Write("client/ComInterop.cs", """
            using System;
            using System.Runtime.InteropServices;

            [ComImport, Guid("00000000-0000-0000-0000-000000000001"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            interface ILegacyWidget { }

            class ComBridge
            {
                object Activate() => Activator.CreateInstance(Type.GetTypeFromCLSID(Guid.Empty)!);
                object CreateOrdinary() => Activator.CreateInstance(typeof(Ordinary));
                object Wrap(IntPtr pointer) => Marshal.GetObjectForIUnknown(pointer);
            }
            """);
        Write("client/ComInterop.vb", """
            Imports System
            Imports System.Runtime.InteropServices

            <ComImport, Guid("00000000-0000-0000-0000-000000000001"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
            Public Interface ILegacyWidget
            End Interface

            Public Class ComBridge
                Public Function Activate() As Object
                    Return marshal.getactiveobject("Legacy.Widget")
                End Function
                Public Function CreateOrdinary() As Object
                    Return Activator.CreateInstance(GetType(Ordinary))
                End Function
                Public Function Wrap(pointer As IntPtr) As Object
                    Return Marshal.GetObjectForIUnknown(pointer)
                End Function
            End Class
            """);

        var inventory = RepositoryScanner.Scan(_root);
        var signals = inventory.ArchitecturePatterns.Signals;
        var categories = signals.Select(signal => signal.Category).ToArray();

        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Factory type"
            && signal.Path == "client/Composition.vb" && signal.Line > 0);
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Factory invocation");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "DI registration");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "DI resolution");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "COM activation");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Native interop");
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "Constructor injection"
            && signal.Path == "client/Injected.cs");
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "Factory delegate"
            && signal.Path == "client/Injected.cs");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Constructor injection"
            && signal.Path == "client/Injected.vb");
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Factory delegate"
            && signal.Path == "client/Injected.vb");
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "COM activation"
            && signal.Evidence.Contains("GetTypeFromCLSID", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "COM marshalling"
            && signal.Evidence.Contains("GetObjectForIUnknown", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "COM activation"
            && signal.Evidence.Contains("getactiveobject", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(signals, signal => signal.Category == "COM activation"
            && (signal.Evidence.Contains("typeof(Ordinary)", StringComparison.Ordinal)
                || signal.Evidence.Contains("GetType(Ordinary)", StringComparison.Ordinal)));
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "COM marshalling"
            && signal.Evidence.Contains("GetObjectForIUnknown", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "C#" && signal.Category == "Native interop"
            && signal.Evidence.Contains("InterfaceType", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Language == "VB.NET" && signal.Category == "Native interop"
            && signal.Evidence.Contains("ComImport", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Evidence.Contains("CreateDirectory", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Path.Contains("Injected", StringComparison.Ordinal)
            && signal.Evidence.Contains("PlainConsumer", StringComparison.Ordinal));
        Assert.All(signals, signal => Assert.Equal("Candidate", signal.EvidenceKind));

        var markdown = MarkdownReport.Render(inventory);
        Assert.Contains("Factory invocation [Candidate] (VB.NET)", markdown);
        Assert.Contains("Visual Basic invocation expressions (unresolved)", markdown);
        Assert.Contains("services.addtransient", markdown);
        Assert.DoesNotContain(categories, category => category == "Confirmed relationship");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "inspect", _root, "--format", "json" }, output, error));
        using var document = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.Contains(document.RootElement.GetProperty("ArchitecturePatterns").GetProperty("Signals").EnumerateArray(),
            signal => signal.GetProperty("EvidenceKind").GetString() == "Candidate"
                && signal.GetProperty("Language").GetString() == "VB.NET");
    }

    [Fact]
    public void Scan_ExcludesVoidCreateMethodsFromFactoryMethodCandidates()
    {
        Write("client/FactoryMethods.cs", """
            public class Widget { }

            public class WidgetProvider
            {
                public void Create() { }
            }

            public class WidgetFactory
            {
                public Widget CreateWidget() => new Widget();
            }
            """);
        Write("client/FactoryMethods.vb", """
            Public Class WidgetProvider
                Public Sub Create()
                End Sub
            End Class

            Public Class WidgetFactory
                Public Function CreateWidget() As Object
                    Return New Object()
                End Function
            End Class
            """);

        var signals = RepositoryScanner.Scan(_root).ArchitecturePatterns.Signals;

        Assert.DoesNotContain(signals, signal => signal.Category == "Factory method"
            && signal.Evidence.Contains("void Create", StringComparison.Ordinal));
        Assert.DoesNotContain(signals, signal => signal.Category == "Factory method"
            && signal.Evidence.Contains("Sub Create", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(signals, signal => signal.Category == "Factory method" && signal.Language == "C#"
            && signal.Evidence.Contains("CreateWidget", StringComparison.Ordinal));
        Assert.Contains(signals, signal => signal.Category == "Factory method" && signal.Language == "VB.NET"
            && signal.Evidence.Contains("CreateWidget", StringComparison.Ordinal));
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesInvocationTargetsFromCompilationSymbols()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace Legacy.App
            {
                public class Store { public void Save() { } }
                public class Controller
                {
                    public void Run() { new Store().Save(); }
                    public void Missing() { Unknown(); }
                    public string NameOfStore() { return nameof(Store); }
                }
            }
            """, path: "src/Api.cs");
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = CSharpCompilation.Create("LegacyFixture", new[] { tree }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("LegacyFixture.csproj", (Compilation)compilation) }, 1, 10, _root);

        Assert.Equal(1, analysis.ProjectsLoaded);
        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Equal(1, analysis.UnresolvedInvocationCount);
        Assert.Contains(analysis.ResolvedCalls, call => call.Target.Contains("Legacy.App.Store.Save", StringComparison.Ordinal));
        Assert.Contains(analysis.ResolvedCalls, call => call.Caller.Contains("Legacy.App.Controller.Run", StringComparison.Ordinal));
        Assert.Contains(analysis.UnresolvedCalls, call => call.Expression == "Unknown");
    }

    [Fact]
    public void SemanticAnalyzer_ResolvesVisualBasicInvocationTargets()
    {
        var tree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText("""
            Namespace Legacy.App
                Public Class Store
                    Public Sub Save()
                    End Sub
                End Class
                Public Class Controller
                    Public Sub Run()
                        Dim store As New Store()
                        store.Save()
                    End Sub
                End Class
            End Namespace
            """, path: "src/Api.vb");
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.Create("LegacyFixture",
            new[] { tree }, references,
            new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analysis = CSharpSemanticAnalyzer.AnalyzeCompilations(
            new[] { ("LegacyFixture.vbproj", (Compilation)compilation) }, 1, 10, _root);

        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Contains(analysis.ResolvedCalls, call => call.Target.Contains("Legacy.App.Store.Save", StringComparison.Ordinal));
        Assert.Contains(analysis.ResolvedCalls, call => call.Caller.Contains("Legacy.App.Controller.Run", StringComparison.Ordinal));
    }

    [Fact]
    public void SemanticAnalyzer_LoadsClassicNetFrameworkProjectWithoutBuildingIt()
    {
        Write("Legacy.csproj", """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
              <PropertyGroup>
                <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
                <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
                <ProjectGuid>{12345678-1234-1234-1234-123456789012}</ProjectGuid>
                <OutputType>Library</OutputType>
                <RootNamespace>LegacyFixture</RootNamespace>
                <AssemblyName>LegacyFixture</AssemblyName>
                <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
                <LangVersion>7.3</LangVersion>
              </PropertyGroup>
              <ItemGroup>
                <Reference Include="System" />
                <Compile Include="Legacy.cs" />
              </ItemGroup>
            </Project>
            """);
        Write("Legacy.cs", """
            namespace LegacyFixture
            {
                public class WidgetFactory { public Widget Create() { return new Widget(); } }
                public class Widget { }
                public class Consumer { public Widget Run(WidgetFactory factory) { return factory.Create(); } }
            }
            """);

        var analysis = CSharpSemanticAnalyzer.AnalyzeRepository(
            _root, new[] { "Legacy.csproj" }, Array.Empty<string>(), 20);

        Assert.Equal(1, analysis.ProjectsDiscovered);
        Assert.Equal(1, analysis.ProjectsLoaded);
        Assert.Equal(1, analysis.ResolvedInvocationCount);
        Assert.Contains(analysis.ResolvedCalls, call => call.Target.Contains("LegacyFixture.WidgetFactory.Create", StringComparison.Ordinal));
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




