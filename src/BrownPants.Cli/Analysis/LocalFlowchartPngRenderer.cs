using System.IO.Compression;
using System.Text;

namespace BrownPants.Cli.Analysis;

internal static class LocalFlowchartPngRenderer
{
    // U+0020 through U+007E are from https://github.com/dhepper/font8x8/blob/master/font8x8_basic.h,
    // based on IBM's public-domain VGA font; the font8x8 header identifies the glyph data as public domain.
    private static readonly byte[] Font = Convert.FromBase64String(
        "AAAAAAAAAAAYPDwYGAAYADY2AAAAAAAANjZ/Nn82NgAMPgMeMB8MAABjMxgMZmMAHDYcbjszbgAGBgMAAAAAABgMBgYGDBgABgwYGBgMBgAAZjz/PGYAAAAMDD8MDAAAAAAAAAAMDAYAAAA/AAAAAAAAAAAADAwAYDAYDAYDAQA+Y3N7b2c+AAwODAwMDD8AHjMwHAYzPwAeMzAcMDMeADg8NjN/MHgAPwMfMDAzHgAcBgMfMzMeAD8zMBgMDAwAHjMzHjMzHgAeMzM+MBgOAAAMDAAADAwAAAwMAAAMDAYYDAYDBgwYAAAAPwAAPwAABgwYMBgMBgAeMzAYDAAMAD5je3t7Ax4ADB4zMz8zMwA/ZmY+ZmY/ADxmAwMDZjwAHzZmZmY2HwB/RhYeFkZ/AH9GFh4WBg8APGYDA3NmfAAzMzM/MzMzAB4MDAwMDB4AeDAwMDMzHgBnZjYeNmZnAA8GBgZGZn8AY3d/f2tjYwBjZ297c2NjABw2Y2NjNhwAP2ZmPgYGDwAeMzMzOx44AD9mZj42ZmcAHjMHDjgzHgA/LQwMDAweADMzMzMzMz8AMzMzMzMeDABjY2Nrf3djAGNjNhwcNmMAMzMzHgwMHgB/YzEYTGZ/AB4GBgYGBh4AAwYMGDBgQAAeGBgYGBgeAAgcNmMAAAAAAAAAAAAAAP8MDBgAAAAAAAAAHjA+M24ABwYGPmZmOwAAAB4zAzMeADgwMD4zM24AAAAeMz8DHgAcNgYPBgYPAAAAbjMzPjAfBwY2bmZmZwAMAA4MDAweADAAMDAwMzMeBwZmNh42ZwAODAwMDAweAAAAM39/a2MAAAAfMzMzMwAAAB4zMzMeAAAAO2ZmPgYPAABuMzM+MHgAADtuZgYPAAAAPgMeMB8ACAw+DAwsGAAAADMzMzNuAAAAMzMzHgwAAABja39/NgAAAGM2HDZjAAAAMzMzPjAfAAA/GQwmPwA4DAwHDAw4ABgYGAAYGBgABwwMOAwMBwBuOwAAAAAAAA==");
    private static readonly uint[] CrcTable = CreateCrcTable();

    internal static byte[] Render(LocalFlowchartDocument document)
    {
        var layout = LocalFlowchartLayoutEngine.Create(document);
        var raster = new Raster(layout.Width, layout.Height);
        raster.DrawEdges(layout);
        raster.DrawNodes(layout);
        return EncodePng(raster);
    }

    private static byte[] EncodePng(Raster raster)
    {
        var rowLength = checked(raster.Width * 3 + 1);
        var raw = new byte[checked(rowLength * raster.Height)];
        for (var y = 0; y < raster.Height; y++)
        {
            var target = y * rowLength;
            raw[target] = 0;
            Buffer.BlockCopy(raster.Pixels, y * raster.Width * 3, raw, target + 1, raster.Width * 3);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.NoCompression, leaveOpen: true))
            zlib.Write(raw);

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
        private static readonly byte[] Background = [255, 255, 255];
        private static readonly byte[] Ink = [36, 52, 71];
        private static readonly byte[] ProcessFill = [232, 241, 250];
        private static readonly byte[] DecisionFill = [255, 245, 214];
        private static readonly byte[] TerminalFill = [224, 245, 235];

        internal Raster(int width, int height)
        {
            Width = width;
            Height = height;
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
        internal byte[] Pixels { get; }

        internal void DrawEdges(LocalFlowchartLayout layout)
        {
            foreach (var routed in layout.Edges)
            {
                var edge = routed.Edge;
                var source = layout.Node(edge.FromId);
                var target = layout.Node(edge.ToId);
                if (routed.ReturnLane < 0)
                {
                    var sourceX = source.X + source.Width / 2;
                    var sourceY = source.Y + source.Height;
                    var targetX = target.X + target.Width / 2;
                    var targetY = target.Y;
                    var middleY = sourceY + Math.Max(12, (targetY - sourceY) / 2);
                    DrawLine(sourceX, sourceY, sourceX, middleY, Ink);
                    DrawLine(sourceX, middleY, targetX, middleY, Ink);
                    DrawLine(targetX, middleY, targetX, targetY, Ink);
                    DrawDownArrow(targetX, targetY);
                    if (edge.Label is not null)
                        DrawTextCentered(edge.Label, (sourceX + targetX) / 2, middleY - 12);
                }
                else
                {
                    var laneX = 8 + routed.ReturnLane * 8;
                    var sourceX = source.X;
                    var sourceY = source.Y + source.Height / 2;
                    var targetX = target.X;
                    var targetY = target.Y + target.Height / 2;
                    DrawLine(sourceX, sourceY, laneX, sourceY, Ink);
                    DrawLine(laneX, sourceY, laneX, targetY, Ink);
                    DrawLine(laneX, targetY, targetX, targetY, Ink);
                    DrawRightArrow(targetX, targetY);
                    if (edge.Label is not null)
                        DrawTextCentered(edge.Label, (laneX + targetX) / 2, (sourceY + targetY) / 2 - 10);
                }
            }
        }

        internal void DrawNodes(LocalFlowchartLayout layout)
        {
            foreach (var node in layout.Nodes.OrderBy(item => item.Node.Order))
            {
                var fill = node.Node.Shape switch
                {
                    LocalFlowchartNodeShape.Start or LocalFlowchartNodeShape.End => TerminalFill,
                    LocalFlowchartNodeShape.Decision => DecisionFill,
                    _ => ProcessFill
                };
                switch (node.Node.Shape)
                {
                    case LocalFlowchartNodeShape.Start:
                    case LocalFlowchartNodeShape.End:
                        FillEllipse(node.X, node.Y, node.Width, node.Height, fill);
                        DrawEllipse(node.X, node.Y, node.Width, node.Height, Ink);
                        break;
                    case LocalFlowchartNodeShape.Decision:
                        FillPolygon([(node.X + node.Width / 2, node.Y), (node.X + node.Width, node.Y + node.Height / 2),
                            (node.X + node.Width / 2, node.Y + node.Height), (node.X, node.Y + node.Height / 2)], fill);
                        DrawPolygon([(node.X + node.Width / 2, node.Y), (node.X + node.Width, node.Y + node.Height / 2),
                            (node.X + node.Width / 2, node.Y + node.Height), (node.X, node.Y + node.Height / 2)], Ink);
                        break;
                    default:
                        FillRectangle(node.X, node.Y, node.Width, node.Height, fill);
                        DrawRectangle(node.X, node.Y, node.Width, node.Height, Ink);
                        break;
                }

                var textHeight = node.LabelLines.Count * 20;
                var textY = node.Y + (node.Height - textHeight) / 2;
                foreach (var line in node.LabelLines)
                {
                    var textWidth = line.Length * 16;
                    DrawText(line, node.X + (node.Width - textWidth) / 2, textY);
                    textY += 20;
                }
            }
        }

        private void DrawTextCentered(string text, int centerX, int top)
        {
            var wrapped = WrapText(text);
            var y = top - wrapped.Count * 20 / 2;
            foreach (var line in wrapped)
            {
                var width = line.Length * 16;
                FillRectangle(centerX - width / 2 - 3, y - 2, width + 6, 20, Background);
                DrawText(line, centerX - width / 2, y);
                y += 20;
            }
        }

        private static IReadOnlyList<string> WrapText(string text)
        {
            var lines = new List<string>();
            for (var start = 0; start < text.Length; start += 24)
                lines.Add(text.Substring(start, Math.Min(24, text.Length - start)));
            return lines;
        }

        private void DrawText(string text, int x, int y)
        {
            foreach (var character in text)
            {
                var glyphIndex = (character - 32) * 8;
                for (var row = 0; row < 8; row++)
                {
                    var bits = Font[glyphIndex + row];
                    for (var column = 0; column < 8; column++)
                    {
                        if ((bits & (1 << column)) == 0) continue;
                        FillRectangle(x + column * 2, y + row * 2, 2, 2, Ink);
                    }
                }
                x += 16;
            }
        }

        private void DrawDownArrow(int x, int y)
        {
            FillPolygon([(x, y), (x - 7, y - 9), (x + 7, y - 9)], Ink);
        }

        private void DrawRightArrow(int x, int y)
        {
            FillPolygon([(x, y), (x - 9, y - 7), (x - 9, y + 7)], Ink);
        }

        private void FillRectangle(int x, int y, int width, int height, byte[] color)
        {
            var startX = Math.Max(0, x);
            var endX = Math.Min(Width, x + width);
            var startY = Math.Max(0, y);
            var endY = Math.Min(Height, y + height);
            for (var row = startY; row < endY; row++)
                for (var column = startX; column < endX; column++) SetPixel(column, row, color);
        }

        private void FillEllipse(int x, int y, int width, int height, byte[] color)
        {
            var rx = width / 2.0;
            var ry = height / 2.0;
            var cx = x + rx;
            var cy = y + ry;
            for (var py = y; py < y + height; py++)
                for (var px = x; px < x + width; px++)
                {
                    var dx = (px + 0.5 - cx) / rx;
                    var dy = (py + 0.5 - cy) / ry;
                    if (dx * dx + dy * dy <= 1) SetPixel(px, py, color);
                }
        }

        private void DrawEllipse(int x, int y, int width, int height, byte[] color)
        {
            var rx = width / 2.0;
            var ry = height / 2.0;
            var cx = x + rx;
            var cy = y + ry;
            for (var py = y; py <= y + height; py++)
                for (var px = x; px <= x + width; px++)
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
            var minY = Math.Max(0, points.Min(point => point.Y));
            var maxY = Math.Min(Height - 1, points.Max(point => point.Y));
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
                    var left = Math.Max(0, (int)Math.Ceiling(intersections[index]));
                    var right = Math.Min(Width - 1, (int)Math.Floor(intersections[index + 1]));
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
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
            var index = (y * Width + x) * 3;
            Pixels[index] = color[0];
            Pixels[index + 1] = color[1];
            Pixels[index + 2] = color[2];
        }
    }
}
