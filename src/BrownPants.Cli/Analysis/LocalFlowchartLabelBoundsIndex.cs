namespace BrownPants.Cli.Analysis;

internal sealed class LocalFlowchartLabelBoundsIndex
{
    private const int CellSize = 512;
    private const int MaximumCellsPerLabel = 64;
    private readonly Dictionary<long, List<(int Id, Bounds Bounds)>> _buckets = [];
    private readonly List<(int Id, Bounds Bounds)> _largeLabels = [];
    private int _nextId;

    internal bool Intersects(int left, int top, int right, int bottom)
    {
        var query = new Bounds(left, top, right, bottom);
        var candidates = new HashSet<int>();
        QueryCells(query, (x, y) =>
        {
            if (_buckets.TryGetValue(Key(x, y), out var entries))
                foreach (var entry in entries)
                    if (entry.Bounds.Intersects(query)) candidates.Add(entry.Id);
        });
        foreach (var entry in _largeLabels)
            if (entry.Bounds.Intersects(query)) candidates.Add(entry.Id);
        return candidates.Count > 0;
    }

    internal void Add(int left, int top, int right, int bottom)
    {
        var bounds = new Bounds(left, top, right, bottom);
        var id = _nextId++;
        var cells = CellCount(bounds);
        if (cells > MaximumCellsPerLabel)
        {
            _largeLabels.Add((id, bounds));
            return;
        }
        QueryCells(bounds, (x, y) =>
        {
            var key = Key(x, y);
            if (!_buckets.TryGetValue(key, out var entries)) _buckets.Add(key, entries = []);
            entries.Add((id, bounds));
        });
    }

    private static long CellCount(Bounds bounds) =>
        ((long)FloorCell(bounds.Right) - FloorCell(bounds.Left) + 1)
        * ((long)FloorCell(bounds.Bottom) - FloorCell(bounds.Top) + 1);

    private static void QueryCells(Bounds bounds, Action<int, int> action)
    {
        for (var y = FloorCell(bounds.Top); y <= FloorCell(bounds.Bottom); y++)
        for (var x = FloorCell(bounds.Left); x <= FloorCell(bounds.Right); x++)
            action(x, y);
    }

    private static int FloorCell(int coordinate)
    {
        var quotient = coordinate / CellSize;
        return coordinate % CellSize < 0 ? quotient - 1 : quotient;
    }

    private static long Key(int x, int y) => ((long)x << 32) | (uint)y;

    private readonly record struct Bounds(int Left, int Top, int Right, int Bottom)
    {
        internal bool Intersects(Bounds other) => Left < other.Right && Right > other.Left
            && Top < other.Bottom && Bottom > other.Top;
    }
}
