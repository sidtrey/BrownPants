using System.Globalization;
using System.Text;
using System.Xml;

namespace BrownPants.Cli.Analysis;

internal static class LocalFlowchartDrawioWriter
{
    internal static byte[] Write(LocalFlowchartDocument document)
    {
        var layout = LocalFlowchartLayoutEngine.Create(document);
        using var stream = new MemoryStream();
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
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("mxfile");
            writer.WriteAttributeString("host", "app.diagrams.net");
            writer.WriteAttributeString("agent", "BrownPants");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("type", "device");
            writer.WriteStartElement("diagram");
            writer.WriteAttributeString("id", document.Id);
            writer.WriteAttributeString("name", "Page-1");
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
            writer.WriteAttributeString("pageWidth", Math.Max(850, layout.Width).ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("pageHeight", Math.Max(1100, layout.Height).ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("math", "0");
            writer.WriteAttributeString("shadow", "0");
            writer.WriteStartElement("root");
            WriteBaseCell(writer, "0", null);
            WriteBaseCell(writer, "1", "0");

            foreach (var node in layout.Nodes.OrderBy(item => item.Node.Order))
            {
                writer.WriteStartElement("mxCell");
                writer.WriteAttributeString("id", NodeCellId(node.Node));
                writer.WriteAttributeString("value", node.Node.Label);
                writer.WriteAttributeString("style", NodeStyle(node.Node.Shape));
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
                writer.WriteAttributeString("style", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=0;endArrow=block;endFill=1;");
                writer.WriteAttributeString("edge", "1");
                writer.WriteAttributeString("parent", "1");
                writer.WriteAttributeString("source", NodeCellId(layout.Node(edge.FromId).Node));
                writer.WriteAttributeString("target", NodeCellId(layout.Node(edge.ToId).Node));
                writer.WriteStartElement("mxGeometry");
                writer.WriteAttributeString("relative", "1");
                writer.WriteAttributeString("as", "geometry");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // root
            writer.WriteEndElement(); // mxGraphModel
            writer.WriteEndElement(); // diagram
            writer.WriteEndElement(); // mxfile
            writer.WriteEndDocument();
        }
        return stream.ToArray();
    }

    private static void WriteBaseCell(XmlWriter writer, string id, string? parent)
    {
        writer.WriteStartElement("mxCell");
        writer.WriteAttributeString("id", id);
        if (parent is not null) writer.WriteAttributeString("parent", parent);
        writer.WriteEndElement();
    }

    private static string NodeCellId(LocalFlowchartNode node) => $"n{node.Order + 1}";
    private static string EdgeCellId(LocalFlowchartEdge edge) => $"e{edge.Order + 1}";

    private static string NodeStyle(LocalFlowchartNodeShape shape) => shape switch
    {
        LocalFlowchartNodeShape.Start => "ellipse;perimeter=ellipsePerimeter;whiteSpace=wrap;html=0;fillColor=#e0f5eb;strokeColor=#243447;",
        LocalFlowchartNodeShape.Process => "rounded=0;whiteSpace=wrap;html=0;fillColor=#e8f1fa;strokeColor=#243447;",
        LocalFlowchartNodeShape.Decision => "rhombus;perimeter=rhombusPerimeter;whiteSpace=wrap;html=0;fillColor=#fff5d6;strokeColor=#243447;",
        LocalFlowchartNodeShape.End => "ellipse;perimeter=ellipsePerimeter;whiteSpace=wrap;html=0;fillColor=#e0f5eb;strokeColor=#243447;",
        _ => throw new InvalidOperationException("The node shape expression was not exhaustive.")
    };
}
