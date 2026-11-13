namespace BrownPants.Cli.Analysis;

internal sealed class LocalFlowchartSpatialIndex
{
    private const int CellSize = 1024;
    private const int MaximumCellsPerItem = 64;
    private const int Padding = 64;

    private readonly Dictionary<long, List<int>> _nodeBuckets = [];
    private readonly Dictionary<long, List<int>> _edgeBuckets = [];
    private readonly List<(int Index, Bounds Bounds)> _largeNodes = [];
    private readonly List<(int Index, Bounds Bounds)> _largeEdges = [];
    private readonly Bounds[] _nodeBounds;
    private readonly Bounds[] _edgeBounds;

    internal LocalFlowchartSpatialIndex(LocalFlowchartLayout layout)
    {
        _nodeBounds = new Bounds[layout.Nodes.Count];
        for (var index = 0; index < layout.Nodes.Count; index++)
        {
            var node = layout.Nodes[index];
            var bounds = new Bounds(node.X, node.Y, node.X + node.Width, node.Y + node.Height).Expand(Padding);
            _nodeBounds[index] = bounds;
            Add(_nodeBuckets, _largeNodes, index, bounds);
        }

        _edgeBounds = new Bounds[layout.Edges.Count];
        for (var index = 0; index < layout.Edges.Count; index++)
        {
            var edge = layout.Edges[index];
            var minX = edge.Route.Min(point => point.X);
            var minY = edge.Route.Min(point => point.Y);
            var maxX = edge.Route.Max(point => point.X);
            var maxY = edge.Route.Max(point => point.Y);
            if (edge.Edge.Label is not null)
            {
                minX = Math.Min(minX, edge.LabelX - edge.LabelWidth / 2);
                maxX = Math.Max(maxX, edge.LabelX + (edge.LabelWidth + 1) / 2);
                minY = Math.Min(minY, edge.LabelY - edge.LabelHeight / 2);
                maxY = Math.Max(maxY, edge.LabelY + (edge.LabelHeight + 1) / 2);
            }
            var bounds = new Bounds(minX, minY, maxX, maxY).Expand(Padding);
            _edgeBounds[index] = bounds;
            Add(_edgeBuckets, _largeEdges, index, bounds);
        }
    }

    internal int[] NodesIntersecting(int x, int y, int width, int height) =>
        Query(_nodeBuckets, _largeNodes, _nodeBounds, x, y, width, height);

    internal int[] EdgesIntersecting(int x, int y, int width, int height) =>
        Query(_edgeBuckets, _largeEdges, _edgeBounds, x, y, width, height);

    private static int[] Query(Dictionary<long, List<int>> buckets,
        IReadOnlyList<(int Index, Bounds Bounds)> largeItems, IReadOnlyList<Bounds> bounds,
        int x, int y, int width, int height)
    {
        var region = new Bounds(x, y, x + width, y + height);
        var candidates = new HashSet<int>();
        var left = FloorCell(region.Left);
        var right = FloorCell(region.Right);
        var top = FloorCell(region.Top);
        var bottom = FloorCell(region.Bottom);
        for (var cellY = top; cellY <= bottom; cellY++)
        for (var cellX = left; cellX <= right; cellX++)
            if (buckets.TryGetValue(Key(cellX, cellY), out var indices))
                foreach (var index in indices) candidates.Add(index);

        foreach (var item in largeItems)
            if (item.Bounds.Intersects(region)) candidates.Add(item.Index);

        return candidates.Where(index => bounds[index].Intersects(region)).Order().ToArray();
    }

    private static void Add(Dictionary<long, List<int>> buckets,
        ICollection<(int Index, Bounds Bounds)> largeItems, int index, Bounds bounds)
    {
        var left = FloorCell(bounds.Left);
        var right = FloorCell(bounds.Right);
        var top = FloorCell(bounds.Top);
        var bottom = FloorCell(bounds.Bottom);
        var cells = ((long)right - left + 1) * ((long)bottom - top + 1);
        if (cells > MaximumCellsPerItem)
        {
            largeItems.Add((index, bounds));
            return;
        }

        for (var cellY = top; cellY <= bottom; cellY++)
        for (var cellX = left; cellX <= right; cellX++)
        {
            var key = Key(cellX, cellY);
            if (!buckets.TryGetValue(key, out var indices)) buckets.Add(key, indices = []);
            indices.Add(index);
        }
    }

    private static int FloorCell(int coordinate)
    {
        var quotient = coordinate / CellSize;
        return coordinate % CellSize < 0 ? quotient - 1 : quotient;
    }

    private static long Key(int x, int y) => ((long)x << 32) | (uint)y;

    private readonly record struct Bounds(int Left, int Top, int Right, int Bottom)
    {
        internal Bounds Expand(int pixels) => new(Left - pixels, Top - pixels, Right + pixels, Bottom + pixels);
        internal bool Intersects(Bounds other) => Left <= other.Right && Right >= other.Left
            && Top <= other.Bottom && Bottom >= other.Top;
    }
}
