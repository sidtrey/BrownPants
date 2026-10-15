namespace BrownPants.Cli.Analysis;

internal sealed record LocalFlowchartLayoutNode(LocalFlowchartNode Node, int X, int Y, int Width, int Height,
    IReadOnlyList<string> LabelLines, int Rank);

internal sealed record LocalFlowchartLayoutEdge(LocalFlowchartEdge Edge, int ReturnLane);

internal sealed record LocalFlowchartLayout(int Width, int Height, int NodeRegionLeft,
    IReadOnlyList<LocalFlowchartLayoutNode> Nodes, IReadOnlyList<LocalFlowchartLayoutEdge> Edges)
{
    internal LocalFlowchartLayoutNode Node(string id) => Nodes.First(node => node.Node.Id == id);
}

internal static class LocalFlowchartLayoutEngine
{
    private const int CharacterScale = 2;
    private const int CharacterAdvance = 8 * CharacterScale;
    private const int MaximumLineCharacters = 24;
    private const int HorizontalNodeGap = 48;
    private const int VerticalRowGap = 88;
    private const int Margin = 24;

    internal static LocalFlowchartLayout Create(LocalFlowchartDocument document)
    {
        var ranks = AssignRanks(document);
        var backEdgeCount = document.Edges.Count(edge => ranks[edge.ToId] <= ranks[edge.FromId]);
        var nodeRegionLeft = Math.Max(48, Margin + backEdgeCount * 8);
        var wrapped = document.Nodes.ToDictionary(node => node.Id, node => Wrap(node.Label), StringComparer.Ordinal);
        var sizes = document.Nodes.ToDictionary(node => node.Id, node =>
        {
            var lines = wrapped[node.Id];
            var textWidth = lines.Max(line => line.Length) * CharacterAdvance;
            return (Width: Math.Max(144, textWidth + 40), Height: 36 + lines.Count * (8 * CharacterScale + 4));
        }, StringComparer.Ordinal);
        var rows = document.Nodes.GroupBy(node => ranks[node.Id]).OrderBy(group => group.Key).ToArray();
        var rowWidths = rows.ToDictionary(row => row.Key,
            row => row.Sum(node => sizes[node.Id].Width) + Math.Max(0, row.Count() - 1) * HorizontalNodeGap);
        var maximumRowWidth = rowWidths.Values.Max();
        var longestEdgeLabel = document.Edges.Where(edge => edge.Label is not null)
            .Select(edge => edge.Label!.Length * CharacterAdvance + 32).DefaultIfEmpty(0).Max();
        var width = nodeRegionLeft + Math.Max(maximumRowWidth, longestEdgeLabel) + Margin;

        var rowTops = new Dictionary<int, int>();
        var y = Margin;
        foreach (var row in rows)
        {
            rowTops[row.Key] = y;
            y += row.Max(node => sizes[node.Id].Height) + VerticalRowGap;
        }
        var height = Math.Max(200, y - VerticalRowGap + Margin);

        var laidOutNodes = new List<LocalFlowchartLayoutNode>(document.Nodes.Count);
        foreach (var row in rows)
        {
            var rowWidth = rowWidths[row.Key];
            var x = nodeRegionLeft + (maximumRowWidth - rowWidth) / 2;
            var top = rowTops[row.Key];
            var rowHeight = row.Max(node => sizes[node.Id].Height);
            foreach (var node in row.OrderBy(node => node.Order))
            {
                var size = sizes[node.Id];
                var nodeY = top + (rowHeight - size.Height) / 2;
                laidOutNodes.Add(new LocalFlowchartLayoutNode(node, x, nodeY, size.Width, size.Height,
                    wrapped[node.Id], row.Key));
                x += size.Width + HorizontalNodeGap;
            }
        }

        var lanes = 0;
        var laidOutEdges = document.Edges.Select(edge =>
        {
            var lane = ranks[edge.ToId] <= ranks[edge.FromId] ? lanes++ : -1;
            return new LocalFlowchartLayoutEdge(edge, lane);
        }).ToArray();

        if (width > 4096 || height > 4096 || (long)width * height > 16_000_000)
            throw new LocalFlowchartFormatException(
                $"{document.SourcePath}: the calculated flowchart canvas ({width} by {height} pixels) exceeds the 4096-pixel dimension or 16-million-pixel area limit.");

        return new LocalFlowchartLayout(width, height, nodeRegionLeft, laidOutNodes, laidOutEdges);
    }

    private static Dictionary<string, int> AssignRanks(LocalFlowchartDocument document)
    {
        var outgoing = document.Nodes.ToDictionary(node => node.Id,
            node => document.Edges.Where(edge => edge.FromId == node.Id).OrderBy(edge => edge.Order).Select(edge => edge.ToId).ToArray(),
            StringComparer.Ordinal);
        var indegree = document.Nodes.ToDictionary(node => node.Id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in document.Edges) indegree[edge.ToId]++;

        var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        foreach (var node in document.Nodes.Where(node => indegree[node.Id] == 0))
        {
            ranks[node.Id] = 0;
            queue.Enqueue(node.Id);
        }

        void DrainQueue()
        {
            while (queue.Count > 0)
            {
                var source = queue.Dequeue();
                foreach (var target in outgoing[source])
                {
                    if (ranks.ContainsKey(target)) continue;
                    ranks[target] = ranks[source] + 1;
                    queue.Enqueue(target);
                }
            }
        }

        DrainQueue();
        foreach (var node in document.Nodes)
        {
            if (ranks.ContainsKey(node.Id)) continue;
            var seedRank = ranks.Count == 0 ? 0 : ranks.Values.Max() + 1;
            ranks[node.Id] = seedRank;
            queue.Enqueue(node.Id);
            DrainQueue();
        }

        return ranks;
    }

    private static IReadOnlyList<string> Wrap(string text)
    {
        var result = new List<string>();
        for (var start = 0; start < text.Length; start += MaximumLineCharacters)
            result.Add(text.Substring(start, Math.Min(MaximumLineCharacters, text.Length - start)));
        return result;
    }
}
