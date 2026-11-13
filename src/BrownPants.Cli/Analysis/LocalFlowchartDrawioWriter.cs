using System.Globalization;
using System.Text;
using System.Xml;

namespace BrownPants.Cli.Analysis;

internal static class LocalFlowchartDrawioWriter
{
    internal static byte[] Write(LocalFlowchartDocument document, LocalFlowchartLayoutOptions? options = null,
        ChartTheme? theme = null)
    {
        using var stream = new MemoryStream();
        Write(document, stream, options, theme);
        return stream.ToArray();
    }

    internal static void Write(LocalFlowchartDocument document, Stream destination,
        LocalFlowchartLayoutOptions? options = null, ChartTheme? theme = null)
    {
        Write(document, LocalFlowchartLayoutEngine.Create(document, options), destination, theme);
    }

    internal static void Write(LocalFlowchartDocument document, LocalFlowchartLayout layout, Stream destination,
        ChartTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        if (theme is null && layout.Nodes.All(node => node.Node.ImportedCellId is null))
            theme = ChartThemeCatalog.Default;
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = false,
            CloseOutput = false
        };
        using (var writer = XmlWriter.Create(destination, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("mxfile");
            writer.WriteAttributeString("host", "app.diagrams.net");
            writer.WriteAttributeString("agent", "BrownPants");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("type", "device");
            writer.WriteStartElement("diagram");
            writer.WriteAttributeString("id", document.Id);
            writer.WriteAttributeString("name", document.ImportedPageName ?? "Page-1");
            writer.WriteStartElement("mxGraphModel");
            writer.WriteAttributeString("dx", layout.Width.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("dy", layout.Height.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("grid", "1");
            writer.WriteAttributeString("gridSize", "10");
            writer.WriteAttributeString("guides", "1");
            writer.WriteAttributeString("tooltips", "1");
            writer.WriteAttributeString("connect", "1");
            writer.WriteAttributeString("arrows", "1");
            writer.WriteAttributeString("fold", "1");
            writer.WriteAttributeString("page", "1");
            writer.WriteAttributeString("pageScale", "1");
            writer.WriteAttributeString("pageWidth", (document.ImportedPageGeometry?.Width ?? Math.Max(850, layout.Width)).ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("pageHeight", (document.ImportedPageGeometry?.Height ?? Math.Max(1100, layout.Height)).ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("math", "0");
            writer.WriteAttributeString("shadow", "0");
            if (theme is not null) writer.WriteAttributeString("background", theme.Canvas.ToHex());
            else if (document.ImportedBackground is not null)
                writer.WriteAttributeString("background", document.ImportedBackground);
            writer.WriteStartElement("root");
            WriteBaseCell(writer, "0", null);
            WriteBaseCell(writer, "1", "0");

            foreach (var node in layout.Nodes.OrderBy(item => item.Node.Order))
            {
                writer.WriteStartElement("mxCell");
                writer.WriteAttributeString("id", NodeCellId(node.Node));
                writer.WriteAttributeString("value", node.Node.Label);
                var nodeStyle = node.Node.ImportedStyle ?? NodeStyle(node.Node.Shape);
                if (theme is not null)
                {
                    var colors = theme.Shape(node.Node.Shape);
                    nodeStyle = ApplyColorOverrides(nodeStyle,
                        ("fillColor", colors.Fill.ToHex()), ("strokeColor", colors.Stroke.ToHex()),
                        ("fontColor", theme.Text.ToHex()));
                }
                writer.WriteAttributeString("style", nodeStyle);
                if (!string.IsNullOrWhiteSpace(node.Node.Details))
                    writer.WriteAttributeString("tooltip", node.Node.Details);
                writer.WriteAttributeString("vertex", "1");
                writer.WriteAttributeString("parent", "1");
                writer.WriteStartElement("mxGeometry");
                writer.WriteAttributeString("x", node.X.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("y", node.Y.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("width", node.Width.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("height", node.Height.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("as", "geometry");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            foreach (var routed in layout.Edges.OrderBy(item => item.Edge.Order))
            {
                var edge = routed.Edge;
                writer.WriteStartElement("mxCell");
                writer.WriteAttributeString("id", EdgeCellId(edge));
                writer.WriteAttributeString("value", edge.Label ?? string.Empty);
                var edgeStyle = edge.ImportedStyle ?? "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=0;endArrow=block;endFill=1;";
                if (theme is not null)
                    edgeStyle = ApplyColorOverrides(edgeStyle, ("strokeColor", theme.Connector.ToHex()),
                        ("fontColor", theme.EdgeLabelText.ToHex()), ("labelBackgroundColor", theme.EdgeLabelBackground.ToHex()));
                writer.WriteAttributeString("style", edgeStyle);
                if (!string.IsNullOrWhiteSpace(edge.Details))
                    writer.WriteAttributeString("tooltip", edge.Details);
                writer.WriteAttributeString("edge", "1");
                writer.WriteAttributeString("parent", "1");
                writer.WriteAttributeString("source", NodeCellId(layout.Node(edge.FromId).Node));
                writer.WriteAttributeString("target", NodeCellId(layout.Node(edge.ToId).Node));
                writer.WriteStartElement("mxGeometry");
                writer.WriteAttributeString("relative", "1");
                writer.WriteAttributeString("as", "geometry");
                var waypoints = edge.ImportedWaypoints is { Count: > 0 }
                    ? edge.ImportedWaypoints.Select(point => new LocalFlowchartLayoutPoint(point.X, point.Y)).ToArray()
                    : routed.Route.Skip(1).SkipLast(1).ToArray();
                if (waypoints.Length > 0)
                {
                    writer.WriteStartElement("Array");
                    writer.WriteAttributeString("as", "points");
                    foreach (var point in waypoints)
                    {
                        writer.WriteStartElement("mxPoint");
                        writer.WriteAttributeString("x", point.X.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("y", point.Y.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("as", "point");
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // root
            writer.WriteEndElement(); // mxGraphModel
            writer.WriteEndElement(); // diagram
            writer.WriteEndElement(); // mxfile
            writer.WriteEndDocument();
        }
    }

    internal static void WriteTile(LocalFlowchartDocument document, LocalFlowchartLayout layout,
        LocalFlowchartSpatialIndex spatialIndex, LocalFlowchartPngRenderer.TileRegion region,
        Stream destination, ChartTheme? theme = null)
    {
        var visibleNodes = spatialIndex.NodesIntersecting(region.X, region.Y, region.Width, region.Height)
            .Where(index => NodeIntersectsTile(layout.Nodes[index], region))
            .ToHashSet();
        var visibleEdges = spatialIndex.EdgesIntersecting(region.X, region.Y, region.Width, region.Height)
            .Where(index => EdgeIntersectsTile(layout.Edges[index], region))
            .ToHashSet();

        var requiredNodeIds = visibleNodes.Select(index => layout.Nodes[index].Node.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var index in visibleEdges)
        {
            var edge = layout.Edges[index].Edge;
            requiredNodeIds.Add(edge.FromId);
            requiredNodeIds.Add(edge.ToId);
        }

        var tileNodes = layout.Nodes.Where(node => requiredNodeIds.Contains(node.Node.Id))
            .Select(node => node with { X = node.X - region.X, Y = node.Y - region.Y }).ToArray();
        var tileEdges = layout.Edges.Where((_, index) => visibleEdges.Contains(index))
            .Select(edge => edge with
            {
                Route = edge.Route.Select(point => new LocalFlowchartLayoutPoint(point.X - region.X, point.Y - region.Y)).ToArray(),
                LabelX = edge.LabelX - region.X,
                LabelY = edge.LabelY - region.Y
            }).ToArray();
        var tileDocument = document with
        {
            Nodes = tileNodes.Select(node => node.Node).ToArray(),
            Edges = tileEdges.Select(edge => edge.Edge with
            {
                ImportedWaypoints = edge.Edge.ImportedWaypoints?.Select(point =>
                    new LocalFlowchartImportedWaypoint(point.X - region.X, point.Y - region.Y)).ToArray()
            }).ToArray(),
            ImportedPageGeometry = new LocalFlowchartImportedPageGeometry(region.Width, region.Height)
        };
        var tileLayout = new LocalFlowchartLayout(region.Width, region.Height, 0, tileNodes, tileEdges,
            layout.Direction, Array.Empty<LocalFlowchartLayoutDiagnostic>());
        Write(tileDocument, tileLayout, destination, theme);
    }

    private static bool NodeIntersectsTile(LocalFlowchartLayoutNode node,
        LocalFlowchartPngRenderer.TileRegion region) =>
        node.X <= region.X + region.Width && node.X + node.Width >= region.X
        && node.Y <= region.Y + region.Height && node.Y + node.Height >= region.Y;

    private static bool EdgeIntersectsTile(LocalFlowchartLayoutEdge edge,
        LocalFlowchartPngRenderer.TileRegion region)
    {
        var right = region.X + region.Width;
        var bottom = region.Y + region.Height;
        if (edge.Edge.Label is not null
            && edge.LabelX - edge.LabelWidth / 2 <= right
            && edge.LabelX + (edge.LabelWidth + 1) / 2 >= region.X
            && edge.LabelY - edge.LabelHeight / 2 <= bottom
            && edge.LabelY + (edge.LabelHeight + 1) / 2 >= region.Y)
            return true;

        return edge.Route.Zip(edge.Route.Skip(1)).Any(segment => SegmentIntersectsTile(
            segment.First, segment.Second, region.X, region.Y, right, bottom));
    }

    private static bool SegmentIntersectsTile(LocalFlowchartLayoutPoint first, LocalFlowchartLayoutPoint second,
        int left, int top, int right, int bottom) => first.Y == second.Y
        ? first.Y >= top && first.Y <= bottom && Math.Max(Math.Min(first.X, second.X), left) <= Math.Min(Math.Max(first.X, second.X), right)
        : first.X >= left && first.X <= right && Math.Max(Math.Min(first.Y, second.Y), top) <= Math.Min(Math.Max(first.Y, second.Y), bottom);

    private static void WriteBaseCell(XmlWriter writer, string id, string? parent)
    {
        writer.WriteStartElement("mxCell");
        writer.WriteAttributeString("id", id);
        if (parent is not null) writer.WriteAttributeString("parent", parent);
        writer.WriteEndElement();
    }

    private static string NodeCellId(LocalFlowchartNode node) => node.ImportedCellId ?? $"n{node.Order + 1}";
    private static string EdgeCellId(LocalFlowchartEdge edge) => edge.ImportedCellId ?? $"e{edge.Order + 1}";

    private static string NodeStyle(LocalFlowchartNodeShape shape) => shape switch
    {
        LocalFlowchartNodeShape.Start => "ellipse;perimeter=ellipsePerimeter;whiteSpace=wrap;html=0;fillColor=#e0f5eb;strokeColor=#243447;",
        LocalFlowchartNodeShape.Process => "rounded=0;whiteSpace=wrap;html=0;fillColor=#e8f1fa;strokeColor=#243447;",
        LocalFlowchartNodeShape.Decision => "rhombus;perimeter=rhombusPerimeter;whiteSpace=wrap;html=0;fillColor=#fff5d6;strokeColor=#243447;",
        LocalFlowchartNodeShape.InputOutput => "parallelogram;perimeter=parallelogramPerimeter;whiteSpace=wrap;html=0;fillColor=#e4f3fd;strokeColor=#243447;",
        LocalFlowchartNodeShape.Person => "rounded=1;arcSize=12;whiteSpace=wrap;html=0;fillColor=#e8f1ff;strokeColor=#243447;",
        LocalFlowchartNodeShape.Cloud => "cloud;whiteSpace=wrap;html=0;fillColor=#fff7e1;strokeColor=#243447;",
        LocalFlowchartNodeShape.End => "ellipse;perimeter=ellipsePerimeter;whiteSpace=wrap;html=0;fillColor=#e0f5eb;strokeColor=#243447;",
        _ => throw new InvalidOperationException("The node shape expression was not exhaustive.")
    };

    private static string ApplyColorOverrides(string style, params (string Key, string Value)[] overrides)
    {
        var values = overrides.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        var applied = new HashSet<string>(StringComparer.Ordinal);
        var tokens = style.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(token =>
        {
            var separator = token.IndexOf('=');
            if (separator <= 0) return token;
            var key = token[..separator];
            if (!values.TryGetValue(key, out var value)) return token;
            applied.Add(key);
            return $"{key}={value}";
        }).ToList();
        foreach (var item in overrides)
            if (applied.Add(item.Key)) tokens.Add($"{item.Key}={item.Value}");
        return string.Join(';', tokens) + ";";
    }
}
