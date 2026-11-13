using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

internal enum LocalFlowchartNodeShape
{
    Start,
    Process,
    Decision,
    InputOutput,
    Person,
    Cloud,
    End
}

internal enum LocalFlowchartKind
{
    ProcessFlow,
    Organization,
    Brainstorm
}

internal sealed record LocalFlowchartImportedGeometry(int X, int Y, int Width, int Height);
internal sealed record LocalFlowchartImportedPageGeometry(int Width, int Height);
internal sealed record LocalFlowchartImportedWaypoint(int X, int Y);

internal sealed record LocalFlowchartNode(string Id, LocalFlowchartNodeShape Shape, string Label, int SourceLine, int Order,
    string? Details = null, LocalFlowchartImportedGeometry? ImportedGeometry = null, string? ImportedCellId = null,
    string? ImportedStyle = null);

internal sealed record LocalFlowchartEdge(string FromId, string ToId, string? Label, int SourceLine, int Order,
    string? ImportedCellId = null, string? Details = null, string? ImportedStyle = null,
    IReadOnlyList<LocalFlowchartImportedWaypoint>? ImportedWaypoints = null);

internal sealed record LocalFlowchartDocument(string Id, string SourcePath,
    IReadOnlyList<LocalFlowchartNode> Nodes, IReadOnlyList<LocalFlowchartEdge> Edges,
    LocalFlowchartKind Kind = LocalFlowchartKind.ProcessFlow,
    LocalFlowchartImportedPageGeometry? ImportedPageGeometry = null,
    string? ImportedPageName = null,
    string? ImportedBackground = null);

internal sealed class LocalFlowchartFormatException(string message) : Exception(message);

internal static class LocalFlowchartParser
{
    internal const int MaximumSourceBytes = 128 * 1024 * 1024;
    internal const int MaximumNodes = 1_000_000;
    internal const int MaximumEdges = 1_000_000;
    internal const int MaximumLabelLength = 80;
    internal const int MaximumDetailsLength = 1000;

    private static readonly Regex FenceStart = new(@"^\s*(?<fence>`{3,}|~{3,})(?<info>.*)$", RegexOptions.CultureInvariant);
    private static readonly Regex DiagramLine = new(@"^flowchart\s+([A-Za-z][A-Za-z0-9_-]{0,39})$", RegexOptions.CultureInvariant);
    private static readonly Regex NodeLine = new(@"^node\s+([A-Za-z][A-Za-z0-9_-]{0,39})\s+(start|process|decision|input-output|end)\s+""([^""\r\n]*)""(?:\s+details\s+""([^""\r\n]*)"")?$", RegexOptions.CultureInvariant);
    private static readonly Regex EdgeLine = new(@"^edge\s+([A-Za-z][A-Za-z0-9_-]{0,39})\s*->\s*([A-Za-z][A-Za-z0-9_-]{0,39})(?:\s+label\s+""([^""\r\n]*)"")?(?:\s+details\s+""([^""\r\n]*)"")?$", RegexOptions.CultureInvariant);

    internal static LocalFlowchartDocument Parse(string markdown, string sourcePath)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var blocks = FindFlowBlocks(lines);
        if (blocks.Count == 0)
            throw Error(sourcePath, 1, "A `brownpants-flow`, `brownpants-org`, or `brownpants-brainstorm` fenced block was not found.");
        if (blocks.Count != 1)
            throw Error(sourcePath, blocks[1].Opening + 1, "Exactly one BrownPants chart block is supported.");

        if (blocks[0].Kind != "brownpants-flow")
            return ParseTreeChart(sourcePath, lines, blocks[0]);

        var (opening, closing, _) = blocks[0];
        if (closing < 0)
            throw Error(sourcePath, opening + 1, "The `brownpants-flow` block has no closing fence.");

        string? diagramId = null;
        var nodes = new List<LocalFlowchartNode>();
        var edges = new List<LocalFlowchartEdge>();
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var edgeIds = new HashSet<(string From, string To, string? Label)>();
        var sawEdge = false;

        for (var index = opening + 1; index < closing; index++)
        {
            var line = lines[index].Trim();
            var sourceLine = index + 1;
            if (line.Length == 0 || line.StartsWith('#')) continue;

            if (diagramId is null)
            {
                var diagramMatch = DiagramLine.Match(line);
                if (!diagramMatch.Success)
                    throw Error(sourcePath, sourceLine, "Expected `flowchart <id>` as the first statement.");
                diagramId = diagramMatch.Groups[1].Value;
                continue;
            }

            var nodeMatch = NodeLine.Match(line);
            if (nodeMatch.Success)
            {
                if (sawEdge)
                    throw Error(sourcePath, sourceLine, "Declare all nodes before edge statements.");
                if (!nodeIds.Add(nodeMatch.Groups[1].Value))
                    throw Error(sourcePath, sourceLine, $"Duplicate node id '{nodeMatch.Groups[1].Value}'.");
                if (nodes.Count == MaximumNodes)
                    throw Error(sourcePath, sourceLine, $"The flowchart exceeds the {MaximumNodes}-node limit.");
                var label = nodeMatch.Groups[3].Value;
                ValidateLabel(label, sourcePath, sourceLine);
                var shape = nodeMatch.Groups[2].Value switch
                {
                    "start" => LocalFlowchartNodeShape.Start,
                    "process" => LocalFlowchartNodeShape.Process,
                    "decision" => LocalFlowchartNodeShape.Decision,
                    "input-output" => LocalFlowchartNodeShape.InputOutput,
                    "end" => LocalFlowchartNodeShape.End,
                    _ => throw new InvalidOperationException("The node shape expression was not exhaustive.")
                };
                var details = nodeMatch.Groups[4].Success ? ValidateDetails(nodeMatch.Groups[4].Value, sourcePath, sourceLine) : null;
                nodes.Add(new LocalFlowchartNode(nodeMatch.Groups[1].Value, shape, label, sourceLine, nodes.Count, details));
                continue;
            }

            var edgeMatch = EdgeLine.Match(line);
            if (edgeMatch.Success)
            {
                sawEdge = true;
                if (edges.Count == MaximumEdges)
                    throw Error(sourcePath, sourceLine, $"The flowchart exceeds the {MaximumEdges}-edge limit.");
                var from = edgeMatch.Groups[1].Value;
                var to = edgeMatch.Groups[2].Value;
                var label = edgeMatch.Groups[3].Success ? edgeMatch.Groups[3].Value : null;
                var details = edgeMatch.Groups[4].Success ? ValidateDetails(edgeMatch.Groups[4].Value, sourcePath, sourceLine) : null;
                if (!edgeIds.Add((from, to, label)))
                    throw Error(sourcePath, sourceLine, $"Duplicate edge '{from} -> {to}' with the same label.");
                if (label is not null) ValidateLabel(label, sourcePath, sourceLine);
                edges.Add(new LocalFlowchartEdge(from, to, label, sourceLine, edges.Count, Details: details));
                continue;
            }

            throw Error(sourcePath, sourceLine, "Unsupported flowchart syntax.");
        }

        if (diagramId is null)
            throw Error(sourcePath, opening + 2, "Expected `flowchart <id>` as the first statement.");
        if (nodes.Count == 0)
            throw Error(sourcePath, opening + 2, "A flowchart must declare at least one node.");
        foreach (var edge in edges)
        {
            if (!nodeIds.Contains(edge.FromId))
                throw Error(sourcePath, edge.SourceLine, $"Edge source node '{edge.FromId}' is not declared.");
            if (!nodeIds.Contains(edge.ToId))
                throw Error(sourcePath, edge.SourceLine, $"Edge target node '{edge.ToId}' is not declared.");
        }

        return new LocalFlowchartDocument(diagramId, sourcePath, nodes, edges);
    }

    private static void ValidateLabel(string label, string sourcePath, int line)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw Error(sourcePath, line, "Labels must contain at least one non-space character.");
        if (label.Length > MaximumLabelLength)
            throw Error(sourcePath, line, $"Labels may contain at most {MaximumLabelLength} characters.");
        if (label.Any(char.IsControl))
            throw Error(sourcePath, line, "Labels cannot contain control characters.");
    }

    private static string ValidateDetails(string details, string sourcePath, int line)
    {
        if (string.IsNullOrWhiteSpace(details)) throw Error(sourcePath, line, "Details must contain at least one non-space character.");
        if (details.Length > MaximumDetailsLength) throw Error(sourcePath, line, $"Details may contain at most {MaximumDetailsLength} characters.");
        if (details.Any(char.IsControl)) throw Error(sourcePath, line, "Details cannot contain control characters.");
        return details;
    }

    private static LocalFlowchartFormatException Error(string path, int line, string message) =>
        new($"{path}:{line}: {message}");

    private static LocalFlowchartDocument ParseTreeChart(string sourcePath, string[] lines,
        (int Opening, int Closing, string Kind) block)
    {
        var chartKind = block.Kind == "brownpants-org" ? LocalFlowchartKind.Organization : LocalFlowchartKind.Brainstorm;
        var expectedHeader = chartKind == LocalFlowchartKind.Organization ? "organization" : "brainstorm";
        var relationship = chartKind == LocalFlowchartKind.Organization ? "reports" : "branch";
        var nodeKind = chartKind == LocalFlowchartKind.Organization ? "person" : "idea";
        lines[block.Opening] = lines[block.Opening].Replace(block.Kind, "brownpants-flow", StringComparison.Ordinal);
        var header = new Regex($"^{expectedHeader}\\s+([A-Za-z][A-Za-z0-9_-]{{0,39}})$", RegexOptions.CultureInvariant);
        var node = new Regex($"^{nodeKind}\\s+([A-Za-z][A-Za-z0-9_-]{{0,39}})\\s+\"([^\"\\r\\n]{{1,80}})\"$", RegexOptions.CultureInvariant);
        var edge = new Regex($"^{relationship}\\s+([A-Za-z][A-Za-z0-9_-]{{0,39}})\\s*->\\s*([A-Za-z][A-Za-z0-9_-]{{0,39}})$", RegexOptions.CultureInvariant);
        var foundHeader = false;
        for (var index = block.Opening + 1; index < block.Closing; index++)
        {
            var statement = lines[index].Trim();
            if (statement.Length == 0 || statement.StartsWith('#')) continue;
            if (!foundHeader)
            {
                var match = header.Match(statement);
                if (!match.Success) throw Error(sourcePath, index + 1, $"Expected `{expectedHeader} <id>` as the first statement.");
                lines[index] = $"flowchart {match.Groups[1].Value}";
                foundHeader = true;
                continue;
            }

            var nodeMatch = node.Match(statement);
            if (nodeMatch.Success)
            {
                lines[index] = $"node {nodeMatch.Groups[1].Value} process \"{nodeMatch.Groups[2].Value}\"";
                continue;
            }

            var edgeMatch = edge.Match(statement);
            if (edgeMatch.Success)
            {
                lines[index] = $"edge {edgeMatch.Groups[1].Value} -> {edgeMatch.Groups[2].Value}";
                continue;
            }

            throw Error(sourcePath, index + 1, $"Unsupported {chartKind.ToString().ToLowerInvariant()} chart syntax.");
        }

        if (!foundHeader) throw Error(sourcePath, block.Opening + 2, $"Expected `{expectedHeader} <id>` as the first statement.");
        var parsed = Parse(string.Join('\n', lines), sourcePath);
        ValidateTree(parsed, chartKind);
        var shape = chartKind == LocalFlowchartKind.Organization ? LocalFlowchartNodeShape.Person : LocalFlowchartNodeShape.Cloud;
        return parsed with { Kind = chartKind, Nodes = parsed.Nodes.Select(item => item with { Shape = shape }).ToArray() };
    }

    private static void ValidateTree(LocalFlowchartDocument document, LocalFlowchartKind kind)
    {
        var incoming = document.Edges.GroupBy(edge => edge.ToId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var multipleParents = incoming.FirstOrDefault(pair => pair.Value > 1);
        if (multipleParents.Value > 1)
        {
            var edge = document.Edges.First(item => item.ToId == multipleParents.Key);
            throw Error(document.SourcePath, edge.SourceLine,
                kind == LocalFlowchartKind.Organization
                    ? $"'{multipleParents.Key}' has more than one manager; organization charts require one manager per person."
                    : $"'{multipleParents.Key}' has more than one parent; brainstorm maps require one parent per idea.");
        }

        var roots = document.Nodes.Where(node => !incoming.ContainsKey(node.Id)).ToArray();
        if (roots.Length != 1)
            throw Error(document.SourcePath, document.Nodes[0].SourceLine, $"A {kind.ToString().ToLowerInvariant()} chart must have exactly one root; found {roots.Length}.");

        var outgoing = document.Edges.GroupBy(edge => edge.FromId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.ToId).ToArray(), StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(roots[0].Id);
        while (pending.TryPop(out var current))
        {
            if (!seen.Add(current))
            {
                var cycleEdge = document.Edges.First(edge => edge.ToId == current);
                throw Error(document.SourcePath, cycleEdge.SourceLine, $"Cycles are not supported in a {kind.ToString().ToLowerInvariant()} chart.");
            }
            if (outgoing.TryGetValue(current, out var children))
                foreach (var child in children) pending.Push(child);
        }
        if (seen.Count != document.Nodes.Count)
            throw Error(document.SourcePath, document.Nodes.First(node => !seen.Contains(node.Id)).SourceLine,
                $"Every node must connect to the single root of a {kind.ToString().ToLowerInvariant()} chart.");
    }

    private static List<(int Opening, int Closing, string Kind)> FindFlowBlocks(IReadOnlyList<string> lines)
    {
        var blocks = new List<(int Opening, int Closing, string Kind)>();
        char? openFenceCharacter = null;
        var openFenceLength = 0;
        var openFlowBlock = -1;
        var openKind = string.Empty;
        for (var index = 0; index < lines.Count; index++)
        {
            if (openFenceCharacter is null)
            {
                var match = FenceStart.Match(lines[index]);
                if (!match.Success) continue;
                var fence = match.Groups["fence"].Value;
                var information = match.Groups["info"].Value.Trim();
                openFenceCharacter = fence[0];
                openFenceLength = fence.Length;
                if (information is "brownpants-flow" or "brownpants-org" or "brownpants-brainstorm")
                {
                    openFlowBlock = index;
                    openKind = information;
                }
                continue;
            }

            var closing = lines[index].Trim();
            if (closing.Length < openFenceLength || closing.Any(character => character != openFenceCharacter)) continue;
            if (openFlowBlock >= 0) blocks.Add((openFlowBlock, index, openKind));
            openFenceCharacter = null;
            openFenceLength = 0;
            openFlowBlock = -1;
        }
        if (openFlowBlock >= 0) blocks.Add((openFlowBlock, -1, openKind));
        return blocks;
    }
}
