using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using BrownPants.Cli;

namespace BrownPants.Tests;

public sealed class LocalFlowchartTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-flowchart-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Diagram_DefaultPngIsValidBoundedAndByteDeterministic()
    {
        Write("docs/flow.md", Fixture);
        var first = Run("docs/flow.md", "out/flow.png");
        Assert.Equal(0, first.ExitCode);
        Assert.Empty(first.Error);
        var firstBytes = File.ReadAllBytes(Path.Combine(_root, "out", "flow.png"));
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, firstBytes[..8]);
        Assert.Equal("IHDR", Encoding.ASCII.GetString(firstBytes, 12, 4));
        var width = ReadBigEndian(firstBytes, 16);
        var height = ReadBigEndian(firstBytes, 20);
        Assert.InRange(width, 1u, 4096u);
        Assert.InRange(height, 1u, 4096u);
        Assert.True((long)width * height <= 16_000_000);
        Assert.Equal("IEND", Encoding.ASCII.GetString(firstBytes, firstBytes.Length - 8, 4));
        var idat = new MemoryStream();
        var chunkTypes = new List<string>();
        for (var offset = 8; offset < firstBytes.Length;)
        {
            var length = checked((int)ReadBigEndian(firstBytes, offset));
            var type = Encoding.ASCII.GetString(firstBytes, offset + 4, 4);
            var data = firstBytes.AsSpan(offset + 8, length);
            var crcOffset = offset + 8 + length;
            Assert.Equal(ReadBigEndian(firstBytes, crcOffset), ComputeCrc(firstBytes.AsSpan(offset + 4, length + 4)));
            chunkTypes.Add(type);
            if (type == "IDAT") idat.Write(data);
            offset = crcOffset + 4;
        }
        Assert.Equal(new[] { "IHDR", "IDAT", "IEND" }, chunkTypes);
        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var pixels = new MemoryStream();
        zlib.CopyTo(pixels);
        Assert.Equal((long)height * (width * 3L + 1), pixels.Length);
        Assert.All(Enumerable.Range(0, checked((int)height)), row => Assert.Equal(0, pixels.GetBuffer()[row * (checked((int)width) * 3 + 1)]));
        Assert.Contains((byte)36, pixels.GetBuffer());

        Assert.Equal(0, Run("docs/flow.md", "out/again.png").ExitCode);
        Assert.Equal(firstBytes, File.ReadAllBytes(Path.Combine(_root, "out", "again.png")));
    }

    [Fact]
    public void Diagram_PngPixelsChangeWhenAnyNodeEdgeLabelOrRelationshipChanges()
    {
        Write("docs/flow.md", Fixture);
        Assert.Equal(0, Run("docs/flow.md", "out/base.png").ExitCode);
        var baseline = File.ReadAllBytes(Path.Combine(_root, "out", "base.png"));
        var variants = new[]
        {
            Fixture.Replace("Start request", "Start process", StringComparison.Ordinal),
            Fixture.Replace("Approved?", "Rejected?", StringComparison.Ordinal),
            Fixture.Replace("Grant access", "Grant account", StringComparison.Ordinal),
            Fixture.Replace("Show rejection", "Show guidance", StringComparison.Ordinal),
            Fixture.Replace("label \"yes\"", "label \"accept\"", StringComparison.Ordinal),
            Fixture.Replace("label \"no\"", "label \"reject\"", StringComparison.Ordinal),
            Fixture.Replace("label \"verify\"", "label \"again\"", StringComparison.Ordinal),
            Fixture.Replace("edge start -> review", "edge start -> deny", StringComparison.Ordinal),
            Fixture.Replace("node grant process", "node grant decision", StringComparison.Ordinal)
        };

        for (var index = 0; index < variants.Length; index++)
        {
            Write("docs/flow.md", variants[index]);
            var path = $"out/variant-{index}.png";
            Assert.Equal(0, Run("docs/flow.md", path).ExitCode);
            Assert.NotEqual(baseline, File.ReadAllBytes(Path.Combine(_root, path)));
        }
    }

    [Fact]
    public void Diagram_DrawioIsWellFormedEscapedAndContainsEveryNodeAndEdge()
    {
        Write("docs/flow.md", Fixture.Replace("Grant access", "Grant & review <group>", StringComparison.Ordinal));

        var result = Run("docs/flow.md", "out/flow.drawio", "--format", "drawio");

        Assert.Equal(0, result.ExitCode);
        var document = XDocument.Load(Path.Combine(_root, "out", "flow.drawio"));
        Assert.Equal("mxfile", document.Root!.Name.LocalName);
        Assert.Equal(new[] { "Start request", "Approved?", "Grant & review <group>", "Show rejection" },
            document.Descendants("mxCell").Where(cell => (string?)cell.Attribute("vertex") == "1")
                .Select(cell => (string?)cell.Attribute("value")));
        Assert.Contains(document.Descendants("mxCell"), cell =>
            cell.Attribute("value")?.Value == "Grant & review <group>");
        Assert.Equal(4, document.Descendants("mxCell").Count(cell => (string?)cell.Attribute("vertex") == "1"));
        Assert.Equal(4, document.Descendants("mxCell").Count(cell => (string?)cell.Attribute("edge") == "1"));
        Assert.Contains(document.Descendants("mxCell"), cell => cell.Attribute("value")?.Value == "yes");
        Assert.Contains(document.Descendants("mxCell"), cell => cell.Attribute("value")?.Value == "no");
        Assert.Contains(document.Descendants("mxCell"), cell => cell.Attribute("value")?.Value == "verify");
        Assert.DoesNotContain(document.Descendants(), element =>
            element.Name.LocalName is "script" or "img" or "a"
            || element.Attributes().Any(attribute => attribute.Name.LocalName is "href" or "src"));
        Assert.DoesNotContain("https://", File.ReadAllText(Path.Combine(_root, "out", "flow.drawio")), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(File.ReadAllBytes(Path.Combine(_root, "out", "flow.drawio")),
            ReadAfterRun("docs/flow.md", "out/repeat.drawio", "--format", "drawio"));
    }

    [Fact]
    public void Diagram_PreservesDisconnectedComponentsAndRootlessCycles()
    {
        Write("docs/flow.md", """
            ```brownpants-flow
            flowchart islands
            node first start "First"
            node cyclea process "Cycle A"
            node cycleb process "Cycle B"
            node last end "Last"
            edge cyclea -> cycleb
            edge cycleb -> cyclea
            ```
            """);

        var result = Run("docs/flow.md", "out/islands.drawio", "--format", "drawio");

        Assert.Equal(0, result.ExitCode);
        var document = XDocument.Load(Path.Combine(_root, "out", "islands.drawio"));
        Assert.Equal(4, document.Descendants("mxCell").Count(cell => (string?)cell.Attribute("vertex") == "1"));
        Assert.Equal(2, document.Descendants("mxCell").Count(cell => (string?)cell.Attribute("edge") == "1"));
    }

    [Theory]
    [InlineData("# no flow block", "not found")]
    [InlineData("```brownpants-flow\nflowchart a\n```\n```brownpants-flow\nflowchart b\n```", "exactly one")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nedge start -> missing\n```", "not declared")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nnode start end \"Again\"\n```", "duplicate node")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nnode done end \"Done\"\nedge start -> done\nedge start -> done\n```", "duplicate edge")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nnode done end \"Døne\"\n```", "ASCII")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nunsupported start --> done\n```", "unsupported flowchart syntax")]
    public void Diagram_InvalidInputReportsSourceAndDoesNotCreateOutput(string markdown, string message)
    {
        Write("docs/flow.md", markdown);

        var result = Run("docs/flow.md", "out/flow.png");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("docs/flow.md", result.Error, StringComparison.Ordinal);
        Assert.Contains(message, result.Error, StringComparison.OrdinalIgnoreCase);
        if (message == "unsupported flowchart syntax")
            Assert.Contains("docs/flow.md:4:", result.Error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_root, "out", "flow.png")));
    }

    [Fact]
    public void Diagram_IgnoresFlowBlocksInsideOtherMarkdownCodeFences()
    {
        Write("docs/example.md", "````markdown\n" + Fixture + "\n````\n");

        var result = Run("docs/example.md", "out/flow.png");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(_root, "out", "flow.png")));
    }

    [Fact]
    public void Diagram_ReportsMissingInputWithoutCreatingOutput()
    {
        Directory.CreateDirectory(_root);

        var result = Run("missing.md", "out/flow.png");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("missing.md", result.Error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_root, "out", "flow.png")));
    }

    [Fact]
    public void Diagram_RejectsInputAndOutputEscapes()
    {
        Write("flow.md", Fixture);
        var outside = Path.Combine(Path.GetDirectoryName(_root)!, "outside-flow.md");
        File.WriteAllText(outside, Fixture);
        try
        {
            var sourceEscape = Program.Run(new[] { "diagram", _root, "../outside-flow.md", "--output", "out.png" }, TextWriter.Null, new StringWriter());
            var output = new StringWriter();
            var outputEscape = Program.Run(new[] { "diagram", _root, "flow.md", "--output", "../outside.png" }, output, new StringWriter());

            Assert.NotEqual(0, sourceEscape);
            Assert.NotEqual(0, outputEscape);
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(_root)!, "outside.png")));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public void Diagram_InvalidInputDoesNotChangeAnExistingOutputEvenWithOverwrite()
    {
        Write("docs/flow.md", "```brownpants-flow\nflowchart incomplete\nnode start start \"Start\"\nedge start -> missing\n```");
        Write("out/flow.png", "keep the previous file");

        var result = Run("docs/flow.md", "out/flow.png", "--overwrite");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("keep the previous file", File.ReadAllText(Path.Combine(_root, "out", "flow.png")));
    }

    [Fact]
    public void Diagram_RejectsInputAndOutputPathsThroughSymbolicLinksWhenSupported()
    {
        Write("real/flow.md", Fixture);
        Write("real-output/keep.txt", "keep");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_root, "linked-source"), Path.Combine(_root, "real"));
            Directory.CreateSymbolicLink(Path.Combine(_root, "linked-output"), Path.Combine(_root, "real-output"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or PlatformNotSupportedException or ArgumentException)
        {
            return;
        }

        var source = Run("linked-source/flow.md", "out/source.png");
        var output = Run("real/flow.md", "linked-output/flow.png");

        Assert.NotEqual(0, source.ExitCode);
        Assert.NotEqual(0, output.ExitCode);
        Assert.False(File.Exists(Path.Combine(_root, "real-output", "flow.png")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(_root, "real-output", "keep.txt")));
    }

    [Fact]
    public void Diagram_RequiresExplicitOverwriteAndReplacesOnlySelectedOutput()
    {
        Write("docs/flow.md", Fixture);
        Write("out/flow.png", "keep this output");

        var conflict = Run("docs/flow.md", "out/flow.png");
        Assert.NotEqual(0, conflict.ExitCode);
        Assert.Contains("--overwrite", conflict.Error, StringComparison.Ordinal);
        Assert.Equal("keep this output", File.ReadAllText(Path.Combine(_root, "out", "flow.png")));

        var overwrite = Run("docs/flow.md", "out/flow.png", "--overwrite");
        Assert.Equal(0, overwrite.ExitCode);
        Assert.NotEqual("keep this output", File.ReadAllText(Path.Combine(_root, "out", "flow.png")));
    }

    [Fact]
    public void Diagram_RejectsMismatchedFormatAndExtension()
    {
        Write("docs/flow.md", Fixture);

        var result = Run("docs/flow.md", "out/flow.png", "--format", "drawio");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("extension", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(_root, "out", "flow.png")));
    }

    [Fact]
    public void Diagram_EnforcesSourceSizeNodeAndEdgeLimits()
    {
        Write("large.md", new string('x', 1_048_577));
        Assert.NotEqual(0, Run("large.md", "large.png").ExitCode);
        Assert.False(File.Exists(Path.Combine(_root, "large.png")));

        var tooManyNodes = new StringBuilder("```brownpants-flow\nflowchart limit\n");
        for (var i = 0; i < 201; i++) tooManyNodes.AppendLine($"node n{i} process \"Node {i}\"");
        tooManyNodes.AppendLine("```");
        Write("nodes.md", tooManyNodes.ToString());
        Assert.NotEqual(0, Run("nodes.md", "nodes.png").ExitCode);
        Assert.False(File.Exists(Path.Combine(_root, "nodes.png")));

        var tooManyEdges = new StringBuilder("```brownpants-flow\nflowchart limit\n");
        for (var i = 0; i < 40; i++) tooManyEdges.AppendLine($"node n{i} process \"Node {i}\"");
        for (var i = 0; i < 501; i++) tooManyEdges.AppendLine($"edge n{i / 40} -> n{i % 40}");
        tooManyEdges.AppendLine("```");
        Write("edges.md", tooManyEdges.ToString());
        Assert.NotEqual(0, Run("edges.md", "edges.png").ExitCode);
        Assert.False(File.Exists(Path.Combine(_root, "edges.png")));

        var deepGraph = new StringBuilder("```brownpants-flow\nflowchart canvas\n");
        for (var i = 0; i < 80; i++) deepGraph.AppendLine($"node n{i} process \"Node {i}\"");
        for (var i = 0; i < 79; i++) deepGraph.AppendLine($"edge n{i} -> n{i + 1}");
        deepGraph.AppendLine("```");
        Write("canvas.md", deepGraph.ToString());
        var canvasLimit = Run("canvas.md", "canvas.png");
        Assert.NotEqual(0, canvasLimit.ExitCode);
        Assert.Contains("canvas", canvasLimit.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(_root, "canvas.png")));
    }

    [Fact]
    public void Diagram_HelpDocumentsFormatAndSecurityBoundaries()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        Assert.Equal(0, Program.Run(new[] { "diagram", "--help" }, output, error));

        Assert.Contains("brownpants-flow", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("--format png|drawio", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("no network", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(error.ToString());
    }

    private (int ExitCode, string Output, string Error) Run(string source, string destination, params string[] options)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var args = new[] { "diagram", _root, source, "--output", destination }.Concat(options).ToArray();
        return (Program.Run(args, output, error), output.ToString(), error.ToString());
    }

    private byte[] ReadAfterRun(string source, string destination, params string[] options)
    {
        var result = Run(source, destination, options);
        Assert.Equal(0, result.ExitCode);
        return File.ReadAllBytes(Path.Combine(_root, destination.Replace('/', Path.DirectorySeparatorChar)));
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static uint ReadBigEndian(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));

    private static uint ComputeCrc(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xffffffffu;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
        }
        return ~crc;
    }

    private const string Fixture = """
        # Approval flow

        ```brownpants-flow
        flowchart approvals
        node start start "Start request"
        node review decision "Approved?"
        node grant process "Grant access"
        node deny end "Show rejection"
        edge start -> review
        edge review -> grant label "yes"
        edge review -> deny label "no"
        edge grant -> review label "verify"
        ```
        """;

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
