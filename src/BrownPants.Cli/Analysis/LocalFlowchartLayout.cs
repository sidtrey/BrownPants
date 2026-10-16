using System.Globalization;
using System.Text;

namespace BrownPants.Cli.Analysis;

internal enum LocalFlowchartDirection
{
    TopDown,
    LeftToRight
}

internal sealed record LocalFlowchartLayoutPoint(int X, int Y);

internal sealed record LocalFlowchartLayoutOptions(LocalFlowchartDirection Direction = LocalFlowchartDirection.TopDown);

internal sealed record LocalFlowchartLayoutNode(LocalFlowchartNode Node, int X, int Y, int Width, int Height,
    IReadOnlyList<string> LabelLines, int Rank);

internal sealed record LocalFlowchartLayoutEdge(LocalFlowchartEdge Edge, IReadOnlyList<LocalFlowchartLayoutPoint> Route,
    int LabelX, int LabelY, int LabelWidth, int LabelHeight, IReadOnlyList<string> LabelLines);

internal sealed record LocalFlowchartLayoutDiagnostic(string Code, string Message);

internal sealed record LocalFlowchartLayout(int Width, int Height, int NodeRegionLeft,
    IReadOnlyList<LocalFlowchartLayoutNode> Nodes, IReadOnlyList<LocalFlowchartLayoutEdge> Edges,
    LocalFlowchartDirection Direction, IReadOnlyList<LocalFlowchartLayoutDiagnostic>? Diagnostics = null)
{
    internal LocalFlowchartLayoutNode Node(string id) => Nodes.First(node => node.Node.Id == id);
}

internal static class LocalFlowchartLayoutEngine
{
    internal static int MaximumLogicalDimension => (int)(int.MaxValue * 9L / 10);
    private const int CharacterAdvance = 16;
    private const int CharacterHeight = 16;
    private const int MaximumLineCharacters = 22;
    private const int HorizontalNodeGap = 56;
    private const int VerticalNodeGap = 52;
    private const int RankGap = 112;
    private const int Margin = 48;
    private const int RouteLaneGap = 24;
    private const int MaximumEdgeLabelLineCharacters = 24;
    internal const int MaximumCrossingAnalysisEdges = 20_000;
    private const long MaximumLocalSkipRoutingComparisons = 2_000_000;
    private const int MaximumAdjacentRouteOptimizationEdges = 2_000;

    internal static LocalFlowchartLayout Create(LocalFlowchartDocument document, LocalFlowchartLayoutOptions? options = null)
    {
        options ??= new LocalFlowchartLayoutOptions();
        var ranks = AssignRanks(document);
        var rows = document.Nodes.GroupBy(node => ranks[node.Id]).OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.OrderBy(node => node.Order).ToList());
        ReduceCrossings(document, ranks, rows);

        var wrapped = document.Nodes.ToDictionary(node => node.Id, node => Wrap(node.Label), StringComparer.Ordinal);
        var importedGeometryCount = document.Nodes.Count(node => node.ImportedGeometry is not null);
        if (importedGeometryCount != 0 && importedGeometryCount != document.Nodes.Count)
            throw new LocalFlowchartFormatException($"{document.SourcePath}: imported geometry must be supplied for every node.");
        var sizes = document.Nodes.ToDictionary(node => node.Id,
            node => node.ImportedGeometry is { } geometry ? (geometry.Width, geometry.Height) : Measure(node, wrapped[node.Id]), StringComparer.Ordinal);
        foreach (var node in document.Nodes)
        {
            var lines = wrapped[node.Id];
            var size = sizes[node.Id];
            if (lines.Max(line => line.Length) * CharacterAdvance > size.Width - 24
                || lines.Count * 20 > size.Height - 16)
                throw new LocalFlowchartFormatException($"{document.SourcePath}:{node.SourceLine}: node label '{node.Label}' cannot fit inside its shape.");
        }
        var edgeLabelSizes = document.Edges.Where(edge => edge.Label is not null)
            .Select(edge => MeasureEdgeLabel(WrapEdgeLabel(edge.Label!))).ToArray();
        var maximumEdgeLabel = (Width: edgeLabelSizes.Select(size => size.Width).DefaultIfEmpty(0).Max(),
            Height: edgeLabelSizes.Select(size => size.Height).DefaultIfEmpty(0).Max());
        var maximumNodeSize = (Width: sizes.Values.Max(size => size.Width), Height: sizes.Values.Max(size => size.Height));
        var backEdges = document.Edges.Where(edge => ranks[edge.ToId] <= ranks[edge.FromId]).OrderBy(edge => edge.Order).ToArray();
        var terminalNodeIds = document.Nodes.Where(node => node.Shape == LocalFlowchartNodeShape.End)
            .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var terminalEdgeOrders = document.Edges.Where(edge => terminalNodeIds.Contains(edge.ToId)
                && edge.Label is "return" or "throw" or "rethrow" or "exception handling" or "terminate")
            .Select(edge => edge.Order).ToHashSet();
        var leftLanes = options.Direction == LocalFlowchartDirection.TopDown
            ? backEdges.Count(edge => edge.FromId != edge.ToId) : 0;
        var topDownBackEdgeCount = options.Direction == LocalFlowchartDirection.TopDown
            ? backEdges.Count(edge => edge.FromId != edge.ToId) : 0;
        var rightLanes = options.Direction == LocalFlowchartDirection.TopDown
            ? document.Edges.Count(edge => edge.FromId == edge.ToId
                || ranks[edge.ToId] > ranks[edge.FromId] + 1 || terminalEdgeOrders.Contains(edge.Order))
                + topDownBackEdgeCount : 0;
        var topLanes = options.Direction == LocalFlowchartDirection.LeftToRight
            ? backEdges.Count(edge => edge.FromId != edge.ToId) : 0;
        var bottomLanes = options.Direction == LocalFlowchartDirection.LeftToRight
            ? document.Edges.Count(edge => edge.FromId == edge.ToId
                || ranks[edge.ToId] > ranks[edge.FromId] + 1 || terminalEdgeOrders.Contains(edge.Order)) : 0;
        var horizontalLabelMargin = Math.Max(Margin, maximumEdgeLabel.Width / 2 + 16);
        var verticalLabelMargin = Math.Max(Margin, maximumEdgeLabel.Height / 2 + 16);
        var nodeRegionLeft = horizontalLabelMargin + leftLanes * RouteLaneGap;

        var positions = importedGeometryCount != 0
            ? PlaceImportedGeometry(document.Nodes, sizes, ranks, wrapped)
            : options.Direction == LocalFlowchartDirection.TopDown
                ? PlaceTopDown(rows, sizes, nodeRegionLeft, Math.Max(HorizontalNodeGap, maximumEdgeLabel.Width + 32),
                    Math.Max(RankGap, maximumEdgeLabel.Height + maximumNodeSize.Height + 32))
                : PlaceLeftToRight(rows, sizes, verticalLabelMargin + topLanes * RouteLaneGap,
                    Math.Max(VerticalNodeGap, maximumEdgeLabel.Height + 32),
                    Math.Max(RankGap, maximumEdgeLabel.Width + maximumNodeSize.Width + 32));

        var nodeRight = positions.Values.Max(node => node.X + node.Width);
        var nodeBottom = positions.Values.Max(node => node.Y + node.Height);
        var labelLanePadding = document.Edges.Where(edge => edge.Label is not null).Select(edge => edge.Label!.Length * CharacterAdvance + 24).DefaultIfEmpty(0).Max();
        var importedWaypoints = document.Edges.SelectMany(edge => edge.ImportedWaypoints ?? Array.Empty<LocalFlowchartImportedWaypoint>()).ToArray();
        var width = Math.Max(nodeRight + horizontalLabelMargin + rightLanes * RouteLaneGap + (rightLanes > 0 ? labelLanePadding : 0),
            importedWaypoints.Select(point => point.X + Margin).DefaultIfEmpty(0).Max());
        var height = Math.Max(nodeBottom + verticalLabelMargin + bottomLanes * RouteLaneGap,
            importedWaypoints.Select(point => point.Y + Margin).DefaultIfEmpty(0).Max());
        if (width > MaximumLogicalDimension || height > MaximumLogicalDimension)
            throw new LocalFlowchartFormatException(
                $"{document.SourcePath}: calculated canvas dimensions exceed 90% of PNG's 2^31-1 pixel-side limit.");
        var laidOutNodes = document.Nodes.Select(node =>
        {
            var position = positions[node.Id];
            return new LocalFlowchartLayoutNode(node, position.X, position.Y, position.Width, position.Height,
                wrapped[node.Id], ranks[node.Id]);
        }).ToArray();
        var nodesById = laidOutNodes.ToDictionary(node => node.Node.Id, StringComparer.Ordinal);
        var laidOutEdges = RouteEdges(document, ranks, nodesById, options.Direction, width, height,
            leftLanes, rightLanes, topLanes, bottomLanes, terminalEdgeOrders);
        var layout = new LocalFlowchartLayout(width, height, nodeRegionLeft, laidOutNodes, laidOutEdges, options.Direction);
        return layout with { Diagnostics = DiagnoseCrossings(layout) };
    }

    internal static IReadOnlyList<LocalFlowchartLayoutDiagnostic> DiagnoseCrossings(LocalFlowchartLayout layout)
    {
        if (layout.Edges.Count > MaximumCrossingAnalysisEdges)
            return [new("crossing-analysis-limit",
                $"Connector crossing analysis was skipped above {MaximumCrossingAnalysisEdges:N0} edges; graph connectivity and node routing are unchanged.")];

        if (layout.Edges.Count < 2) return Array.Empty<LocalFlowchartLayoutDiagnostic>();
        var index = new LocalFlowchartSpatialIndex(layout);
        var crossings = 0;
        var examples = new List<string>(3);
        for (var firstIndex = 0; firstIndex < layout.Edges.Count; firstIndex++)
        {
            var first = layout.Edges[firstIndex];
            var minX = first.Route.Min(point => point.X);
            var minY = first.Route.Min(point => point.Y);
            var maxX = first.Route.Max(point => point.X);
            var maxY = first.Route.Max(point => point.Y);
            foreach (var secondIndex in index.EdgesIntersecting(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY)))
            {
                if (secondIndex <= firstIndex) continue;
                var second = layout.Edges[secondIndex];
                if (first.Edge.FromId == second.Edge.FromId || first.Edge.FromId == second.Edge.ToId
                    || first.Edge.ToId == second.Edge.FromId || first.Edge.ToId == second.Edge.ToId) continue;
                if (TryGetRouteCrossing(first.Route, second.Route, out var crossingPoint))
                {
                    crossings++;
                    if (examples.Count < 3)
                    {
                        var x = crossingPoint.X.ToString(CultureInfo.InvariantCulture);
                        var y = crossingPoint.Y.ToString(CultureInfo.InvariantCulture);
                        examples.Add($"{first.Edge.FromId}->{first.Edge.ToId} with {second.Edge.FromId}->{second.Edge.ToId} at canvas ({x}, {y})");
                    }
                }
            }
        }
        return crossings == 0 ? Array.Empty<LocalFlowchartLayoutDiagnostic>() :
            [new("connector-crossings", $"{crossings:N0} connector pair(s) still cross after layout optimization ({string.Join("; ", examples)}); all declared connections were preserved.")];
    }

    private static bool RoutesCross(IReadOnlyList<LocalFlowchartLayoutPoint> first, IReadOnlyList<LocalFlowchartLayoutPoint> second)
        => TryGetRouteCrossing(first, second, out _);

    private static bool TryGetRouteCrossing(IReadOnlyList<LocalFlowchartLayoutPoint> first,
        IReadOnlyList<LocalFlowchartLayoutPoint> second, out LocalFlowchartLayoutPoint crossing)
    {
        for (var firstIndex = 0; firstIndex + 1 < first.Count; firstIndex++)
        for (var secondIndex = 0; secondIndex + 1 < second.Count; secondIndex++)
        {
            var a = first[firstIndex];
            var b = first[firstIndex + 1];
            var c = second[secondIndex];
            var d = second[secondIndex + 1];
            if (a.Y == b.Y && c.X == d.X
                && c.X > Math.Min(a.X, b.X) && c.X < Math.Max(a.X, b.X)
                && a.Y > Math.Min(c.Y, d.Y) && a.Y < Math.Max(c.Y, d.Y))
            {
                crossing = new(c.X, a.Y);
                return true;
            }
            if (a.X == b.X && c.Y == d.Y
                && c.Y > Math.Min(a.Y, b.Y) && c.Y < Math.Max(a.Y, b.Y)
                && a.X > Math.Min(c.X, d.X) && a.X < Math.Max(c.X, d.X))
            {
                crossing = new(a.X, c.Y);
                return true;
            }
        }
        crossing = null!;
        return false;
    }

    private static (int Width, int Height) Measure(LocalFlowchartNode node, IReadOnlyList<string> lines)
    {
        var textWidth = lines.Max(line => line.Length) * CharacterAdvance;
        var width = node.Shape switch
        {
            LocalFlowchartNodeShape.Decision => Math.Max(220, textWidth * 2 + 48),
            LocalFlowchartNodeShape.InputOutput => Math.Max(200, textWidth + 72),
            LocalFlowchartNodeShape.Person => Math.Max(224, textWidth + 76),
            LocalFlowchartNodeShape.Cloud => Math.Max(240, textWidth + 96),
            _ => Math.Max(184, textWidth + 56)
        };
        var height = Math.Max(76, 44 + lines.Count * (CharacterHeight + 4));
        if (node.Shape == LocalFlowchartNodeShape.Decision) height = Math.Max(104, height + 18);
        if (node.Shape == LocalFlowchartNodeShape.Cloud) height = Math.Max(108, height + 24);
        return (width, height);
    }

    private static Dictionary<string, LocalFlowchartLayoutNode> PlaceImportedGeometry(
        IReadOnlyList<LocalFlowchartNode> nodes, IReadOnlyDictionary<string, (int Width, int Height)> sizes,
        IReadOnlyDictionary<string, int> ranks, IReadOnlyDictionary<string, IReadOnlyList<string>> wrapped)
    {
        var positions = new Dictionary<string, LocalFlowchartLayoutNode>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            var geometry = node.ImportedGeometry!;
            positions.Add(node.Id, new LocalFlowchartLayoutNode(node, geometry.X, geometry.Y,
                sizes[node.Id].Width, sizes[node.Id].Height, wrapped[node.Id], ranks[node.Id]));
        }
        return positions;
    }

    private static Dictionary<string, LocalFlowchartLayoutNode> PlaceTopDown(
        IReadOnlyDictionary<int, List<LocalFlowchartNode>> rows,
        IReadOnlyDictionary<string, (int Width, int Height)> sizes, int left, int nodeGap, int rankGap)
    {
        var rowWidths = rows.ToDictionary(row => row.Key,
            row => row.Value.Sum(node => sizes[node.Id].Width) + Math.Max(0, row.Value.Count - 1) * nodeGap);
        var maximumWidth = rowWidths.Values.Max();
        var positions = new Dictionary<string, LocalFlowchartLayoutNode>(StringComparer.Ordinal);
        var y = Margin;
        foreach (var row in rows.OrderBy(row => row.Key))
        {
            var rowHeight = row.Value.Max(node => sizes[node.Id].Height);
            var x = left + (maximumWidth - rowWidths[row.Key]) / 2;
            foreach (var node in row.Value)
            {
                var size = sizes[node.Id];
                positions[node.Id] = new LocalFlowchartLayoutNode(node, x, y + (rowHeight - size.Height) / 2,
                    size.Width, size.Height, Array.Empty<string>(), row.Key);
                x += size.Width + nodeGap;
            }
            y += rowHeight + rankGap;
        }
        return positions;
    }

    private static Dictionary<string, LocalFlowchartLayoutNode> PlaceLeftToRight(
        IReadOnlyDictionary<int, List<LocalFlowchartNode>> columns,
        IReadOnlyDictionary<string, (int Width, int Height)> sizes, int top, int nodeGap, int rankGap)
    {
        var columnWidths = columns.ToDictionary(column => column.Key, column => column.Value.Max(node => sizes[node.Id].Width));
        var columnHeights = columns.ToDictionary(column => column.Key,
            column => column.Value.Sum(node => sizes[node.Id].Height) + Math.Max(0, column.Value.Count - 1) * nodeGap);
        var maximumHeight = columnHeights.Values.Max();
        var positions = new Dictionary<string, LocalFlowchartLayoutNode>(StringComparer.Ordinal);
        var x = Margin;
        foreach (var column in columns.OrderBy(column => column.Key))
        {
            var y = top + (maximumHeight - columnHeights[column.Key]) / 2;
            foreach (var node in column.Value)
            {
                var size = sizes[node.Id];
                positions[node.Id] = new LocalFlowchartLayoutNode(node,
                    x + (columnWidths[column.Key] - size.Width) / 2, y, size.Width, size.Height,
                    Array.Empty<string>(), column.Key);
                y += size.Height + nodeGap;
            }
            x += columnWidths[column.Key] + rankGap;
        }
        return positions;
    }

    private static LocalFlowchartLayoutEdge[] RouteEdges(LocalFlowchartDocument document,
        IReadOnlyDictionary<string, int> ranks, IReadOnlyDictionary<string, LocalFlowchartLayoutNode> nodes,
        LocalFlowchartDirection direction, int width, int height, int leftLaneCount, int rightLaneCount,
        int topLaneCount, int bottomLaneCount, IReadOnlySet<int> terminalEdgeOrders)
    {
        var nextRightLane = 0;
        var nextLeftLane = 0;
        var nextTopLane = 0;
        var nextBottomLane = 0;
        var rankTops = nodes.Values.GroupBy(node => node.Rank).ToDictionary(group => group.Key, group => group.Min(node => node.Y));
        var rankBottoms = nodes.Values.GroupBy(node => node.Rank).ToDictionary(group => group.Key, group => group.Max(node => node.Y + node.Height));
        var rankLefts = nodes.Values.GroupBy(node => node.Rank).ToDictionary(group => group.Key, group => group.Min(node => node.X));
        var rankRights = nodes.Values.GroupBy(node => node.Rank).ToDictionary(group => group.Key, group => group.Max(node => node.X + node.Width));
        var allowLocalSkipRouting = (long)nodes.Count * document.Edges.Count <= MaximumLocalSkipRoutingComparisons;
        var results = new List<LocalFlowchartLayoutEdge>(document.Edges.Count);
        foreach (var edge in document.Edges.OrderBy(edge => edge.Order))
        {
            var source = nodes[edge.FromId];
            var target = nodes[edge.ToId];
            var sourceRank = ranks[edge.FromId];
            var targetRank = ranks[edge.ToId];
            var route = new List<LocalFlowchartLayoutPoint>();
            if (direction == LocalFlowchartDirection.TopDown)
            {
                if (edge.FromId == edge.ToId)
                {
                    var lane = width - Margin - rightLaneCount * RouteLaneGap + RouteLaneGap + (nextRightLane++ * RouteLaneGap);
                    route.AddRange([new(source.X + source.Width * 3 / 4, source.Y), new(lane, source.Y - 24),
                        new(lane, source.Y + source.Height + 24), new(source.X + source.Width / 2, source.Y + source.Height + 24),
                        new(source.X + source.Width / 2, source.Y + source.Height)]);
                }
                else if (terminalEdgeOrders.Contains(edge.Order))
                {
                    var lane = width - Margin - rightLaneCount * RouteLaneGap + RouteLaneGap + (nextRightLane++ * RouteLaneGap);
                    var sourceX = source.X + source.Width / 2;
                    var targetX = target.X + target.Width / 2;
                    var sourceChannelY = rankBottoms[sourceRank] + 16;
                    var targetChannelY = Math.Max(0, rankTops[targetRank] - 16);
                    var directRoute = FindDirectTerminalRoute(
                        new(sourceX, source.Y + source.Height), new(targetX, target.Y),
                        nodes.Values, results, source.Node.Id, target.Node.Id, vertical: true);
                    route.AddRange(directRoute ??
                        [new(sourceX, source.Y + source.Height), new(sourceX, sourceChannelY),
                            new(lane, sourceChannelY), new(lane, targetChannelY), new(targetX, targetChannelY), new(targetX, target.Y)]);
                }
                else if (targetRank == sourceRank + 1)
                {
                    var sourceX = source.X + source.Width / 2;
                    var sourceY = source.Y + source.Height;
                    var targetX = target.X + target.Width / 2;
                    var targetY = target.Y;
                    var middleY = sourceY + Math.Max(12, (targetY - sourceY) / 2);
                    LocalFlowchartLayoutPoint[] directRoute =
                    [new(sourceX, sourceY), new(sourceX, middleY), new(targetX, middleY), new(targetX, targetY)];
                    if (document.Edges.Count <= MaximumAdjacentRouteOptimizationEdges)
                    {
                        var alternatives = AdjacentSideRoutes(source, target, middleY, width, leftLaneCount, rightLaneCount);
                        var candidates = new List<IReadOnlyList<LocalFlowchartLayoutPoint>>(alternatives.Count + 1) { directRoute };
                        candidates.AddRange(alternatives);
                        route.AddRange(HasCrossingWithRoutedEdges(directRoute, results, source.Node.Id, target.Node.Id)
                            ? ChooseRoute(candidates.ToArray(), nodes.Values, results, source.Node.Id, target.Node.Id, directRoute)
                            : directRoute);
                    }
                    else
                    {
                        route.AddRange(directRoute);
                    }
                }
                else if (targetRank <= sourceRank)
                {
                    var sourceX = source.X + source.Width / 2;
                    var targetX = target.X + target.Width / 2;
                    var sourceChannelY = Math.Max(0, rankTops[sourceRank] - 16);
                    var targetChannelY = Math.Max(0, rankTops[targetRank] - 16);
                    var leftLane = Margin + (nextLeftLane++ * RouteLaneGap);
                    LocalFlowchartLayoutPoint[] leftRoute =
                    [new(sourceX, source.Y), new(sourceX, sourceChannelY), new(leftLane, sourceChannelY),
                        new(leftLane, targetChannelY), new(targetX, targetChannelY), new(targetX, target.Y)];
                    var targetY = target.Y + target.Height / 2;
                    var rightLane = width - Margin - rightLaneCount * RouteLaneGap + RouteLaneGap
                        + (nextRightLane++ * RouteLaneGap);
                    LocalFlowchartLayoutPoint[] rightRoute =
                    [new(sourceX, source.Y), new(sourceX, sourceChannelY), new(rightLane, sourceChannelY),
                        new(rightLane, targetY), new(target.X + target.Width, targetY)];
                    var fallback = RouteAvoidsNodes(rightRoute, nodes.Values, source.Node.Id, target.Node.Id)
                        ? rightRoute : leftRoute;
                    route.AddRange(ChooseRoute([leftRoute, rightRoute], nodes.Values, results,
                        source.Node.Id, target.Node.Id, fallback));
                }
                else if (targetRank > sourceRank + 1)
                {
                    var sourceX = source.X + source.Width / 2;
                    var targetX = target.X + target.Width / 2;
                    var sourceY = source.Y + source.Height;
                    var targetY = target.Y;
                    var sourceChannelY = rankBottoms[sourceRank] + 16;
                    var targetChannelY = Math.Max(0, rankTops[targetRank] - 16);
                    var localRoute = FindLocalSkipRoute(source, target, sourceX, sourceY, targetX, targetY,
                        sourceChannelY, targetChannelY, nodes.Values, results, allowLocalSkipRouting, out var localAlternatives);
                    if (localRoute is not null)
                    {
                        route.AddRange(localRoute);
                    }
                    else
                    {
                        var lane = width - Margin - rightLaneCount * RouteLaneGap + RouteLaneGap
                            + (nextRightLane++ * RouteLaneGap);
                        route.AddRange([new(sourceX, sourceY), new(sourceX, sourceChannelY), new(lane, sourceChannelY),
                            new(lane, targetChannelY), new(targetX, targetChannelY), new(targetX, targetY)]);
                    }
                }
                else
                {
                    var lane = width - Margin - rightLaneCount * RouteLaneGap + RouteLaneGap + (nextRightLane++ * RouteLaneGap);
                    var sourceX = source.X + source.Width / 2;
                    var targetX = target.X + target.Width / 2;
                    var sourceChannelY = rankBottoms[sourceRank] + 16;
                    var targetChannelY = Math.Max(0, rankTops[targetRank] - 16);
                    route.AddRange([new(sourceX, source.Y + source.Height), new(sourceX, sourceChannelY),
                        new(lane, sourceChannelY), new(lane, targetChannelY), new(targetX, targetChannelY), new(targetX, target.Y)]);
                }
            }
            else
            {
                if (edge.FromId == edge.ToId)
                {
                    var lane = height - Margin - bottomLaneCount * RouteLaneGap + RouteLaneGap + (nextBottomLane++ * RouteLaneGap);
                    route.AddRange([new(source.X, source.Y + source.Height / 4), new(source.X - 24, lane),
                        new(source.X + source.Width + 24, lane), new(source.X + source.Width + 24, source.Y + source.Height / 2),
                        new(source.X + source.Width, source.Y + source.Height / 2)]);
                }
                else if (terminalEdgeOrders.Contains(edge.Order))
                {
                    var lane = height - Margin - bottomLaneCount * RouteLaneGap + RouteLaneGap + (nextBottomLane++ * RouteLaneGap);
                    var sourceY = source.Y + source.Height / 2;
                    var targetY = target.Y + target.Height / 2;
                    var sourceChannelX = rankRights[sourceRank] + 16;
                    var targetChannelX = Math.Max(0, rankLefts[targetRank] - 16);
                    var directRoute = FindDirectTerminalRoute(
                        new(source.X + source.Width, sourceY), new(target.X, targetY),
                        nodes.Values, results, source.Node.Id, target.Node.Id, vertical: false);
                    route.AddRange(directRoute ??
                        [new(source.X + source.Width, sourceY), new(sourceChannelX, sourceY),
                            new(sourceChannelX, lane), new(targetChannelX, lane), new(targetChannelX, targetY), new(target.X, targetY)]);
                }
                else if (targetRank == sourceRank + 1)
                {
                    var sourceX = source.X + source.Width;
                    var sourceY = source.Y + source.Height / 2;
                    var targetX = target.X;
                    var targetY = target.Y + target.Height / 2;
                    var middleX = sourceX + Math.Max(12, (targetX - sourceX) / 2);
                    route.AddRange([new(sourceX, sourceY), new(middleX, sourceY), new(middleX, targetY), new(targetX, targetY)]);
                }
                else if (targetRank <= sourceRank)
                {
                    var sourceY = source.Y + source.Height / 2;
                    var targetY = target.Y + target.Height / 2;
                    var sourceChannelX = Math.Max(0, rankLefts[sourceRank] - 16);
                    var targetChannelX = Math.Max(0, rankLefts[targetRank] - 16);
                    var lane = Margin + (nextTopLane++ * RouteLaneGap);
                    route.AddRange([new(source.X, sourceY), new(sourceChannelX, sourceY), new(sourceChannelX, lane),
                        new(targetChannelX, lane), new(targetChannelX, targetY), new(target.X, targetY)]);
                }
                else
                {
                    var lane = height - Margin - bottomLaneCount * RouteLaneGap + RouteLaneGap + (nextBottomLane++ * RouteLaneGap);
                    var sourceY = source.Y + source.Height / 2;
                    var targetY = target.Y + target.Height / 2;
                    var sourceChannelX = rankRights[sourceRank] + 16;
                    var targetChannelX = Math.Max(0, rankLefts[targetRank] - 16);
                    route.AddRange([new(source.X + source.Width, sourceY), new(sourceChannelX, sourceY),
                        new(sourceChannelX, lane), new(targetChannelX, lane), new(targetChannelX, targetY), new(target.X, targetY)]);
                }
            }

            if (edge.ImportedWaypoints is { Count: > 0 })
                route.InsertRange(1, edge.ImportedWaypoints.Select(point => new LocalFlowchartLayoutPoint(point.X, point.Y)));

            var labelLines = edge.Label is null ? Array.Empty<string>() : WrapEdgeLabel(edge.Label);
            var label = MeasureEdgeLabel(labelLines);
            results.Add(new LocalFlowchartLayoutEdge(edge, route, 0, 0, label.Width, label.Height, labelLines));
        }

        return PlaceEdgeLabels(document, results, nodes, width, height, direction);
    }

    private static IReadOnlyList<LocalFlowchartLayoutPoint>? FindDirectTerminalRoute(
        LocalFlowchartLayoutPoint start, LocalFlowchartLayoutPoint end,
        IEnumerable<LocalFlowchartLayoutNode> nodes, IReadOnlyList<LocalFlowchartLayoutEdge> routedEdges,
        string sourceId, string targetId, bool vertical)
    {
        var candidates = new List<IReadOnlyList<LocalFlowchartLayoutPoint>>();
        if (vertical && end.Y > start.Y + 2)
        {
            var gap = end.Y - start.Y;
            foreach (var channel in new[] { start.Y + gap / 2, start.Y + 16, end.Y - 16 }.Distinct())
                candidates.Add([start, new(start.X, channel), new(end.X, channel), end]);
        }
        else if (!vertical && end.X > start.X + 2)
        {
            var gap = end.X - start.X;
            foreach (var channel in new[] { start.X + gap / 2, start.X + 16, end.X - 16 }.Distinct())
                candidates.Add([start, new(channel, start.Y), new(channel, end.Y), end]);
        }
        if (candidates.Count == 0) return null;

        var nodeArray = nodes.ToArray();
        var viable = candidates.Where(candidate => RouteAvoidsNodes(candidate, nodeArray, sourceId, targetId));
        return viable.Select(candidate =>
        {
            var crossings = routedEdges.Count(edge => edge.Edge.FromId != sourceId && edge.Edge.ToId != sourceId
                && edge.Edge.FromId != targetId && edge.Edge.ToId != targetId && RoutesCross(candidate, edge.Route));
            var length = candidate.Zip(candidate.Skip(1)).Sum(segment =>
                Math.Abs(segment.First.X - segment.Second.X) + Math.Abs(segment.First.Y - segment.Second.Y));
            return (Route: candidate, Cost: crossings * (long)int.MaxValue + length);
        }).OrderBy(item => item.Cost).Select(item => item.Route).FirstOrDefault();
    }

    private static bool RouteAvoidsNodes(IReadOnlyList<LocalFlowchartLayoutPoint> route,
        IEnumerable<LocalFlowchartLayoutNode> nodes, string sourceId, string targetId)
    {
        var nodeArray = nodes.Where(node => node.Node.Id != sourceId && node.Node.Id != targetId).ToArray();
        return route.Zip(route.Skip(1)).All(segment => nodeArray.All(node =>
            !SegmentIntersectsRectangle(segment.First, segment.Second,
                node.X - 4, node.Y - 4, node.X + node.Width + 4, node.Y + node.Height + 4)));
    }

    private static bool HasCrossingWithRoutedEdges(IReadOnlyList<LocalFlowchartLayoutPoint> route,
        IReadOnlyList<LocalFlowchartLayoutEdge> routedEdges, string sourceId, string targetId) =>
        routedEdges.Any(edge => edge.Edge.FromId != sourceId && edge.Edge.ToId != sourceId
            && edge.Edge.FromId != targetId && edge.Edge.ToId != targetId && RoutesCross(route, edge.Route));

    private static IReadOnlyList<IReadOnlyList<LocalFlowchartLayoutPoint>> AdjacentSideRoutes(
        LocalFlowchartLayoutNode source, LocalFlowchartLayoutNode target, int sourceChannelY,
        int width, int leftLaneCount, int rightLaneCount)
    {
        var sourceX = source.X + source.Width / 2;
        var sourceY = source.Y + source.Height;
        var targetY = target.Y + target.Height / 2;
        var candidates = new List<IReadOnlyList<LocalFlowchartLayoutPoint>>();
        var leftCount = Math.Min(leftLaneCount, 4);
        for (var laneIndex = 0; laneIndex < leftCount; laneIndex++)
        {
            var lane = Margin + laneIndex * RouteLaneGap;
            candidates.Add([new(sourceX, sourceY), new(sourceX, sourceChannelY), new(lane, sourceChannelY),
                new(lane, targetY), new(target.X, targetY)]);
        }
        var rightCount = Math.Min(rightLaneCount, 4);
        var firstRightLane = Math.Max(0, rightLaneCount - rightCount);
        for (var laneIndex = firstRightLane; laneIndex < rightLaneCount; laneIndex++)
        {
            var lane = width - Margin - rightLaneCount * RouteLaneGap + RouteLaneGap + laneIndex * RouteLaneGap;
            candidates.Add([new(sourceX, sourceY), new(sourceX, sourceChannelY), new(lane, sourceChannelY),
                new(lane, targetY), new(target.X + target.Width, targetY)]);
        }

        return candidates;
    }

    private static IReadOnlyList<LocalFlowchartLayoutPoint> ChooseRoute(
        IReadOnlyList<LocalFlowchartLayoutPoint>[] candidates, IEnumerable<LocalFlowchartLayoutNode> nodes,
        IReadOnlyList<LocalFlowchartLayoutEdge> routedEdges, string sourceId, string targetId,
        IReadOnlyList<LocalFlowchartLayoutPoint> fallback)
    {
        return candidates.Where(candidate => RouteAvoidsNodes(candidate, nodes, sourceId, targetId))
            .Select(candidate =>
            {
                var crossings = routedEdges.Count(edge => edge.Edge.FromId != sourceId && edge.Edge.ToId != sourceId
                    && edge.Edge.FromId != targetId && edge.Edge.ToId != targetId && RoutesCross(candidate, edge.Route));
                var length = candidate.Zip(candidate.Skip(1)).Sum(segment =>
                    Math.Abs(segment.First.X - segment.Second.X) + Math.Abs(segment.First.Y - segment.Second.Y));
                return (Route: candidate, Crossings: crossings, Length: length);
            })
            .OrderBy(candidate => candidate.Crossings)
            .ThenBy(candidate => candidate.Length)
            .Select(candidate => candidate.Route)
            .FirstOrDefault() ?? fallback;
    }

    private static IReadOnlyList<LocalFlowchartLayoutPoint>? FindLocalSkipRoute(
        LocalFlowchartLayoutNode source, LocalFlowchartLayoutNode target, int sourceX, int sourceY,
        int targetX, int targetY, int sourceChannelY, int targetChannelY,
        IEnumerable<LocalFlowchartLayoutNode> nodes, IReadOnlyList<LocalFlowchartLayoutEdge> routedEdges,
        bool allowGeometrySearch, out IReadOnlyList<LocalFlowchartLayoutPoint>[] viableAlternatives)
    {
        viableAlternatives = [];
        if (!allowGeometrySearch || targetY <= sourceY) return null;
        var obstacles = nodes.Where(node => node.Node.Id != source.Node.Id && node.Node.Id != target.Node.Id
            && node.Y < targetY && node.Y + node.Height > sourceY).ToArray();
        if (obstacles.Length == 0) return null;
        var intervals = obstacles.Select(node => (Left: node.X, Right: node.X + node.Width))
            .OrderBy(interval => interval.Left).ToArray();
        var lanes = new List<int> { Math.Max(Margin, intervals[0].Left - RouteLaneGap) };
        var occupiedRight = intervals[0].Right;
        foreach (var interval in intervals.Skip(1))
        {
            if (interval.Left - occupiedRight > 8)
                lanes.Add(occupiedRight + (interval.Left - occupiedRight) / 2);
            occupiedRight = Math.Max(occupiedRight, interval.Right);
        }
        lanes.Add(occupiedRight + RouteLaneGap);
        var candidates = lanes.Distinct().SelectMany(lane => new IReadOnlyList<LocalFlowchartLayoutPoint>[]
        {
            [new(sourceX, sourceY), new(sourceX, sourceChannelY), new(lane, sourceChannelY),
                new(lane, targetChannelY), new(targetX, targetChannelY), new(targetX, targetY)],
            [new(sourceX, sourceY), new(sourceX, sourceChannelY), new(lane, sourceChannelY),
                new(lane, targetY), new(target.X, targetY)],
            [new(sourceX, sourceY), new(sourceX, sourceChannelY), new(lane, sourceChannelY),
                new(lane, targetY), new(target.X + target.Width, targetY)]
        });
        viableAlternatives = candidates
            .Where(candidate => RouteAvoidsNodes(candidate, nodes, source.Node.Id, target.Node.Id)).ToArray();
        return viableAlternatives.Select(candidate =>
        {
            var crossings = routedEdges.Count(edge => edge.Edge.FromId != source.Node.Id && edge.Edge.ToId != source.Node.Id
                && edge.Edge.FromId != target.Node.Id && edge.Edge.ToId != target.Node.Id && RoutesCross(candidate, edge.Route));
            var length = candidate.Zip(candidate.Skip(1)).Sum(segment =>
                Math.Abs(segment.First.X - segment.Second.X) + Math.Abs(segment.First.Y - segment.Second.Y));
            return (Route: candidate, Cost: crossings * (long)int.MaxValue + length);
        }).OrderBy(item => item.Cost).Select(item => item.Route).FirstOrDefault();
    }

    private static LocalFlowchartLayoutEdge[] PlaceEdgeLabels(LocalFlowchartDocument document,
        IReadOnlyList<LocalFlowchartLayoutEdge> edges, IReadOnlyDictionary<string, LocalFlowchartLayoutNode> nodes,
        int width, int height, LocalFlowchartDirection direction)
    {
        var nodeArray = nodes.Values.OrderBy(node => node.Node.Order).ToArray();
        var edgeArray = edges.Select(edge => edge with
        {
            Edge = edge.Edge with { Label = null },
            LabelX = 0,
            LabelY = 0,
            LabelWidth = 0,
            LabelHeight = 0
        }).ToArray();
        var obstacleLayout = new LocalFlowchartLayout(width, height, 0, nodeArray, edgeArray, direction);
        var obstacles = new LocalFlowchartSpatialIndex(obstacleLayout);
        var placed = new LocalFlowchartLabelBoundsIndex();
        var positioned = new LocalFlowchartLayoutEdge[edges.Count];
        for (var edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
        {
            var edge = edges[edgeIndex];
            if (edge.Edge.Label is null)
            {
                positioned[edgeIndex] = edge;
                continue;
            }
            var candidate = LabelCandidates(edge.Route, edge.LabelWidth, edge.LabelHeight)
                .FirstOrDefault(point => LabelFits(point.X, point.Y, edge, edgeIndex, edgeArray, nodeArray, obstacles, placed, width, height));
            if (candidate is null)
                throw new LocalFlowchartFormatException($"{document.SourcePath}:{edge.Edge.SourceLine}: edge label '{edge.Edge.Label}' cannot fit without covering a shape, another label, or an unrelated connector.");
            var bounds = (Left: candidate.X - edge.LabelWidth / 2, Top: candidate.Y - edge.LabelHeight / 2,
                Right: candidate.X + (edge.LabelWidth + 1) / 2, Bottom: candidate.Y + (edge.LabelHeight + 1) / 2);
            placed.Add(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            positioned[edgeIndex] = edge with { LabelX = candidate.X, LabelY = candidate.Y };
        }
        return positioned;
    }

    private static IEnumerable<LocalFlowchartLayoutPoint> LabelCandidates(IReadOnlyList<LocalFlowchartLayoutPoint> route,
        int labelWidth, int labelHeight)
    {
        var segments = route.Zip(route.Skip(1), (first, second) => (First: first, Second: second,
                Length: Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y)))
            .Where(segment => segment.Length > 0).OrderByDescending(segment => segment.Length);
        foreach (var segment in segments)
        {
            var horizontal = segment.First.Y == segment.Second.Y;
            var offsets = Enumerable.Range(0, 9).Select(index => index == 0 ? 0 : ((index + 1) / 2) * 24 * (index % 2 == 1 ? 1 : -1));
            foreach (var offset in offsets)
            foreach (var side in new[] { 0, -1, 1 })
            {
                var x = horizontal ? (segment.First.X + segment.Second.X) / 2 + offset
                    : segment.First.X + side * (labelWidth / 2 + 8);
                var y = horizontal ? segment.First.Y + side * (labelHeight / 2 + 8)
                    : (segment.First.Y + segment.Second.Y) / 2 + offset;
                yield return new LocalFlowchartLayoutPoint(x, y);
            }
        }
    }

    private static bool LabelFits(int centerX, int centerY, LocalFlowchartLayoutEdge edge, int edgeIndex,
        IReadOnlyList<LocalFlowchartLayoutEdge> edges, IReadOnlyList<LocalFlowchartLayoutNode> nodes,
        LocalFlowchartSpatialIndex obstacles, LocalFlowchartLabelBoundsIndex placed, int width, int height)
    {
        var left = centerX - edge.LabelWidth / 2;
        var top = centerY - edge.LabelHeight / 2;
        var right = left + edge.LabelWidth;
        var bottom = top + edge.LabelHeight;
        if (left < 8 || top < 8 || right > width - 8 || bottom > height - 8) return false;
        foreach (var nodeIndex in obstacles.NodesIntersecting(left, top, right - left, bottom - top))
        {
            var node = nodes[nodeIndex];
            if (RectanglesOverlap(left, top, right, bottom, node.X - 6, node.Y - 6,
                    node.X + node.Width + 6, node.Y + node.Height + 6)) return false;
        }
        if (placed.Intersects(left - 8, top - 8, right + 8, bottom + 8)) return false;
        foreach (var index in obstacles.EdgesIntersecting(left, top, right - left, bottom - top))
        {
            if (index == edgeIndex) continue;
            var points = edges[index].Route;
            for (var segment = 0; segment + 1 < points.Count; segment++)
                if (SegmentIntersectsRectangle(points[segment], points[segment + 1], left - 4, top - 4, right + 4, bottom + 4))
                    return false;
        }
        return true;
    }

    private static bool RectanglesOverlap(int leftA, int topA, int rightA, int bottomA,
        int leftB, int topB, int rightB, int bottomB) => leftA < rightB && rightA > leftB && topA < bottomB && bottomA > topB;

    private static bool SegmentIntersectsRectangle(LocalFlowchartLayoutPoint a, LocalFlowchartLayoutPoint b,
        int left, int top, int right, int bottom) => a.Y == b.Y
        ? a.Y >= top && a.Y <= bottom && Math.Max(Math.Min(a.X, b.X), left) <= Math.Min(Math.Max(a.X, b.X), right)
        : a.X >= left && a.X <= right && Math.Max(Math.Min(a.Y, b.Y), top) <= Math.Min(Math.Max(a.Y, b.Y), bottom);

    private static (int Width, int Height) MeasureEdgeLabel(IReadOnlyList<string> lines) => lines.Count == 0
        ? (0, 0) : (lines.Max(line => line.Length) * CharacterAdvance + 30, lines.Count * 20 + 20);

    private static IReadOnlyList<string> WrapEdgeLabel(string text)
    {
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length > MaximumEdgeLabelLineCharacters)
            {
                if (current.Length > 0) { lines.Add(current.ToString()); current.Clear(); }
                for (var offset = 0; offset < word.Length; offset += MaximumEdgeLabelLineCharacters)
                    lines.Add(word.Substring(offset, Math.Min(MaximumEdgeLabelLineCharacters, word.Length - offset)));
            }
            else if (current.Length > 0 && current.Length + 1 + word.Length > MaximumEdgeLabelLineCharacters)
            {
                lines.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }
            else
            {
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines.Count == 0 ? [text] : lines;
    }

    private static void ReduceCrossings(LocalFlowchartDocument document, IReadOnlyDictionary<string, int> ranks,
        IReadOnlyDictionary<int, List<LocalFlowchartNode>> rows)
    {
        // A rank with one node has no ordering decision to make. Long chains
        // often have one node per rank, so avoid building millions of tiny
        // adjacency dictionaries when crossings are structurally impossible.
        if (rows.Values.All(row => row.Count <= 1)) return;

        var edgesByRankPair = document.Edges
            .GroupBy(edge => (FromRank: ranks[edge.FromId], ToRank: ranks[edge.ToId]))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<LocalFlowchartEdge>)group.ToArray());
        var maxRank = ranks.Values.Max();
        for (var pass = 0; pass < 4; pass++)
        {
            for (var rank = 1; rank <= maxRank; rank++)
                if (edgesByRankPair.TryGetValue((rank - 1, rank), out var incomingEdges))
                    SortByNeighborBarycenter(rows, rank, rank - 1, incoming: true, incomingEdges);
            for (var rank = maxRank - 1; rank >= 0; rank--)
                if (edgesByRankPair.TryGetValue((rank, rank + 1), out var outgoingEdges))
                    SortByNeighborBarycenter(rows, rank, rank + 1, incoming: false, outgoingEdges);
        }
    }

    private static void SortByNeighborBarycenter(IReadOnlyDictionary<int, List<LocalFlowchartNode>> rows,
        int rank, int neighborRank, bool incoming, IReadOnlyList<LocalFlowchartEdge> edges)
    {
        if (!rows.TryGetValue(rank, out var row) || !rows.TryGetValue(neighborRank, out var neighbors)) return;
        var neighborIndex = neighbors.Select((node, index) => (node.Id, index))
            .ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);
        var positions = row.Select((node, index) => (node.Id, index)).ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);
        var adjacent = edges
            .GroupBy(edge => incoming ? edge.ToId : edge.FromId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(edge => neighborIndex[incoming ? edge.FromId : edge.ToId]).ToArray(), StringComparer.Ordinal);
        var reordered = row.Select(node =>
        {
            var values = adjacent.GetValueOrDefault(node.Id) ?? [];
            var barycenter = values.Length == 0 ? positions[node.Id] : values.Average();
            return (Node: node, Barycenter: barycenter, Original: node.Order);
        }).OrderBy(item => item.Barycenter).ThenBy(item => item.Original).Select(item => item.Node).ToList();
        row.Clear();
        row.AddRange(reordered);
    }

    private static Dictionary<string, int> AssignRanks(LocalFlowchartDocument document)
    {
        var nodeOrder = document.Nodes.ToDictionary(node => node.Id, node => node.Order, StringComparer.Ordinal);
        var directionalEdges = document.Edges.Where(edge => edge.FromId != edge.ToId).ToArray();
        var ascendingCount = directionalEdges.Count(edge => nodeOrder[edge.FromId] < nodeOrder[edge.ToId]);
        var descendingOrder = ascendingCount * 2 < directionalEdges.Length;
        var rankEdges = directionalEdges.Where(edge => descendingOrder
            ? nodeOrder[edge.FromId] > nodeOrder[edge.ToId]
            : nodeOrder[edge.FromId] < nodeOrder[edge.ToId]);
        var outgoing = document.Nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        var incomingCounts = document.Nodes.ToDictionary(node => node.Id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in rankEdges)
        {
            outgoing[edge.FromId].Add(edge.ToId);
            incomingCounts[edge.ToId]++;
        }
        var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        foreach (var node in document.Nodes.Where(node => incomingCounts[node.Id] == 0).OrderBy(node => node.Order))
        {
            ranks[node.Id] = 0;
            queue.Enqueue(node.Id);
        }
        while (queue.Count > 0)
        {
            var source = queue.Dequeue();
            foreach (var target in outgoing[source])
            {
                ranks[target] = Math.Max(ranks.GetValueOrDefault(target), ranks[source] + 1);
                if (--incomingCounts[target] == 0) queue.Enqueue(target);
            }
        }
        foreach (var node in document.Nodes)
            ranks.TryAdd(node.Id, 0);
        return ranks;
    }

    private static IReadOnlyList<string> Wrap(string text)
    {
        var result = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;
        foreach (var word in words)
        {
            if (word.Length > MaximumLineCharacters)
            {
                if (line.Length > 0) { result.Add(line); line = string.Empty; }
                for (var offset = 0; offset < word.Length; offset += MaximumLineCharacters)
                    result.Add(word.Substring(offset, Math.Min(MaximumLineCharacters, word.Length - offset)));
                continue;
            }
            if (line.Length > 0 && line.Length + 1 + word.Length > MaximumLineCharacters)
            {
                result.Add(line);
                line = word;
            }
            else line = line.Length == 0 ? word : $"{line} {word}";
        }
        if (line.Length > 0) result.Add(line);
        return result.Count == 0 ? [text] : result;
    }
}
