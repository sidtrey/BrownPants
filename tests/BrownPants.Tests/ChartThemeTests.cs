using System.Text;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Xml.Linq;
using BrownPants.Cli;
using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class ChartThemeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-chart-theme-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void ChartThemeCatalog_ProvidesDeterministicAccessiblePalettes()
    {
        Assert.Equal(["blue-water", "default", "high-contrast"], ChartThemeCatalog.All.Select(theme => theme.Name));
        Assert.All(ChartThemeCatalog.All, theme =>
        {
            Assert.Equal(1, theme.SchemaVersion);
            Assert.True(theme.HasAccessibleTextContrast, $"Theme '{theme.Name}' does not meet 4.5:1 text contrast.");
            Assert.Equal(7, theme.Shapes.Count);
        });

        Assert.True(ChartThemeCatalog.TryGet("DEFAULT", out var defaultTheme));
        Assert.Equal("#223146", defaultTheme.Text.ToHex());
        Assert.Equal("#fff5d6", defaultTheme.Shape(LocalFlowchartNodeShape.Decision).Fill.ToHex());
        Assert.False(ChartThemeCatalog.TryGet("unknown", out _));
    }

    [Fact]
    public void ChartThemeParser_ParsesCompleteCustomThemeAndChecksContrast()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ChartThemes", "blue-water.json");

        var theme = ChartThemeParser.Parse(File.ReadAllBytes(fixture), "blue-water.json");

        Assert.Equal("Blue Water", theme.Name);
        Assert.Equal(1, theme.SchemaVersion);
        Assert.Equal("#eaf6fa", theme.Canvas.ToHex());
        Assert.Equal("#cde8f5", theme.Shape(LocalFlowchartNodeShape.Decision).Fill.ToHex());
        Assert.Equal(7, theme.Shapes.Count);
        Assert.True(theme.HasAccessibleTextContrast);
    }

    [Fact]
    public void ChartThemeParser_RejectsOversizedOrInvalidUtf8()
    {
        var oversized = new byte[ChartThemeParser.MaximumThemeBytes + 1];
        var sizeError = Assert.Throws<LocalFlowchartFormatException>(() =>
            ChartThemeParser.Parse(oversized, "large-theme.json"));
        Assert.Contains("byte limit", sizeError.Message, StringComparison.Ordinal);

        var encodingError = Assert.Throws<LocalFlowchartFormatException>(() =>
            ChartThemeParser.Parse([0xff, 0xfe], "invalid-encoding.json"));
        Assert.Contains("valid UTF-8 JSON", encodingError.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"schemaVersion\": 1", "\"schemaVersion\": 2", "schemaVersion must be the integer 1")]
    [InlineData("\"name\": \"Blue Water\",", "\"name\": \"Blue Water\", \"extra\": 1,", "unknown property")]
    [InlineData("\"name\": \"Blue Water\",", "\"name\": \"Blue Water\", \"name\": \"Duplicate\",", "duplicate property")]
    [InlineData("\"connector\": \"#28758C\"", "\"connector\": \"red\"", "must use #RRGGBB")]
    [InlineData("\"text\": \"#16323D\"", "\"text\": \"#FFFFFF\"", "at least 4.5:1 contrast")]
    public void ChartThemeParser_RejectsInvalidSchemaAndLowContrast(string oldValue, string newValue, string expectedMessage)
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ChartThemes", "blue-water.json");
        var json = File.ReadAllText(fixture).Replace(oldValue, newValue, StringComparison.Ordinal);

        var exception = Assert.Throws<LocalFlowchartFormatException>(() =>
            ChartThemeParser.Parse(Encoding.UTF8.GetBytes(json), "invalid-theme.json"));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Diagram_BuiltInThemeStylesPngAndEditableXmlWithoutChangingGraphGeometry()
    {
        Directory.CreateDirectory(_root);
        Write("input.md", """
            ```brownpants-flow
            flowchart ThemeCheck
            node start start "Begin"
            node decide decision "Ready?"
            node process process "Run task"
            edge start -> decide
            edge decide -> process label "yes"
            ```
            """);

        var plain = Run("input.md", "plain.png");
        var themed = Run("input.md", "blue.png", "--theme", "blue-water");
        Assert.Equal(0, plain.ExitCode);
        Assert.Equal(0, themed.ExitCode);

        var plainXml = XDocument.Load(Path.Combine(_root, "plain.drawio"));
        var blueXml = XDocument.Load(Path.Combine(_root, "blue.drawio"));
        Assert.Equal("#eaf6fa", (string?)blueXml.Descendants("mxGraphModel").Single().Attribute("background"));
        var blueRoot = blueXml.Descendants("root").Single();
        var decision = blueRoot.Elements("mxCell").Single(cell => (string?)cell.Attribute("value") == "Ready?");
        Assert.Equal("#cde8f5", StyleValue((string?)decision.Attribute("style"), "fillColor"));
        Assert.Equal("#16323d", StyleValue((string?)decision.Attribute("style"), "fontColor"));
        Assert.All(blueRoot.Elements("mxCell").Where(cell => (string?)cell.Attribute("edge") == "1"), edge =>
            Assert.Equal("#28758c", StyleValue((string?)edge.Attribute("style"), "strokeColor")));
        Assert.Equal(GraphGeometry(plainXml), GraphGeometry(blueXml));
        var bluePixels = PngPixels(File.ReadAllBytes(Path.Combine(_root, "blue.png"))).ToHashSet();
        Assert.Contains(((byte)234, (byte)246, (byte)250), bluePixels);
        Assert.Contains(((byte)205, (byte)232, (byte)245), bluePixels);
        Assert.Contains(((byte)40, (byte)117, (byte)140), bluePixels);
        Assert.NotEqual(File.ReadAllBytes(Path.Combine(_root, "plain.png")), File.ReadAllBytes(Path.Combine(_root, "blue.png")));

        var repeated = Run("input.md", "blue-repeat.png", "--theme", "blue-water");
        Assert.Equal(0, repeated.ExitCode);
        Assert.Equal(File.ReadAllBytes(Path.Combine(_root, "blue.png")), File.ReadAllBytes(Path.Combine(_root, "blue-repeat.png")));
        Assert.Equal(File.ReadAllBytes(Path.Combine(_root, "blue.drawio")), File.ReadAllBytes(Path.Combine(_root, "blue-repeat.drawio")));
    }

    [Theory]
    [InlineData("default", "#f7f9fc")]
    [InlineData("blue-water", "#eaf6fa")]
    [InlineData("high-contrast", "#ffffff")]
    public void Diagram_EachNamedThemeUsesItsDeclaredCanvas(string themeName, string expectedCanvas)
    {
        Directory.CreateDirectory(_root);
        Write("input.md", """
            ```brownpants-flow
            flowchart Palette
            node start start "Begin"
            node end end "Done"
            edge start -> end
            ```
            """);

        var result = Run("input.md", "named.png", "--theme", themeName);

        Assert.True(result.ExitCode == 0, result.Error);
        var xml = XDocument.Load(Path.Combine(_root, "named.drawio"));
        Assert.Equal(expectedCanvas, (string?)xml.Descendants("mxGraphModel").Single().Attribute("background"));
        var expectedRgb = (Convert.ToByte(expectedCanvas.Substring(1, 2), 16),
            Convert.ToByte(expectedCanvas.Substring(3, 2), 16), Convert.ToByte(expectedCanvas.Substring(5, 2), 16));
        Assert.Contains(PngPixels(File.ReadAllBytes(Path.Combine(_root, "named.png"))), pixel => pixel == expectedRgb);
    }

    [Fact]
    public void Diagram_CustomThemeFileIsRepositoryLocalAndInvalidSelectionWritesNothing()
    {
        Directory.CreateDirectory(_root);
        Write("input.md", """
            ```brownpants-flow
            flowchart CustomTheme
            node start start "Begin"
            node finish end "Done"
            edge start -> finish
            ```
            """);
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ChartThemes", "blue-water.json");
        Write("themes/team.json", File.ReadAllText(fixture));

        var valid = Run("input.md", "custom.png", "--theme-file", "themes/team.json");
        Assert.Equal(0, valid.ExitCode);
        var themedXml = XDocument.Load(Path.Combine(_root, "custom.drawio"));
        Assert.Equal("#eaf6fa", (string?)themedXml.Descendants("mxGraphModel").Single().Attribute("background"));

        var escaped = Run("input.md", "escaped.png", "--theme-file", "../outside.json");
        Assert.NotEqual(0, escaped.ExitCode);
        Assert.Contains("escapes the selected repository", escaped.Error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_root, "escaped.png")));
        Assert.False(File.Exists(Path.Combine(_root, "escaped.drawio")));

        var conflicting = Run("input.md", "conflicting.png", "--theme", "blue-water", "--theme-file", "themes/team.json");
        Assert.NotEqual(0, conflicting.ExitCode);
        Assert.Contains("either --theme or --theme-file", conflicting.Error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_root, "conflicting.png")));

        Write("themes/invalid.json", "{\"schemaVersion\": 2}");
        var invalid = Run("input.md", "invalid.png", "--theme-file", "themes/invalid.json");
        Assert.NotEqual(0, invalid.ExitCode);
        Assert.Contains("schemaVersion must be the integer 1", invalid.Error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_root, "invalid.png")));
        Assert.False(File.Exists(Path.Combine(_root, "invalid.drawio")));

        var oversizedThemePath = Path.Combine(_root, "themes", "large.json");
        File.WriteAllBytes(oversizedThemePath, new byte[ChartThemeParser.MaximumThemeBytes + 1]);
        var oversized = Run("input.md", "large.png", "--theme-file", "themes/large.json");
        Assert.NotEqual(0, oversized.ExitCode);
        Assert.Contains("byte limit", oversized.Error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_root, "large.png")));
    }

    [Fact]
    public void Diagram_ThemesSupportedDrawioImportAndPreservesGeometryWaypointsAndOtherStyleKeys()
    {
        Directory.CreateDirectory(_root);
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DrawioCompatibility", "waypoints-page.drawio");
        File.Copy(fixture, Path.Combine(_root, "input.drawio"));

        var original = Run("input.drawio", "original.drawio", "--input-format", "drawio", "--format", "drawio");
        var themed = Run("input.drawio", "themed.png", "--input-format", "drawio", "--theme", "blue-water");
        Assert.Equal(0, original.ExitCode);
        Assert.Equal(0, themed.ExitCode);

        var originalXml = XDocument.Load(Path.Combine(_root, "original.drawio"));
        var themedXml = XDocument.Load(Path.Combine(_root, "themed.drawio"));
        Assert.Equal(GraphGeometry(originalXml), GraphGeometry(themedXml));
        var graphModel = themedXml.Descendants("mxGraphModel").Single();
        Assert.Equal("#eaf6fa", (string?)graphModel.Attribute("background"));
        var root = graphModel.Element("root")!;
        var node = root.Elements("mxCell").Single(cell => (string?)cell.Attribute("id") == "left");
        Assert.StartsWith("rounded=", (string?)node.Attribute("style"));
        Assert.Equal("#ffffff", StyleValue((string?)node.Attribute("style"), "fillColor"));
        var edge = root.Elements("mxCell").Single(cell => (string?)cell.Attribute("id") == "route");
        Assert.StartsWith("edgeStyle=orthogonalEdgeStyle", (string?)edge.Attribute("style"));
        Assert.Equal("#28758c", StyleValue((string?)edge.Attribute("style"), "strokeColor"));
        Assert.Equal(new (string? X, string? Y)[] { ("250", "110"), ("250", "430") }, edge.Descendants("mxPoint")
            .Select(point => ((string?)point.Attribute("x"), (string?)point.Attribute("y"))).ToArray());
    }

    [Fact]
    public void Diagram_CustomThemeIsSharedByEveryTiledPngAndEditableSource()
    {
        Directory.CreateDirectory(_root);
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LocalFlowcharts", "tiled-release-pipeline.md");
        File.Copy(fixture, Path.Combine(_root, "pipeline.md"));
        var themeFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ChartThemes", "blue-water.json");
        Directory.CreateDirectory(Path.Combine(_root, "themes"));
        File.Copy(themeFixture, Path.Combine(_root, "themes", "team.json"));

        var result = Run("pipeline.md", "release/pipeline.png", "--theme-file", "themes/team.json",
            "--tile-size", "1800", "--overlap", "128");

        Assert.True(result.ExitCode == 0, result.Error);
        var manifestPath = Path.Combine(_root, "release", "pipeline", "pipeline.tiles.json");
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var entries = manifest.RootElement.GetProperty("tiles").EnumerateArray().ToArray();
        Assert.Equal(4, entries.Length);
        foreach (var entry in entries)
        {
            var imagePath = Path.Combine(_root, entry.GetProperty("image").GetString()!.Replace('/', Path.DirectorySeparatorChar));
            var xmlPath = Path.Combine(_root, entry.GetProperty("editableSource").GetString()!.Replace('/', Path.DirectorySeparatorChar));
            Assert.Contains(PngPixels(File.ReadAllBytes(imagePath)), pixel => pixel == ((byte)234, (byte)246, (byte)250));
            var xml = XDocument.Load(xmlPath);
            Assert.Equal("#eaf6fa", (string?)xml.Descendants("mxGraphModel").Single().Attribute("background"));
            var tileNodeCount = xml.Descendants("mxCell").Count(cell => (string?)cell.Attribute("vertex") == "1");
            Assert.InRange(tileNodeCount, 1, 27);
            _ = LocalFlowchartDrawioParser.Parse(File.ReadAllBytes(xmlPath), xmlPath);
        }
    }

    [Fact]
    public void Diagram_OmittedThemeMatchesExplicitDefaultBytes()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LocalFlowcharts", "compact-release-check.md");
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "compact-release-check.md"), File.ReadAllText(fixture));
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = Program.Run(["diagram", _root, "compact-release-check.md", "--output", "compact-release-check.png"], output, error);

        Assert.True(exitCode == 0, error.ToString());
        var explicitDefaultOutput = new StringWriter();
        var explicitDefaultError = new StringWriter();
        var explicitDefaultExitCode = Program.Run(
            ["diagram", _root, "compact-release-check.md", "--output", "explicit-default.png", "--theme", "default"],
            explicitDefaultOutput, explicitDefaultError);

        Assert.True(explicitDefaultExitCode == 0, explicitDefaultError.ToString());
        Assert.Equal(File.ReadAllBytes(Path.Combine(_root, "compact-release-check.png")),
            File.ReadAllBytes(Path.Combine(_root, "explicit-default.png")));
        Assert.Equal(File.ReadAllBytes(Path.Combine(_root, "compact-release-check.drawio")),
            File.ReadAllBytes(Path.Combine(_root, "explicit-default.drawio")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private (int ExitCode, string Output, string Error) Run(string source, string destination, params string[] options)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var args = new[] { "diagram", _root, source, "--output", destination }.Concat(options).ToArray();
        return (Program.Run(args, output, error), output.ToString(), error.ToString());
    }

    private void Write(string path, string content)
    {
        var fullPath = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private static string GraphGeometry(XDocument document) => string.Join('|',
        document.Descendants("mxCell").Where(cell => (string?)cell.Attribute("vertex") == "1")
            .OrderBy(cell => (string?)cell.Attribute("id"), StringComparer.Ordinal)
            .Select(cell => $"{cell.Attribute("id")?.Value}:{cell.Element("mxGeometry")?.Attribute("x")?.Value},{cell.Element("mxGeometry")?.Attribute("y")?.Value},{cell.Element("mxGeometry")?.Attribute("width")?.Value},{cell.Element("mxGeometry")?.Attribute("height")?.Value}")
        .Concat(document.Descendants("mxCell").Where(cell => (string?)cell.Attribute("edge") == "1")
            .OrderBy(cell => (string?)cell.Attribute("id"), StringComparer.Ordinal)
            .Select(cell => $"{cell.Attribute("id")?.Value}:{cell.Attribute("source")?.Value}->{cell.Attribute("target")?.Value}")));

    private static string? StyleValue(string? style, string key) => style?.Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2)).Where(parts => parts.Length == 2 && parts[0] == key)
        .Select(parts => parts[1]).SingleOrDefault();

    private static IEnumerable<(byte Red, byte Green, byte Blue)> PngPixels(byte[] png)
    {
        var width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        using var compressed = new MemoryStream();
        var offset = 8;
        while (offset < png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            var kind = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (kind == "IDAT") compressed.Write(png, offset + 8, length);
            offset += 12 + length;
            if (kind == "IEND") break;
        }
        compressed.Position = 0;
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        var rowLength = checked(width * 3 + 1);
        var scanlines = new byte[checked(rowLength * height)];
        zlib.ReadExactly(scanlines);
        for (var y = 0; y < height; y++)
        {
            var row = y * rowLength;
            if (scanlines[row] != 0) throw new InvalidDataException("Expected unfiltered BrownPants PNG scanlines.");
            for (var x = 0; x < width; x++)
            {
                var pixel = row + 1 + x * 3;
                yield return (scanlines[pixel], scanlines[pixel + 1], scanlines[pixel + 2]);
            }
        }
    }
}
