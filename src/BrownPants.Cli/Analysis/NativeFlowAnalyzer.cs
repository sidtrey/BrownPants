using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

internal static class NativeFlowAnalyzer
{
    private static readonly HashSet<string> SupportedMessageMacros = new(StringComparer.Ordinal)
    {
        "ON_COMMAND", "ON_COMMAND_EX", "ON_COMMAND_RANGE", "ON_COMMAND_RANGE_EX",
        "ON_UPDATE_COMMAND_UI", "ON_UPDATE_COMMAND_UI_RANGE", "ON_BN_CLICKED", "ON_CONTROL",
        "ON_CONTROL_RANGE", "ON_NOTIFY", "ON_NOTIFY_RANGE", "ON_REGISTERED_MESSAGE"
    };

    private static readonly Regex MessageMap = new(
        @"\bBEGIN_MESSAGE_MAP\s*\(\s*(?<class>[A-Za-z_]\w*(?:::\w+)*)\s*,[^)]*\)(?<body>.*?)\bEND_MESSAGE_MAP\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex MessageEntry = new(
        @"\b(?<macro>ON_[A-Z0-9_]+)\s*\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex HandlerReference = new(
        @"^(?:&\s*)?(?<owner>(?:[A-Za-z_]\w*::)+)?(?<method>[A-Za-z_]\w*)$",
        RegexOptions.CultureInvariant);

    private static readonly Regex MethodDefinition = new(
        @"(?<qualified>(?<owner>(?:[A-Za-z_]\w*::)+)(?<method>~?[A-Za-z_]\w*))\s*\([^;{}]*\)\s*(?:(?:const|noexcept|override|final)\s*)*\{",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex ClassDefinition = new(
        @"\b(?:(?:ref)\s+)?(?:class|struct)\s+(?<name>[A-Za-z_]\w*)[^;{}]*\{",
        RegexOptions.CultureInvariant);

    private static readonly Regex InlineMethodDefinition = new(
        @"^\s*(?:(?:public|protected|private|internal|static|virtual|override|final|inline|friend|constexpr|explicit|extern|new)\s+)*(?:[A-Za-z_~][\w:<>,*&\s]*?)\s+(?<method>~?[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?:(?:const|noexcept|override|final)\s*)*\{",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex QualifiedCall = new(
        @"(?<qualified>(?<owner>(?:[A-Za-z_]\w*::)+)(?<method>[A-Za-z_]\w*))\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly string[] EvidenceLimits =
    [
        "Native flow links are lexical candidates, not compiler-resolved calls.",
        "Macro expansion, active preprocessor branches, overload binding, unqualified calls, runtime dispatch, and C++/CLI-to-managed project linkage are not resolved."
    ];

    public static NativeFlowAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit)
    {
        var sources = new List<NativeSource>();
        foreach (var path in sourcePaths.Where(IsNativeSource).OrderBy(path => path, RepositoryPathComparer.Instance))
        {
            try
            {
                var source = File.ReadAllText(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                var lines = source.Split('\n');
                var offsets = GetLineOffsets(lines);
                var sanitizedLines = Sanitize(lines);
                sources.Add(new NativeSource(path.Replace('\\', '/'),
                    string.Join("\n", sanitizedLines), offsets));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Source readability failures are handled by the existing inventory evidence.
            }
        }

        var definitions = sources.SelectMany(ParseDefinitions).ToArray();
        var bySymbol = definitions.GroupBy(definition => SymbolKey(definition.Owner, definition.Method), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Source.Path, RepositoryPathComparer.Instance)
                .ThenBy(item => item.Line).ToArray(), StringComparer.Ordinal);
        var relations = new List<NativeFlowRelation>();
        foreach (var source in sources)
        foreach (Match map in MessageMap.Matches(source.Sanitized))
        {
            var mapClass = map.Groups["class"].Value;
            var body = map.Groups["body"];
            foreach (Match entry in MessageEntry.Matches(body.Value))
            {
                if (!SupportedMessageMacros.Contains(entry.Groups["macro"].Value)) continue;
                var open = body.Index + entry.Index + entry.Length - 1;
                if (!TryReadArguments(source.Sanitized, open, out var arguments, out var close)) continue;
                var handler = HandlerReference.Match(arguments[^1].Trim());
                if (!handler.Success) continue;
                var owner = handler.Groups["owner"].Success
                    ? handler.Groups["owner"].Value.TrimEnd(':')
                    : mapClass;
                var method = handler.Groups["method"].Value;
                var targetSymbol = owner + "::" + method;
                var matches = FindTargets(bySymbol, owner, method);
                var line = source.LineAt(open);
                var sourceSymbol = "MFC message map " + mapClass;
                var command = string.Join(", ", arguments.Take(arguments.Length - 1).Select(value => value.Trim()));
                relations.Add(CreateRelation("mfc-dispatch", source, line, sourceSymbol, targetSymbol,
                    command, matches, $"{entry.Groups["macro"].Value}({command}, {targetSymbol})"));

                if (matches.Length != 1) continue;
                var definition = matches[0];
                var methodBody = definition.Source.Sanitized.Substring(definition.BodyStart, definition.BodyLength);
                foreach (Match call in QualifiedCall.Matches(methodBody))
                {
                    var callOwner = call.Groups["owner"].Value.TrimEnd(':');
                    var callMethod = call.Groups["method"].Value;
                    var callTargets = FindTargets(bySymbol, callOwner, callMethod);
                    var callLine = definition.Source.LineAt(definition.BodyStart + call.Index);
                    relations.Add(CreateRelation("qualified-native-call", definition.Source, callLine,
                        definition.Symbol, callOwner + "::" + callMethod, null, callTargets,
                        call.Groups["qualified"].Value + "(...)"));
                }
            }
        }

        var ordered = relations.DistinctBy(item => item.Id, StringComparer.Ordinal)
            .OrderBy(item => item.SourcePath, RepositoryPathComparer.Instance).ThenBy(item => item.SourceLine)
            .ThenBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.TargetSymbol, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
        return new NativeFlowAnalysis(sources.Count, ordered.Length,
            ordered.Count(item => item.Status == "unresolved"),
            ordered.Count(item => item.Status == "ambiguous"),
            Math.Max(0, ordered.Length - sampleLimit), ordered.Take(sampleLimit).ToArray(), EvidenceLimits);
    }

    private static IEnumerable<NativeDefinition> ParseDefinitions(NativeSource source)
    {
        foreach (Match match in MethodDefinition.Matches(source.Sanitized))
        {
            var open = match.Index + match.Length - 1;
            var close = FindClosingBrace(source.Sanitized, open);
            if (close < 0) continue;
            var owner = match.Groups["owner"].Value.TrimEnd(':');
            var method = match.Groups["method"].Value;
            yield return new NativeDefinition(source, owner, method, owner + "::" + method,
                source.LineAt(match.Index), open + 1, close - open - 1);
        }

        foreach (Match classMatch in ClassDefinition.Matches(source.Sanitized))
        {
            var classOpen = classMatch.Index + classMatch.Length - 1;
            var classClose = FindClosingBrace(source.Sanitized, classOpen);
            if (classClose < 0) continue;
            var classBody = source.Sanitized.Substring(classOpen + 1, classClose - classOpen - 1);
            foreach (Match methodMatch in InlineMethodDefinition.Matches(classBody))
            {
                var absoluteStart = classOpen + 1 + methodMatch.Index;
                if (BraceDepth(source.Sanitized, classOpen + 1, absoluteStart) != 0) continue;
                var open = absoluteStart + methodMatch.Length - 1;
                var close = FindClosingBrace(source.Sanitized, open);
                if (close < 0 || close > classClose) continue;
                var owner = classMatch.Groups["name"].Value;
                var method = methodMatch.Groups["method"].Value;
                yield return new NativeDefinition(source, owner, method, owner + "::" + method,
                    source.LineAt(classOpen + 1 + methodMatch.Groups["method"].Index), open + 1,
                    close - open - 1);
            }
        }
    }

    private static NativeFlowRelation CreateRelation(string kind, NativeSource source, int line, string sourceSymbol,
        string targetSymbol, string? commandId, NativeDefinition[] targets, string evidence)
    {
        var status = targets.Length switch { 0 => "unresolved", 1 => "candidate", _ => "ambiguous" };
        var target = targets.Length == 1 ? targets[0] : null;
        var identity = string.Join("\n", kind, source.Path, line, sourceSymbol, targetSymbol,
            target?.Source.Path ?? "", target?.Line.ToString() ?? "", commandId ?? "", targets.Length,
            string.Join("|", targets.Select(item => $"{item.Source.Path}:{item.Line}")));
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16].ToLowerInvariant();
        return new NativeFlowRelation(id, kind, status, source.Path, line, sourceSymbol,
            target?.Source.Path, target?.Line, targetSymbol, commandId, targets.Length, evidence);
    }

    private static NativeDefinition[] FindTargets(IReadOnlyDictionary<string, NativeDefinition[]> bySymbol,
        string owner, string method) => bySymbol.GetValueOrDefault(SymbolKey(owner, method)) ?? [];

    private static string SymbolKey(string owner, string method) => owner + "\n" + method;

    private static bool TryReadArguments(string source, int open, out string[] arguments, out int close)
    {
        var values = new List<string>();
        var start = open + 1;
        var depth = 0;
        for (var index = start; index < source.Length; index++)
        {
            switch (source[index])
            {
                case '(' or '[' or '{': depth++; break;
                case ')' when depth == 0:
                    values.Add(source[start..index]);
                    arguments = values.ToArray();
                    close = index;
                    return values.Count >= 2;
                case ')' or ']' or '}': depth--; break;
                case ',' when depth == 0:
                    values.Add(source[start..index]);
                    start = index + 1;
                    break;
            }
        }
        arguments = [];
        close = -1;
        return false;
    }

    private static int FindClosingBrace(string source, int open)
    {
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0) return index;
        }
        return -1;
    }

    private static int BraceDepth(string source, int start, int end)
    {
        var depth = 0;
        for (var index = start; index < end; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}') depth--;
        }
        return depth;
    }

    private static int[] GetLineOffsets(string[] lines)
    {
        var offsets = new int[lines.Length];
        var offset = 0;
        for (var index = 0; index < lines.Length; index++)
        {
            offsets[index] = offset;
            offset += lines[index].Length + 1;
        }
        return offsets;
    }

    private static string[] Sanitize(string[] lines)
    {
        var result = new string[lines.Length];
        var inBlockComment = false;
        var quote = '\0';
        var escaped = false;
        string? rawStringTerminator = null;
        for (var index = 0; index < lines.Length; index++)
            result[index] = ArchitecturePatternAnalyzer.RemoveCommentsAndStrings(lines[index], ref inBlockComment,
                ref quote, ref escaped, ref rawStringTerminator);
        return result;
    }

    private static int FindLine(int[] offsets, int position)
    {
        var index = Array.BinarySearch(offsets, position);
        if (index >= 0) return index;
        return Math.Max(0, ~index - 1);
    }

    private static bool IsNativeSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".c" or ".cc" or ".cpp" or ".cxx" or ".h" or ".hh" or ".hpp" or ".hxx" or ".inl" or ".ipp";

    private sealed record NativeSource(string Path, string Sanitized, int[] LineOffsets)
    {
        public int LineAt(int position) => FindLine(LineOffsets, position) + 1;
    }

    private sealed record NativeDefinition(NativeSource Source, string Owner, string Method, string Symbol,
        int Line, int BodyStart, int BodyLength);
}
