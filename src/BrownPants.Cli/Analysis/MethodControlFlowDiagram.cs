using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

internal sealed record MethodControlFlowBranch(int? DestinationOrdinal, string Semantics, bool IsConditional);

internal sealed record MethodControlFlowBlock(int Ordinal, string Kind, int SourceLine,
    IReadOnlyList<string> Operations, string? BranchValue, string ConditionKind,
    MethodControlFlowBranch? FallThrough, MethodControlFlowBranch? Conditional);

internal sealed record MethodControlFlowSnapshot(string Symbol, string SourcePath, int DeclarationLine,
    string Language, IReadOnlyList<MethodControlFlowBlock> Blocks, string? Failure)
{
    internal static MethodControlFlowSnapshot Create(SyntaxNode declaration, SemanticModel semanticModel,
        string symbol, string sourcePath, int declarationLine)
    {
        var body = declaration;
        if (declaration.Language == LanguageNames.VisualBasic)
        {
            body = declaration.Parent is { } parent ? parent : declaration;
        }

        ControlFlowGraph? graph;
        try
        {
            graph = ControlFlowGraph.Create(body, semanticModel);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or NotSupportedException)
        {
            return new(symbol, sourcePath, declarationLine, declaration.Language,
                Array.Empty<MethodControlFlowBlock>(), $"Roslyn could not create a control-flow graph: {exception.Message}");
        }

        if (graph is null)
            return new(symbol, sourcePath, declarationLine, declaration.Language,
                Array.Empty<MethodControlFlowBlock>(), "Roslyn returned no control-flow graph; the selected declaration may not have a source body.");

        var blocks = graph.Blocks.Select(block =>
        {
            var operations = block.Operations.Select(operation => operation.Syntax.ToString())
                .Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
            var branchValue = block.BranchValue?.Syntax.ToString();
            var sourceLines = block.Operations.Select(operation => operation.Syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1)
                .Concat(block.BranchValue is null ? Array.Empty<int>()
                    : new[] { block.BranchValue.Syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1 })
                .ToArray();
            var sourceLine = sourceLines.Length == 0 ? declarationLine : sourceLines.Min();
            return new MethodControlFlowBlock(block.Ordinal, block.Kind.ToString(), sourceLine,
                operations, branchValue, block.ConditionKind.ToString(),
                Snapshot(block.FallThroughSuccessor), Snapshot(block.ConditionalSuccessor));
        }).ToArray();

        return new(symbol, sourcePath, declarationLine, declaration.Language, blocks, null);
    }

    private static MethodControlFlowBranch? Snapshot(ControlFlowBranch? branch) => branch is null
        ? null
        : new MethodControlFlowBranch(branch.Destination?.Ordinal, branch.Semantics.ToString(),
            branch.IsConditionalSuccessor);
}

internal sealed record MethodControlFlowDiagramResult(LocalFlowchartDocument Document,
    int BlockCount, int DecisionCount, int ExplicitThrowCount, string Summary);

internal static class MethodControlFlowDiagramAnalyzer
{
    internal static MethodControlFlowDiagramResult Create(MethodControlFlowSnapshot snapshot)
    {
        if (snapshot.Failure is not null)
            throw new LocalFlowchartFormatException(snapshot.Failure);
        if (snapshot.Blocks.Count < 2)
            throw new LocalFlowchartFormatException("Roslyn's control-flow graph did not contain entry and exit blocks.");

        var ordinals = snapshot.Blocks.Select(block => block.Ordinal).ToHashSet();
        var outgoing = snapshot.Blocks.ToDictionary(block => block.Ordinal,
            block => Successors(block).Where(item => item.Branch.DestinationOrdinal is not null).ToArray());
        var incomingCounts = snapshot.Blocks.ToDictionary(block => block.Ordinal, _ => 0);
        foreach (var successor in outgoing.Values.SelectMany(items => items))
            incomingCounts[successor.Branch.DestinationOrdinal!.Value]++;

        var groups = new List<IReadOnlyList<MethodControlFlowBlock>>();
        var groupedOrdinals = new HashSet<int>();
        foreach (var block in snapshot.Blocks.OrderBy(block => block.Ordinal))
        {
            if (!groupedOrdinals.Add(block.Ordinal)) continue;
            var group = new List<MethodControlFlowBlock> { block };
            while (true)
            {
                var last = group[^1];
                if (!CanAppend(last)) break;
                var nextOrdinal = outgoing[last.Ordinal][0].Branch.DestinationOrdinal!.Value;
                if (groupedOrdinals.Contains(nextOrdinal)) break;
                var next = snapshot.Blocks.Single(item => item.Ordinal == nextOrdinal);
                if (next.Kind != nameof(BasicBlockKind.Block) || next.BranchValue is not null
                    || incomingCounts[nextOrdinal] != 1) break;
                groupedOrdinals.Add(nextOrdinal);
                group.Add(next);
            }
            groups.Add(group);
        }
        var groupByOrdinal = groups.SelectMany(group => group.Select(block => (block.Ordinal, Group: group)))
            .ToDictionary(item => item.Ordinal, item => item.Group);
        var nodes = new List<LocalFlowchartNode>(groups.Count + 8);
        var edges = new List<LocalFlowchartEdge>();
        var decisions = 0;
        var explicitThrows = 0;
        foreach (var group in groups)
        {
            var block = group[0];
            var shape = block.Kind switch
            {
                nameof(BasicBlockKind.Entry) => LocalFlowchartNodeShape.Start,
                nameof(BasicBlockKind.Exit) => LocalFlowchartNodeShape.End,
                _ when block.BranchValue is not null && block.ConditionKind is "WhenTrue" or "WhenFalse" => LocalFlowchartNodeShape.Decision,
                _ => LocalFlowchartNodeShape.Process
            };
            if (shape == LocalFlowchartNodeShape.Decision) decisions++;
            var operationText = block.Operations.FirstOrDefault() ?? "control transfer";
            var terminalSemantics = Successors(block).Select(item => item.Branch.Semantics).FirstOrDefault(value =>
                value is "Return" or "Throw" or "Rethrow");
            var behaviorText = block.BranchValue is not null && terminalSemantics is not null
                ? TerminalLabel(terminalSemantics, block.BranchValue)
                : operationText;
            var blockRange = group.Count == 1 ? $"B{block.Ordinal}" : $"B{block.Ordinal}-B{group[^1].Ordinal}";
            var label = shape switch
            {
                LocalFlowchartNodeShape.Start => "Method entry",
                LocalFlowchartNodeShape.End => "Method exit",
                LocalFlowchartNodeShape.Decision => $"B{block.Ordinal}: {ReadableLabel(block.BranchValue!)}?",
                _ when group.Count > 1 => $"{blockRange}: {ReadableLabel(behaviorText)} (+{group.Count - 1} steps)",
                _ => $"B{block.Ordinal}: {ReadableLabel(behaviorText)}"
            };
            var details = BuildDetails(snapshot, group);
            nodes.Add(new LocalFlowchartNode($"b{block.Ordinal:D5}", shape, label,
                block.SourceLine, block.Ordinal, details));
        }

        foreach (var group in groups)
        foreach (var block in group)
        foreach (var (branch, conditional) in Successors(block))
        {
            if (branch.DestinationOrdinal is int internalDestination
                && groupByOrdinal[internalDestination] == group
                && internalDestination != group[^1].Ordinal)
                continue;
            AddSuccessor(block, group, branch, conditional);
        }

        var summary = $"This chart shows Roslyn's {snapshot.Language} control-flow graph for {ShortMethodName(snapshot.Symbol)}: "
            + $"{snapshot.Blocks.Count} blocks and {decisions} decision block(s). "
            + $"Connectors preserve compiler-reported branch and exit semantics, including {explicitThrows} explicit throw/rethrow path(s). "
            + "Implicit runtime exceptions and behavior absent from the compiler graph are not modeled.";
        // CFG ordinals can have gaps after straight-line blocks are grouped.
        // Synthetic terminal nodes use append order, so normalize the chart
        // order to guarantee unique diagrams.net cell identifiers.
        var orderedNodes = nodes.Select((node, index) => node with { Order = index }).ToArray();
        return new MethodControlFlowDiagramResult(
            new LocalFlowchartDocument("method-flow", snapshot.SourcePath, orderedNodes, edges),
            snapshot.Blocks.Count, decisions, explicitThrows, summary);

        bool CanAppend(MethodControlFlowBlock source)
        {
            var branches = Successors(source).ToArray();
            if (source.Kind != nameof(BasicBlockKind.Block) || source.BranchValue is not null
                || branches.Length != 1 || branches[0].Branch.Semantics != "Regular"
                || branches[0].Branch.DestinationOrdinal is not int destination) return false;
            return snapshot.Blocks.Any(item => item.Ordinal == destination
                && item.Kind == nameof(BasicBlockKind.Block) && item.BranchValue is null
                && incomingCounts[destination] == 1);
        }

        void AddSuccessor(MethodControlFlowBlock source, IReadOnlyList<MethodControlFlowBlock> group,
            MethodControlFlowBranch branch, bool conditional)
        {
            if (branch.Semantics is "Throw" or "Rethrow") explicitThrows++;
            if (branch.DestinationOrdinal is not int destination)
            {
                var terminal = branch.Semantics switch
                {
                    "Throw" => ("throw", "Exception exit"),
                    "Rethrow" => ("rethrow", "Rethrow exit"),
                    "StructuredExceptionHandling" => ("exception handling", "Exception-handling exit"),
                    "ProgramTermination" => ("terminate", "Program termination"),
                    _ => throw new LocalFlowchartFormatException($"Roslyn {branch.Semantics} branch from block {source.Ordinal} has no destination; no partial behavior chart was written.")
                };
                var terminalId = $"t{source.Ordinal:D5}{(conditional ? 'c' : 'f')}";
                if (nodes.All(node => node.Id != terminalId))
                    nodes.Add(new LocalFlowchartNode(terminalId, LocalFlowchartNodeShape.End,
                        terminal.Item2, source.SourceLine, nodes.Count,
                        $"Compiler-reported terminal branch from block {source.Ordinal}.\nSource: {snapshot.SourcePath}:{source.SourceLine}\nBranch semantics: {branch.Semantics}\nNo destination block exists in the Roslyn CFG."));
                edges.Add(new LocalFlowchartEdge($"b{source.Ordinal:D5}", terminalId,
                    terminal.Item1, source.SourceLine, edges.Count,
                    Details: $"Roslyn branch semantics: {branch.Semantics}; CFG destination is null."));
                return;
            }
            if (!ordinals.Contains(destination))
                throw new LocalFlowchartFormatException($"Roslyn branch from block {source.Ordinal} points to missing block {destination}; no partial behavior chart was written.");
            if (groupByOrdinal[destination] == group)
                return;

            var branchSemantics = branch.Semantics;
            var label = BranchLabel(source, branch, conditional);
            edges.Add(new LocalFlowchartEdge($"b{group[0].Ordinal:D5}", $"b{groupByOrdinal[destination][0].Ordinal:D5}",
                label, source.SourceLine, edges.Count, Details: $"Roslyn branch semantics: {branchSemantics}"));
        }
    }

    private static string BranchLabel(MethodControlFlowBlock block, MethodControlFlowBranch branch, bool conditional)
    {
        if (block.BranchValue is not null && block.ConditionKind is "WhenTrue" or "WhenFalse")
        {
            var takesConditional = conditional;
            var value = takesConditional
                ? block.ConditionKind == "WhenTrue" ? "true" : "false"
                : block.ConditionKind == "WhenTrue" ? "false" : "true";
            return value;
        }
        return branch.Semantics switch
        {
            "Return" => "return",
            "Throw" => "throw",
            "Rethrow" => "rethrow",
            "StructuredExceptionHandling" => "exception handling",
            "ProgramTermination" => "terminate",
            "Regular" => "next",
            _ => branch.Semantics.ToLowerInvariant()
        };
    }

    private static string BuildDetails(MethodControlFlowSnapshot snapshot, IReadOnlyList<MethodControlFlowBlock> group)
    {
        var lines = new List<string> { $"Roslyn {snapshot.Language} control-flow blocks: {string.Join(", ", group.Select(block => $"B{block.Ordinal}"))}" };
        foreach (var block in group)
        {
            lines.Add($"\nB{block.Ordinal} ({block.Kind}) — {snapshot.SourcePath}:{block.SourceLine}");
            if (block.BranchValue is not null)
            {
                lines.Add($"Branch value: {block.BranchValue}");
                lines.Add($"Condition kind: {block.ConditionKind}");
            }
            lines.AddRange(block.Operations.Select(operation => "  " + operation));
            AddBranch("Conditional successor", block.Conditional);
            AddBranch("Fall-through successor", block.FallThrough);
        }
        return string.Join('\n', lines);

        void AddBranch(string name, MethodControlFlowBranch? branch)
        {
            if (branch is not null)
                lines.Add($"{name}: {(branch.DestinationOrdinal is int destination ? $"B{destination}" : "none")} ({branch.Semantics})");
        }
    }

    private static IEnumerable<(MethodControlFlowBranch Branch, bool Conditional)> Successors(MethodControlFlowBlock block)
    {
        if (block.Conditional is not null) yield return (block.Conditional, true);
        if (block.FallThrough is not null) yield return (block.FallThrough, false);
    }

    private static string TerminalLabel(string semantics, string branchValue)
    {
        if (semantics == "Return") return $"return {branchValue}";
        if (semantics == "Rethrow") return "rethrow exception";
        var exception = Regex.Match(branchValue, @"\bnew\s+(?:global::)?(?<type>[\w.]+)");
        if (exception.Success)
        {
            var type = exception.Groups["type"].Value;
            var separator = type.LastIndexOf('.');
            return $"throw {type[(separator + 1)..]}";
        }
        return "throw exception";
    }

    private static string ReadableLabel(string value)
    {
        var simpleCall = Regex.Match(value, @"^(?:string|String)\.IsNullOrWhiteSpace\((?<argument>[\w.]+)\)$");
        if (simpleCall.Success) return $"{ReadableWords(simpleCall.Groups["argument"].Value)} is blank";

        simpleCall = Regex.Match(value, @"^(?:System\.IO\.)?Path\.IsPathRooted\((?<argument>[\w.]+)\)$");
        if (simpleCall.Success) return $"{ReadableWords(simpleCall.Groups["argument"].Value)} is rooted";

        simpleCall = Regex.Match(value, @"^(?:System\.)?OperatingSystem\.IsWindows\(\)$");
        if (simpleCall.Success) return "running on Windows";

        simpleCall = Regex.Match(value, @"^(?<receiver>[\w.]+)\.EndsInDirectorySeparator\((?<argument>[\w.]+)\)$");
        if (simpleCall.Success) return $"{ReadableWords(simpleCall.Groups["argument"].Value)} ends in separator";

        var startsWith = Regex.Match(value, @"^(?<receiver>[\w.]+)\.StartsWith\((?<argument>[\w.]+)(?:, [\w.]+)?\)$");
        if (startsWith.Success) return $"{ReadableWords(startsWith.Groups["receiver"].Value)} starts with {ReadableWords(startsWith.Groups["argument"].Value)}";

        var spaced = Regex.Replace(value,
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        spaced = spaced.Replace("&&", " and ", StringComparison.Ordinal)
            .Replace("||", " or ", StringComparison.Ordinal)
            .Replace("==", " equals ", StringComparison.Ordinal)
            .Replace("!=", " not equals ", StringComparison.Ordinal);
        spaced = Regex.Replace(spaced, @"[.(),]", " ");
        return string.Join(' ', spaced.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ReadableWords(string value)
    {
        var spaced = Regex.Replace(value,
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        spaced = spaced.Replace('.', ' ');
        return string.Join(' ', spaced.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ShortMethodName(string symbol)
    {
        var parameters = symbol.IndexOf('(');
        var head = parameters < 0 ? symbol : symbol[..parameters];
        var separator = head.LastIndexOf('.');
        return separator < 0 ? head : head[(separator + 1)..];
    }
}
