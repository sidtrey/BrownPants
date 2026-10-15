using BrownPants.Cli.Analysis;
using System.Text.Json;

namespace BrownPants.Tests;

public sealed class NHibernateAwarenessTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-nhibernate-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Analyze_ReportsDirectPackagesMappingsAndConfigurationWithoutLeakingXmlValues()
    {
        Write("Legacy.csproj", """
            <Project><ItemGroup><PackageReference Include="NHibernate" Version="5.5.0" /></ItemGroup></Project>
            """);
        Write("packages.config", """
            <packages><package id="FluentNHibernate" version="3.4.1" /></packages>
            """);
        Write("Mappings/Customer.hbm.xml", """
            <hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"><class name="Customer" /></hibernate-mapping>
            """);
        Write("hibernate.cfg.xml", """
            <hibernate-configuration xmlns="urn:nhibernate-configuration-2.2"><session-factory><property name="connection.password">NeverPrintThisValue123</property></session-factory></hibernate-configuration>
            """);
        Write("app.config", "<configuration><broken>");

        var result = Analyze();
        Assert.Contains(result.Signals, signal => signal.Category == "NHibernate dependency" && signal.Path == "Legacy.csproj");
        Assert.Contains(result.Signals, signal => signal.Category == "NHibernate dependency" && signal.Path == "packages.config");
        Assert.Contains(result.Signals, signal => signal.Category == "NHibernate mapping" && signal.Path == "Mappings/Customer.hbm.xml");
        Assert.Contains(result.Signals, signal => signal.Category == "NHibernate configuration" && signal.Path == "hibernate.cfg.xml");
        Assert.DoesNotContain(result.Signals, signal => signal.Evidence.Contains("NeverPrintThisValue123", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Signals, signal => signal.Path == "app.config");
        var json = JsonSerializer.Serialize(RepositoryScanner.Scan(_root, 100));
        Assert.Contains("NHibernate candidates do not prove", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_ReportsCSharpAndVisualBasicApisOnlyWithNHibernateNamespaceEvidence()
    {
        Write("src/Store.cs", """
            using NHibernate;
            using NHibernate.Cfg;
            class Store {
              ISessionFactory factory;
              ISession Open() { var session = factory.OpenSession(); var tx = session.BeginTransaction(); var query = session.CreateQuery("select x"); return session; }
              void Configure() { new Configuration().SetProperty("connection.password", "NeverPrintThisValue456"); }
              void Register(IServiceCollection services) { services.AddScoped<ISessionFactory, Store>(); }
            }
            class Unrelated { void OpenSession() { new Db().BeginTransaction(); } }
            class Db { public void BeginTransaction() { } }
            """);
        Write("src/Store.vb", """
            Imports NHibernate
            Public Class Store
              Public Function Open(factory As ISessionFactory) As ISession
                Dim session = factory.OpenSession()
                Dim tx = session.BeginTransaction()
                Dim query = session.CreateQuery("select x")
                Return session
              End Function
            End Class
            """);
        Write("src/Noise.cs", """
            class Noise { string note = "session.BeginTransaction()"; // ISessionFactory
              void Run() { new Db().BeginTransaction(); }
            }
            class Db { public void BeginTransaction() { } }
            """);

        var result = Analyze();
        Assert.Contains(result.Signals, s => s.Category == "NHibernate session factory" && s.Language == "C#");
        Assert.Contains(result.Signals, s => s.Category == "NHibernate session" && s.Language == "C#");
        Assert.Contains(result.Signals, s => s.Category == "NHibernate query" && s.Language == "C#");
        Assert.Contains(result.Signals, s => s.Category == "NHibernate transaction" && s.Language == "C#");
        Assert.Contains(result.Signals, s => s.Category == "NHibernate DI registration" && s.Language == "C#");
        Assert.Contains(result.Signals, s => s.Category == "NHibernate session" && s.Language == "VB.NET");
        Assert.Contains(result.Signals, s => s.Category == "NHibernate query" && s.Language == "VB.NET");
        Assert.DoesNotContain(result.Signals, s => s.Path == "src/Noise.cs");
        Assert.DoesNotContain(result.Signals, s => s.Evidence.Contains("NeverPrintThisValue456", StringComparison.Ordinal));
        Assert.All(result.Signals, signal => Assert.Equal("Candidate", signal.EvidenceKind));
    }

    [Fact]
    public void Analyze_UsesStableBoundedSignalSamples()
    {
        Write("src/A.cs", "using NHibernate; class A { ISessionFactory f; ISession Go() => f.OpenSession(); }");
        Write("src/B.cs", "using NHibernate; class B { ISessionFactory f; ISession Go() => f.OpenSession(); }");
        var files = RepositoryScanner.DiscoverFiles(_root);
        var first = ArchitecturePatternAnalyzer.Analyze(_root, files, 1);
        var second = ArchitecturePatternAnalyzer.Analyze(_root, files, 1);
        Assert.Equal(6, first.SignalCount);
        Assert.Single(first.Signals);
        Assert.Equal(first.Signals, second.Signals);
    }

    private ArchitecturePatternAnalysis Analyze() => ArchitecturePatternAnalyzer.Analyze(_root,
        RepositoryScanner.DiscoverFiles(_root), 100);

    private void Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
