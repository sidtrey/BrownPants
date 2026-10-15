using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

internal enum LocalFlowchartNodeShape
{
    Start,
    Process,
    Decision,
    End
}

internal sealed record LocalFlowchartNode(string Id, LocalFlowchartNodeShape Shape, string Label, int SourceLine, int Order);

internal sealed record LocalFlowchartEdge(string FromId, string ToId, string? Label, int SourceLine, int Order);

internal sealed record LocalFlowchartDocument(string Id, string SourcePath,
    IReadOnlyList<LocalFlowchartNode> Nodes, IReadOnlyList<LocalFlowchartEdge> Edges);

internal sealed class LocalFlowchartFormatException(string message) : Exception(message);

internal static class LocalFlowchartParser
{
    internal const int MaximumSourceBytes = 1_048_576;
    internal const int MaximumNodes = 200;
    internal const int MaximumEdges = 500;
    internal const int MaximumLabelLength = 80;

    private static readonly Regex FenceStart = new(@"^\s*(?<fence>`{3,}|~{3,})(?<info>.*)$", RegexOptions.CultureInvariant);
    private static readonly Regex DiagramLine = new(@"^flowchart\s+([A-Za-z][A-Za-z0-9_-]{0,39})$", RegexOptions.CultureInvariant);
    private static readonly Regex NodeLine = new(@"^node\s+([A-Za-z][A-Za-z0-9_-]{0,39})\s+(start|process|decision|end)\s+""([^""\r\n]{1,80})""$", RegexOptions.CultureInvariant);
    private static readonly Regex EdgeLine = new(@"^edge\s+([A-Za-z][A-Za-z0-9_-]{0,39})\s*->\s*([A-Za-z][A-Za-z0-9_-]{0,39})(?:\s+label\s+""([^""\r\n]{1,80})"")?$", RegexOptions.CultureInvariant);

    internal static LocalFlowchartDocument Parse(string markdown, string sourcePath)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var blocks = FindFlowBlocks(lines);
        if (blocks.Count == 0)
            throw Error(sourcePath, 1, "A `brownpants-flow` fenced block was not found.");
        if (blocks.Count != 1)
            throw Error(sourcePath, blocks[1].Opening + 1, "Exactly one `brownpants-flow` block is supported.");

        var (opening, closing) = blocks[0];
        if (closing < 0)
            throw Error(sourcePath, opening + 1, "The `brownpants-flow` block has no closing fence.");

        string? diagramId = null;
        var nodes = new List<LocalFlowchartNode>();
        var edges = new List<LocalFlowchartEdge>();
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var edgeIds = new HashSet<(string From, string To)>();
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
                    "end" => LocalFlowchartNodeShape.End,
                    _ => throw new InvalidOperationException("The node shape expression was not exhaustive.")
                };
                nodes.Add(new LocalFlowchartNode(nodeMatch.Groups[1].Value, shape, label, sourceLine, nodes.Count));
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
                if (!edgeIds.Add((from, to)))
                    throw Error(sourcePath, sourceLine, $"Duplicate edge '{from} -> {to}'.");
                var label = edgeMatch.Groups[3].Success ? edgeMatch.Groups[3].Value : null;
                if (label is not null) ValidateLabel(label, sourcePath, sourceLine);
                edges.Add(new LocalFlowchartEdge(from, to, label, sourceLine, edges.Count));
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
        if (label.Any(character => character is < ' ' or > '~'))
            throw Error(sourcePath, line, "Labels support printable ASCII characters only.");
    }

    private static LocalFlowchartFormatException Error(string path, int line, string message) =>
        new($"{path}:{line}: {message}");

    private static List<(int Opening, int Closing)> FindFlowBlocks(IReadOnlyList<string> lines)
    {
        var blocks = new List<(int Opening, int Closing)>();
        char? openFenceCharacter = null;
        var openFenceLength = 0;
        var openFlowBlock = -1;
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
                if (information.Equals("brownpants-flow", StringComparison.Ordinal)) openFlowBlock = index;
                continue;
            }

            var closing = lines[index].Trim();
            if (closing.Length < openFenceLength || closing.Any(character => character != openFenceCharacter)) continue;
            if (openFlowBlock >= 0) blocks.Add((openFlowBlock, index));
            openFenceCharacter = null;
            openFenceLength = 0;
            openFlowBlock = -1;
        }
        if (openFlowBlock >= 0) blocks.Add((openFlowBlock, -1));
        return blocks;
    }
}
