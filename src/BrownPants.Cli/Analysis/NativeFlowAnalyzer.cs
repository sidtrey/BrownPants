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

    private static readonly Regex ManagedHandleDeclaration = new(
        @"(?<type>(?:[A-Za-z_]\w*::)*[A-Za-z_]\w*(?:\s*<[^;{}=]+>)?)\s*\^\s*(?<variable>[A-Za-z_]\w*)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManagedHandleCall = new(
        @"(?<variable>[A-Za-z_]\w*)\s*->\s*(?<method>[A-Za-z_]\w*)\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManagedNamespace = new(
        @"^\s*using\s+namespace\s+(?<namespace>[A-Za-z_]\w*(?:::\w+)*)\s*;",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly string[] EvidenceLimits =
    [
        "Native flow links are lexical candidates, not compiler-resolved calls.",
        "Macro expansion, active preprocessor branches, overload binding, unqualified calls, project references, C++/CLI-to-managed compiler binding, runtime dispatch, and execution are not resolved."
    ];

    public static NativeFlowAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit,
        IReadOnlyList<ManagedMethodTarget> managedMethodIndex, bool managedMethodIndexTruncated)
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

        foreach (var definition in definitions)
        {
            var body = definition.Source.Sanitized.Substring(definition.BodyStart, definition.BodyLength);
            var handles = ManagedHandleDeclaration.Matches(body).Select(match => new ManagedHandle(
                match.Groups["type"].Value, match.Groups["variable"].Value, match.Index)).ToArray();
            if (handles.Length == 0) continue;
            var namespaces = ManagedNamespace.Matches(definition.Source.Sanitized)
                .Select(match => NormalizeNamespace(match.Groups["namespace"].Value)).Distinct(StringComparer.Ordinal).ToArray();
            foreach (Match call in ManagedHandleCall.Matches(body))
            {
                var receiver = handles.Where(item => item.Variable == call.Groups["variable"].Value && item.Position < call.Index)
                    .OrderByDescending(item => item.Position).FirstOrDefault();
                if (receiver is null) continue;
                var targets = ResolveManagedTargets(receiver.Type, call.Groups["method"].Value,
                    namespaces, managedMethodIndex);
                if (targets.TypeCount == 0) continue;
                var status = targets.Methods.Length == 0 ? "unresolved"
                    : targets.TypeCount > 1 || targets.Methods.Length > 1 ? "ambiguous" : "candidate";
                var targetSymbol = targets.Methods.Length == 1
                    ? ManagedSymbol(targets.Methods[0])
                    : $"{targets.DisplayType}.{call.Groups["method"].Value}";
                var candidateTargets = targets.Methods.Select(item => new NativeFlowTarget(
                        item.Path.Replace('\\', '/'), item.Line, ManagedSymbol(item)))
                    .DistinctBy(item => $"{item.Path}\n{item.Line}\n{item.Symbol}", StringComparer.Ordinal)
                    .OrderBy(item => item.Path, RepositoryPathComparer.Instance).ThenBy(item => item.Line)
                    .ThenBy(item => item.Symbol, StringComparer.Ordinal).ToArray();
                var line = definition.Source.LineAt(definition.BodyStart + call.Index);
                var identity = string.Join("\n", "cppcli-managed-call", definition.Source.Path, line,
                    definition.Symbol, targetSymbol, status,
                    string.Join("|", candidateTargets.Select(item => $"{item.Path}:{item.Line}:{item.Symbol}")));
                var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16].ToLowerInvariant();
                var evidence = $"{receiver.Type} {receiver.Variable}; {receiver.Variable}->{call.Groups["method"].Value}(...)";
                relations.Add(new NativeFlowRelation(id, "cppcli-managed-call", status, definition.Source.Path,
                    line, definition.Symbol, status == "candidate" ? candidateTargets[0].Path : null,
                    status == "candidate" ? candidateTargets[0].Line : null, targetSymbol, null,
                    targets.Methods.Length, evidence, candidateTargets));
            }
        }

        var ordered = relations.DistinctBy(item => item.Id, StringComparer.Ordinal)
            .OrderBy(item => item.SourcePath, RepositoryPathComparer.Instance).ThenBy(item => item.SourceLine)
            .ThenBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.TargetSymbol, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
        var evidenceLimits = managedMethodIndexTruncated
            ? EvidenceLimits.Append("The managed method target index reached its bound; additional C#/VB declarations may be missing.").ToArray()
            : EvidenceLimits;
        return new NativeFlowAnalysis(sources.Count, ordered.Length,
            ordered.Count(item => item.Status == "unresolved"),
            ordered.Count(item => item.Status == "ambiguous"),
            Math.Max(0, ordered.Length - sampleLimit), ordered.Take(sampleLimit).ToArray(), evidenceLimits,
            managedMethodIndexTruncated);
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
            target?.Source.Path, target?.Line, targetSymbol, commandId, targets.Length, evidence,
            targets.Select(item => new NativeFlowTarget(item.Source.Path, item.Line, item.Symbol))
                .OrderBy(item => item.Path, RepositoryPathComparer.Instance).ThenBy(item => item.Line)
                .ThenBy(item => item.Symbol, StringComparer.Ordinal).ToArray());
    }

    private static ManagedTargetResolution ResolveManagedTargets(string nativeType, string method,
        IReadOnlyList<string> usingNamespaces, IReadOnlyList<ManagedMethodTarget> managedMethodIndex)
    {
        var typeWithoutGeneric = nativeType.Split('<', 2)[0].Trim();
        var parts = typeWithoutGeneric.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
        var simpleName = parts.LastOrDefault() ?? string.Empty;
        var qualifiedType = NormalizeNamespace(string.Join(".", parts));
        var typeMatches = managedMethodIndex.Where(target => parts.Length > 1
                ? NameEquals(NormalizeNamespace($"{target.Namespace}.{target.TypeName}"), qualifiedType, target.Language)
                : NameEquals(target.TypeName.Split('.').Last(), simpleName, target.Language))
            .ToArray();
        if (parts.Length == 1 && usingNamespaces.Count > 0)
        {
            var importedMatches = typeMatches.Where(target => usingNamespaces.Any(ns =>
                    NameEquals(NormalizeNamespace(target.Namespace), ns, target.Language)))
                .ToArray();
            if (importedMatches.Length > 0) typeMatches = importedMatches;
        }

        var uniqueTypes = typeMatches.Select(target => $"{target.Language}\n{NormalizeNamespace(target.Namespace)}\n{target.DisplayTypeName}")
            .Distinct(StringComparer.Ordinal).ToArray();
        var methods = typeMatches.Where(target => NameEquals(target.MethodName, method, target.Language))
            .OrderBy(target => target.Path, RepositoryPathComparer.Instance).ThenBy(target => target.Line)
            .ThenBy(target => target.Language, StringComparer.Ordinal).ToArray();
        var displayType = typeMatches.Length > 0
            ? string.Join(".", ManagedSymbol(typeMatches[0]).Split('.').SkipLast(1))
            : qualifiedType;
        return new ManagedTargetResolution(uniqueTypes.Length, displayType, methods);
    }

    private static string ManagedSymbol(ManagedMethodTarget target) =>
        string.Join(".", new[] { NormalizeNamespace(target.Namespace),
                string.IsNullOrWhiteSpace(target.DisplayTypeName) ? target.TypeName : target.DisplayTypeName,
                target.MethodName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string NormalizeNamespace(string value) => value.Replace("::", ".", StringComparison.Ordinal).Trim('.');

    private static bool NameEquals(string left, string right, string language) => string.Equals(left, right,
        language == "Visual Basic" ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

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

    private sealed record ManagedHandle(string Type, string Variable, int Position);

    private sealed record ManagedTargetResolution(int TypeCount, string DisplayType, ManagedMethodTarget[] Methods);
}
