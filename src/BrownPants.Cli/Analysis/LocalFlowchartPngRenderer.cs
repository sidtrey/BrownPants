using System.IO.Compression;
using System.Text;

namespace BrownPants.Cli.Analysis;

internal static class LocalFlowchartPngRenderer
{
    internal sealed record Tile(int X, int Y, int Width, int Height, byte[] Png);
    internal sealed record TileRegion(int X, int Y, int Width, int Height);
    internal static long MaximumSingleRasterPixels => (Array.MaxLength / 3L) * 9 / 10;
    internal static bool FitsSingleRaster(int width, int height) => width > 0 && height > 0
        && (long)width * height <= MaximumSingleRasterPixels;
    // U+0020 through U+007E are from https://github.com/dhepper/font8x8/blob/master/font8x8_basic.h,
    // based on IBM's public-domain VGA font; the font8x8 header identifies the glyph data as public domain.
    private static readonly byte[] Font = Convert.FromBase64String(
        "AAAAAAAAAAAYPDwYGAAYADY2AAAAAAAANjZ/Nn82NgAMPgMeMB8MAABjMxgMZmMAHDYcbjszbgAGBgMAAAAAABgMBgYGDBgABgwYGBgMBgAAZjz/PGYAAAAMDD8MDAAAAAAAAAAMDAYAAAA/AAAAAAAAAAAADAwAYDAYDAYDAQA+Y3N7b2c+AAwODAwMDD8AHjMwHAYzPwAeMzAcMDMeADg8NjN/MHgAPwMfMDAzHgAcBgMfMzMeAD8zMBgMDAwAHjMzHjMzHgAeMzM+MBgOAAAMDAAADAwAAAwMAAAMDAYYDAYDBgwYAAAAPwAAPwAABgwYMBgMBgAeMzAYDAAMAD5je3t7Ax4ADB4zMz8zMwA/ZmY+ZmY/ADxmAwMDZjwAHzZmZmY2HwB/RhYeFkZ/AH9GFh4WBg8APGYDA3NmfAAzMzM/MzMzAB4MDAwMDB4AeDAwMDMzHgBnZjYeNmZnAA8GBgZGZn8AY3d/f2tjYwBjZ297c2NjABw2Y2NjNhwAP2ZmPgYGDwAeMzMzOx44AD9mZj42ZmcAHjMHDjgzHgA/LQwMDAweADMzMzMzMz8AMzMzMzMeDABjY2Nrf3djAGNjNhwcNmMAMzMzHgwMHgB/YzEYTGZ/AB4GBgYGBh4AAwYMGDBgQAAeGBgYGBgeAAgcNmMAAAAAAAAAAAAAAP8MDBgAAAAAAAAAHjA+M24ABwYGPmZmOwAAAB4zAzMeADgwMD4zM24AAAAeMz8DHgAcNgYPBgYPAAAAbjMzPjAfBwY2bmZmZwAMAA4MDAweADAAMDAwMzMeBwZmNh42ZwAODAwMDAweAAAAM39/a2MAAAAfMzMzMwAAAB4zMzMeAAAAO2ZmPgYPAABuMzM+MHgAADtuZgYPAAAAPgMeMB8ACAw+DAwsGAAAADMzMzNuAAAAMzMzHgwAAABja39/NgAAAGM2HDZjAAAAMzMzPjAfAAA/GQwmPwA4DAwHDAw4ABgYGAAYGBgABwwMOAwMBwBuOwAAAAAAAA==");
    private static readonly uint[] CrcTable = CreateCrcTable();

    internal static byte[] Render(LocalFlowchartDocument document, LocalFlowchartLayoutOptions? options = null, ChartTheme? theme = null)
    {
        var layout = LocalFlowchartLayoutEngine.Create(document, options);
        if (!FitsSingleRaster(layout.Width, layout.Height))
            throw new LocalFlowchartFormatException(
                $"{document.SourcePath}: the calculated flowchart canvas ({layout.Width} by {layout.Height} pixels) exceeds the single-PNG raster capacity ({MaximumSingleRasterPixels:N0} pixels, with 10% headroom below the .NET byte-array limit). Use --tile-size to render bounded PNG tiles.");
        var spatialIndex = new LocalFlowchartSpatialIndex(layout);
        return RenderRegion(layout, spatialIndex, 0, 0, layout.Width, layout.Height, theme);
    }

    internal static IReadOnlyList<Tile> RenderTiles(LocalFlowchartDocument document, int tileSize, int overlap,
        LocalFlowchartLayoutOptions? options = null, ChartTheme? theme = null)
    {
        var layout = LocalFlowchartLayoutEngine.Create(document, options);
        var spatialIndex = new LocalFlowchartSpatialIndex(layout);
        return EnumerateTileRegions(layout, tileSize, overlap)
            .Select(region => RenderTile(layout, region, spatialIndex, theme)).ToArray();
    }

    internal static IEnumerable<TileRegion> EnumerateTileRegions(LocalFlowchartLayout layout, int tileSize, int overlap)
    {
        ValidateTileOptions(tileSize, overlap);
        var yStarts = TileStarts(layout.Height, tileSize, overlap);
        foreach (var y in yStarts)
        foreach (var x in TileStarts(layout.Width, tileSize, overlap))
            yield return new TileRegion(x, y, Math.Min(tileSize, layout.Width - x), Math.Min(tileSize, layout.Height - y));
    }

    internal static Tile RenderTile(LocalFlowchartLayout layout, TileRegion region,
        LocalFlowchartSpatialIndex? spatialIndex = null, ChartTheme? theme = null)
    {
        spatialIndex ??= new LocalFlowchartSpatialIndex(layout);
        return new Tile(region.X, region.Y, region.Width, region.Height,
            RenderRegion(layout, spatialIndex, region.X, region.Y, region.Width, region.Height, theme));
    }

    private static void ValidateTileOptions(int tileSize, int overlap)
    {
        if (tileSize is < 256 or > 4000)
            throw new LocalFlowchartFormatException("Tile size must be between 256 and 4000 pixels so every tile stays within the 16-million-pixel raster limit.");
        if (overlap < 0 || overlap >= tileSize / 2)
            throw new LocalFlowchartFormatException("Tile overlap must be zero or greater and less than half the tile size.");
    }

    private static IEnumerable<int> TileStarts(int length, int tileSize, int overlap)
    {
        var step = tileSize - overlap;
        for (var position = 0; position < length;)
        {
            yield return position;
            if ((long)position + tileSize >= length) yield break;
            position += step;
        }
    }

    private static byte[] RenderRegion(LocalFlowchartLayout layout, LocalFlowchartSpatialIndex spatialIndex,
        int originX, int originY, int width, int height, ChartTheme? theme)
    {
        theme ??= ChartThemeCatalog.Default;
        var raster = new Raster(width, height, originX, originY, theme);
        var edgeCandidates = spatialIndex.EdgesIntersecting(originX, originY, width, height);
        raster.DrawEdges(layout, edgeCandidates);
        raster.DrawNodes(layout, spatialIndex.NodesIntersecting(originX, originY, width, height));
        raster.DrawArrowheads(layout, edgeCandidates);
        return EncodePng(raster);
    }

    private static byte[] EncodePng(Raster raster)
    {
        var rowLength = checked(raster.Width * 3 + 1);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            var row = new byte[rowLength];
            for (var y = 0; y < raster.Height; y++)
            {
                Buffer.BlockCopy(raster.Pixels, y * raster.Width * 3, row, 1, raster.Width * 3);
                zlib.Write(row);
            }
        }

        using var output = new MemoryStream();
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        Span<byte> header = stackalloc byte[13];
        WriteUInt32(header, 0, (uint)raster.Width);
        WriteUInt32(header, 4, (uint)raster.Height);
        header[8] = 8;
        header[9] = 2;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(output, "IHDR", header);
        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", ReadOnlySpan<byte>.Empty);
        return output.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        WriteUInt32(length, 0, (uint)data.Length);
        stream.Write(length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);
        var crc = 0xffffffffu;
        foreach (var value in typeBytes) crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        foreach (var value in data) crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        Span<byte> crcBytes = stackalloc byte[4];
        WriteUInt32(crcBytes, 0, ~crc);
        stream.Write(crcBytes);
    }

    private static void WriteUInt32(Span<byte> destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            var value = index;
            for (var bit = 0; bit < 8; bit++)
                value = (value & 1) != 0 ? 0xedb88320u ^ (value >> 1) : value >> 1;
            table[index] = value;
        }
        return table;
    }

    private sealed class Raster
    {
        private readonly ChartTheme? _theme;
        private readonly byte[] Background;
        private readonly byte[] Ink;
        private readonly byte[] EdgeLabelInk;
        private readonly byte[] EdgeInk;
        private readonly byte[] ArrowInk;
        private readonly byte[] NodeBorder = [76, 93, 119];
        private readonly byte[] Shadow;
        private readonly byte[] EdgeLabelBackground;
        private static readonly byte[] DataFill = [228, 243, 253];
        private static readonly byte[] ProcessFill = [255, 255, 255];
        private static readonly byte[] DecisionFill = [238, 237, 255];
        private static readonly byte[] TerminalFill = [223, 246, 237];
        private static readonly byte[] PersonFill = [232, 241, 255];
        private static readonly byte[] CloudFill = [255, 247, 225];

        internal Raster(int width, int height, int originX, int originY, ChartTheme? theme)
        {
            _theme = theme;
            Background = theme?.Canvas.ToRgb() ?? [247, 249, 252];
            Ink = theme?.Text.ToRgb() ?? [34, 49, 70];
            EdgeLabelInk = theme?.EdgeLabelText.ToRgb() ?? Ink;
            EdgeInk = theme?.Connector.ToRgb() ?? [124, 141, 164];
            ArrowInk = EdgeInk;
            Shadow = theme?.Shadow.ToRgb() ?? [224, 231, 240];
            EdgeLabelBackground = theme?.EdgeLabelBackground.ToRgb() ?? Background;
            Width = width;
            Height = height;
            OriginX = originX;
            OriginY = originY;
            Pixels = new byte[checked(width * height * 3)];
            for (var index = 0; index < Pixels.Length; index += 3)
            {
                Pixels[index] = Background[0];
                Pixels[index + 1] = Background[1];
                Pixels[index + 2] = Background[2];
            }
        }

        internal int Width { get; }
        internal int Height { get; }
        private int OriginX { get; }
        private int OriginY { get; }
        internal byte[] Pixels { get; }

        internal void DrawEdges(LocalFlowchartLayout layout, IReadOnlyList<int> candidates)
        {
            foreach (var candidate in candidates)
            {
                var routed = layout.Edges[candidate];
                for (var index = 0; index + 1 < routed.Route.Count; index++)
                {
                    var first = routed.Route[index];
                    var second = routed.Route[index + 1];
                    DrawLine(first.X, first.Y, second.X, second.Y, EdgeInk, 3);
                }
                if (routed.Edge.Label is not null)
                    DrawEdgeLabel(routed);
            }
        }

        internal void DrawArrowheads(LocalFlowchartLayout layout, IReadOnlyList<int> candidates)
        {
            foreach (var candidate in candidates)
            {
                var route = layout.Edges[candidate].Route;
                if (route.Count > 1) DrawArrow(route[^2], route[^1]);
            }
        }

        internal void DrawNodes(LocalFlowchartLayout layout, IReadOnlyList<int> candidates)
        {
            foreach (var candidate in candidates)
            {
                var node = layout.Nodes[candidate];
                var colors = _theme?.Shape(node.Node.Shape);
                var fill = colors?.Fill.ToRgb() ?? node.Node.Shape switch
                {
                    LocalFlowchartNodeShape.Start or LocalFlowchartNodeShape.End => TerminalFill,
                    LocalFlowchartNodeShape.Decision => DecisionFill,
                    LocalFlowchartNodeShape.InputOutput => DataFill,
                    LocalFlowchartNodeShape.Person => PersonFill,
                    LocalFlowchartNodeShape.Cloud => CloudFill,
                    _ => ProcessFill
                };
                var border = colors?.Stroke.ToRgb() ?? NodeBorder;
                switch (node.Node.Shape)
                {
                    case LocalFlowchartNodeShape.Start:
                    case LocalFlowchartNodeShape.End:
                        FillEllipse(node.X + 2, node.Y + 6, node.Width, node.Height, Shadow);
                        FillEllipse(node.X, node.Y, node.Width, node.Height, border);
                        FillEllipse(node.X + 3, node.Y + 3, node.Width - 6, node.Height - 6, fill);
                        break;
                    case LocalFlowchartNodeShape.Decision:
                        var diamond = new[] { (node.X + node.Width / 2, node.Y), (node.X + node.Width, node.Y + node.Height / 2),
                            (node.X + node.Width / 2, node.Y + node.Height), (node.X, node.Y + node.Height / 2) };
                        FillPolygon(diamond.Select(point => (point.Item1 + 2, point.Item2 + 6)).ToArray(), Shadow);
                        FillPolygon(diamond, border);
                        FillPolygon([(node.X + node.Width / 2, node.Y + 3), (node.X + node.Width - 4, node.Y + node.Height / 2),
                            (node.X + node.Width / 2, node.Y + node.Height - 3), (node.X + 4, node.Y + node.Height / 2)], fill);
                        break;
                    case LocalFlowchartNodeShape.InputOutput:
                        var dataShape = new[] { (node.X + 20, node.Y), (node.X + node.Width, node.Y),
                            (node.X + node.Width - 20, node.Y + node.Height), (node.X, node.Y + node.Height) };
                        FillPolygon(dataShape.Select(point => (point.Item1 + 2, point.Item2 + 6)).ToArray(), Shadow);
                        FillPolygon(dataShape, border);
                        FillPolygon([(node.X + 24, node.Y + 3), (node.X + node.Width - 4, node.Y + 3),
                            (node.X + node.Width - 24, node.Y + node.Height - 3), (node.X + 4, node.Y + node.Height - 3)], fill);
                        break;
                    case LocalFlowchartNodeShape.Person:
                        FillRoundedRectangle(node.X + 2, node.Y + 6, node.Width, node.Height, 16, Shadow);
                        FillRoundedRectangle(node.X, node.Y, node.Width, node.Height, 16, border);
                        FillRoundedRectangle(node.X + 3, node.Y + 3, node.Width - 6, node.Height - 6, 13, fill);
                        FillRectangle(node.X + 3, node.Y + 3, 12, node.Height - 6, colors?.Stroke.ToRgb() ?? [119, 151, 205]);
                        break;
                    case LocalFlowchartNodeShape.Cloud:
                        DrawCloud(node.X + 2, node.Y + 6, node.Width, node.Height, Shadow, outline: false);
                        DrawCloud(node.X, node.Y, node.Width, node.Height, border, outline: false);
                        DrawCloud(node.X + 3, node.Y + 3, node.Width - 6, node.Height - 6, fill, outline: false);
                        DrawCloud(node.X + 3, node.Y + 3, node.Width - 6, node.Height - 6, border, outline: true);
                        break;
                    default:
                        FillRoundedRectangle(node.X + 2, node.Y + 6, node.Width, node.Height, 14, Shadow);
                        FillRoundedRectangle(node.X, node.Y, node.Width, node.Height, 14, border);
                        FillRoundedRectangle(node.X + 3, node.Y + 3, node.Width - 6, node.Height - 6, 11, fill);
                        break;
                }

                var textHeight = node.LabelLines.Count * 20;
                var textY = node.Y + (node.Height - textHeight) / 2;
                if (node.Node.Shape == LocalFlowchartNodeShape.Cloud)
                {
                    textY += 12;
                    var labelWidth = node.LabelLines.Max(line => line.Length * 16);
                    FillRoundedRectangle(node.X + (node.Width - labelWidth) / 2 - 10, textY - 6,
                        labelWidth + 20, textHeight + 12, 8, fill);
                }
                foreach (var line in node.LabelLines)
                {
                    var textWidth = line.Length * 16;
                    DrawText(line, node.X + (node.Width - textWidth) / 2, textY);
                    textY += 20;
                }
            }
        }
        private void DrawEdgeLabel(LocalFlowchartLayoutEdge edge)
        {
            var left = edge.LabelX - edge.LabelWidth / 2;
            var top = edge.LabelY - edge.LabelHeight / 2;
            FillRoundedRectangle(left, top, edge.LabelWidth, edge.LabelHeight, 6, EdgeLabelBackground);
            var y = top + (edge.LabelHeight - edge.LabelLines.Count * 20) / 2;
            foreach (var line in edge.LabelLines)
            {
                var width = line.Length * 16;
                DrawText(line, edge.LabelX - width / 2, y, EdgeLabelInk);
                y += 20;
            }
        }

        private void DrawText(string text, int x, int y, byte[]? color = null)
        {
            color ??= Ink;
            foreach (var character in text)
            {
                var renderableCharacter = character is >= ' ' and <= '~' ? character : '?';
                var glyphIndex = (renderableCharacter - 32) * 8;
                for (var row = 0; row < 8; row++)
                {
                    var bits = Font[glyphIndex + row];
                    for (var column = 0; column < 8; column++)
                    {
                        if ((bits & (1 << column)) == 0) continue;
                        FillRectangle(x + column * 2, y + row * 2, 2, 2, color);
                    }
                }
                x += 16;
            }
        }

        private void DrawArrow(LocalFlowchartLayoutPoint before, LocalFlowchartLayoutPoint tip)
        {
            if (tip.Y > before.Y) DrawDownArrow(tip.X, tip.Y);
            else if (tip.Y < before.Y) DrawUpArrow(tip.X, tip.Y);
            else if (tip.X > before.X) DrawRightArrow(tip.X, tip.Y);
            else DrawLeftArrow(tip.X, tip.Y);
        }

        private void DrawUpArrow(int x, int y)
        {
            FillPolygon([(x, y), (x - 7, y + 9), (x + 7, y + 9)], ArrowInk);
            SetPixel(x, y, ArrowInk);
        }

        private void DrawLeftArrow(int x, int y)
        {
            FillPolygon([(x, y), (x + 9, y - 7), (x + 9, y + 7)], ArrowInk);
            SetPixel(x, y, ArrowInk);
        }

        private void DrawDownArrow(int x, int y)
        {
            FillPolygon([(x, y), (x - 7, y - 9), (x + 7, y - 9)], ArrowInk);
            SetPixel(x, y, ArrowInk);
        }

        private void DrawRightArrow(int x, int y)
        {
            FillPolygon([(x, y), (x - 9, y - 7), (x - 9, y + 7)], ArrowInk);
            SetPixel(x, y, ArrowInk);
        }

        private void FillRoundedRectangle(int x, int y, int width, int height, int radius, byte[] color)
        {
            var minX = Math.Max(OriginX, x);
            var maxX = Math.Min(OriginX + Width, x + width);
            var minY = Math.Max(OriginY, y);
            var maxY = Math.Min(OriginY + Height, y + height);
            var squaredRadius = radius * radius;
            for (var py = minY; py < maxY; py++)
            for (var px = minX; px < maxX; px++)
            {
                var cx = Math.Clamp(px, x + radius, x + width - radius - 1);
                var cy = Math.Clamp(py, y + radius, y + height - radius - 1);
                var dx = px - cx;
                var dy = py - cy;
                if (dx * dx + dy * dy <= squaredRadius) SetPixel(px, py, color);
            }
        }

        private void FillRectangle(int x, int y, int width, int height, byte[] color)
        {
            var startX = Math.Max(OriginX, x);
            var endX = Math.Min(OriginX + Width, x + width);
            var startY = Math.Max(OriginY, y);
            var endY = Math.Min(OriginY + Height, y + height);
            for (var row = startY; row < endY; row++)
                for (var column = startX; column < endX; column++) SetPixel(column, row, color);
        }

        private void FillEllipse(int x, int y, int width, int height, byte[] color)
        {
            var rx = width / 2.0;
            var ry = height / 2.0;
            var cx = x + rx;
            var cy = y + ry;
            for (var py = Math.Max(OriginY, y); py < Math.Min(OriginY + Height, y + height); py++)
                for (var px = Math.Max(OriginX, x); px < Math.Min(OriginX + Width, x + width); px++)
                {
                    var dx = (px + 0.5 - cx) / rx;
                    var dy = (py + 0.5 - cy) / ry;
                    if (dx * dx + dy * dy <= 1) SetPixel(px, py, color);
                }
        }

        private void DrawCloud(int x, int y, int width, int height, byte[] color, bool outline)
        {
            var left = x + width / 12;
            var right = x + width - width / 12;
            var top = y + height / 8;
            var bottom = y + height - height / 8;
            for (var py = Math.Max(OriginY, top); py <= Math.Min(OriginY + Height - 1, bottom); py++)
            for (var px = Math.Max(OriginX, left); px <= Math.Min(OriginX + Width - 1, right); px++)
            {
                if (!InsideCloud(px, py, x, y, width, height)) continue;
                if (outline && InsideCloud(px - 1, py, x, y, width, height)
                    && InsideCloud(px + 1, py, x, y, width, height)
                    && InsideCloud(px, py - 1, x, y, width, height)
                    && InsideCloud(px, py + 1, x, y, width, height)) continue;
                SetPixel(px, py, color);
            }
        }

        private static bool InsideCloud(int px, int py, int x, int y, int width, int height)
        {
            static bool InEllipse(double px, double py, double cx, double cy, double rx, double ry)
            {
                var dx = (px - cx) / rx;
                var dy = (py - cy) / ry;
                return dx * dx + dy * dy <= 1;
            }

            var centerY = y + height * 0.58;
            return InEllipse(px, py, x + width * 0.29, centerY, width * 0.22, height * 0.31)
                || InEllipse(px, py, x + width * 0.49, y + height * 0.39, width * 0.27, height * 0.39)
                || InEllipse(px, py, x + width * 0.72, centerY, width * 0.22, height * 0.31)
                || InEllipse(px, py, x + width * 0.5, y + height * 0.65, width * 0.42, height * 0.27);
        }

        private void DrawEllipse(int x, int y, int width, int height, byte[] color)
        {
            var rx = width / 2.0;
            var ry = height / 2.0;
            var cx = x + rx;
            var cy = y + ry;
            for (var py = Math.Max(OriginY, y); py <= Math.Min(OriginY + Height - 1, y + height); py++)
                for (var px = Math.Max(OriginX, x); px <= Math.Min(OriginX + Width - 1, x + width); px++)
                {
                    var dx = (px - cx) / rx;
                    var dy = (py - cy) / ry;
                    var distance = dx * dx + dy * dy;
                    if (distance is >= 0.94 and <= 1.08) SetPixel(px, py, color);
                }
        }

        private void DrawRectangle(int x, int y, int width, int height, byte[] color)
        {
            DrawLine(x, y, x + width, y, color, 2);
            DrawLine(x + width, y, x + width, y + height, color, 2);
            DrawLine(x + width, y + height, x, y + height, color, 2);
            DrawLine(x, y + height, x, y, color, 2);
        }

        private void FillPolygon((int X, int Y)[] points, byte[] color)
        {
            var minY = Math.Max(OriginY, points.Min(point => point.Y));
            var maxY = Math.Min(OriginY + Height - 1, points.Max(point => point.Y));
            for (var y = minY; y <= maxY; y++)
            {
                var intersections = new List<double>();
                for (var index = 0; index < points.Length; index++)
                {
                    var first = points[index];
                    var second = points[(index + 1) % points.Length];
                    if ((first.Y > y) == (second.Y > y)) continue;
                    intersections.Add(first.X + (double)(y - first.Y) * (second.X - first.X) / (second.Y - first.Y));
                }
                intersections.Sort();
                for (var index = 0; index + 1 < intersections.Count; index += 2)
                {
                    var left = Math.Max(OriginX, (int)Math.Ceiling(intersections[index]));
                    var right = Math.Min(OriginX + Width - 1, (int)Math.Floor(intersections[index + 1]));
                    for (var x = left; x <= right; x++) SetPixel(x, y, color);
                }
            }
        }

        private void DrawPolygon((int X, int Y)[] points, byte[] color)
        {
            for (var index = 0; index < points.Length; index++)
            {
                var first = points[index];
                var second = points[(index + 1) % points.Length];
                DrawLine(first.X, first.Y, second.X, second.Y, color, 2);
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, byte[] color, int thickness = 2)
        {
            if (y0 == y1)
            {
                if (y0 < OriginY - thickness || y0 >= OriginY + Height + thickness
                    || Math.Max(x0, x1) < OriginX - thickness || Math.Min(x0, x1) >= OriginX + Width + thickness) return;
                x0 = Math.Clamp(x0, OriginX - thickness, OriginX + Width + thickness);
                x1 = Math.Clamp(x1, OriginX - thickness, OriginX + Width + thickness);
            }
            else if (x0 == x1)
            {
                if (x0 < OriginX - thickness || x0 >= OriginX + Width + thickness
                    || Math.Max(y0, y1) < OriginY - thickness || Math.Min(y0, y1) >= OriginY + Height + thickness) return;
                y0 = Math.Clamp(y0, OriginY - thickness, OriginY + Height + thickness);
                y1 = Math.Clamp(y1, OriginY - thickness, OriginY + Height + thickness);
            }
            var dx = Math.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Math.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                FillRectangle(x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
                if (x0 == x1 && y0 == y1) break;
                var twiceError = 2 * error;
                if (twiceError >= dy) { error += dy; x0 += sx; }
                if (twiceError <= dx) { error += dx; y0 += sy; }
            }
        }

        private void SetPixel(int x, int y, byte[] color)
        {
            x -= OriginX;
            y -= OriginY;
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
            var index = (y * Width + x) * 3;
            Pixels[index] = color[0];
            Pixels[index + 1] = color[1];
            Pixels[index + 2] = color[2];
        }
    }
}
