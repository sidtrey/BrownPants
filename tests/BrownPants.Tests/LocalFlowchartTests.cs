using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using BrownPants.Cli;
using BrownPants.Cli.Analysis;

namespace BrownPants.Tests;

public sealed class LocalFlowchartTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brownpants-flowchart-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void DrawioParser_AcceptsSupportedCorpusAndRejectsUnsafeOrUnmodeledDocuments()
    {
        var fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DrawioCompatibility");
        foreach (var name in new[] { "plain-basic.drawio", "legacy-model.drawio", "compressed-page.drawio", "waypoints-page.drawio" })
        {
            var bytes = File.ReadAllBytes(Path.Combine(fixtures, name));
            var document = LocalFlowchartDrawioParser.Parse(bytes, name);
            Assert.NotEmpty(document.Nodes);
        }

        foreach (var name in new[] { "multiple-pages.drawio", "unsupported-custom-shape.drawio", "unsafe-doctype.drawio" })
        {
            var bytes = File.ReadAllBytes(Path.Combine(fixtures, name));
            var exception = Assert.Throws<LocalFlowchartFormatException>(() => LocalFlowchartDrawioParser.Parse(bytes, name));
            Assert.Contains(name, exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Diagram_ImportsDrawioToPngAndRoundTripsSupportedIdentityStyleAndGeometry()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DrawioCompatibility", "plain-basic.drawio");
        Write("input.drawio", File.ReadAllText(fixture));

        var result = Run("input.drawio", "out/import.png", "--input-format", "drawio");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(_root, "out", "import.png")));
        var pngSidecar = XDocument.Load(Path.Combine(_root, "out", "import.drawio"));
        var root = pngSidecar.Descendants("root").Single();
        var start = root.Elements("mxCell").Single(cell => (string?)cell.Attribute("id") == "start");
        Assert.Equal("Start", (string?)start.Attribute("value"));
        Assert.Equal("ellipse;whiteSpace=wrap;html=0;", (string?)start.Attribute("style"));
        Assert.Equal("40", (string?)start.Element("mxGeometry")?.Attribute("x"));
        Assert.Equal("60", (string?)start.Element("mxGeometry")?.Attribute("y"));
        Assert.Equal("120", (string?)start.Element("mxGeometry")?.Attribute("width"));
        Assert.Equal("60", (string?)start.Element("mxGeometry")?.Attribute("height"));
        var importedEdge = root.Elements("mxCell").Single(cell => (string?)cell.Attribute("id") == "edge1");
        Assert.Equal("start", (string?)importedEdge.Attribute("source"));
        Assert.Equal("process", (string?)importedEdge.Attribute("target"));
        Assert.Equal("edgeStyle=orthogonalEdgeStyle;endArrow=block;html=0;", (string?)importedEdge.Attribute("style"));

        var selfOverwrite = Run("input.drawio", "input.drawio", "--input-format", "drawio", "--format", "drawio", "--overwrite");
        Assert.NotEqual(0, selfOverwrite.ExitCode);
        Assert.Contains("cannot overwrite", selfOverwrite.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DrawioParser_RejectsUnknownShapeInsteadOfSilentlyChangingItsMeaning()
    {
        const string xml = """
            <mxGraphModel><root><mxCell id="0"/><mxCell id="1" parent="0"/>
            <mxCell id="x" value="Custom" style="shape=mxgraph.custom.server;" vertex="1" parent="1">
            <mxGeometry x="1" y="1" width="120" height="60" as="geometry"/></mxCell></root></mxGraphModel>
            """;

        var exception = Assert.Throws<LocalFlowchartFormatException>(() =>
            LocalFlowchartDrawioParser.Parse(Encoding.UTF8.GetBytes(xml), "custom.drawio"));

        Assert.Contains("unsupported shape", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DrawioParser_AcceptsRendererEdgeStyleAndBoundedOffPageCoordinates()
    {
        const string xml = """
            <mxGraphModel><root><mxCell id="0"/><mxCell id="1" parent="0"/>
            <mxCell id="source" value="Source" style="rounded=0;whiteSpace=wrap;html=0;fillColor=#e8f1fa;strokeColor=#243447;" vertex="1" parent="1"><mxGeometry x="-96" y="-32" width="120" height="60" as="geometry"/></mxCell>
            <mxCell id="target" value="Target" style="rounded=0;whiteSpace=wrap;html=0;fillColor=#e8f1fa;strokeColor=#243447;" vertex="1" parent="1"><mxGeometry x="160" y="64" width="120" height="60" as="geometry"/></mxCell>
            <mxCell id="route" value="" style="edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=0;endArrow=block;endFill=1;" edge="1" parent="1" source="source" target="target"><mxGeometry relative="1" as="geometry"/></mxCell>
            </root></mxGraphModel>
            """;

        var document = LocalFlowchartDrawioParser.Parse(Encoding.UTF8.GetBytes(xml), "tile.drawio");

        Assert.Equal(-96, document.Nodes.Single(node => node.ImportedCellId == "source").ImportedGeometry?.X);
        Assert.Equal(-32, document.Nodes.Single(node => node.ImportedCellId == "source").ImportedGeometry?.Y);
        Assert.Equal("edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=0;endArrow=block;endFill=1;",
            document.Edges.Single().ImportedStyle);
    }

    [Fact]
    public void DrawioParser_RejectsOffPageCoordinatesBeyondLogicalBounds()
    {
        var outOfBounds = -LocalFlowchartLayoutEngine.MaximumLogicalDimension - 1;
        var xml = $"""
            <mxGraphModel><root><mxCell id="0"/><mxCell id="1" parent="0"/>
            <mxCell id="node" value="Node" style="rounded=0;whiteSpace=wrap;html=0;" vertex="1" parent="1"><mxGeometry x="{outOfBounds}" y="0" width="120" height="60" as="geometry"/></mxCell>
            </root></mxGraphModel>
            """;

        var exception = Assert.Throws<LocalFlowchartFormatException>(() =>
            LocalFlowchartDrawioParser.Parse(Encoding.UTF8.GetBytes(xml), "out-of-bounds.drawio"));

        Assert.Contains("out-of-bounds.drawio", exception.Message, StringComparison.Ordinal);
        Assert.Contains("must be an integer between", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DrawioParser_RejectsUnknownEdgeRoutingStyles()
    {
        const string xml = """
            <mxGraphModel><root><mxCell id="0"/><mxCell id="1" parent="0"/>
            <mxCell id="a" value="A" style="ellipse;" vertex="1" parent="1"><mxGeometry x="0" y="0" width="100" height="60" as="geometry"/></mxCell>
            <mxCell id="b" value="B" style="ellipse;" vertex="1" parent="1"><mxGeometry x="200" y="0" width="100" height="60" as="geometry"/></mxCell>
            <mxCell id="e" value="" style="edgeStyle=javascript;" edge="1" parent="1" source="a" target="b"><mxGeometry relative="1" as="geometry"/></mxCell>
            </root></mxGraphModel>
            """;

        var exception = Assert.Throws<LocalFlowchartFormatException>(() =>
            LocalFlowchartDrawioParser.Parse(Encoding.UTF8.GetBytes(xml), "unknown-edge-style.drawio"));

        Assert.Contains("unsupported or unsafe style", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Diagram_PreservesImportedPageBoundsAndOrthogonalWaypoints()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DrawioCompatibility", "waypoints-page.drawio");
        Write("input.drawio", File.ReadAllText(fixture));

        var result = Run("input.drawio", "out/roundtrip.drawio", "--input-format", "drawio", "--format", "drawio");

        Assert.Equal(0, result.ExitCode);
        var xml = XDocument.Load(Path.Combine(_root, "out", "roundtrip.drawio"));
        var model = xml.Descendants("mxGraphModel").Single();
        Assert.Equal("900", (string?)model.Attribute("pageWidth"));
        Assert.Equal("700", (string?)model.Attribute("pageHeight"));
        var root = model.Element("root")!;
        var vertex = root.Elements("mxCell").Single(cell => (string?)cell.Attribute("id") == "left");
        Assert.Equal("#E8F1FA", ExtractStyle((string?)vertex.Attribute("style"), "fillColor"));
        var edge = root.Elements("mxCell").Single(cell => (string?)cell.Attribute("id") == "route");
        var points = edge.Descendants("mxPoint").Select(point => ((string?)point.Attribute("x"), (string?)point.Attribute("y"))).ToArray();
        Assert.Equal(new (string? X, string? Y)[] { ("250", "110"), ("250", "430") }, points);
        Assert.Equal("Approval", (string?)xml.Descendants("diagram").Single().Attribute("name"));
    }

    private static string? ExtractStyle(string? style, string key) => style?.Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Select(item => item.Split('=', 2)).Where(parts => parts.Length == 2 && parts[0] == key)
        .Select(parts => parts[1]).SingleOrDefault();

    [Fact]
    public void Diagram_RepresentativeLayoutHasNoNodeOrLabelOverlap()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LocalFlowcharts", "access-review.md");
        var document = LocalFlowchartParser.Parse(File.ReadAllText(fixture), "access-review.md");
        var layout = LocalFlowchartLayoutEngine.Create(document);

        AssertNodeBoundsDoNotOverlap(layout);
        AssertEdgesAvoidUnrelatedNodes(layout);
        Assert.InRange(CountUnrelatedEdgeCrossings(layout), 0, layout.Edges.Count);
        foreach (var node in layout.Nodes)
        {
            var maxTextWidth = node.LabelLines.Max(line => line.Length * 16);
            Assert.True(maxTextWidth <= node.Width - 24, $"Label for {node.Node.Id} exceeds node width.");
            Assert.True(node.LabelLines.Count * 20 <= node.Height - 16, $"Label for {node.Node.Id} exceeds node height.");
        }
    }

    [Fact]
    public void Diagram_CompactReleaseCheckRemainsReadableAtItsDocumentedDisplaySize()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LocalFlowcharts", "compact-release-check.md");
        var source = File.ReadAllText(fixture);
        var document = LocalFlowchartParser.Parse(source, "compact-release-check.md");
        var layout = LocalFlowchartLayoutEngine.Create(document);
        Write("compact-release-check.md", source);

        AssertNodeBoundsDoNotOverlap(layout);
        AssertEdgeLabelBoundsDoNotOverlap(layout);
        Assert.All(layout.Nodes, node => Assert.DoesNotContain("...", node.Node.Label, StringComparison.Ordinal));
        foreach (var node in layout.Nodes)
        {
            Assert.True(node.LabelLines.Max(line => line.Length * 16) <= node.Width - 24,
                $"Label for {node.Node.Id} does not fit its compact shape.");
            Assert.True(node.LabelLines.Count * 20 <= node.Height - 16,
                $"Label for {node.Node.Id} does not fit vertically in its compact shape.");
        }
        var result = Run("compact-release-check.md", "compact/release-check.png");

        Assert.True(result.ExitCode == 0, result.Error);
        var pngPath = Path.Combine(_root, "compact", "release-check.png");
        var png = File.ReadAllBytes(pngPath);
        var width = (int)ReadBigEndian(png, 16);
        var height = (int)ReadBigEndian(png, 20);
        Assert.InRange(width, 1, 1200);
        Assert.InRange(height, 1, 1250);
        Assert.True(File.Exists(Path.Combine(_root, "compact", "release-check.drawio")));
    }

    [Fact]
    public void Diagram_ReleasePipelineShowcaseHasOrderedTilesAndContinuityOverlap()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LocalFlowcharts", "tiled-release-pipeline.md");
        var source = File.ReadAllText(fixture);
        var document = LocalFlowchartParser.Parse(source, "tiled-release-pipeline.md");
        var layout = LocalFlowchartLayoutEngine.Create(document);
        Write("tiled-release-pipeline.md", source);

        Assert.Equal(0, CountUnrelatedEdgeCrossings(layout));
        AssertEdgeLabelBoundsDoNotOverlap(layout);

        const int tileSize = 1800;
        const int overlap = 128;
        var result = Run("tiled-release-pipeline.md", "showcase/release-pipeline.png", "--tile-size", tileSize.ToString(), "--overlap", overlap.ToString());

        Assert.True(result.ExitCode == 0, result.Error);
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(_root, "showcase", "release-pipeline", "release-pipeline.tiles.json")));
        var root = manifest.RootElement;
        var tiles = root.GetProperty("tiles").EnumerateArray().ToArray();
        Assert.True(root.GetProperty("canvasHeight").GetInt32() > tileSize * 3);
        Assert.Equal(4, tiles.Length);
        Assert.Equal(Enumerable.Range(1, tiles.Length), tiles.Select(tile => tile.GetProperty("order").GetInt32()));
        var orderedPositions = tiles.Select(tile => (Row: tile.GetProperty("row").GetInt32(), Column: tile.GetProperty("column").GetInt32())).ToArray();
        Assert.Equal(orderedPositions.OrderBy(position => position.Row).ThenBy(position => position.Column), orderedPositions);
        Assert.All(tiles, tile =>
        {
            Assert.InRange(tile.GetProperty("width").GetInt32(), 1, tileSize);
            Assert.InRange(tile.GetProperty("height").GetInt32(), 1, tileSize);
            Assert.True(File.Exists(Path.Combine(_root, tile.GetProperty("image").GetString()!.Replace('/', Path.DirectorySeparatorChar))));
            Assert.True(File.Exists(Path.Combine(_root, tile.GetProperty("editableSource").GetString()!.Replace('/', Path.DirectorySeparatorChar))));
        });
        var rows = tiles.GroupBy(tile => tile.GetProperty("row").GetInt32());
        foreach (var row in rows)
        {
            var rowTiles = row.OrderBy(tile => tile.GetProperty("column").GetInt32()).ToArray();
            for (var index = 1; index < rowTiles.Length; index++)
            {
                Assert.Equal(overlap, rowTiles[index - 1].GetProperty("x").GetInt32()
                    + rowTiles[index - 1].GetProperty("width").GetInt32() - rowTiles[index].GetProperty("x").GetInt32());
                Assert.Equal(overlap, rowTiles[index].GetProperty("overlapWithLeft").GetInt32());
            }
        }
        var columns = tiles.GroupBy(tile => tile.GetProperty("column").GetInt32());
        foreach (var column in columns)
        {
            var columnTiles = column.OrderBy(tile => tile.GetProperty("row").GetInt32()).ToArray();
            for (var index = 1; index < columnTiles.Length; index++)
            {
                Assert.Equal(overlap, columnTiles[index - 1].GetProperty("y").GetInt32()
                    + columnTiles[index - 1].GetProperty("height").GetInt32() - columnTiles[index].GetProperty("y").GetInt32());
                Assert.Equal(overlap, columnTiles[index].GetProperty("overlapWithTop").GetInt32());
            }
        }
    }

    [Fact]
    public void Layout_RanksDeclaredFlowForwardAndLeavesCycleReturnAsBackEdge()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LocalFlowcharts", "access-review.md");
        var document = LocalFlowchartParser.Parse(File.ReadAllText(fixture), "access-review.md");

        var layout = LocalFlowchartLayoutEngine.Create(document);

        Assert.True(layout.Node("register").Rank < layout.Node("risk").Rank);
        Assert.True(layout.Node("manual").Rank < layout.Node("grant").Rank);
        Assert.True(layout.Node("retry").Rank > layout.Node("parse").Rank);
        Assert.Contains(layout.Edges, edge => edge.Edge.FromId == "retry" && edge.Edge.ToId == "parse");
        AssertEdgesAvoidUnrelatedNodes(layout);
    }

    [Fact]
    public void Layout_ReportsRemainingConnectorCrossingsAndPreservesEdges()
    {
        var edges = new[]
        {
            new LocalFlowchartLayoutEdge(new LocalFlowchartEdge("a", "b", null, 1, 0),
                [new(0, 100), new(200, 100)], 0, 0, 0, 0, []),
            new LocalFlowchartLayoutEdge(new LocalFlowchartEdge("c", "d", null, 2, 1),
                [new(100, 0), new(100, 200)], 0, 0, 0, 0, [])
        };
        var layout = new LocalFlowchartLayout(240, 240, 0, [], edges, LocalFlowchartDirection.TopDown);

        var diagnostics = LocalFlowchartLayoutEngine.DiagnoseCrossings(layout);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("connector-crossings", diagnostic.Code);
        Assert.Contains("1 connector pair", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("a->b with c->d at canvas (100, 100)", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(2, layout.Edges.Count);
        var output = new StringWriter();
        LocalFlowchartCommand.WriteLayoutDiagnostics(layout with { Diagnostics = diagnostics }, output);
        Assert.Contains("Layout diagnostic [connector-crossings]", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Layout_ReportsWhenCrossingAnalysisExceedsItsBound()
    {
        var edges = Enumerable.Range(0, LocalFlowchartLayoutEngine.MaximumCrossingAnalysisEdges + 1)
            .Select(index => new LocalFlowchartLayoutEdge(new LocalFlowchartEdge($"a{index}", $"b{index}", null, index + 1, index),
                [new(0, index), new(1, index)], 0, 0, 0, 0, []))
            .ToArray();
        var layout = new LocalFlowchartLayout(100, edges.Length + 10, 0, [], edges, LocalFlowchartDirection.TopDown);

        var diagnostic = Assert.Single(LocalFlowchartLayoutEngine.DiagnoseCrossings(layout));

        Assert.Equal("crossing-analysis-limit", diagnostic.Code);
        Assert.Contains("20,000 edges", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Layout_WrapsLongNodeAndEdgeLabelsAndKeepsTheirBoundsClear()
    {
        var longNodeLabel = "A long service responsibility that must remain readable and complete across multiple wrapped lines";
        var nodes = new[]
        {
            new LocalFlowchartNode("source", LocalFlowchartNodeShape.Start, "Request received", 1, 0),
            new LocalFlowchartNode("approve", LocalFlowchartNodeShape.Decision, longNodeLabel, 2, 1),
            new LocalFlowchartNode("reject", LocalFlowchartNodeShape.Process, "Reject request", 3, 2),
            new LocalFlowchartNode("accept", LocalFlowchartNodeShape.End, "Approve request", 4, 3)
        };
        var edges = new[]
        {
            new LocalFlowchartEdge("source", "approve", "Check ownership, permissions, and policy before approval", 5, 0),
            new LocalFlowchartEdge("approve", "reject", "Policy failed; record the reason and notify the request owner", 6, 1),
            new LocalFlowchartEdge("approve", "accept", "All required checks passed; grant the smallest required access", 7, 2)
        };
        var document = new LocalFlowchartDocument("label-stress", "labels.md", nodes, edges);

        var layout = LocalFlowchartLayoutEngine.Create(document);

        AssertNodeBoundsDoNotOverlap(layout);
        Assert.All(layout.Nodes, node =>
        {
            Assert.True(node.LabelLines.Max(line => line.Length) * 16 <= node.Width - 24);
            Assert.True(node.LabelLines.Count * 20 <= node.Height - 16);
        });
        Assert.Contains(layout.Nodes.Single(node => node.Node.Id == "approve").LabelLines, line => line.Length < longNodeLabel.Length);
        Assert.All(layout.Edges.Where(edge => edge.Edge.Label is not null), edge => Assert.True(edge.LabelLines.Count > 1));
        AssertEdgeLabelBoundsDoNotOverlap(layout);
    }

    [Fact]
    public void Layout_RejectsImportedGeometryThatCannotFitItsFullLabel()
    {
        var node = new LocalFlowchartNode("n", LocalFlowchartNodeShape.Process, "A label that cannot fit", 12, 0,
            ImportedGeometry: new LocalFlowchartImportedGeometry(20, 20, 60, 40));
        var document = new LocalFlowchartDocument("unfit", "source.drawio", [node], []);

        var exception = Assert.Throws<LocalFlowchartFormatException>(() => LocalFlowchartLayoutEngine.Create(document));

        Assert.Contains("cannot fit inside its shape", exception.Message, StringComparison.Ordinal);
        Assert.Contains("source.drawio:12", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Diagram_DetailsStayInEditableTooltipsWithoutCrowdingVisibleLabels()
    {
        const string withDetails = """
            ```brownpants-flow
            flowchart details
            node entry start "Request received" details "The old adapter validates the request before the workflow begins."
            node finish end "Complete"
            edge entry -> finish label "accepted" details "The request passed the legacy policy and persistence checks."
            ```
            """;
        const string withoutDetails = """
            ```brownpants-flow
            flowchart details
            node entry start "Request received"
            node finish end "Complete"
            edge entry -> finish label "accepted"
            ```
            """;
        var detailedDocument = LocalFlowchartParser.Parse(withDetails, "details.md");
        var plainDocument = LocalFlowchartParser.Parse(withoutDetails, "plain.md");

        var detailedLayout = LocalFlowchartLayoutEngine.Create(detailedDocument);
        var plainLayout = LocalFlowchartLayoutEngine.Create(plainDocument);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(LocalFlowchartDrawioWriter.Write(detailedDocument)));

        Assert.Equal((plainLayout.Width, plainLayout.Height), (detailedLayout.Width, detailedLayout.Height));
        Assert.Contains("The old adapter validates", xml.Descendants("mxCell").Single(cell => (string?)cell.Attribute("value") == "Request received").Attribute("tooltip")?.Value);
        Assert.Contains("The request passed", xml.Descendants("mxCell").Single(cell => (string?)cell.Attribute("value") == "accepted").Attribute("tooltip")?.Value);
    }

    [Fact]
    public void DrawioWriter_StreamsTheSameDeterministicXmlAndLeavesDestinationOpen()
    {
        var document = new LocalFlowchartDocument("stream-check", "flow.md",
            [new LocalFlowchartNode("start", LocalFlowchartNodeShape.Start, "Start", 1, 0),
             new LocalFlowchartNode("finish", LocalFlowchartNodeShape.End, "Finish", 2, 1)],
            [new LocalFlowchartEdge("start", "finish", null, 3, 0)]);
        var layout = LocalFlowchartLayoutEngine.Create(document);
        using var destination = new MemoryStream();

        LocalFlowchartDrawioWriter.Write(document, layout, destination);

        Assert.True(destination.CanWrite);
        Assert.Equal(LocalFlowchartDrawioWriter.Write(document), destination.ToArray());
    }

    [Fact]
    public void DrawioWriter_PreservesComputedReturnRoutesInEditableXml()
    {
        const string source = """
            ```brownpants-flow
            flowchart routes
            node start start "Start"
            node review process "Review"
            node finish end "Finish"
            edge start -> review
            edge review -> finish
            edge finish -> start label "retry"
            ```
            """;
        var document = LocalFlowchartParser.Parse(source, "routes.md");
        var layout = LocalFlowchartLayoutEngine.Create(document);
        var routedEdge = layout.Edges.Single(edge => edge.Edge.FromId == "finish");
        var expected = routedEdge.Route.Skip(1).SkipLast(1)
            .Select(point => ((string?)point.X.ToString(CultureInfo.InvariantCulture), (string?)point.Y.ToString(CultureInfo.InvariantCulture)))
            .ToArray();
        Assert.NotEmpty(expected);

        using var firstOutput = new MemoryStream();
        LocalFlowchartDrawioWriter.Write(document, layout, firstOutput);
        using var output = new MemoryStream();
        LocalFlowchartDrawioWriter.Write(document, layout, output);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(output.ToArray()));
        var edgeCell = xml.Descendants("mxCell").Single(cell => (string?)cell.Attribute("id") == $"e{routedEdge.Edge.Order + 1}");
        var actual = edgeCell.Descendants("mxPoint")
            .Select(point => ((string?)point.Attribute("x"), (string?)point.Attribute("y"))).ToArray();

        Assert.Equal(expected, actual);
        Assert.Equal(firstOutput.ToArray(), output.ToArray());
        var targetNode = document.Nodes.Single(node => node.Id == routedEdge.Edge.ToId);
        Assert.Equal($"n{targetNode.Order + 1}", (string?)edgeCell.Attribute("target"));
    }

    [Fact]
    public void Parser_ReportsActionableLimitsForVisibleLabelsAndTooltipDetails()
    {
        var longLabel = $"```brownpants-flow\nflowchart limits\nnode item process \"{new string('x', 81)}\"\n```";
        var longDetails = $"```brownpants-flow\nflowchart limits\nnode item process \"Short\" details \"{new string('x', 1001)}\"\n```";

        var labelError = Assert.Throws<LocalFlowchartFormatException>(() => LocalFlowchartParser.Parse(longLabel, "labels.md"));
        var detailsError = Assert.Throws<LocalFlowchartFormatException>(() => LocalFlowchartParser.Parse(longDetails, "details.md"));

        Assert.Contains("at most 80 characters", labelError.Message, StringComparison.Ordinal);
        Assert.Contains("at most 1000 characters", detailsError.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Diagram_RendersLongLabelsWithMatchingXmlAndReservedEdgeBounds()
    {
        const string source = """
            ```brownpants-flow
            flowchart review
            node start start "Request received"
            node check decision "Verify ownership and required access policy"
            node done end "Complete"
            edge start -> check label "Inspect the current owner and requested permissions"
            edge check -> done label "All checks passed; record the approval"
            ```
            """;
        Write("docs/long-labels.md", source);

        var result = Run("docs/long-labels.md", "out/long-labels.png");

        Assert.Equal(0, result.ExitCode);
        var png = File.ReadAllBytes(Path.Combine(_root, "out", "long-labels.png"));
        var xml = XDocument.Load(Path.Combine(_root, "out", "long-labels.drawio"));
        var document = LocalFlowchartParser.Parse(source, "docs/long-labels.md");
        var layout = LocalFlowchartLayoutEngine.Create(document);
        Assert.Equal((uint)layout.Width, ReadBigEndian(png, 16));
        Assert.Equal((uint)layout.Height, ReadBigEndian(png, 20));
        Assert.Contains(xml.Descendants("mxCell"), cell => (string?)cell.Attribute("value") == "Inspect the current owner and requested permissions");
        Assert.Contains(xml.Descendants("mxCell"), cell => (string?)cell.Attribute("value") == "All checks passed; record the approval");
        AssertEdgeLabelBoundsDoNotOverlap(layout);
    }

    [Fact]
    public void Diagram_DirectionAndProfileOptionsAreExplicitAndExplainTheResult()
    {
        Write("docs/flow.md", Fixture);
        var result = Run("docs/flow.md", "out/flow.png", "--direction", "left-to-right", "--profile", "software");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("This chart shows 4 workflow steps and 4 connections.", result.Output);
        Assert.Contains("not observed runtime behavior", result.Output);
        var png = File.ReadAllBytes(Path.Combine(_root, "out", "flow.png"));
        var document = LocalFlowchartParser.Parse(Fixture, "docs/flow.md");
        var expectedLayout = LocalFlowchartLayoutEngine.Create(document,
            new LocalFlowchartLayoutOptions(LocalFlowchartDirection.LeftToRight));
        Assert.Equal((uint)expectedLayout.Width, ReadBigEndian(png, 16));
        Assert.Equal((uint)expectedLayout.Height, ReadBigEndian(png, 20));
        var xml = XDocument.Load(Path.Combine(_root, "out", "flow.drawio"));
        var reviewCell = xml.Descendants("mxCell").Single(cell => (string?)cell.Attribute("value") == "Approved?");
        var reviewGeometry = reviewCell.Element("mxGeometry")!;
        Assert.Equal(expectedLayout.Node("review").X.ToString(), (string?)reviewGeometry.Attribute("x"));
        Assert.Equal(expectedLayout.Node("review").Y.ToString(), (string?)reviewGeometry.Attribute("y"));
        Assert.Equal(2, Run("docs/flow.md", "out/bad.png", "--profile", "unknown").ExitCode);
    }

    [Fact]
    public void CallerChart_RequiresSemanticOptInAndWritesLocalPngWithEditableSource()
    {
        Write("Demo.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        Write("src/Core.cs", "namespace Demo; public class Core { public void Save() { } }");
        Write("src/Service.cs", "namespace Demo; public class Service { public void Run(Core core) { core.Save(); } }");
        Write("tests/CoreTests.cs", "namespace Demo; public class CoreTests { public void Saves(Core core, Service service) { service.Run(core); } }");
        var args = new[] { "chart", "callers", _root, "--source", "src/Core.cs", "--method", "global::Demo.Core.Save()", "--output", "out/callers.png" };

        var optInError = new StringWriter();
        Assert.Equal(2, Program.Run(args, TextWriter.Null, optInError));
        Assert.Contains("explicit --semantic", optInError.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_root, "out", "callers.png")));

        var output = new StringWriter();
        var error = new StringWriter();
        var result = Program.Run(args.Append("--semantic").ToArray(), output, error);

        Assert.True(result == 0, error.ToString());
        Assert.Contains("1 direct and 1 transitive callers", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("test-path candidate", output.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_root, "out", "callers.png")));
        var xml = XDocument.Load(Path.Combine(_root, "out", "callers.drawio"));
        Assert.Contains(xml.Descendants().Attributes().Select(attribute => attribute.Value), value =>
            value.Contains("Test candidate:", StringComparison.Ordinal));
        Assert.Contains(xml.Descendants().Attributes("tooltip").Select(attribute => attribute.Value), value =>
            value.Contains("Source: src/Service.cs:1", StringComparison.Ordinal)
            && value.Contains("Target: global::Demo.Core.Save()", StringComparison.Ordinal));
    }

    [Fact]
    public void Diagram_InputOutputAndParallelEdgesAreRetained()
    {
        const string source = """
            ```brownpants-flow
            flowchart symbols
            node begin start "Begin"
            node input input-output "Read input"
            node finish end "Done"
            edge begin -> input label "file"
            edge begin -> input label "message"
            edge input -> finish
            ```
            """;
        var document = LocalFlowchartParser.Parse(source, "symbols.md");
        Assert.Equal(LocalFlowchartNodeShape.InputOutput, document.Nodes[1].Shape);
        Assert.Equal(3, document.Edges.Count);
        Assert.Equal(3, LocalFlowchartLayoutEngine.Create(document).Edges.Count);
    }

    [Fact]
    public void Diagram_NonAsciiLabelsRenderSafelyAndRemainExactInEditableXml()
    {
        const string source = "```brownpants-flow\nflowchart unicode\nnode start start \"Review Ω\"\nnode done end \"Готово\"\nedge start -> done label \"café\"\n```";
        Write("docs/unicode.md", source);

        var result = Run("docs/unicode.md", "out/unicode.png");

        Assert.True(result.ExitCode == 0, result.Error);
        Assert.Equal("mxfile", XDocument.Load(Path.Combine(_root, "out", "unicode.drawio")).Root!.Name.LocalName);
        var xml = File.ReadAllText(Path.Combine(_root, "out", "unicode.drawio"));
        Assert.Contains("Review Ω", xml, StringComparison.Ordinal);
        Assert.Contains("Готово", xml, StringComparison.Ordinal);
        Assert.Contains("café", xml, StringComparison.Ordinal);
        Assert.Equal("IHDR", Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(_root, "out", "unicode.png")), 12, 4));
    }
    [Fact]
    public void Diagram_OrganizationAndBrainstormFamiliesPreserveOnlyDeclaredTrees()
    {
        const string organization = """
            ```brownpants-org
            organization support
            person director "Service Director"
            person qa "Quality Assurance"
            person dev "Development Team"
            reports director -> qa
            reports director -> dev
            ```
            """;
        const string brainstorm = """
            ```brownpants-brainstorm
            brainstorm handoff
            idea root "Safer releases"
            idea qa "Better QA handoff"
            idea build "Link release to build"
            idea sample "Provide test examples"
            branch root -> qa
            branch root -> build
            branch qa -> sample
            ```
            """;
        var orgDocument = LocalFlowchartParser.Parse(organization, "org.md");
        var ideaDocument = LocalFlowchartParser.Parse(brainstorm, "ideas.md");
        Assert.Equal(LocalFlowchartKind.Organization, orgDocument.Kind);
        Assert.All(orgDocument.Nodes, node => Assert.Equal(LocalFlowchartNodeShape.Person, node.Shape));
        Assert.Equal(LocalFlowchartKind.Brainstorm, ideaDocument.Kind);
        Assert.All(ideaDocument.Nodes, node => Assert.Equal(LocalFlowchartNodeShape.Cloud, node.Shape));
        Assert.Equal(3, ideaDocument.Edges.Count);

        Write("docs/org.md", organization);
        var orgResult = Run("docs/org.md", "out/org.png");
        Assert.True(orgResult.ExitCode == 0, orgResult.Error);
        Assert.Contains("3 people and 2 reporting links", orgResult.Output);
        Write("docs/ideas.md", brainstorm);
        var ideaResult = Run("docs/ideas.md", "out/ideas.drawio", "--format", "drawio");
        Assert.True(ideaResult.ExitCode == 0, ideaResult.Error);
        Assert.Contains("groups 4 ideas into 3 branches", ideaResult.Output);
        Assert.Contains("cloud", File.ReadAllText(Path.Combine(_root, "out", "ideas.drawio")), StringComparison.Ordinal);

        const string invalid = """
            ```brownpants-org
            organization broken
            person ceo "Director"
            person one "Team One"
            person two "Team Two"
            person child "Shared Team"
            reports ceo -> one
            reports ceo -> two
            reports one -> child
            reports two -> child
            ```
            """;
        Write("docs/invalid-org.md", invalid);
        var rejected = Run("docs/invalid-org.md", "out/invalid.png");
        Assert.NotEqual(0, rejected.ExitCode);
        Assert.Contains("more than one manager", rejected.Error, StringComparison.OrdinalIgnoreCase);
    }
    private static void AssertEdgesAvoidUnrelatedNodes(LocalFlowchartLayout layout)
    {
        foreach (var edge in layout.Edges)
        foreach (var node in layout.Nodes.Where(node => node.Node.Id != edge.Edge.FromId && node.Node.Id != edge.Edge.ToId))
        foreach (var (start, end) in edge.Route.Zip(edge.Route.Skip(1)))
        {
            var horizontalHit = start.Y == end.Y && start.Y > node.Y && start.Y < node.Y + node.Height
                && Math.Max(start.X, end.X) > node.X && Math.Min(start.X, end.X) < node.X + node.Width;
            var verticalHit = start.X == end.X && start.X > node.X && start.X < node.X + node.Width
                && Math.Max(start.Y, end.Y) > node.Y && Math.Min(start.Y, end.Y) < node.Y + node.Height;
            Assert.False(horizontalHit || verticalHit,
                $"Edge {edge.Edge.FromId} -> {edge.Edge.ToId} crosses unrelated node {node.Node.Id} at ({node.X},{node.Y},{node.Width},{node.Height}); route: {string.Join(" ", edge.Route.Select(point => $"{point.X},{point.Y}"))}.");
        }
    }

    private static int CountUnrelatedEdgeCrossings(LocalFlowchartLayout layout)
    {
        var crossings = 0;
        for (var firstIndex = 0; firstIndex < layout.Edges.Count; firstIndex++)
        for (var secondIndex = firstIndex + 1; secondIndex < layout.Edges.Count; secondIndex++)
        {
            var first = layout.Edges[firstIndex];
            var second = layout.Edges[secondIndex];
            if (new[] { first.Edge.FromId, first.Edge.ToId }.Intersect([second.Edge.FromId, second.Edge.ToId]).Any()) continue;
            foreach (var (a, b) in first.Route.Zip(first.Route.Skip(1)))
            foreach (var (c, d) in second.Route.Zip(second.Route.Skip(1)))
            {
                var horizontalVertical = a.Y == b.Y && c.X == d.X
                    && c.X > Math.Min(a.X, b.X) && c.X < Math.Max(a.X, b.X)
                    && a.Y > Math.Min(c.Y, d.Y) && a.Y < Math.Max(c.Y, d.Y);
                var verticalHorizontal = a.X == b.X && c.Y == d.Y
                    && a.X > Math.Min(c.X, d.X) && a.X < Math.Max(c.X, d.X)
                    && c.Y > Math.Min(a.Y, b.Y) && c.Y < Math.Max(a.Y, b.Y);
                if (horizontalVertical || verticalHorizontal) crossings++;
            }
        }
        return crossings;
    }
    private static void AssertNodeBoundsDoNotOverlap(LocalFlowchartLayout layout)
    {
        for (var firstIndex = 0; firstIndex < layout.Nodes.Count; firstIndex++)
        for (var secondIndex = firstIndex + 1; secondIndex < layout.Nodes.Count; secondIndex++)
        {
            var first = layout.Nodes[firstIndex];
            var second = layout.Nodes[secondIndex];
            var overlaps = first.X < second.X + second.Width && second.X < first.X + first.Width
                && first.Y < second.Y + second.Height && second.Y < first.Y + first.Height;
            Assert.False(overlaps, $"Nodes {first.Node.Id} and {second.Node.Id} overlap.");
        }
    }

    private static void AssertEdgeLabelBoundsDoNotOverlap(LocalFlowchartLayout layout)
    {
        var labeled = layout.Edges.Where(edge => edge.Edge.Label is not null).ToArray();
        foreach (var edge in labeled)
        {
            var rect = EdgeLabelRect(edge);
            Assert.True(rect.Left >= 0 && rect.Top >= 0 && rect.Right <= layout.Width && rect.Bottom <= layout.Height,
                $"Edge label '{edge.Edge.Label}' extends beyond the canvas.");
            Assert.DoesNotContain(layout.Nodes, node => RectanglesOverlap(rect.Left, rect.Top, rect.Right, rect.Bottom,
                node.X, node.Y, node.X + node.Width, node.Y + node.Height));
            foreach (var other in layout.Edges.Where(other => other.Edge.Order != edge.Edge.Order))
            foreach (var (start, end) in other.Route.Zip(other.Route.Skip(1)))
                Assert.False(SegmentIntersectsRect(start, end, rect.Left, rect.Top, rect.Right, rect.Bottom),
                    $"Edge label '{edge.Edge.Label}' covers unrelated connector {other.Edge.Order}.");
        }
        for (var first = 0; first < labeled.Length; first++)
        for (var second = first + 1; second < labeled.Length; second++)
        {
            var a = EdgeLabelRect(labeled[first]);
            var b = EdgeLabelRect(labeled[second]);
            Assert.False(RectanglesOverlap(a.Left, a.Top, a.Right, a.Bottom, b.Left, b.Top, b.Right, b.Bottom),
                $"Edge labels '{labeled[first].Edge.Label}' and '{labeled[second].Edge.Label}' overlap.");
        }
    }

    private static (int Left, int Top, int Right, int Bottom) EdgeLabelRect(LocalFlowchartLayoutEdge edge) =>
        (edge.LabelX - edge.LabelWidth / 2, edge.LabelY - edge.LabelHeight / 2,
            edge.LabelX + (edge.LabelWidth + 1) / 2, edge.LabelY + (edge.LabelHeight + 1) / 2);

    private static bool RectanglesOverlap(int leftA, int topA, int rightA, int bottomA,
        int leftB, int topB, int rightB, int bottomB) =>
        leftA < rightB && rightA > leftB && topA < bottomB && bottomA > topB;

    private static bool SegmentIntersectsRect(LocalFlowchartLayoutPoint first, LocalFlowchartLayoutPoint second,
        int left, int top, int right, int bottom) => first.Y == second.Y
        ? first.Y >= top && first.Y <= bottom && Math.Max(Math.Min(first.X, second.X), left) <= Math.Min(Math.Max(first.X, second.X), right)
        : first.X >= left && first.X <= right && Math.Max(Math.Min(first.Y, second.Y), top) <= Math.Min(Math.Max(first.Y, second.Y), bottom);

    [Theory]
    [InlineData("Start")]
    [InlineData("Process")]
    [InlineData("Decision")]
    [InlineData("InputOutput")]
    [InlineData("Person")]
    [InlineData("Cloud")]
    [InlineData("End")]
    public void Diagram_ArrowheadTipsRemainVisibleAtTargetShapePerimeter(string targetShapeName)
    {
        var targetShape = Enum.Parse<LocalFlowchartNodeShape>(targetShapeName);
        var document = new LocalFlowchartDocument("arrowhead", "arrowhead.md",
            [new("source", LocalFlowchartNodeShape.Start, "Begin", 1, 0),
                new("target", targetShape, "Continue", 2, 1)],
            [new("source", "target", null, 3, 0)]);
        var layout = LocalFlowchartLayoutEngine.Create(document);
        var tip = layout.Edges.Single().Route[^1];
        var target = layout.Node("target");
        var png = LocalFlowchartPngRenderer.Render(document);
        var pixel = ReadPngPixel(png, tip.X, tip.Y);

        Assert.True(pixel == ((byte)124, (byte)141, (byte)164),
            $"Expected arrowhead ink at route tip ({tip.X},{tip.Y}) for target bounds " +
            $"({target.X},{target.Y},{target.Width},{target.Height}) on {targetShape}; actual pixel {pixel}.");
    }

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
        Assert.Contains((byte)34, pixels.GetBuffer());

        Assert.Equal(0, Run("docs/flow.md", "out/again.png").ExitCode);
        Assert.Equal(firstBytes, File.ReadAllBytes(Path.Combine(_root, "out", "again.png")));
        var xml = File.ReadAllBytes(Path.Combine(_root, "out", "flow.drawio"));
        Assert.Equal(xml, File.ReadAllBytes(Path.Combine(_root, "out", "again.drawio")));
        Assert.Equal("mxfile", XDocument.Load(Path.Combine(_root, "out", "flow.drawio")).Root!.Name.LocalName);
    }
    [Fact]
    public void Diagram_ExistingXmlSidecarPreventsPartialPngCreation()
    {
        Write("docs/flow.md", Fixture);
        Write("out/flow.drawio", "keep existing XML");
        var result = Run("docs/flow.md", "out/flow.png");
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("already exists", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("keep existing XML", File.ReadAllText(Path.Combine(_root, "out", "flow.drawio")));
        Assert.False(File.Exists(Path.Combine(_root, "out", "flow.png")));
    }

    [Fact]
    public void Diagram_OverwriteReplacesPngAndEditableSidecarTogether()
    {
        Write("docs/flow.md", Fixture);
        Write("out/flow.png", "old image");
        Write("out/flow.drawio", "old source");

        var result = Run("docs/flow.md", "out/flow.png", "--overwrite");

        Assert.Equal(0, result.ExitCode);
        Assert.NotEqual("old image", File.ReadAllText(Path.Combine(_root, "out", "flow.png")));
        Assert.NotEqual("old source", File.ReadAllText(Path.Combine(_root, "out", "flow.drawio")));
        Assert.Equal("mxfile", XDocument.Load(Path.Combine(_root, "out", "flow.drawio")).Root!.Name.LocalName);
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
            Assert.True(!baseline.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, path))), $"Rendered pixels did not change for variant {index}.");
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

    [Fact]
    public void Layout_KeepsDenseBranchesCyclesParallelEdgesLoopsAndDisconnectedPathsClear()
    {
        var nodes = new[]
        {
            new LocalFlowchartNode("a", LocalFlowchartNodeShape.Start, "Begin", 1, 0),
            new LocalFlowchartNode("b", LocalFlowchartNodeShape.Decision, "Check one", 2, 1),
            new LocalFlowchartNode("c", LocalFlowchartNodeShape.Decision, "Check two", 3, 2),
            new LocalFlowchartNode("d", LocalFlowchartNodeShape.Process, "Combine", 4, 3),
            new LocalFlowchartNode("e", LocalFlowchartNodeShape.End, "Finish", 5, 4),
            new LocalFlowchartNode("f", LocalFlowchartNodeShape.Process, "Extra review", 6, 5),
            new LocalFlowchartNode("x", LocalFlowchartNodeShape.Start, "Other path", 7, 6),
            new LocalFlowchartNode("y", LocalFlowchartNodeShape.End, "Other finish", 8, 7)
        };
        var edges = new[]
        {
            new LocalFlowchartEdge("a", "b", "first", 9, 0),
            new LocalFlowchartEdge("a", "c", "second", 10, 1),
            new LocalFlowchartEdge("b", "d", "route one", 11, 2),
            new LocalFlowchartEdge("b", "d", "route two", 12, 3),
            new LocalFlowchartEdge("c", "d", null, 13, 4),
            new LocalFlowchartEdge("d", "b", "review again", 14, 5),
            new LocalFlowchartEdge("c", "c", "repeat", 15, 6),
            new LocalFlowchartEdge("d", "f", null, 16, 7),
            new LocalFlowchartEdge("f", "e", null, 17, 8),
            new LocalFlowchartEdge("a", "e", "direct", 18, 9),
            new LocalFlowchartEdge("x", "y", null, 19, 10)
        };
        var document = new LocalFlowchartDocument("geometry", "geometry.md", nodes, edges);
        var layout = LocalFlowchartLayoutEngine.Create(document);

        AssertNodeBoundsDoNotOverlap(layout);
        AssertEdgesAvoidUnrelatedNodes(layout);
        Assert.Equal(edges.Length, layout.Edges.Count);
        Assert.Contains(layout.Edges, edge => edge.Edge.FromId == "c" && edge.Edge.ToId == "c");
        Assert.Contains(layout.Edges, edge => edge.Edge.FromId == "a" && edge.Edge.ToId == "e");
        Assert.Equal(2, layout.Edges.Count(edge => edge.Edge.FromId == "b" && edge.Edge.ToId == "d"));

        var horizontalLayout = LocalFlowchartLayoutEngine.Create(document,
            new LocalFlowchartLayoutOptions(LocalFlowchartDirection.LeftToRight));
        AssertNodeBoundsDoNotOverlap(horizontalLayout);
        AssertEdgesAvoidUnrelatedNodes(horizontalLayout);
        Assert.Equal(edges.Length, horizontalLayout.Edges.Count);
    }

    [Theory]
    [InlineData("# no flow block", "not found")]
    [InlineData("```brownpants-flow\nflowchart a\n```\n```brownpants-flow\nflowchart b\n```", "exactly one")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nedge start -> missing\n```", "not declared")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nnode start end \"Again\"\n```", "duplicate node")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nnode done end \"Done\"\nedge start -> done\nedge start -> done\n```", "duplicate edge")]
    [InlineData("```brownpants-flow\nflowchart a\nnode start start \"Start\"\nnode done end \"D\u0001ne\"\n```", "control characters")]
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
    public void Diagram_TiledGridCoversBothCanvasDimensionsWithDeterministicOverlap()
    {
        var source = new StringBuilder("```brownpants-flow\nflowchart grid\n");
        for (var rank = 0; rank < 8; rank++)
        for (var column = 0; column < 10; column++)
            source.AppendLine($"node n{rank:D2}{column:D2} process \"Step {rank}-{column}\"");
        for (var rank = 0; rank < 7; rank++)
        for (var column = 0; column < 10; column++)
        {
            source.AppendLine($"edge n{rank:D2}{column:D2} -> n{rank + 1:D2}{column:D2}");
            source.AppendLine($"edge n{rank:D2}{column:D2} -> n{rank + 1:D2}{(column + 1) % 10:D2}");
        }
        source.AppendLine("```");
        Write("grid.md", source.ToString());

        var result = Run("grid.md", "tiles/grid.png", "--tile-size", "768", "--overlap", "96");

        Assert.True(result.ExitCode == 0, result.Error);
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(Path.Combine(_root, "tiles", "grid", "grid.tiles.json")));
        var root = manifest.RootElement;
        var canvasWidth = root.GetProperty("canvasWidth").GetInt32();
        var canvasHeight = root.GetProperty("canvasHeight").GetInt32();
        var entries = root.GetProperty("tiles").EnumerateArray().ToArray();
        Assert.True(canvasWidth > 768);
        Assert.True(canvasHeight > 768);
        var rows = entries.Select(entry => entry.GetProperty("row").GetInt32()).Distinct().ToArray();
        var columns = entries.Select(entry => entry.GetProperty("column").GetInt32()).Distinct().ToArray();
        Assert.True(rows.Length > 1);
        Assert.True(columns.Length > 1);
        Assert.Equal("tiles/grid/grid-x0000000-y0000000.png", entries[0].GetProperty("image").GetString());
        Assert.Equal("tiles/grid/grid-x0000672-y0000000.png", entries[1].GetProperty("image").GetString());
        Assert.Equal(rows.Length * columns.Length, entries.Length);
        Assert.Equal(entries.Length, entries.Select(entry =>
            (entry.GetProperty("row").GetInt32(), entry.GetProperty("column").GetInt32())).Distinct().Count());
        foreach (var entry in entries)
        {
            var x = entry.GetProperty("x").GetInt32();
            var y = entry.GetProperty("y").GetInt32();
            var width = entry.GetProperty("width").GetInt32();
            var height = entry.GetProperty("height").GetInt32();
            Assert.InRange(x, 0, canvasWidth - 1);
            Assert.InRange(y, 0, canvasHeight - 1);
            Assert.True(x + width <= canvasWidth);
            Assert.True(y + height <= canvasHeight);
            Assert.True(width <= 768 && height <= 768);
            Assert.Equal(entry.GetProperty("column").GetInt32() == 1 ? 0 : 96,
                entry.GetProperty("overlapWithLeft").GetInt32());
            Assert.Equal(entry.GetProperty("row").GetInt32() == 1 ? 0 : 96,
                entry.GetProperty("overlapWithTop").GetInt32());
        }
        foreach (var row in rows)
        {
            var rowEntries = entries.Where(entry => entry.GetProperty("row").GetInt32() == row)
                .OrderBy(entry => entry.GetProperty("column").GetInt32()).ToArray();
            for (var index = 1; index < rowEntries.Length; index++)
                Assert.Equal(96, rowEntries[index - 1].GetProperty("x").GetInt32()
                    + rowEntries[index - 1].GetProperty("width").GetInt32() - rowEntries[index].GetProperty("x").GetInt32());
        }
        foreach (var column in columns)
        {
            var columnEntries = entries.Where(entry => entry.GetProperty("column").GetInt32() == column)
                .OrderBy(entry => entry.GetProperty("row").GetInt32()).ToArray();
            for (var index = 1; index < columnEntries.Length; index++)
                Assert.Equal(96, columnEntries[index - 1].GetProperty("y").GetInt32()
                    + columnEntries[index - 1].GetProperty("height").GetInt32() - columnEntries[index].GetProperty("y").GetInt32());
        }

        var protectedSidecar = Path.Combine(_root, "blocked", "grid", "grid-x0000672-y0000000.drawio");
        Directory.CreateDirectory(Path.GetDirectoryName(protectedSidecar)!);
        File.WriteAllText(protectedSidecar, "preserve");
        var blocked = Run("grid.md", "blocked/grid.png", "--tile-size", "768", "--overlap", "96");
        Assert.NotEqual(0, blocked.ExitCode);
        Assert.Equal("preserve", File.ReadAllText(protectedSidecar));
        Assert.False(File.Exists(Path.Combine(_root, "blocked", "grid", "grid-x0000000-y0000000.png")));
        Assert.False(File.Exists(Path.Combine(_root, "blocked", "grid", "grid.tiles.json")));

        var protectedMaster = Path.Combine(_root, "blocked-master", "grid", "grid.drawio");
        Directory.CreateDirectory(Path.GetDirectoryName(protectedMaster)!);
        File.WriteAllText(protectedMaster, "preserve master");
        var blockedMaster = Run("grid.md", "blocked-master/grid.png", "--tile-size", "768", "--overlap", "96");
        Assert.NotEqual(0, blockedMaster.ExitCode);
        Assert.Equal("preserve master", File.ReadAllText(protectedMaster));
        Assert.False(File.Exists(Path.Combine(_root, "blocked-master", "grid", "grid-x0000000-y0000000.png")));
        Assert.False(File.Exists(Path.Combine(_root, "blocked-master", "grid", "grid.tiles.json")));
    }

    [Fact]
    public void Diagram_LayoutsOneThousandNodesWithinTheDeclaredGraphLimit()
    {
        var source = new StringBuilder("```brownpants-flow\nflowchart thousand\n");
        for (var index = 0; index < 1000; index++)
            source.AppendLine($"node n{index} process \"Service {index}\"");
        source.AppendLine("```");
        var document = LocalFlowchartParser.Parse(source.ToString(), "thousand.md");

        var layout = LocalFlowchartLayoutEngine.Create(document);

        Assert.Equal(1000, layout.Nodes.Count);
        AssertNodeBoundsDoNotOverlap(layout);
        Assert.True(layout.Width > 4096);
    }

    [Fact]
    public void CallerChart_TiledOutputUsesDedicatedBundleDirectory()
    {
        var nodes = Enumerable.Range(0, 8)
            .Select(index => new LocalFlowchartNode($"n{index}", LocalFlowchartNodeShape.Process, $"Call site {index}", index + 1, index))
            .ToArray();
        var edges = Enumerable.Range(0, nodes.Length - 1)
            .Select(index => new LocalFlowchartEdge(nodes[index].Id, nodes[index + 1].Id, null, index + 1, index))
            .ToArray();
        var document = new LocalFlowchartDocument("callers", "callers.cs", nodes, edges);
        var output = new StringWriter();
        var error = new StringWriter();
        Directory.CreateDirectory(_root);

        var exitCode = LocalFlowchartCommand.WriteGeneratedDocument(_root, "callers.png", "png", document,
            LocalFlowchartDirection.TopDown, 256, 32, false, "summary", output, error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.True(File.Exists(Path.Combine(_root, "callers", "callers.tiles.json")));
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(Path.Combine(_root, "callers", "callers.tiles.json")));
        Assert.All(manifest.RootElement.GetProperty("tiles").EnumerateArray(), tile =>
        {
            Assert.StartsWith("callers/", tile.GetProperty("image").GetString(), StringComparison.Ordinal);
            Assert.StartsWith("callers/", tile.GetProperty("editableSource").GetString(), StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Diagram_LayoutsLongSparseGraphWithoutRepeatedWholeGraphScans()
    {
        const int nodeCount = 25_000;
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(index => new LocalFlowchartNode($"n{index}", LocalFlowchartNodeShape.Process,
                $"Step {index}", index + 1, index))
            .ToArray();
        var edges = Enumerable.Range(0, nodeCount - 1)
            .Select(index => new LocalFlowchartEdge($"n{index}", $"n{index + 1}", null, index + 1, index))
            .ToArray();
        var document = new LocalFlowchartDocument("sparse-chain", "chain.md", nodes, edges);

        var layout = LocalFlowchartLayoutEngine.Create(document);

        Assert.Equal(nodeCount, layout.Nodes.Count);
        Assert.Equal(nodeCount - 1, layout.Edges.Count);
        Assert.Equal(0, layout.Node("n0").Rank);
        Assert.Equal(nodeCount - 1, layout.Node($"n{nodeCount - 1}").Rank);
        Assert.True(layout.Height < 5_000_000);

        var spatialIndex = new LocalFlowchartSpatialIndex(layout);
        var lastTileY = layout.Height - 256;
        Assert.InRange(spatialIndex.NodesIntersecting(0, lastTileY, layout.Width, 256).Length, 1, 10);
        Assert.InRange(spatialIndex.EdgesIntersecting(0, lastTileY, layout.Width, 256).Length, 1, 12);
    }

    [Fact]
    public void SpatialIndex_ReturnsEveryPaddedNodeAndEdgeThatIntersectsEachTile()
    {
        var document = LocalFlowchartParser.Parse(Fixture, "fixture.md");
        var layout = LocalFlowchartLayoutEngine.Create(document);
        var index = new LocalFlowchartSpatialIndex(layout);
        const int padding = 64;

        foreach (var tile in LocalFlowchartPngRenderer.EnumerateTileRegions(layout, 256, 32))
        {
            var left = tile.X;
            var top = tile.Y;
            var right = tile.X + tile.Width;
            var bottom = tile.Y + tile.Height;
            var expectedNodes = layout.Nodes.Select((node, nodeIndex) => (node, nodeIndex))
                .Where(item => item.node.X - padding <= right && item.node.X + item.node.Width + padding >= left
                    && item.node.Y - padding <= bottom && item.node.Y + item.node.Height + padding >= top)
                .Select(item => item.nodeIndex).ToArray();
            var expectedEdges = layout.Edges.Select((edge, edgeIndex) => (edge, edgeIndex))
                .Where(item =>
                {
                    var minX = item.edge.Route.Min(point => point.X);
                    var minY = item.edge.Route.Min(point => point.Y);
                    var maxX = item.edge.Route.Max(point => point.X);
                    var maxY = item.edge.Route.Max(point => point.Y);
                    if (item.edge.Edge.Label is not null)
                    {
                        minX = Math.Min(minX, item.edge.LabelX - item.edge.LabelWidth / 2);
                        maxX = Math.Max(maxX, item.edge.LabelX + (item.edge.LabelWidth + 1) / 2);
                        minY = Math.Min(minY, item.edge.LabelY - item.edge.LabelHeight / 2);
                        maxY = Math.Max(maxY, item.edge.LabelY + (item.edge.LabelHeight + 1) / 2);
                    }
                    return minX - padding <= right && maxX + padding >= left
                        && minY - padding <= bottom && maxY + padding >= top;
                }).Select(item => item.edgeIndex).ToArray();

            Assert.Equal(expectedNodes, index.NodesIntersecting(tile.X, tile.Y, tile.Width, tile.Height));
            Assert.Equal(expectedEdges, index.EdgesIntersecting(tile.X, tile.Y, tile.Width, tile.Height));
        }
    }

    [Fact]
    public void Diagram_RendersLargeSinglePngBeyondOldDimensionCap()
    {
        const int nodeCount = 64;
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(index => new LocalFlowchartNode($"n{index}", LocalFlowchartNodeShape.Process,
                $"Step {index}", index + 1, index))
            .ToArray();
        var document = new LocalFlowchartDocument("wide-chart", "wide.md", nodes, []);
        var layout = LocalFlowchartLayoutEngine.Create(document);

        var png = LocalFlowchartPngRenderer.Render(document);

        Assert.True(layout.Width > 4096);
        Assert.Equal((uint)layout.Width, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal((uint)layout.Height, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4)));
        Assert.Equal("IHDR", Encoding.ASCII.GetString(png, 12, 4));
    }

    [Fact]
    public void Renderer_ReservesTenPercentBelowPngAndRgbArrayLimits()
    {
        Assert.Equal((int)(int.MaxValue * 9L / 10), LocalFlowchartLayoutEngine.MaximumLogicalDimension);
        Assert.Equal((Array.MaxLength / 3L) * 9 / 10, LocalFlowchartPngRenderer.MaximumSingleRasterPixels);
        Assert.True(LocalFlowchartPngRenderer.FitsSingleRaster(1,
            (int)LocalFlowchartPngRenderer.MaximumSingleRasterPixels));
        Assert.False(LocalFlowchartPngRenderer.FitsSingleRaster(1,
            (int)LocalFlowchartPngRenderer.MaximumSingleRasterPixels + 1));
    }

    [Fact]
    public void Renderer_EnumeratesMoreThanOneThousandTilesWithoutBufferingThem()
    {
        var layout = new LocalFlowchartLayout(20_000, 20_000, 0, [], [], LocalFlowchartDirection.TopDown);

        var firstBatch = LocalFlowchartPngRenderer.EnumerateTileRegions(layout, 256, 0).Take(1025).ToArray();

        Assert.Equal(1025, firstBatch.Length);
        Assert.Equal((0, 0), (firstBatch[0].X, firstBatch[0].Y));
        Assert.True(firstBatch[^1].X > 0 || firstBatch[^1].Y > 0);
    }

    [Fact]
    public void Diagram_SelectsTilingOnlyWhenRequestedOrRequiredByRasterCapacity()
    {
        var fitsSingleImage = new LocalFlowchartLayout(1000, 1000, 0, [], [], LocalFlowchartDirection.TopDown);
        var exceedsSingleImage = new LocalFlowchartLayout(
            (int)LocalFlowchartPngRenderer.MaximumSingleRasterPixels, 2, 0, [], [], LocalFlowchartDirection.TopDown);

        Assert.False(LocalFlowchartCommand.RequiresTiling("png", null, fitsSingleImage));
        Assert.True(LocalFlowchartCommand.RequiresTiling("png", null, exceedsSingleImage));
        Assert.True(LocalFlowchartCommand.RequiresTiling("png", 1024, fitsSingleImage));
        Assert.False(LocalFlowchartCommand.RequiresTiling("drawio", null, exceedsSingleImage));
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
    public void Diagram_EnforcesSourceSizeAndAcceptsExpandedGraphLimits()
    {
        Directory.CreateDirectory(_root);
        using (var largeSource = File.Create(Path.Combine(_root, "large.md")))
            largeSource.SetLength(LocalFlowchartParser.MaximumSourceBytes + 1L);
        Assert.NotEqual(0, Run("large.md", "large.png").ExitCode);
        Assert.False(File.Exists(Path.Combine(_root, "large.png")));

        var expandedNodes = new StringBuilder("```brownpants-flow\nflowchart limit\n");
        for (var i = 0; i < 1001; i++) expandedNodes.AppendLine($"node n{i} process \"Node {i}\"");
        expandedNodes.AppendLine("```");
        var parsedNodes = LocalFlowchartParser.Parse(expandedNodes.ToString(), "nodes.md");
        Assert.Equal(1001, parsedNodes.Nodes.Count);

        var expandedEdges = new StringBuilder("```brownpants-flow\nflowchart limit\n");
        for (var i = 0; i < 101; i++) expandedEdges.AppendLine($"node n{i} process \"Node {i}\"");
        for (var source = 0; source < 101; source++)
        for (var target = 0; target < 101; target++)
            if (source != target) expandedEdges.AppendLine($"edge n{source} -> n{target}");
        expandedEdges.AppendLine("```");
        var parsedEdges = LocalFlowchartParser.Parse(expandedEdges.ToString(), "edges.md");
        Assert.Equal(10_100, parsedEdges.Edges.Count);

        var deepGraph = new StringBuilder("```brownpants-flow\nflowchart canvas\n");
        for (var i = 0; i < 80; i++) deepGraph.AppendLine($"node n{i} process \"Node {i}\"");
        for (var i = 0; i < 79; i++) deepGraph.AppendLine($"edge n{i} -> n{i + 1}");
        deepGraph.AppendLine("```");
        Write("canvas.md", deepGraph.ToString());
        var singlePng = Run("canvas.md", "canvas.png");
        Assert.Equal(0, singlePng.ExitCode);
        Assert.True(File.Exists(Path.Combine(_root, "canvas.png")));

        var tiled = Run("canvas.md", "tiles/canvas.png", "--tile-size", "768", "--overlap", "96");
        Assert.True(tiled.ExitCode == 0, tiled.Error);
        Assert.Contains("PNG tiles with tile-local editable XML sidecars", tiled.Output);
        var manifestPath = Path.Combine(_root, "tiles", "canvas", "canvas.tiles.json");
        var manifestBytes = File.ReadAllBytes(manifestPath);
        using var manifest = System.Text.Json.JsonDocument.Parse(manifestBytes);
        var root = manifest.RootElement;
        var completeSourceRelative = root.GetProperty("editableSource").GetString()!;
        Assert.Equal("tiles/canvas/canvas.drawio", completeSourceRelative);
        var completeSourcePath = Path.Combine(_root, completeSourceRelative.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(completeSourcePath));
        var completeSourceBytes = File.ReadAllBytes(completeSourcePath);
        var completeDocument = XDocument.Load(completeSourcePath);
        Assert.Equal(80, completeDocument.Descendants("mxCell").Count(cell => (string?)cell.Attribute("vertex") == "1"));
        Assert.Equal(79, completeDocument.Descendants("mxCell").Count(cell => (string?)cell.Attribute("edge") == "1"));
        var canvasHeight = root.GetProperty("canvasHeight").GetInt32();
        var entries = root.GetProperty("tiles").EnumerateArray().ToArray();
        Assert.True(canvasHeight > 4096);
        Assert.True(entries.Length > 1);
        Assert.Equal(96, root.GetProperty("requestedOverlap").GetInt32());
        Assert.Equal(Enumerable.Range(1, entries.Length), entries.Select(entry => entry.GetProperty("order").GetInt32()));
        Assert.Equal(Enumerable.Range(1, entries.Length), entries.Select(entry => entry.GetProperty("row").GetInt32()));
        var tileXmlBytes = 0L;
        Assert.All(entries, entry =>
        {
            var image = entry.GetProperty("image").GetString()!;
            var editable = entry.GetProperty("editableSource").GetString()!;
            var imageBytes = File.ReadAllBytes(Path.Combine(_root, image.Replace('/', Path.DirectorySeparatorChar)));
            Assert.InRange(ReadBigEndian(imageBytes, 16), 1u, 768u);
            Assert.InRange(ReadBigEndian(imageBytes, 20), 1u, 768u);
            var editablePath = Path.Combine(_root, editable.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(editablePath));
            tileXmlBytes += new FileInfo(editablePath).Length;
            var tileDocument = XDocument.Load(editablePath);
            var graphModel = tileDocument.Descendants("mxGraphModel").Single();
            Assert.Equal(entry.GetProperty("width").GetInt32().ToString(CultureInfo.InvariantCulture),
                (string?)graphModel.Attribute("pageWidth"));
            Assert.Equal(entry.GetProperty("height").GetInt32().ToString(CultureInfo.InvariantCulture),
                (string?)graphModel.Attribute("pageHeight"));
            var cells = tileDocument.Descendants("mxCell").ToArray();
            var cellIds = cells.Select(cell => (string?)cell.Attribute("id")).Where(id => id is not null).ToHashSet(StringComparer.Ordinal);
            foreach (var edge in cells.Where(cell => (string?)cell.Attribute("edge") == "1"))
            {
                Assert.Contains((string?)edge.Attribute("source"), cellIds);
                Assert.Contains((string?)edge.Attribute("target"), cellIds);
            }
            _ = LocalFlowchartDrawioParser.Parse(File.ReadAllBytes(editablePath), editable);
        });
        Assert.True(tileXmlBytes < completeSourceBytes.Length * 5L,
            $"Tile-local XML ({tileXmlBytes} bytes) should stay bounded against one full source ({completeSourceBytes.Length} bytes).");
        for (var index = 1; index < entries.Length; index++)
        {
            var prior = entries[index - 1];
            var current = entries[index];
            Assert.Equal(96, prior.GetProperty("y").GetInt32() + prior.GetProperty("height").GetInt32() - current.GetProperty("y").GetInt32());
            Assert.Equal(96, current.GetProperty("overlapWithTop").GetInt32());
        }
        var firstTilePath = Path.Combine(_root, entries[0].GetProperty("image").GetString()!.Replace('/', Path.DirectorySeparatorChar));
        var firstTileBytes = File.ReadAllBytes(firstTilePath);
        var firstEditablePath = Path.Combine(_root, entries[0].GetProperty("editableSource").GetString()!.Replace('/', Path.DirectorySeparatorChar));
        var firstEditableBytes = File.ReadAllBytes(firstEditablePath);
        var unrelated = Path.Combine(_root, "tiles", "canvas", "review-notes.txt");
        File.WriteAllText(unrelated, "keep this user file");
        var repeated = Run("canvas.md", "tiles/canvas.png", "--tile-size", "768", "--overlap", "96", "--overwrite");
        Assert.True(repeated.ExitCode == 0, repeated.Error);
        Assert.Equal(manifestBytes, File.ReadAllBytes(manifestPath));
        Assert.Equal(firstTileBytes, File.ReadAllBytes(firstTilePath));
        Assert.Equal(firstEditableBytes, File.ReadAllBytes(firstEditablePath));
        Assert.Equal(completeSourceBytes, File.ReadAllBytes(completeSourcePath));
        Assert.Equal("keep this user file", File.ReadAllText(unrelated));

        var staleImage = Path.Combine(_root, entries[^1].GetProperty("image").GetString()!.Replace('/', Path.DirectorySeparatorChar));
        var staleXml = Path.Combine(_root, entries[^1].GetProperty("editableSource").GetString()!.Replace('/', Path.DirectorySeparatorChar));
        Write("canvas.md", "```brownpants-flow\nflowchart smaller\nnode only process \"One step\"\n```\n");
        var smaller = Run("canvas.md", "tiles/canvas.png", "--tile-size", "768", "--overlap", "96", "--overwrite");
        Assert.True(smaller.ExitCode == 0, smaller.Error);
        using var smallerManifest = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        Assert.Single(smallerManifest.RootElement.GetProperty("tiles").EnumerateArray());
        Assert.False(File.Exists(staleImage));
        Assert.False(File.Exists(staleXml));
        Assert.True(File.Exists(completeSourcePath));
        var smallerFullSource = XDocument.Load(completeSourcePath);
        Assert.Single(smallerFullSource.Descendants("mxCell"), cell => (string?)cell.Attribute("vertex") == "1");
        Assert.Equal("keep this user file", File.ReadAllText(unrelated));
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
        Assert.Contains("connector crossings", output.ToString(), StringComparison.OrdinalIgnoreCase);
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

    private static (byte Red, byte Green, byte Blue) ReadPngPixel(byte[] png, int x, int y)
    {
        var width = checked((int)ReadBigEndian(png, 16));
        var height = checked((int)ReadBigEndian(png, 20));
        Assert.InRange(x, 0, width - 1);
        Assert.InRange(y, 0, height - 1);
        using var compressed = new MemoryStream();
        for (var offset = 8; offset < png.Length;)
        {
            var length = checked((int)ReadBigEndian(png, offset));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "IDAT") compressed.Write(png, offset + 8, length);
            offset += length + 12;
            if (type == "IEND") break;
        }
        compressed.Position = 0;
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        var rowLength = checked(width * 3 + 1);
        var row = new byte[rowLength];
        for (var rowIndex = 0; rowIndex <= y; rowIndex++)
            Assert.Equal(rowLength, zlib.ReadAtLeast(row, rowLength, throwOnEndOfStream: true));
        Assert.Equal(0, row[0]);
        var pixel = 1 + x * 3;
        return (row[pixel], row[pixel + 1], row[pixel + 2]);
    }

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
