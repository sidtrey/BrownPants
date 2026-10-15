using BrownPants.Cli;
using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class WinFormsUiThreadingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-winforms-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Analyzer_ReportsRecognizedControlWritesAndOmitsOrdinaryObjects()
    {
        Write("src/A.cs", """
            using System.Windows.Forms;
            class Screen : Form {
              void Update() { this.Text = "safe-looking"; Button button = new Button(); button.Text = "x"; Model model = new Model(); model.Text = "no"; }
            }
            class Model { public string Text { get; set; } }
            """);
        var result = WinFormsUiThreadingAnalyzer.Analyze(_root, 100);
        Assert.True(result.CandidateCount == 2, string.Join(" | ", result.Findings.Select(f => $"{f.Receiver}.{f.Property} {f.TypeEvidence}")) + " issues: " + string.Join(" | ", result.Issues.Select(i => $"{i.Id}@{i.Line}")));
        Assert.Contains(result.Findings, f => f.Property == "Text" && f.Receiver == "this");
        Assert.DoesNotContain(result.Findings, f => f.Receiver == "model");
        Assert.All(result.Findings, f => Assert.Equal("Candidate", f.Classification));
        Assert.Contains(result.Findings, f => f.Receiver == "this" && f.ExecutionContext == "Unknown");
    }

    [Fact]
    public void Analyzer_SeparatesBackgroundAndMarshalSyntaxAndLeavesUnrelatedCodeUnknown()
    {
        Write("src/Screen.cs", """
            using System;
            using System.Threading.Tasks;
            using System.Windows.Forms;
            class Screen : Form {
              void Update() {
                Task.Run(() => this.Text = "background");
                this.BeginInvoke((Action)(() => this.Text = "marshaled"));
                Console.WriteLine(this.Text);
              }
              void Worker(object sender, System.ComponentModel.DoWorkEventArgs e) { this.Text = "handler"; }
            }
            """);
        var findings = WinFormsUiThreadingAnalyzer.Analyze(_root, 100).Findings;
        Assert.Equal(3, findings.Count);
        Assert.Contains(findings, f => f.BackgroundCallback == "Task.Run" && f.MarshalingObservation is null);
        Assert.Contains(findings, f => f.BackgroundCallback is null && f.MarshalingObservation?.Contains("BeginInvoke") == true);
        Assert.Contains(findings, f => f.BackgroundCallback == "DoWorkEventArgs handler signature");
        Assert.All(findings, f => Assert.DoesNotContain("safe", f.Classification, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Program_ReportsVisualBasicAndProducesDeterministicJsonWithoutWritingToRepository()
    {
        Write("src/Form.vb", """
            Imports System.Windows.Forms
            Public Class Form1
              Inherits Form
              Public Sub UpdateText()
                Dim button As Button
                button.Text = "hello"
                Me.Text = "secret excerpt"
              End Sub
            End Class
            """);
        var before = Directory.GetFiles(_root, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();
        var first = Run("json"); var second = Run("json");
        Assert.Equal(0, first.code);
        Assert.Equal(first.output, second.output);
        Assert.Contains("Form.vb", first.output);
        Assert.DoesNotContain("hello", first.output);
        Assert.DoesNotContain("secret excerpt", first.output);
        Assert.Contains("CandidateCount\": 2", first.output);
        Assert.Equal(before, Directory.GetFiles(_root, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray());
    }

    [Fact]
    public void Program_ValidatesFormatAndLimit()
    {
        var invalidLimit = Program.Run(["winforms", "analyze", _root, "--limit", "0"], TextWriter.Null, new StringWriter());
        var invalidFormat = Program.Run(["winforms", "analyze", _root, "--format", "xml"], TextWriter.Null, new StringWriter());
        Assert.Equal(2, invalidLimit);
        Assert.Equal(2, invalidFormat);
    }

    [Fact]
    public void Analyzer_BoundsFindingsAndSurfacesParseErrors()
    {
        Write("src/Bad.cs", "using System.Windows.Forms; class Form1 : Form { void Go() { Text = \"one\"; Name = \"two\"; } class Broken : { }");
        var report = WinFormsUiThreadingAnalyzer.Analyze(_root, 1);
        Assert.Equal(2, report.CandidateCount);
        Assert.Equal(1, report.OmittedCount);
        Assert.Single(report.Findings);
        Assert.True(report.ParseErrorCount > 0);

        var markdown = Run("markdown");
        Assert.Contains("Candidate", markdown.output);
        Assert.DoesNotContain("one", markdown.output);
    }

    [Fact]
    public void Analyzer_ResolvesDerivedControlMembersAcrossCSharpFilesWithoutScopeContamination()
    {
        Write("src/Screen.Controls.cs", """
            using System.Windows.Forms;
            namespace Sample;
            public partial class BaseScreen : Form
            {
                protected Label _caption = null!;
            }
            """);
        Write("src/Screen.Members.cs", """
            using System.Windows.Forms;
            namespace Sample;
            public partial class BaseScreen
            {
                protected Holder _holder = null!;
            }
            public class Holder { public Button Button { get; set; } = null!; }
            """);
        Write("src/Screen.Update.cs", """
            using WinButton = System.Windows.Forms.Button;
            using System.Windows.Forms;
            namespace Sample;
            public class Screen : BaseScreen
            {
                void Update()
                {
                    this.Text = "title";
                    _caption.Text = "caption";
                    _holder.Button.Text = "chained";
                    WinButton button = new();
                    button.Text = "button";
                    var inferred = new Label();
                    inferred.Text = "inferred";
                    System.Windows.Forms.TextBox qualified = new();
                    qualified.Text = "qualified";
                    Button shadowed = new();
                    shadowed.Text = "control";
                    {
                        Model shadowed = new();
                        shadowed.Text = "ordinary";
                    }
                }
            }
            class Model { public string Text { get; set; } = ""; }
            """);

        var findings = WinFormsUiThreadingAnalyzer.Analyze(_root, 100).Findings;

        Assert.True(findings.Count == 7, string.Join(" | ", findings.Select(f => $"{f.Receiver}.{f.Property} {f.TypeEvidence}"))
            + $" [types={WinFormsUiThreadingAnalyzer.Analyze(_root, 100).IndexedTypeCount}; unresolved={WinFormsUiThreadingAnalyzer.Analyze(_root, 100).UnresolvedReceiverCount}]");
        Assert.Contains(findings, finding => finding.Path == "src/Screen.Update.cs" && finding.Property == "Text" && finding.Receiver == "this");
        Assert.Contains(findings, finding => finding.Receiver == "_caption" && finding.Property == "Text");
        Assert.Contains(findings, finding => finding.Receiver == "_holder.Button" && finding.Property == "Text");
        Assert.Contains(findings, finding => finding.Receiver == "button" && finding.TypeEvidence.Contains("System.Windows.Forms.Button", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Receiver == "inferred" && finding.TypeEvidence.Contains("System.Windows.Forms.Label", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Receiver == "qualified" && finding.TypeEvidence.Contains("System.Windows.Forms.TextBox", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Receiver == "shadowed" && finding.Property == "Text");
        Assert.DoesNotContain(findings, finding => finding.Line > 0 && finding.Path == "src/Screen.Update.cs"
            && finding.Receiver == "shadowed" && finding.TypeEvidence.Contains("Model", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyzer_ResolvesPartialVisualBasicControlsAndMembersAcrossFiles()
    {
        Write("src/Screen.Controls.vb", """
            Imports System.Windows.Forms
            Namespace Sample
            Partial Public Class Screen
                Inherits Form
                Private caption As Label
                Private holder As Holder
            End Class
            End Namespace
            """);
        Write("src/Holder.vb", """
            Imports System.Windows.Forms
            Namespace Sample
            Public Class Holder
                Public Property Button As Button
            End Class
            Public Class Model
                Public Property Text As String
            End Class
            End Namespace
            """);
        Write("src/Screen.Update.vb", """
            Imports System.Windows.Forms
            Namespace Sample
            Partial Public Class Screen
                Public Sub UpdateText()
                    caption.Text = "caption"
                    holder.Button.Text = "chained"
                    Me.Text = "title"
                    Dim shadowed As Button = New Button()
                    shadowed.Text = "control"
                    If True Then
                        Dim shadowed As Model = New Model()
                        shadowed.Text = "ordinary"
                    End If
                End Sub
            End Class
            End Namespace
            """);

        var findings = WinFormsUiThreadingAnalyzer.Analyze(_root, 100).Findings;

        Assert.True(findings.Count == 4, string.Join(" | ", findings.Select(f => $"{f.Receiver}.{f.Property} {f.TypeEvidence}")));
        Assert.Contains(findings, finding => finding.Path == "src/Screen.Update.vb" && finding.Receiver == "caption");
        Assert.Contains(findings, finding => finding.Path == "src/Screen.Update.vb" && finding.Receiver == "holder.Button");
        Assert.Contains(findings, finding => finding.Path == "src/Screen.Update.vb" && finding.Receiver == "Me" && finding.Property == "Text");
        Assert.Contains(findings, finding => finding.Path == "src/Screen.Update.vb" && finding.Receiver == "shadowed" && finding.Property == "Text");
    }

    [Fact]
    public void Analyzer_ReportsUnresolvedReceiversAsBoundedCoverageWithoutGuessing()
    {
        Write("src/Screen.cs", """
            using System.Windows.Forms;
            class Screen : Form
            {
                void Update(dynamic unknown)
                {
                    unknown.Text = "not enough type evidence";
                    this.Text = "known control";
                    this.Name = "also known";
                }
            }
            """);

        var first = WinFormsUiThreadingAnalyzer.Analyze(_root, 1);
        var second = WinFormsUiThreadingAnalyzer.Analyze(_root, 1);

        Assert.Equal(2, first.CandidateCount);
        Assert.Equal(1, first.UnresolvedReceiverCount);
        Assert.Equal(1, first.OmittedCount);
        Assert.Single(first.Findings);
        Assert.Contains(first.Issues, issue => issue.Id == "BPWF100" && issue.Path == "src/Screen.cs");
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(first), System.Text.Json.JsonSerializer.Serialize(second));
    }

    [Fact]
    public void Analyzer_UsesNamespaceScopedImportsInsteadOfLeakingControlAliases()
    {
        Write("src/Mixed.cs", """
            namespace Ui
            {
                using System.Windows.Forms;
                class Screen : Form
                {
                    void Update() { Button button = new(); button.Text = "control"; }
                }
            }
            namespace Data
            {
                class Record
                {
                    void Update() { Button button = new(); button.Text = "not a resolved Control"; }
                }
            }
            """);

        var report = WinFormsUiThreadingAnalyzer.Analyze(_root, 100);

        Assert.Single(report.Findings);
        Assert.Contains(report.Findings, finding => finding.Path == "src/Mixed.cs" && finding.Receiver == "button");
        Assert.Equal(0, report.UnresolvedReceiverCount);
    }

    private (int code, string output) Run(string format)
    {
        var output = new StringWriter();
        var code = Program.Run(["winforms", "analyze", _root, "--format", format], output, new StringWriter());
        return (code, output.ToString());
    }

    private void Write(string relative, string source)
    {
        var path = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
