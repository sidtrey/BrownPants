using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BrownPants.Cli.Analysis;

internal static class LocalFlowchartDrawioParser
{
    private const int MaximumInflatedDiagramBytes = 128 * 1024 * 1024;

    internal static LocalFlowchartDocument Parse(ReadOnlyMemory<byte> bytes, string sourcePath)
    {
        if (bytes.Length > LocalFlowchartParser.MaximumSourceBytes)
            throw Error(sourcePath, $"input exceeds the {LocalFlowchartParser.MaximumSourceBytes}-byte limit");

        XDocument xml;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = LocalFlowchartParser.MaximumSourceBytes,
                MaxCharactersFromEntities = 0,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            using var stream = new MemoryStream(bytes.ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, settings);
            xml = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw Error(sourcePath, $"invalid or unsafe XML: {exception.Message}");
        }

        var root = xml.Root ?? throw Error(sourcePath, "document has no root element");
        XElement model;
        string diagramId;
        string? pageName = null;
        if (root.Name == "mxGraphModel")
        {
            model = root;
            diagramId = Path.GetFileNameWithoutExtension(sourcePath);
        }
        else if (root.Name == "mxfile")
        {
            RejectUnknownAttributes(root, ["host", "agent", "version", "type", "modified", "etag", "compressed"], sourcePath, "mxfile");
            if (root.Elements().Any(element => element.Name != "diagram"))
                throw Error(sourcePath, "mxfile contains an unsupported element");
            var pages = root.Elements("diagram").ToArray();
            if (pages.Length != 1)
                throw Error(sourcePath, $"expected exactly one diagram page; found {pages.Length}");
            var page = pages[0];
            RejectUnknownAttributes(page, ["id", "name"], sourcePath, "diagram page");
            diagramId = (string?)page.Attribute("id") ?? Path.GetFileNameWithoutExtension(sourcePath);
            pageName = (string?)page.Attribute("name");
            var pageElements = page.Elements().ToArray();
            if (pageElements.Length == 0)
                model = DecodeCompressedPage(page, sourcePath);
            else if (pageElements.Length == 1 && pageElements[0].Name == "mxGraphModel")
                model = pageElements[0];
            else
                throw Error(sourcePath, "diagram page has unsupported child elements");
        }
        else
        {
            throw Error(sourcePath, $"unsupported root element <{root.Name.LocalName}>; expected <mxfile> or <mxGraphModel>");
        }

        if (model.Elements().Any(element => element.Name != "root"))
            throw Error(sourcePath, "mxGraphModel contains an unsupported child element");
        RejectUnknownAttributes(model, ["dx", "dy", "grid", "gridSize", "guides", "tooltips", "connect", "arrows", "fold", "page", "pageScale", "pageWidth", "pageHeight", "math", "shadow", "background"], sourcePath, "mxGraphModel");
        var importedBackground = (string?)model.Attribute("background");
        if (importedBackground is not null && (importedBackground.Length != 7 || importedBackground[0] != '#'
            || importedBackground.AsSpan(1).IndexOfAnyExcept("0123456789abcdefABCDEF") >= 0))
            throw Error(sourcePath, "mxGraphModel background must use #RRGGBB hex format");
        var graphRoot = model.Element("root") ?? throw Error(sourcePath, "mxGraphModel has no root cell container");
        if (graphRoot.Elements().Any(element => element.Name != "mxCell"))
            throw Error(sourcePath, "user objects, embedded shapes, and non-cell root elements are unsupported");

        var cells = graphRoot.Elements("mxCell").ToArray();
        if (cells.Length > LocalFlowchartParser.MaximumNodes + LocalFlowchartParser.MaximumEdges + 2)
            throw Error(sourcePath, "cell count exceeds the supported node and edge limits");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cell in cells)
        {
            RejectUnknownAttributes(cell, ["id", "value", "style", "tooltip", "vertex", "edge", "parent", "source", "target"], sourcePath, "mxCell");
            var id = (string?)cell.Attribute("id");
            if (string.IsNullOrEmpty(id) || !ids.Add(id))
                throw Error(sourcePath, "cell IDs must be non-empty and unique");
            if (id is "__proto__" or "constructor" or "prototype")
                throw Error(sourcePath, $"cell ID '{id}' is reserved");
        }
        if (!cells.Any(cell => (string?)cell.Attribute("id") == "0")
            || !cells.Any(cell => (string?)cell.Attribute("id") == "1" && (string?)cell.Attribute("parent") == "0"))
            throw Error(sourcePath, "mxGraphModel must contain root cell 0 and default layer cell 1");

        var nodeCells = new List<(XElement Cell, string Id, LocalFlowchartNodeShape Shape, string Label, int X, int Y, int Width, int Height)>();
        var edgeCells = new List<XElement>();
        foreach (var cell in cells.Where(cell => (string?)cell.Attribute("id") is not ("0" or "1")))
        {
            if ((string?)cell.Attribute("vertex") == "1" && (string?)cell.Attribute("edge") != "1")
            {
                if ((string?)cell.Attribute("parent") != "1")
                    throw Error(sourcePath, "nested or non-default layers are not supported");
                RejectExternalFeatures(cell, sourcePath);
                var label = (string?)cell.Attribute("value") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(label) || label.Length > LocalFlowchartParser.MaximumLabelLength || label.Any(char.IsControl))
                    throw Error(sourcePath, $"cell '{(string?)cell.Attribute("id")}' has an empty, oversized, or unsupported label");
                if (((string?)cell.Attribute("tooltip"))?.Length > LocalFlowchartParser.MaximumDetailsLength)
                    throw Error(sourcePath, $"vertex '{(string?)cell.Attribute("id")}' has an oversized tooltip");
                var geometries = cell.Elements().ToArray();
                if (geometries.Length != 1) throw Error(sourcePath, $"vertex '{(string?)cell.Attribute("id")}' must contain exactly one geometry");
                var geometry = geometries[0];
                RejectUnknownAttributes(geometry, ["x", "y", "width", "height", "as"], sourcePath, "vertex geometry");
                if (geometry.Name != "mxGeometry" || (string?)geometry.Attribute("as") != "geometry" || geometry.HasElements)
                    throw Error(sourcePath, $"vertex '{(string?)cell.Attribute("id")}' has unsupported geometry");
                var shape = ParseShape((string?)cell.Attribute("style"), sourcePath, (string?)cell.Attribute("id") ?? "?");
                nodeCells.Add((cell, (string)cell.Attribute("id")!, shape, label,
                    ReadCoordinate(geometry, "x", sourcePath), ReadCoordinate(geometry, "y", sourcePath),
                    ReadDimension(geometry, "width", sourcePath), ReadDimension(geometry, "height", sourcePath)));
                if (nodeCells.Count > LocalFlowchartParser.MaximumNodes)
                    throw Error(sourcePath, $"node count exceeds {LocalFlowchartParser.MaximumNodes}");
            }
            else if ((string?)cell.Attribute("edge") == "1" && (string?)cell.Attribute("vertex") != "1")
            {
                if ((string?)cell.Attribute("parent") != "1")
                    throw Error(sourcePath, "nested or non-default layers are not supported");
                RejectExternalFeatures(cell, sourcePath);
                edgeCells.Add(cell);
                if (edgeCells.Count > LocalFlowchartParser.MaximumEdges)
                    throw Error(sourcePath, $"edge count exceeds {LocalFlowchartParser.MaximumEdges}");
            }
            else
            {
                throw Error(sourcePath, $"cell '{(string?)cell.Attribute("id")}' is not a supported vertex or edge");
            }
        }
        if (nodeCells.Count == 0) throw Error(sourcePath, "diagram must contain at least one vertex");

        var nodeIds = nodeCells.Select((item, index) => (item.Id, InternalId: $"n{index}"))
            .ToDictionary(item => item.Id, item => item.InternalId, StringComparer.Ordinal);
        var nodes = nodeCells.Select((item, index) => new LocalFlowchartNode(nodeIds[item.Id], item.Shape, item.Label, 0, index,
            (string?)item.Cell.Attribute("tooltip"), new LocalFlowchartImportedGeometry(item.X, item.Y, item.Width, item.Height), item.Id,
            (string?)item.Cell.Attribute("style"))).ToArray();
        var edges = new List<LocalFlowchartEdge>(edgeCells.Count);
        foreach (var cell in edgeCells)
        {
            var id = (string)cell.Attribute("id")!;
            var geometries = cell.Elements().ToArray();
            if (geometries.Length != 1 || geometries[0].Name != "mxGeometry"
                || (string?)geometries[0].Attribute("as") != "geometry")
                throw Error(sourcePath, $"edge '{id}' has unsupported geometry");
            var geometry = geometries[0];
            RejectUnknownAttributes(geometry, ["relative", "as"], sourcePath, "edge geometry");
            var pointArrays = geometry.Elements().ToArray();
            if (pointArrays.Length > 1 || pointArrays.Any(array => array.Name != "Array" || (string?)array.Attribute("as") != "points"))
                throw Error(sourcePath, $"edge '{id}' has unsupported geometry children");
            var waypoints = new List<LocalFlowchartImportedWaypoint>();
            if (pointArrays.Length == 1)
            {
                foreach (var point in pointArrays[0].Elements())
                {
                    if (point.Name != "mxPoint" || (string?)point.Attribute("as") != "point")
                        throw Error(sourcePath, $"edge '{id}' has unsupported waypoint data");
                    RejectUnknownAttributes(point, ["x", "y", "as"], sourcePath, "edge waypoint");
                    if (waypoints.Count == 10000) throw Error(sourcePath, $"edge '{id}' exceeds 10,000 waypoints");
                    waypoints.Add(new LocalFlowchartImportedWaypoint(ReadCoordinate(point, "x", sourcePath), ReadCoordinate(point, "y", sourcePath)));
                }
            }
            var from = (string?)cell.Attribute("source");
            var to = (string?)cell.Attribute("target");
            if (from is null || to is null || !nodeIds.TryGetValue(from, out var fromId) || !nodeIds.TryGetValue(to, out var toId))
                throw Error(sourcePath, $"edge '{id}' must connect two supported vertices");
            var label = (string?)cell.Attribute("value");
            if (string.IsNullOrWhiteSpace(label)) label = null;
            if (label?.Length > LocalFlowchartParser.MaximumLabelLength || label?.Any(char.IsControl) == true)
                throw Error(sourcePath, $"edge '{id}' has an oversized or unsupported label");
            if (((string?)cell.Attribute("tooltip"))?.Length > LocalFlowchartParser.MaximumDetailsLength)
                throw Error(sourcePath, $"edge '{id}' has an oversized tooltip");
            var edgeStyle = (string?)cell.Attribute("style");
            ValidateEdgeStyle(edgeStyle, sourcePath, id);
            edges.Add(new LocalFlowchartEdge(fromId, toId, label, 0, edges.Count, id,
                (string?)cell.Attribute("tooltip"), edgeStyle, waypoints));
        }
        var pageWidth = ReadOptionalDimension(model, "pageWidth", sourcePath);
        var pageHeight = ReadOptionalDimension(model, "pageHeight", sourcePath);
        var pageGeometry = pageWidth is not null && pageHeight is not null
            ? new LocalFlowchartImportedPageGeometry(pageWidth.Value, pageHeight.Value) : null;
        return new LocalFlowchartDocument(SafeDiagramId(diagramId), sourcePath, nodes, edges,
            ImportedPageGeometry: pageGeometry, ImportedPageName: pageName, ImportedBackground: importedBackground);
    }

    private static LocalFlowchartNodeShape ParseShape(string? style, string sourcePath, string id)
    {
        var values = ParseStyle(style, sourcePath, id);
        if (values.Keys.Any(key => key is not ("ellipse" or "rhombus" or "parallelogram" or "cloud" or "rounded" or "whiteSpace" or "html"
            or "fillColor" or "strokeColor" or "fontColor" or "fontStyle" or "align" or "verticalAlign" or "spacing" or "shadow"
            or "perimeter" or "shape" or "arcSize")))
            throw Error(sourcePath, $"vertex '{id}' uses unsupported style features");
        if (values.TryGetValue("shape", out var declaredShape)
            && declaredShape is not ("rectangle" or "rect" or "rhombus" or "parallelogram" or "ellipse" or "cloud"))
            throw Error(sourcePath, $"vertex '{id}' uses unsupported shape '{declaredShape}'");
        if (values.GetValueOrDefault("html") is "1" or "true")
            throw Error(sourcePath, $"vertex '{id}' uses an HTML label, which is unsupported");
        foreach (var colorKey in new[] { "fillColor", "strokeColor", "fontColor" })
            if (values.TryGetValue(colorKey, out var color) && color is not ("none" or "default")
                && !System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                throw Error(sourcePath, $"vertex '{id}' has an unsupported {colorKey} value");
        if (values.ContainsKey("rhombus") || values.GetValueOrDefault("shape") == "rhombus") return LocalFlowchartNodeShape.Decision;
        if (values.ContainsKey("parallelogram") || values.GetValueOrDefault("shape") == "parallelogram") return LocalFlowchartNodeShape.InputOutput;
        if (values.ContainsKey("ellipse") || values.GetValueOrDefault("shape") == "ellipse") return LocalFlowchartNodeShape.Start;
        if (values.GetValueOrDefault("shape") == "cloud") return LocalFlowchartNodeShape.Cloud;
        return LocalFlowchartNodeShape.Process;
    }

    private static Dictionary<string, string> ParseStyle(string? style, string sourcePath, string id)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in (style ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = item.IndexOf('=');
            var key = separator < 0 ? item : item[..separator];
            var value = separator < 0 ? string.Empty : item[(separator + 1)..];
            if (key.Length == 0 || value.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '#' or '.' or '_' or '-' or '%'))
                || !values.TryAdd(key, value))
                throw Error(sourcePath, $"cell '{id}' has malformed or ambiguous style data");
        }
        return values;
    }

    private static void ValidateEdgeStyle(string? style, string sourcePath, string id)
    {
        var values = ParseStyle(style, sourcePath, id);
        var supportedEdgeStyles = new HashSet<string>(StringComparer.Ordinal)
        {
            "orthogonalEdgeStyle", "noneEdgeStyle", "elbowEdgeStyle", "entityRelationEdgeStyle",
            "isometricEdgeStyle", "segmentEdgeStyle", "sideToSideEdgeStyle", "topToBottomEdgeStyle"
        };
        var supported = new HashSet<string>(StringComparer.Ordinal)
        {
            "edgeStyle", "rounded", "orthogonalLoop", "jettySize", "html", "endArrow", "endFill", "startArrow", "startFill",
            "strokeColor", "fontColor", "labelBackgroundColor", "strokeWidth", "dashed", "dashPattern", "opacity",
            "exitX", "exitY", "entryX", "entryY"
        };
        if (values.Keys.Any(key => !supported.Contains(key))
            || values.TryGetValue("edgeStyle", out var edgeStyle) && !supportedEdgeStyles.Contains(edgeStyle)
            || values.TryGetValue("html", out var html) && html is "1" or "true"
            || new[] { "strokeColor", "fontColor", "labelBackgroundColor" }.Any(colorKey =>
                values.TryGetValue(colorKey, out var color) && color is not ("none" or "default")
                    && !System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)))
            throw Error(sourcePath, $"edge '{id}' uses unsupported or unsafe style features");
    }

    private static XElement DecodeCompressedPage(XElement page, string sourcePath)
    {
        var encoded = page.Value.Trim();
        if (encoded.Length == 0) throw Error(sourcePath, "diagram page is empty");
        if (encoded.Length > LocalFlowchartParser.MaximumSourceBytes * 2L)
            throw Error(sourcePath, "compressed page payload exceeds the input limit");
        byte[] compressed;
        try { compressed = Convert.FromBase64String(encoded); }
        catch (FormatException) { throw Error(sourcePath, "compressed page is not valid Base64"); }
        try
        {
            using var input = new MemoryStream(compressed, writable: false);
            using var inflater = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[32_768];
            while (true)
            {
                var read = inflater.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (output.Length + read > MaximumInflatedDiagramBytes)
                    throw Error(sourcePath, $"inflated page exceeds {MaximumInflatedDiagramBytes} bytes");
                output.Write(buffer, 0, read);
            }
            var uriEncoded = new UTF8Encoding(false, true).GetString(output.ToArray());
            var decoded = Uri.UnescapeDataString(uriEncoded);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumInflatedDiagramBytes,
                MaxCharactersFromEntities = 0
            };
            using var text = new StringReader(decoded);
            using var reader = XmlReader.Create(text, settings);
            var model = XElement.Load(reader, LoadOptions.None);
            if (model.Name != "mxGraphModel") throw Error(sourcePath, "compressed page does not contain an mxGraphModel");
            return model;
        }
        catch (LocalFlowchartFormatException) { throw; }
        catch (Exception exception) when (exception is InvalidDataException or DecoderFallbackException or UriFormatException or XmlException)
        {
            throw Error(sourcePath, $"compressed page is invalid: {exception.Message}");
        }
    }

    private static void RejectExternalFeatures(XElement cell, string sourcePath)
    {
        if (cell.Attributes().Any(attribute => attribute.Name.LocalName is "link" or "image" or "src"))
            throw Error(sourcePath, $"cell '{(string?)cell.Attribute("id")}' contains a link or external resource");
        if (cell.Attributes().Any(attribute => attribute.Name.Namespace != XNamespace.None))
            throw Error(sourcePath, "namespaced cell attributes are unsupported");
    }

    private static void RejectUnknownAttributes(XElement element, IReadOnlyCollection<string> allowed, string sourcePath, string description)
    {
        if (element.Attributes().Any(attribute => attribute.Name.Namespace != XNamespace.None || !allowed.Contains(attribute.Name.LocalName, StringComparer.Ordinal)))
            throw Error(sourcePath, $"{description} contains an unsupported attribute");
    }

    private static int ReadCoordinate(XElement geometry, string name, string sourcePath)
    {
        if (!int.TryParse((string?)geometry.Attribute(name), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            || value < -LocalFlowchartLayoutEngine.MaximumLogicalDimension || value > LocalFlowchartLayoutEngine.MaximumLogicalDimension)
            throw Error(sourcePath, $"geometry {name} must be an integer between {-LocalFlowchartLayoutEngine.MaximumLogicalDimension} and {LocalFlowchartLayoutEngine.MaximumLogicalDimension}");
        return value;
    }

    private static int ReadDimension(XElement geometry, string name, string sourcePath)
    {
        if (!int.TryParse((string?)geometry.Attribute(name), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value is <= 0 || value > LocalFlowchartLayoutEngine.MaximumLogicalDimension)
            throw Error(sourcePath, $"geometry {name} must be a positive integer no greater than {LocalFlowchartLayoutEngine.MaximumLogicalDimension}");
        return value;
    }

    private static int? ReadOptionalDimension(XElement element, string name, string sourcePath)
    {
        var attribute = element.Attribute(name);
        return attribute is null ? null : ReadDimension(new XElement("geometry", attribute), name, sourcePath);
    }

    private static string SafeDiagramId(string id) => string.IsNullOrWhiteSpace(id) ? "imported" : id;
    private static LocalFlowchartFormatException Error(string sourcePath, string message) => new($"{sourcePath}: {message}.");
}
