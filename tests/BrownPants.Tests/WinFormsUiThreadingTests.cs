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
        Assert.Equal(2, result.CandidateCount);
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
