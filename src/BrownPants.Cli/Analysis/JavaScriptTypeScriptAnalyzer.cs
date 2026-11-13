using System.Text.Json;

namespace BrownPants.Cli.Analysis;

/// <summary>Bounded, dependency-free syntax evidence for common JavaScript and TypeScript constructs.</summary>
public static class JavaScriptTypeScriptAnalyzer
{
    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".js", ".jsx", ".mjs", ".cjs", ".ts", ".tsx", ".mts", ".cts" };
    private static readonly HashSet<string> NonCallWords = new(StringComparer.Ordinal)
        { "if", "for", "while", "switch", "catch", "function", "with", "typeof", "void", "delete", "new", "return", "throw", "import", "class" };
    private static readonly string[] ModuleExtensions = { ".ts", ".tsx", ".mts", ".cts", ".js", ".jsx", ".mjs", ".cjs" };
    private static StringComparer PathStringComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private const int MaxSourceCharacters = 10 * 1024 * 1024;
    private const int MaxTokensPerFile = 1_000_000;
    private const int MaxSourceFiles = 20_000;
    private const int MaxResolutionMetadataFiles = 20_000;
    private const int MaxMetadataFileBytes = 1024 * 1024;
    private const int MaxTotalMetadataBytes = 32 * 1024 * 1024;
    private const int MaxTotalSourceCharacters = 100 * 1024 * 1024;
    private const int MaxTotalTokens = 5_000_000;
    private const int MaxRecordsPerKind = 100_000;
    private const int MaxIssues = 10_000;
    private static readonly JsonDocumentOptions JsonOptions = new()
    { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip, MaxDepth = 64 };

    public static JavaScriptTypeScriptAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit)
        => Analyze(root, sourcePaths, sampleLimit, MaxSourceFiles, MaxTotalSourceCharacters,
            MaxTotalTokens, MaxRecordsPerKind, MaxIssues);

    internal static JavaScriptTypeScriptAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit,
        int maxSourceFiles, int maxTotalSourceCharacters, int maxTotalTokens, int maxRecordsPerKind, int maxIssues)
    {
        if (sampleLimit < 1 || maxSourceFiles < 1 || maxTotalSourceCharacters < 1 || maxTotalTokens < 1
            || maxRecordsPerKind < 1 || maxIssues < 1)
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "Analysis limits must be positive.");
        var allPaths = sourcePaths.Distinct(StringComparer.Ordinal).OrderBy(path => path, RepositoryPathComparer.Instance).ToArray();
        var candidatePaths = allPaths.Where(path => SourceExtensions.Contains(Path.GetExtension(path))).ToArray();
        var paths = candidatePaths.Take(maxSourceFiles).ToArray();
        var metadataCandidates = allPaths.Where(IsResolutionMetadataPath).ToArray();
        var metadataPaths = metadataCandidates.Take(MaxResolutionMetadataFiles).ToArray();
        var resolutionPaths = paths.Concat(metadataPaths).Distinct(PathStringComparer)
            .OrderBy(path => path, RepositoryPathComparer.Instance).ToArray();
        var units = new List<SourceUnit>();
        var issues = new IssueCollector(maxIssues);
        if (candidatePaths.Length > paths.Length)
            issues.Add(new JavaScriptTypeScriptIssue(candidatePaths[paths.Length], "SourceFileLimit"));
        if (metadataCandidates.Length > metadataPaths.Length)
            issues.Add(new JavaScriptTypeScriptIssue(metadataCandidates[metadataPaths.Length], "ResolutionMetadataFileLimit"));
        long totalSourceCharacters = 0;
        var totalTokens = 0;
        var processedFileCount = 0;
        var declarationsRetained = 0;
        var moduleSpecsRetained = 0;
        var callsRetained = 0;
        foreach (var path in paths)
        {
            processedFileCount++;
            try
            {
                var fullPath = ResolveUnderRoot(root, path);
                if (new FileInfo(fullPath).Length > MaxSourceCharacters * 4L)
                {
                    issues.Add(new JavaScriptTypeScriptIssue(path, "SourceSizeLimit"));
                    continue;
                }
                var source = File.ReadAllText(fullPath);
                if (source.Length > MaxSourceCharacters)
                {
                    issues.Add(new JavaScriptTypeScriptIssue(path, "SourceSizeLimit"));
                    continue;
                }
                if (source.Length > maxTotalSourceCharacters - totalSourceCharacters)
                {
                    issues.Add(new JavaScriptTypeScriptIssue(path, "TotalSourceSizeLimit"));
                    break;
                }
                totalSourceCharacters += source.Length;
                var tokenized = Tokenize(source);
                issues.AddRange(tokenized.IssueCodes.Select(code => new JavaScriptTypeScriptIssue(path, code)));
                var tokens = tokenized.Tokens;
                if (tokens.Count > MaxTokensPerFile)
                {
                    issues.Add(new JavaScriptTypeScriptIssue(path, "TokenLimit"));
                    continue;
                }
                if (tokens.Count > maxTotalTokens - totalTokens)
                {
                    issues.Add(new JavaScriptTypeScriptIssue(path, "TotalTokenLimit"));
                    break;
                }
                totalTokens += tokens.Count;
                var unit = ParseUnit(path, tokens, maxRecordsPerKind - declarationsRetained,
                    maxRecordsPerKind - moduleSpecsRetained, maxRecordsPerKind - callsRetained);
                units.Add(unit);
                declarationsRetained += unit.Declarations.Count;
                moduleSpecsRetained += unit.Modules.Count;
                callsRetained += unit.Calls.Count;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or JsonException)
            {
                issues.Add(new JavaScriptTypeScriptIssue(path, "SourceReadFailed"));
            }
        }

        var allDeclarations = units.SelectMany(unit => unit.Declarations).ToArray();
        var modules = ResolveModules(root, units, resolutionPaths, issues);
        var calls = ResolveCalls(units, allDeclarations, modules);
        var sortedDeclarations = allDeclarations.OrderBy(item => item.Path, RepositoryPathComparer.Instance)
            .ThenBy(item => item.Line).ThenBy(item => item.Column).ThenBy(item => item.Name, StringComparer.Ordinal).ToArray();
        var sortedModules = modules.OrderBy(item => item.SourcePath, RepositoryPathComparer.Instance)
            .ThenBy(item => item.SourceLine).ThenBy(item => item.SourceColumn).ThenBy(item => item.Specifier, StringComparer.Ordinal).ToArray();
        var sortedCalls = calls.OrderBy(item => item.SourcePath, RepositoryPathComparer.Instance)
            .ThenBy(item => item.SourceLine).ThenBy(item => item.SourceColumn).ThenBy(item => item.Expression, StringComparer.Ordinal).ToArray();
        var recordLimitReached = units.Any(unit => unit.RecordLimitReached)
            || sortedDeclarations.Length > maxRecordsPerKind
            || sortedModules.Length > maxRecordsPerKind || sortedCalls.Length > maxRecordsPerKind;
        var orderedDeclarations = sortedDeclarations.Take(maxRecordsPerKind).ToArray();
        var orderedModules = sortedModules.Take(maxRecordsPerKind).ToArray();
        var orderedCalls = sortedCalls.Take(maxRecordsPerKind).ToArray();
        if (recordLimitReached) issues.Add(new JavaScriptTypeScriptIssue(string.Empty, "AggregateRecordLimit"));
        var limits = new List<string>
        {
            "JavaScript/TypeScript records are produced by bounded syntax recognizers, not a standards-complete parser or compiler.",
            "Syntax-resolved calls do not prove TypeScript type correctness, overload binding, runtime dispatch, module execution, or installed dependencies.",
            $"Analysis caps: {maxSourceFiles:N0} source files, {MaxResolutionMetadataFiles:N0} workspace/config files, {MaxMetadataFileBytes:N0} bytes per metadata file, {MaxTotalMetadataBytes:N0} aggregate metadata bytes, {maxTotalSourceCharacters:N0} total source characters, {maxTotalTokens:N0} total tokens, {maxRecordsPerKind:N0} records per category, and {maxIssues:N0} diagnostics. A corresponding issue identifies a reached cap; reported record or diagnostic counts are lower bounds when their cap is present."
        };
        var orderedIssues = issues.ToOutput();
        return new JavaScriptTypeScriptAnalysis("Bounded JavaScript/TypeScript syntax analysis", processedFileCount,
            orderedDeclarations.Length, orderedModules.Length, orderedCalls.Length,
            orderedDeclarations.Take(sampleLimit).ToArray(), orderedModules.Take(sampleLimit).ToArray(), orderedCalls.Take(sampleLimit).ToArray(),
            Math.Max(0, orderedDeclarations.Length - sampleLimit), Math.Max(0, orderedModules.Length - sampleLimit),
            Math.Max(0, orderedCalls.Length - sampleLimit), orderedIssues.Length, Math.Max(0, orderedIssues.Length - sampleLimit),
            orderedIssues.Take(sampleLimit).ToArray(), limits);
    }

    private static bool IsResolutionMetadataPath(string path)
    {
        var name = Path.GetFileName(path);
        return name.Equals("package.json", StringComparison.OrdinalIgnoreCase)
            || name.Equals("pnpm-workspace.yaml", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("tsconfig", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static SourceUnit ParseUnit(string path, IReadOnlyList<JavaScriptTypeScriptToken> tokens,
        int declarationLimit, int moduleLimit, int callLimit)
    {
        var declarations = new List<JavaScriptTypeScriptDeclaration>();
        var moduleSpecs = new List<ModuleSpec>();
        var calls = new List<CallSite>();
        var braceDepth = new int[tokens.Count];
        var depth = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            braceDepth[index] = depth;
            if (tokens[index].Text == "}") depth = Math.Max(0, depth - 1);
            if (tokens[index].Text == "{") depth++;
        }

        var containers = FindClassContainers(tokens);
        var ordinaryFunctionBodies = FindOrdinaryFunctionBodies(tokens);
        var insideOrdinaryFunction = new bool[tokens.Count];
        var functionDepthChanges = new int[tokens.Count + 1];
        foreach (var (start, end) in ordinaryFunctionBodies)
        { functionDepthChanges[start]++; functionDepthChanges[end]--; }
        var functionDepth = 0;
        for (var index = 0; index < tokens.Count; index++)
        { functionDepth += functionDepthChanges[index]; insideOrdinaryFunction[index] = functionDepth > 0; }
        var methodDeclarationIndices = new HashSet<int>();
        for (var index = 0; index < tokens.Count; index++)
            if (tokens[index].Kind == "identifier" && IsClassMethodDeclaration(tokens, index, containers, braceDepth))
                methodDeclarationIndices.Add(index);
        var orderedContainers = containers.OrderBy(item => item.Start).ThenByDescending(item => item.End).ToArray();
        var containerStartAtIndex = new int?[tokens.Count];
        var activeContainers = new List<(string Name, int Start, int End)>();
        var nextContainer = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            while (nextContainer < orderedContainers.Length && orderedContainers[nextContainer].Start < tokens[index].Offset)
            {
                var candidate = orderedContainers[nextContainer++];
                if (candidate.End > tokens[index].Offset) activeContainers.Add(candidate);
            }
            while (activeContainers.Count > 0 && activeContainers[^1].End <= tokens[index].Offset)
                activeContainers.RemoveAt(activeContainers.Count - 1);
            if (activeContainers.Count > 0)
            {
                containerStartAtIndex[index] = activeContainers[^1].Start;
            }
        }
        var parameterScopes = FindParameterScopes(tokens, methodDeclarationIndices);
        var shadowScopes = parameterScopes.Concat(FindLocalBindingScopes(tokens, parameterScopes)).ToArray();
        var scopesByStart = shadowScopes.GroupBy(scope => scope.Start).ToDictionary(group => group.Key, group => group.ToArray());
        var scopesByEnd = shadowScopes.GroupBy(scope => scope.End).ToDictionary(group => group.Key, group => group.ToArray());
        var activeShadowedNames = new Dictionary<string, int>(StringComparer.Ordinal);
        var activeShadowedSnapshot = Array.Empty<string>();
        var methodContainerStarts = new Dictionary<(int Line, int Column, string Name), int>();
        var recordLimitReached = false;
        void AddBoundedDeclaration(string kind, JavaScriptTypeScriptToken declarationToken, string? exportKind = null)
        {
            if (declarations.Count >= declarationLimit) { recordLimitReached = true; return; }
            AddDeclaration(path, kind, declarationToken, containers, declarations, exportKind);
        }
        void AddBoundedModule(ModuleSpec spec)
        {
            if (moduleSpecs.Count >= moduleLimit) { recordLimitReached = true; return; }
            moduleSpecs.Add(spec);
        }
        void AddBoundedCall(CallSite call)
        {
            if (calls.Count >= callLimit) { recordLimitReached = true; return; }
            calls.Add(call);
        }
        for (var index = 0; index < tokens.Count; index++)
        {
            var parametersChanged = false;
            if (scopesByEnd.TryGetValue(index, out var endingScopes))
                foreach (var name in endingScopes.SelectMany(scope => scope.Names))
                {
                    if (activeShadowedNames[name] == 1) activeShadowedNames.Remove(name);
                    else activeShadowedNames[name]--;
                    parametersChanged = true;
                }
            if (scopesByStart.TryGetValue(index, out var startingScopes))
                foreach (var name in startingScopes.SelectMany(scope => scope.Names))
                {
                    activeShadowedNames[name] = activeShadowedNames.GetValueOrDefault(name) + 1;
                    parametersChanged = true;
                }
            if (parametersChanged) activeShadowedSnapshot = activeShadowedNames.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();

            var token = tokens[index];
            if (token.Text == "function")
            {
                var nameIndex = index + 1;
                if (nameIndex < tokens.Count && tokens[nameIndex].Text == "*") nameIndex++;
                if (nameIndex < tokens.Count && tokens[nameIndex].Kind == "identifier")
                    AddBoundedDeclaration("function", tokens[nameIndex], GetExportKind(tokens, index));
            }
            else if (token.Text is "class" or "interface" or "type" or "enum"
                && !(token.Text == "type" && IsImportExportTypeSpecifier(tokens, index)))
            {
                if (index + 1 < tokens.Count && tokens[index + 1].Kind == "identifier")
                    AddBoundedDeclaration(token.Text, tokens[index + 1], GetExportKind(tokens, index));
            }
            else if ((token.Text is "const" or "let" or "var") && index + 1 < tokens.Count && tokens[index + 1].Kind == "identifier")
            {
                var name = tokens[index + 1];
                if (ContainsArrowBeforeStatementEnd(tokens, index + 2))
                    AddBoundedDeclaration("callable variable", name, GetExportKind(tokens, index));
            }

            if (methodDeclarationIndices.Contains(index))
            {
                var countBefore = declarations.Count;
                AddBoundedDeclaration("method", token);
                if (declarations.Count > countBefore && containerStartAtIndex[index] is int containerStart)
                    methodContainerStarts[(token.Line, token.Column, token.Text)] = containerStart;
            }

            if (token.Text == "import")
            {
                if (index + 1 < tokens.Count && tokens[index + 1].Text == "(")
                {
                    var stringToken = index + 2 < tokens.Count && tokens[index + 2].Kind == "string" ? tokens[index + 2] : null;
                    AddBoundedModule(new ModuleSpec(token, stringToken?.Text, "dynamic-import", null, "DynamicImport"));
                }
                else
                {
                    var end = FindStatementEnd(tokens, index);
                    var from = IndexOf(tokens, "from", index + 1, end);
                    var specToken = from >= 0 && from + 1 < tokens.Count && tokens[from + 1].Kind == "string"
                        ? tokens[from + 1]
                        : index + 1 < tokens.Count && tokens[index + 1].Kind == "string" ? tokens[index + 1] : null;
                    if (specToken is not null)
                    {
                        var bindings = from >= 0 ? ParseImportBindings(tokens, index + 1, from) : Array.Empty<string>();
                        AddBoundedModule(new ModuleSpec(token, specToken.Text, "import", bindings, null));
                    }
                }
            }
            else if (token.Text == "export")
            {
                var end = FindStatementEnd(tokens, index);
                var from = IndexOf(tokens, "from", index + 1, end);
                if (from >= 0 && from + 1 < tokens.Count && tokens[from + 1].Kind == "string")
                    AddBoundedModule(new ModuleSpec(token, tokens[from + 1].Text, "re-export",
                        ParseReExportBindings(tokens, index + 1, from), null));
            }
            else if (token.Text == "require" && index + 2 < tokens.Count && tokens[index + 1].Text == "(" && tokens[index + 2].Kind == "string")
            {
                AddBoundedModule(new ModuleSpec(token, tokens[index + 2].Text, "require", null, null));
            }

            if (token.Kind == "identifier" && FindCallOpenParen(tokens, index) >= 0
                && !NonCallWords.Contains(token.Text) && !IsDeclarationName(tokens, index)
                && !(token.Text == "require" && index + 2 < tokens.Count && tokens[index + 2].Kind == "string")
                && !methodDeclarationIndices.Contains(index)
                && (index == 0 || tokens[index - 1].Text is not "function"))
            {
                var caller = FindEnclosingCallable(tokens, index, declarations);
                var expression = index >= 2 && tokens[index - 1].Text is "." or "?." && tokens[index - 2].Kind == "identifier"
                    ? $"{tokens[index - 2].Text}.{token.Text}" : token.Text;
                AddBoundedCall(new CallSite(token, caller, expression,
                    token.Text == "require" ? "DynamicRequire" : null, containerStartAtIndex[index],
                    activeShadowedSnapshot, insideOrdinaryFunction[index]));
            }
            else if (token.Text == "import" && index + 1 < tokens.Count && tokens[index + 1].Text == "(")
            {
                AddBoundedCall(new CallSite(token, FindEnclosingCallable(tokens, index, declarations), "import", "DynamicImport",
                    containerStartAtIndex[index], activeShadowedSnapshot, insideOrdinaryFunction[index]));
            }
        }

        foreach (var token in tokens.Where(item => item.Text == "=>"))
        {
            var name = FindAssignedArrowName(tokens, token);
            if (name is not null && !declarations.Any(item => item.Line == name.Line && item.Column == name.Column))
                AddBoundedDeclaration("callable variable", name,
                    GetExportKind(tokens, FindTokenIndex(tokens, name)));
        }
        return new SourceUnit(path, declarations, moduleSpecs, calls, methodContainerStarts, recordLimitReached);
    }

    private static TokenizationResult Tokenize(string source)
    {
        var tokens = new List<JavaScriptTypeScriptToken>();
        var issueCodes = new HashSet<string>(StringComparer.Ordinal);
        var line = 1;
        var column = 1;
        var jsxText = false;
        var jsxExpressionDepth = 0;
        for (var offset = 0; offset < source.Length;)
        {
            var current = source[offset];
            if (jsxText)
            {
                if (current == '<') jsxText = false;
                else if (current == '{') { jsxText = false; jsxExpressionDepth = 1; }
                else { Advance(current, ref line, ref column); offset++; continue; }
            }
            if (char.IsWhiteSpace(current)) { Advance(current, ref line, ref column); offset++; continue; }
            if (current == '/' && offset + 1 < source.Length && source[offset + 1] == '/')
            {
                while (offset < source.Length && source[offset] != '\n') { Advance(source[offset], ref line, ref column); offset++; }
                continue;
            }
            if (current == '/' && offset + 1 < source.Length && source[offset + 1] == '*')
            {
                Advance(source[offset++], ref line, ref column); Advance(source[offset++], ref line, ref column);
                var closed = false;
                while (offset < source.Length)
                {
                    if (source[offset] == '*' && offset + 1 < source.Length && source[offset + 1] == '/')
                    { Advance(source[offset++], ref line, ref column); Advance(source[offset++], ref line, ref column); closed = true; break; }
                    Advance(source[offset], ref line, ref column); offset++;
                }
                if (!closed) issueCodes.Add("UnterminatedBlockComment");
                continue;
            }
            if (current == '/' && CanStartRegularExpression(tokens))
            {
                var regexStart = offset; var regexLine = line; var regexColumn = column;
                Advance(current, ref line, ref column); offset++;
                var inCharacterClass = false; var escaped = false; var closed = false;
                while (offset < source.Length && source[offset] is not '\r' and not '\n')
                {
                    current = source[offset];
                    if (escaped) escaped = false;
                    else if (current == '\\') escaped = true;
                    else if (current == '[') inCharacterClass = true;
                    else if (current == ']') inCharacterClass = false;
                    else if (current == '/' && !inCharacterClass) { closed = true; Advance(current, ref line, ref column); offset++; break; }
                    Advance(current, ref line, ref column); offset++;
                }
                if (closed)
                {
                    while (offset < source.Length && char.IsLetter(source[offset])) { Advance(source[offset], ref line, ref column); offset++; }
                    tokens.Add(new JavaScriptTypeScriptToken("<regex>", "regex", regexStart, regexLine, regexColumn));
                }
                else issueCodes.Add("UnterminatedRegularExpression");
                continue;
            }
            var start = offset; var startLine = line; var startColumn = column;
            if (current is '\'' or '"' or '`')
            {
                var quote = current; Advance(current, ref line, ref column); offset++;
                var value = new System.Text.StringBuilder();
                var closed = false;
                while (offset < source.Length)
                {
                    current = source[offset];
                    if (current == '\\' && offset + 1 < source.Length)
                    {
                        Advance(source[offset++], ref line, ref column); current = source[offset]; value.Append(current);
                        Advance(current, ref line, ref column); offset++; continue;
                    }
                    if (current == quote) { Advance(current, ref line, ref column); offset++; closed = true; break; }
                    if (quote != '`' && current is '\r' or '\n') break;
                    value.Append(current); Advance(current, ref line, ref column); offset++;
                }
                if (closed) tokens.Add(new JavaScriptTypeScriptToken(value.ToString(), quote == '`' ? "template" : "string", start, startLine, startColumn));
                else issueCodes.Add(quote == '`' ? "UnterminatedTemplateLiteral" : "UnterminatedStringLiteral");
                continue;
            }
            if (char.IsLetter(current) || current is '_' or '$')
            {
                offset++; Advance(current, ref line, ref column);
                while (offset < source.Length && (char.IsLetterOrDigit(source[offset]) || source[offset] is '_' or '$'))
                { Advance(source[offset], ref line, ref column); offset++; }
                tokens.Add(new JavaScriptTypeScriptToken(source[start..offset], "identifier", start, startLine, startColumn));
                continue;
            }
            if (char.IsDigit(current))
            {
                offset++; Advance(current, ref line, ref column);
                while (offset < source.Length && (char.IsLetterOrDigit(source[offset]) || source[offset] == '.'))
                { Advance(source[offset], ref line, ref column); offset++; }
                tokens.Add(new JavaScriptTypeScriptToken(source[start..offset], "number", start, startLine, startColumn));
                continue;
            }
            var symbol = offset + 2 < source.Length && source.Substring(offset, 3) is "..." or "===" or "!==" ? source.Substring(offset, 3)
                : offset + 1 < source.Length && source.Substring(offset, 2) is "=>" or "?." or "??" or "==" or "!=" or "&&" or "||" or "++" or "--" or "**" or "<=" or ">=" ? source.Substring(offset, 2)
                : current.ToString();
            foreach (var character in symbol) { Advance(character, ref line, ref column); offset++; }
            tokens.Add(new JavaScriptTypeScriptToken(symbol, "symbol", start, startLine, startColumn));
            if (jsxExpressionDepth > 0)
            {
                if (symbol == "{") jsxExpressionDepth++;
                else if (symbol == "}" && --jsxExpressionDepth == 0) jsxText = true;
            }
            else if (symbol == ">" && LooksLikeJsxTextStart(tokens, source, offset)) jsxText = true;
        }
        ValidateDelimiters(tokens, issueCodes);
        return new TokenizationResult(tokens, issueCodes.Order(StringComparer.Ordinal).ToArray());
    }

    private static void ValidateDelimiters(IReadOnlyList<JavaScriptTypeScriptToken> tokens, HashSet<string> issueCodes)
    {
        var stack = new Stack<string>();
        foreach (var token in tokens)
        {
            if (token.Text is "(" or "[" or "{") stack.Push(token.Text);
            else if (token.Text is ")" or "]" or "}")
            {
                var expected = token.Text switch { ")" => "(", "]" => "[", _ => "{" };
                if (stack.Count == 0 || stack.Pop() != expected) issueCodes.Add("UnbalancedDelimiter");
            }
        }
        if (stack.Count > 0) issueCodes.Add("UnbalancedDelimiter");
    }

    private static bool CanStartRegularExpression(IReadOnlyList<JavaScriptTypeScriptToken> tokens)
    {
        if (tokens.Count == 0) return true;
        var previous = tokens[^1];
        return previous.Text is "=" or "(" or "[" or "{" or "," or ":" or ";" or "!" or "?" or "=>" or "return" or "case";
    }

    private static bool LooksLikeJsxTextStart(IReadOnlyList<JavaScriptTypeScriptToken> tokens, string source, int offset)
    {
        if (tokens.Count < 3 || tokens[^2].Text == "/") return false;
        var open = tokens.Count - 3;
        while (open >= 0 && tokens[open].Text != "<" && tokens[open].Text is not ";" and not "{" and not "}") open--;
        if (open < 0 || open + 1 >= tokens.Count || tokens[open + 1].Kind != "identifier") return false;
        var next = offset;
        while (next < source.Length && char.IsWhiteSpace(source[next])) next++;
        if (next >= source.Length) return false;
        return source[next] is not '(' and not ';' and not ')' and not ',' and not '=' and not '>';
    }

    private static List<JavaScriptTypeScriptModuleRelationship> ResolveModules(string root, IReadOnlyList<SourceUnit> units,
        IReadOnlyList<string> allPaths, IssueCollector issues)
    {
        var result = new List<JavaScriptTypeScriptModuleRelationship>();
        var pathSet = allPaths.ToHashSet(PathStringComparer);
        var metadataReader = new MetadataReadBudget(root, MaxMetadataFileBytes, MaxTotalMetadataBytes, issues);
        var workspacePackages = ReadWorkspacePackages(root, allPaths, issues, metadataReader);
        var aliases = ReadPathAliases(root, allPaths, issues, metadataReader);
        foreach (var unit in units)
        foreach (var spec in unit.Modules)
        {
            if (spec.Specifier is null || spec.ForcedReason is not null)
            {
                result.Add(new JavaScriptTypeScriptModuleRelationship(unit.Path, spec.Token.Line, spec.Token.Column,
                    spec.Specifier ?? "", spec.Kind, "unresolved", null, Array.Empty<string>(), spec.ForcedReason ?? "DynamicImport"));
                continue;
            }
            var resolution = ResolveSpecifier(root, unit.Path, spec.Specifier, pathSet, workspacePackages, aliases, issues);
            var targets = resolution.Targets;
            var status = targets.Count switch { 1 => "syntax-resolved", > 1 => "ambiguous", _ => "unresolved" };
            result.Add(new JavaScriptTypeScriptModuleRelationship(unit.Path, spec.Token.Line, spec.Token.Column,
                spec.Specifier, spec.Kind, status, targets.Count == 1 ? targets[0] : null,
                targets.Count > 1 ? targets : Array.Empty<string>(), targets.Count > 1 ? "MultipleModuleTargets" : resolution.Reason));
        }
        return result;
    }

    private static ModuleTargetResolution ResolveSpecifier(string root, string sourcePath, string specifier,
        HashSet<string> paths, IReadOnlyDictionary<string, IReadOnlyList<WorkspacePackage>> workspacePackages,
        IReadOnlyList<PathAlias> aliases, IssueCollector issues)
    {
        var basePaths = new List<string>();
        if (specifier.StartsWith(".", StringComparison.Ordinal))
            basePaths.Add(Path.GetFullPath(Path.Combine(root, Path.GetDirectoryName(sourcePath) ?? "", specifier)));
        else
        {
            var matchingAlias = aliases.Where(alias => alias.Matches(specifier, sourcePath))
                .OrderByDescending(alias => alias.ConfigDirectory.Length)
                .ThenByDescending(alias => alias.Pattern.Length).ToArray();
            if (matchingAlias.Length > 0)
            {
                var nearestConfigLength = matchingAlias[0].ConfigDirectory.Length;
                matchingAlias = matchingAlias.Where(alias => alias.ConfigDirectory.Length == nearestConfigLength).ToArray();
            }
            var aliasCandidates = matchingAlias.SelectMany(alias => alias.Expand(specifier))
                .Select(candidate => SafeFullPath(root, candidate)).Where(candidate => candidate is not null)
                .SelectMany(candidate => CandidateModulePaths(root, candidate!, paths)).Distinct(PathStringComparer)
                .OrderBy(path => path, RepositoryPathComparer.Instance).ToArray();
            if (aliasCandidates.Length > 0) return new ModuleTargetResolution(aliasCandidates, null);
            if (matchingAlias.Length > 0) return new ModuleTargetResolution(Array.Empty<string>(), "TsConfigAliasTargetNotFound");

            var packageNameEnd = specifier.StartsWith('@') ? specifier.IndexOf('/', specifier.IndexOf('/') + 1) : specifier.IndexOf('/');
            var packageName = packageNameEnd < 0 ? specifier : specifier[..packageNameEnd];
            var packageSubpath = packageNameEnd < 0 ? string.Empty : specifier[(packageNameEnd + 1)..];
            if (!workspacePackages.TryGetValue(packageName, out var workspacePackage))
                return new ModuleTargetResolution(Array.Empty<string>(), "ExternalOrUndeclaredPackage");
            if (packageSubpath.Length == 0 && workspacePackage.Any(candidate => candidate.EntryPath is null))
                return new ModuleTargetResolution(Array.Empty<string>(), "PackageExportsSelectionUnsupported");
            basePaths.AddRange(workspacePackage.Select(candidate => Path.GetFullPath(Path.Combine(root,
                candidate.RootPath, packageSubpath.Length > 0 ? packageSubpath : candidate.EntryPath!))));
        }
        if (basePaths.Any(path => !IsUnderRoot(root, path))) return new ModuleTargetResolution(Array.Empty<string>(), "TargetOutsideScanRoot");
        var targets = basePaths.SelectMany(basePath => CandidateModulePaths(root, basePath, paths))
            .Distinct(PathStringComparer).OrderBy(path => path, RepositoryPathComparer.Instance).ToArray();
        return new ModuleTargetResolution(targets, targets.Length == 0 ? "ModuleTargetNotFound" : null);
    }

    private static IEnumerable<string> CandidateModulePaths(string root, string basePath, HashSet<string> paths)
    {
        var candidates = new List<string>();
        if (Path.HasExtension(basePath))
        {
            var exact = Path.GetRelativePath(root, basePath).Replace(Path.DirectorySeparatorChar, '/');
            if (paths.Contains(exact)) return new[] { exact };
            var substitutions = Path.GetExtension(basePath).ToLowerInvariant() switch
            {
                ".js" => new[] { ".ts", ".tsx", ".d.ts" },
                ".jsx" => new[] { ".tsx", ".ts" },
                ".mjs" => new[] { ".mts" },
                ".cjs" => new[] { ".cts" },
                _ => Array.Empty<string>()
            };
            candidates.AddRange(substitutions.Select(extension => Path.ChangeExtension(basePath, extension))
                .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')));
        }
        else
        {
            candidates.AddRange(ModuleExtensions.Select(extension => Path.GetRelativePath(root, basePath + extension).Replace('\\', '/')));
            candidates.AddRange(ModuleExtensions.Select(extension => Path.GetRelativePath(root, Path.Combine(basePath, "index" + extension)).Replace('\\', '/')));
        }
        return candidates.Where(paths.Contains).Distinct(PathStringComparer);
    }

    internal static IReadOnlyDictionary<string, IReadOnlyList<WorkspacePackage>> ReadWorkspacePackages(string root,
        IReadOnlyList<string> paths, List<JavaScriptTypeScriptIssue> issues)
    {
        var collector = new IssueCollector(int.MaxValue);
        var reader = new MetadataReadBudget(root, MaxMetadataFileBytes, MaxTotalMetadataBytes, collector);
        var result = ReadWorkspacePackages(root, paths, collector, reader);
        issues.AddRange(collector.ToOutput());
        return result;
    }

    internal static IReadOnlyDictionary<string, IReadOnlyList<WorkspacePackage>> ReadWorkspacePackages(string root,
        IReadOnlyList<string> paths, List<JavaScriptTypeScriptIssue> issues, int maxMetadataFileBytes, int maxTotalMetadataBytes)
    {
        var collector = new IssueCollector(int.MaxValue);
        var reader = new MetadataReadBudget(root, maxMetadataFileBytes, maxTotalMetadataBytes, collector);
        var result = ReadWorkspacePackages(root, paths, collector, reader);
        issues.AddRange(collector.ToOutput());
        return result;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<WorkspacePackage>> ReadWorkspacePackages(string root,
        IReadOnlyList<string> paths, IssueCollector issues, MetadataReadBudget metadataReader)
    {
        var manifests = paths.Where(path => Path.GetFileName(path).Equals("package.json", StringComparison.OrdinalIgnoreCase)).ToArray();
        var workspaceDeclared = File.Exists(Path.Combine(root, "pnpm-workspace.yaml"));
        var patterns = new List<string>();
        var rootPackage = manifests.FirstOrDefault(path => Path.GetDirectoryName(path) is null or "");
        if (rootPackage is not null)
        {
            try
            {
                if (!metadataReader.TryRead(rootPackage, out var rootPackageText))
                    return new Dictionary<string, IReadOnlyList<WorkspacePackage>>(StringComparer.Ordinal);
                using var document = JsonDocument.Parse(rootPackageText, JsonOptions);
                if (document.RootElement.TryGetProperty("workspaces", out var workspaces))
                {
                    workspaceDeclared = true;
                    var entries = workspaces.ValueKind == JsonValueKind.Array ? workspaces :
                        workspaces.ValueKind == JsonValueKind.Object && workspaces.TryGetProperty("packages", out var nested) ? nested : default;
                    if (entries.ValueKind == JsonValueKind.Array)
                        patterns.AddRange(entries.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString()!));
                }
            }
            catch (JsonException) { issues.Add(new JavaScriptTypeScriptIssue(rootPackage, "InvalidPackageJson")); }
        }
        var pnpm = paths.FirstOrDefault(path => Path.GetFileName(path).Equals("pnpm-workspace.yaml", StringComparison.OrdinalIgnoreCase));
        if (pnpm is not null)
        {
            if (!metadataReader.TryRead(pnpm, out var pnpmText))
                return new Dictionary<string, IReadOnlyList<WorkspacePackage>>(StringComparer.Ordinal);
            foreach (var line in pnpmText.Split('\n'))
            {
                var trimmed = line.Trim().Trim('-', ' ', '\'', '"');
                if (trimmed.Length > 0 && !trimmed.EndsWith(':')) patterns.Add(trimmed);
            }
        }
        var includePatterns = patterns.Where(pattern => !pattern.StartsWith('!')).ToArray();
        var excludePatterns = patterns.Where(pattern => pattern.StartsWith('!')).Select(pattern => pattern[1..]).ToArray();
        if (!workspaceDeclared || includePatterns.Length == 0) return new Dictionary<string, IReadOnlyList<WorkspacePackage>>(StringComparer.Ordinal);

        var result = new Dictionary<string, List<WorkspacePackage>>(StringComparer.Ordinal);
        foreach (var manifest in manifests.Where(path => path != rootPackage))
        {
            var packageDirectory = Path.GetDirectoryName(manifest)?.Replace('\\', '/') ?? string.Empty;
            if (!includePatterns.Any(pattern => WorkspacePatternMatches(pattern, packageDirectory))
                || excludePatterns.Any(pattern => WorkspacePatternMatches(pattern, packageDirectory))) continue;
            try
            {
                if (!metadataReader.TryRead(manifest, out var manifestText)) continue;
                using var document = JsonDocument.Parse(manifestText, JsonOptions);
                var element = document.RootElement;
                if (!element.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String) continue;
                string? entry = element.TryGetProperty("exports", out _) ? null : "index";
                if (entry is not null)
                    foreach (var property in new[] { "source", "types", "module", "main" })
                        if (element.TryGetProperty(property, out var target) && target.ValueKind == JsonValueKind.String)
                        { entry = target.GetString()!; break; }
                var name = nameValue.GetString()!;
                if (!result.TryGetValue(name, out var packages)) result[name] = packages = new List<WorkspacePackage>();
                packages.Add(new WorkspacePackage(packageDirectory, entry));
            }
            catch (JsonException) { issues.Add(new JavaScriptTypeScriptIssue(manifest, "InvalidPackageJson")); }
        }
        return result.ToDictionary(item => item.Key,
            item => (IReadOnlyList<WorkspacePackage>)item.Value.OrderBy(package => package.RootPath, RepositoryPathComparer.Instance).ToArray(),
            StringComparer.Ordinal);
    }

    private static IReadOnlyList<PathAlias> ReadPathAliases(string root, IReadOnlyList<string> paths,
        IssueCollector issues, MetadataReadBudget metadataReader)
    {
        var result = new List<PathAlias>();
        var pathSet = paths.ToHashSet(PathStringComparer);
        foreach (var config in paths.Where(path => Path.GetFileName(path).Equals("tsconfig.json", StringComparison.OrdinalIgnoreCase)))
        {
            result.AddRange(ReadTsConfigAliases(root, config, pathSet, issues, metadataReader,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0));
        }
        return result;
    }

    private static IReadOnlyList<PathAlias> ReadTsConfigAliases(string root, string config, HashSet<string> paths,
        IssueCollector issues, MetadataReadBudget metadataReader, HashSet<string> visiting, int depth)
    {
        if (depth > 8 || !visiting.Add(config))
        { issues.Add(new JavaScriptTypeScriptIssue(config, "TsConfigExtendsCycleOrDepthLimit")); return Array.Empty<PathAlias>(); }
        try
        {
            var configFullPath = Path.Combine(root, config);
            if (!metadataReader.TryRead(config, out var configText)) return Array.Empty<PathAlias>();
            using var document = JsonDocument.Parse(configText, JsonOptions);
            var inherited = new List<PathAlias>();
            var configDirectory = Path.GetDirectoryName(config)?.Replace('\\', '/') ?? string.Empty;
            if (document.RootElement.TryGetProperty("extends", out var extends) && extends.ValueKind == JsonValueKind.String)
            {
                var extendsPath = extends.GetString()!;
                if (!extendsPath.StartsWith(".", StringComparison.Ordinal))
                    issues.Add(new JavaScriptTypeScriptIssue(config, "TsConfigPackageExtendsUnsupported"));
                else
                {
                    var resolved = Path.GetFullPath(Path.Combine(root, configDirectory, extendsPath));
                    if (!IsUnderRoot(root, resolved))
                        issues.Add(new JavaScriptTypeScriptIssue(config, "TsConfigExtendsOutsideRoot"));
                    else
                    {
                        var relative = Path.GetRelativePath(root, resolved).Replace('\\', '/');
                        var candidates = Path.HasExtension(relative) ? new[] { relative }
                            : new[] { relative + ".json", Path.Combine(relative, "tsconfig.json").Replace('\\', '/') };
                        var target = candidates.FirstOrDefault(paths.Contains);
                        if (target is null) issues.Add(new JavaScriptTypeScriptIssue(config, "TsConfigExtendsNotFound"));
                        else inherited.AddRange(ReadTsConfigAliases(root, target, paths, issues, metadataReader, visiting, depth + 1));
                    }
                }
            }
            if (!document.RootElement.TryGetProperty("compilerOptions", out var compilerOptions)
                || compilerOptions.ValueKind != JsonValueKind.Object) return inherited;
            if (!compilerOptions.TryGetProperty("paths", out var mapping) || mapping.ValueKind != JsonValueKind.Object) return inherited;
            var baseUrl = compilerOptions.TryGetProperty("baseUrl", out var baseUrlValue)
                && baseUrlValue.ValueKind == JsonValueKind.String ? baseUrlValue.GetString()! : ".";
            var basePath = Path.GetFullPath(Path.Combine(root, configDirectory, baseUrl));
            if (!IsUnderRoot(root, basePath))
            { issues.Add(new JavaScriptTypeScriptIssue(config, "TsConfigPathOutsideRoot")); return inherited; }
            var direct = new List<PathAlias>();
            foreach (var alias in mapping.EnumerateObject())
                if (alias.Value.ValueKind == JsonValueKind.Array)
                    foreach (var target in alias.Value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String))
                        direct.Add(new PathAlias(configDirectory, alias.Name, target.GetString()!, basePath));
            var overridden = direct.Select(item => item.Pattern).ToHashSet(StringComparer.Ordinal);
            return inherited.Where(item => !overridden.Contains(item.Pattern)).Concat(direct).ToArray();
        }
        catch (JsonException)
        { issues.Add(new JavaScriptTypeScriptIssue(config, "InvalidTsConfig")); return Array.Empty<PathAlias>(); }
        catch (IOException)
        { issues.Add(new JavaScriptTypeScriptIssue(config, "TsConfigReadFailed")); return Array.Empty<PathAlias>(); }
        finally { visiting.Remove(config); }
    }

    private static bool WorkspacePatternMatches(string pattern, string path)
    {
        var patternParts = pattern.Replace('\\', '/').Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathParts = path.Replace('\\', '/').Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var memo = new Dictionary<(int Pattern, int Path), bool>();
        return Match(0, 0);

        bool Match(int patternIndex, int pathIndex)
        {
            if (memo.TryGetValue((patternIndex, pathIndex), out var cached)) return cached;
            bool matched;
            if (patternIndex == patternParts.Length) matched = pathIndex == pathParts.Length;
            else if (patternParts[patternIndex] == "**")
                matched = Match(patternIndex + 1, pathIndex)
                    || pathIndex < pathParts.Length && Match(patternIndex, pathIndex + 1);
            else
                matched = pathIndex < pathParts.Length
                    && System.Text.RegularExpressions.Regex.IsMatch(pathParts[pathIndex],
                        "^" + System.Text.RegularExpressions.Regex.Escape(patternParts[patternIndex]).Replace("\\*", "[^/]*")
                            + "$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                    && Match(patternIndex + 1, pathIndex + 1);
            memo[(patternIndex, pathIndex)] = matched;
            return matched;
        }
    }

    private static string? SafeFullPath(string root, string path)
    {
        var full = Path.GetFullPath(path);
        return IsUnderRoot(root, full) ? full : null;
    }

    private static List<JavaScriptTypeScriptCallRelationship> ResolveCalls(IReadOnlyList<SourceUnit> units,
        IReadOnlyList<JavaScriptTypeScriptDeclaration> declarations, IReadOnlyList<JavaScriptTypeScriptModuleRelationship> modules)
    {
        var result = new List<JavaScriptTypeScriptCallRelationship>();
        var unitsByPath = units.ToDictionary(unit => unit.Path, StringComparer.Ordinal);
        var declarationsByPath = declarations.GroupBy(item => item.Path, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.GroupBy(item => item.Name, StringComparer.Ordinal)
                .ToDictionary(names => names.Key, names => names.ToArray(), StringComparer.Ordinal), StringComparer.Ordinal);
        var modulesBySource = modules.GroupBy(item => item.SourcePath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var exportsByPath = declarations.Where(item => item.ExportKind == "named")
            .GroupBy(item => item.Path, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.GroupBy(item => item.Name, StringComparer.Ordinal)
                .ToDictionary(names => names.Key, names => names.ToArray(), StringComparer.Ordinal), StringComparer.Ordinal);
        var defaultsByPath = declarations.Where(item => item.ExportKind == "default")
            .GroupBy(item => item.Path, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var importsBySource = new Dictionary<string, Dictionary<string, List<ImportedBinding>>>(StringComparer.Ordinal);
        foreach (var unit in units)
        {
            var specsByLocation = unit.Modules.ToDictionary(item =>
                (item.Token.Line, item.Token.Column, Specifier: item.Specifier ?? string.Empty));
            var bindingsByName = new Dictionary<string, List<ImportedBinding>>(StringComparer.Ordinal);
            if (modulesBySource.TryGetValue(unit.Path, out var sourceModules))
            foreach (var module in sourceModules.Where(item => item.Status == "syntax-resolved" && item.TargetPath is not null))
            {
                if (!specsByLocation.TryGetValue((module.SourceLine, module.SourceColumn, module.Specifier), out var spec)
                    || spec.Bindings is null) continue;
                foreach (var binding in spec.Bindings)
                {
                    var (localName, importedName, typeOnly) = ParseBinding(binding);
                    if (typeOnly) continue;
                    if (!bindingsByName.TryGetValue(localName, out var targets))
                        bindingsByName[localName] = targets = new List<ImportedBinding>();
                    targets.Add(new ImportedBinding(module.TargetPath!, importedName));
                }
            }
            importsBySource[unit.Path] = bindingsByName;
        }
        var exportCache = new Dictionary<(string Path, string Name), IReadOnlyList<JavaScriptTypeScriptDeclaration>>();
        foreach (var unit in units)
        foreach (var call in unit.Calls)
        {
            if (call.ForcedReason is not null)
            {
                result.Add(new JavaScriptTypeScriptCallRelationship(unit.Path, call.Token.Line, call.Token.Column, call.Caller,
                    call.Expression, "unresolved", null, null, null, Array.Empty<string>(), call.ForcedReason));
                continue;
            }
            var simpleName = call.Expression.Split('.').Last();
            var qualified = call.Expression.Contains('.');
            var mayResolveLocal = !qualified || call.Expression.StartsWith("this.", StringComparison.Ordinal);
            var localTargets = mayResolveLocal && declarationsByPath.TryGetValue(unit.Path, out var localNames)
                && localNames.TryGetValue(simpleName, out var localMatches) ? localMatches : Array.Empty<JavaScriptTypeScriptDeclaration>();
            if (!qualified && call.ShadowedNames.Contains(simpleName, StringComparer.Ordinal))
                localTargets = Array.Empty<JavaScriptTypeScriptDeclaration>();
            if (call.Expression.StartsWith("this.", StringComparison.Ordinal))
                localTargets = call.ContainerStart is null || call.IsInsideOrdinaryFunction ? Array.Empty<JavaScriptTypeScriptDeclaration>()
                    : localTargets.Where(item => item.Kind == "method"
                        && unit.MethodContainerStarts.TryGetValue((item.Line, item.Column, item.Name), out var start)
                        && start == call.ContainerStart.Value).ToArray();
            var importedTargets = new List<JavaScriptTypeScriptDeclaration>();
            var pieces = call.Expression.Split('.');
            var localName = pieces.Length > 1 ? pieces[0] : simpleName;
            if (!call.ShadowedNames.Contains(localName, StringComparer.Ordinal)
                && importsBySource.TryGetValue(unit.Path, out var importedBindings)
                && importedBindings.TryGetValue(localName, out var matchingBindings))
            {
                foreach (var binding in matchingBindings)
                {
                    var importedName = binding.ImportedName == "*" && pieces.Length > 1 ? simpleName : binding.ImportedName;
                    var cacheKey = (binding.TargetPath, importedName);
                    if (!exportCache.TryGetValue(cacheKey, out var resolved))
                    {
                        resolved = ResolveExportedDeclarations(binding.TargetPath, importedName, unitsByPath,
                            exportsByPath, defaultsByPath, modulesBySource, new HashSet<string>(StringComparer.Ordinal));
                        exportCache[cacheKey] = resolved;
                    }
                    importedTargets.AddRange(resolved);
                }
            }
            var targets = localTargets.Concat(importedTargets).Distinct().ToArray();
            var status = targets.Length switch { 1 => "syntax-resolved", > 1 => "ambiguous", _ => "unresolved" };
            result.Add(new JavaScriptTypeScriptCallRelationship(unit.Path, call.Token.Line, call.Token.Column, call.Caller,
                call.Expression, status, targets.Length == 1 ? targets[0].Path : null, targets.Length == 1 ? targets[0].Line : null,
                targets.Length == 1 ? targets[0].Name : null,
                targets.Length > 1 ? targets.Select(target => $"{target.Path}:{target.Line}:{target.Name}").OrderBy(value => value, StringComparer.Ordinal).ToArray() : Array.Empty<string>(),
                targets.Length == 0 ? qualified && !call.Expression.StartsWith("this.", StringComparison.Ordinal)
                    ? "ReceiverTypeNotResolved" : "NoUniqueInRepositoryDeclaration"
                    : targets.Length > 1 ? "MultipleDeclarations" : null));
        }
        return result;
    }

    private static IReadOnlyList<JavaScriptTypeScriptDeclaration> ResolveExportedDeclarations(string modulePath, string name,
        IReadOnlyDictionary<string, SourceUnit> units,
        IReadOnlyDictionary<string, Dictionary<string, JavaScriptTypeScriptDeclaration[]>> exports,
        IReadOnlyDictionary<string, JavaScriptTypeScriptDeclaration[]> defaults,
        IReadOnlyDictionary<string, JavaScriptTypeScriptModuleRelationship[]> modules, HashSet<string> visited)
    {
        var key = modulePath + "|" + name;
        if (!visited.Add(key)) return Array.Empty<JavaScriptTypeScriptDeclaration>();
        var results = name == "default"
            ? (defaults.TryGetValue(modulePath, out var defaultExports) ? defaultExports : Array.Empty<JavaScriptTypeScriptDeclaration>()).ToList()
            : exports.TryGetValue(modulePath, out var moduleExports) && moduleExports.TryGetValue(name, out var namedExports)
                ? namedExports.ToList() : new List<JavaScriptTypeScriptDeclaration>();
        if (!units.TryGetValue(modulePath, out var unit)) return results;
        foreach (var reExport in unit.Modules.Where(item => item.Kind == "re-export"))
        {
            var relation = (modules.TryGetValue(modulePath, out var sourceModules) ? sourceModules : Array.Empty<JavaScriptTypeScriptModuleRelationship>())
                .FirstOrDefault(item => item.SourceLine == reExport.Token.Line && item.SourceColumn == reExport.Token.Column
                    && item.Specifier == reExport.Specifier && item.Status == "syntax-resolved" && item.TargetPath is not null);
            if (relation is null) continue;
            var hasRuntimeStarExport = reExport.Bindings?.Select(ParseBinding)
                .Any(binding => !binding.TypeOnly && binding.Imported == "*") == true;
            if (reExport.Bindings is null || hasRuntimeStarExport)
            {
                if (name != "default")
                    results.AddRange(ResolveExportedDeclarations(relation.TargetPath!, name, units, exports, defaults, modules, visited));
            }
            else
                foreach (var entry in reExport.Bindings)
                {
                    var (exported, original, typeOnly) = ParseBinding(entry);
                    if (!typeOnly && exported == name)
                        results.AddRange(ResolveExportedDeclarations(relation.TargetPath!, original, units, exports, defaults, modules, visited));
                }
        }
        return results.Distinct().ToArray();
    }

    private static string ResolveUnderRoot(string root, string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnderRoot(root, full)) throw new ArgumentException("Path outside scan root.");
        return full;
    }

    private static bool IsUnderRoot(string root, string fullPath)
    {
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static List<(string Name, int Start, int End)> FindClassContainers(IReadOnlyList<JavaScriptTypeScriptToken> tokens)
    {
        var result = new List<(string, int, int)>();
        for (var index = 0; index + 2 < tokens.Count; index++)
            if (tokens[index].Text is "class" or "interface" && tokens[index + 1].Kind == "identifier")
            {
                var open = IndexOf(tokens, "{", index + 2, tokens.Count);
                if (open < 0) continue;
                var close = FindMatching(tokens, open, "{", "}");
                if (close >= 0) result.Add((tokens[index + 1].Text, tokens[open].Offset, tokens[close].Offset));
            }
        return result;
    }

    private static IReadOnlyList<(int Start, int End)> FindOrdinaryFunctionBodies(IReadOnlyList<JavaScriptTypeScriptToken> tokens)
    {
        var parentheses = FindDelimiterPairs(tokens, "(", ")");
        var braces = FindDelimiterPairs(tokens, "{", "}");
        var result = new List<(int Start, int End)>();
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Text != "function") continue;
            var open = -1;
            var limit = Math.Min(tokens.Count, index + 64);
            for (var cursor = index + 1; cursor < limit; cursor++)
            {
                if (tokens[cursor].Text == "(") { open = cursor; break; }
                if (tokens[cursor].Text is ";" or "{" or "}" or "=>") break;
            }
            if (open < 0 || !parentheses.TryGetValue(open, out var close)) continue;
            var body = FindCallableBodyMarker(tokens, close + 1, functionLike: true);
            if (body < 0 || tokens[body].Text != "{" || !braces.TryGetValue(body, out var bodyClose)) continue;
            result.Add((body + 1, bodyClose));
        }
        return result;
    }

    private static void AddDeclaration(string path, string kind, JavaScriptTypeScriptToken token,
        IReadOnlyList<(string Name, int Start, int End)> containers, List<JavaScriptTypeScriptDeclaration> declarations,
        string? exportKind = null)
    {
        var container = containers.Where(item => token.Offset > item.Start && token.Offset < item.End)
            .OrderBy(item => item.End - item.Start).FirstOrDefault();
        var extension = Path.GetExtension(path);
        var language = extension.ToLowerInvariant() is ".ts" or ".tsx" or ".mts" or ".cts" ? "TypeScript" : "JavaScript";
        declarations.Add(new JavaScriptTypeScriptDeclaration(language, path, token.Line, token.Column, kind, token.Text,
            container.Name, ExportKind: exportKind));
    }

    private static string[] ParseImportBindings(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start, int end)
    {
        var result = new List<string>();
        var importTypeOnly = start < end && tokens[start].Text == "type";
        var open = IndexOf(tokens, "{", start, end);
        var star = IndexOf(tokens, "*", start, end);
        if (star >= 0)
        {
            var asIndex = IndexOf(tokens, "as", star + 1, end);
            if (asIndex >= 0 && asIndex + 1 < end)
                result.Add(FormatBinding(tokens[asIndex + 1].Text, "*", importTypeOnly));
        }
        if (open >= 0)
        {
            var defaultName = Enumerable.Range(start, Math.Max(0, open - start))
                .Where(position => tokens[position].Kind == "identifier" && tokens[position].Text is not "type" and not "as")
                .Select(position => tokens[position].Text).FirstOrDefault();
            if (defaultName is not null) result.Add(FormatBinding(defaultName, "default", importTypeOnly));
            var close = IndexOf(tokens, "}", open + 1, end);
            if (close < 0) close = end;
            var segmentStart = open + 1;
            for (var index = open + 1; index <= close; index++)
            {
                if (index != close && tokens[index].Text != ",") continue;
                var inlineTypeOnly = segmentStart + 1 < index && tokens[segmentStart].Text == "type";
                var isTypeOnly = importTypeOnly || inlineTypeOnly;
                if (inlineTypeOnly) segmentStart++;
                var identifiers = Enumerable.Range(segmentStart, Math.Max(0, index - segmentStart))
                    .Where(position => tokens[position].Kind == "identifier" && tokens[position].Text is not "type" and not "as")
                    .Select(position => tokens[position].Text).ToArray();
                if (identifiers.Length > 0)
                    result.Add(FormatBinding(identifiers.Length == 1 ? identifiers[0] : identifiers[^1], identifiers[0], isTypeOnly));
                segmentStart = index + 1;
            }
        }
        else if (star < 0)
        {
            var defaultName = Enumerable.Range(start, Math.Max(0, end - start))
                .Where(position => tokens[position].Kind == "identifier" && tokens[position].Text is not "type" and not "as")
                .Select(position => tokens[position].Text).FirstOrDefault();
            if (defaultName is not null) result.Add(FormatBinding(defaultName, "default", importTypeOnly));
        }
        return result.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string[]? ParseReExportBindings(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start, int end)
    {
        var exportTypeOnly = start < end && tokens[start].Text == "type";
        var open = IndexOf(tokens, "{", start, end);
        if (open < 0) return new[] { FormatBinding("*", "*", exportTypeOnly) };
        var close = IndexOf(tokens, "}", open + 1, end);
        if (close < 0) return Array.Empty<string>();
        var result = new List<string>();
        var segmentStart = open + 1;
        for (var index = open + 1; index <= close; index++)
        {
            if (index != close && tokens[index].Text != ",") continue;
            var inlineTypeOnly = segmentStart + 1 < index && tokens[segmentStart].Text == "type";
            var isTypeOnly = exportTypeOnly || inlineTypeOnly;
            if (inlineTypeOnly) segmentStart++;
            var identifiers = Enumerable.Range(segmentStart, Math.Max(0, index - segmentStart))
                .Where(position => tokens[position].Kind == "identifier" && tokens[position].Text is not "type" and not "as")
                .Select(position => tokens[position].Text).ToArray();
            if (identifiers.Length > 0)
                result.Add(FormatBinding(identifiers.Length == 1 ? identifiers[0] : identifiers[^1], identifiers[0], isTypeOnly));
            segmentStart = index + 1;
        }
        return result.ToArray();
    }

    private static string FormatBinding(string local, string imported, bool typeOnly) =>
        $"{(typeOnly ? "type:" : string.Empty)}{local}={imported}";

    private static (string Local, string Imported, bool TypeOnly) ParseBinding(string binding)
    {
        var typeOnly = binding.StartsWith("type:", StringComparison.Ordinal);
        if (typeOnly) binding = binding[5..];
        var split = binding.IndexOf('=');
        return split < 0 ? (binding, binding, typeOnly) : (binding[..split], binding[(split + 1)..], typeOnly);
    }

    private static string? GetExportKind(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int index)
    {
        var isDefault = false;
        for (var cursor = index - 1; cursor >= 0 && cursor >= index - 8; cursor--)
        {
            if (tokens[cursor].Text is ";" or "}") break;
            if (tokens[cursor].Text == "default") isDefault = true;
            if (tokens[cursor].Text == "export") return isDefault ? "default" : "named";
        }
        return null;
    }

    private static int FindTokenIndex(IReadOnlyList<JavaScriptTypeScriptToken> tokens, JavaScriptTypeScriptToken token)
    {
        for (var index = 0; index < tokens.Count; index++) if (tokens[index].Offset == token.Offset) return index;
        return -1;
    }

    private static bool ContainsArrowBeforeStatementEnd(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start)
    {
        var end = Math.Min(tokens.Count, start + 512);
        for (var index = start; index < end && tokens[index].Text is not ";" and not "}"; index++)
            if (tokens[index].Text == "=>") return true;
        return false;
    }

    private static bool IsDeclarationName(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int index) =>
        index > 0 && (tokens[index - 1].Text is "function" or "class" or "interface" or "type" or "enum");

    private static bool IsImportExportTypeSpecifier(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int index)
    {
        for (var cursor = index - 1; cursor >= Math.Max(0, index - 64); cursor--)
        {
            if (tokens[cursor].Text is ";" or "}") return false;
            if (tokens[cursor].Text is "import" or "export") return true;
            if (tokens[index].Line - tokens[cursor].Line > 2) break;
        }
        return false;
    }

    private static int FindCallOpenParen(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int index)
    {
        if (index + 1 < tokens.Count && tokens[index + 1].Text == "(") return index + 1;
        if (index + 1 >= tokens.Count || tokens[index + 1].Text != "<") return -1;
        var depth = 0;
        for (var cursor = index + 1; cursor < tokens.Count && cursor < index + 32; cursor++)
        {
            if (tokens[cursor].Text == "<") depth++;
            else if (tokens[cursor].Text == ">" && --depth == 0)
                return cursor + 1 < tokens.Count && tokens[cursor + 1].Text == "(" ? cursor + 1 : -1;
        }
        return -1;
    }

    private static bool IsClassMethodDeclaration(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int index,
        IReadOnlyList<(string Name, int Start, int End)> containers, IReadOnlyList<int> braceDepth)
    {
        var openParen = FindCallOpenParen(tokens, index);
        if (openParen < 0) return false;
        var container = containers.Where(item => tokens[index].Offset > item.Start && tokens[index].Offset < item.End)
            .OrderBy(item => item.End - item.Start).FirstOrDefault();
        if (container.Name is null) return false;
        var openIndex = FindTokenIndexByOffset(tokens, container.Start);
        if (openIndex < 0 || braceDepth[index] != braceDepth[openIndex] + 1) return false;
        var closeParen = FindMatching(tokens, openParen, "(", ")");
        if (closeParen < 0) return false;
        for (var cursor = closeParen + 1; cursor < tokens.Count; cursor++)
        {
            if (tokens[cursor].Text is "{" or ";") return true;
            if (tokens[cursor].Text is "}" or "=" or "=>") return false;
            if (cursor > closeParen + 40 || tokens[cursor].Line > tokens[closeParen].Line + 4) return false;
        }
        return false;
    }

    private static int FindTokenIndexByOffset(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int offset)
    {
        for (var index = 0; index < tokens.Count; index++) if (tokens[index].Offset == offset) return index;
        return -1;
    }

    private static int FindStatementEnd(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start)
    {
        for (var index = start; index < tokens.Count; index++)
            if (tokens[index].Text == ";" || index > start && tokens[index].Line > tokens[index - 1].Line && tokens[index].Text is "import" or "export") return index;
        return tokens.Count;
    }

    private static int IndexOf(IReadOnlyList<JavaScriptTypeScriptToken> tokens, string value, int start, int end)
    { for (var index = start; index < Math.Min(end, tokens.Count); index++) if (tokens[index].Text == value) return index; return -1; }

    private static int FindMatching(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start, string open, string close)
    {
        var depth = 0;
        for (var index = start; index < tokens.Count; index++)
        { if (tokens[index].Text == open) depth++; else if (tokens[index].Text == close && --depth == 0) return index; }
        return -1;
    }

    private static List<ParameterScope> FindParameterScopes(IReadOnlyList<JavaScriptTypeScriptToken> tokens,
        IReadOnlySet<int> methodDeclarationIndices)
    {
        var scopes = new List<ParameterScope>();
        var parentheses = FindDelimiterPairs(tokens, "(", ")");
        var braces = FindDelimiterPairs(tokens, "{", "}");
        foreach (var (open, close) in parentheses.OrderBy(pair => pair.Key))
        {
            if (close + 1 >= tokens.Count) continue;
            var functionLike = open > 0 && tokens[open - 1].Text == "catch"
                || IsFunctionParameterList(tokens, open)
                || methodDeclarationIndices.Contains(open - 1)
                || IsGenericMethodParameterList(tokens, open, methodDeclarationIndices);
            var bodyMarker = FindCallableBodyMarker(tokens, close + 1, functionLike);
            if (bodyMarker < 0) continue;
            var arrow = tokens[bodyMarker].Text == "=>";
            var bodyOpen = arrow ? bodyMarker + 1 : bodyMarker;

            int bodyEnd;
            if (bodyOpen < tokens.Count && tokens[bodyOpen].Text == "{")
            {
                if (!braces.TryGetValue(bodyOpen, out var bodyClose)) continue;
                bodyEnd = bodyClose;
                bodyOpen++;
            }
            else if (arrow)
                bodyEnd = FindExpressionEnd(tokens, bodyOpen);
            else continue;

            var names = ReadParameterNames(tokens, open + 1, close).Distinct(StringComparer.Ordinal).ToArray();
            if (bodyEnd > bodyOpen)
                scopes.Add(new ParameterScope(bodyOpen, bodyEnd, names, IsFunctionScope: open == 0 || tokens[open - 1].Text != "catch"));
        }

        // Single-parameter arrow functions do not require parentheses.
        for (var index = 0; index + 1 < tokens.Count; index++)
        {
            if (tokens[index].Kind != "identifier" || tokens[index + 1].Text != "=>") continue;
            var bodyOpen = index + 2;
            int bodyEnd;
            if (bodyOpen < tokens.Count && tokens[bodyOpen].Text == "{")
            {
                if (!braces.TryGetValue(bodyOpen, out var bodyClose)) continue;
                bodyEnd = bodyClose;
                bodyOpen++;
            }
            else bodyEnd = FindExpressionEnd(tokens, bodyOpen);
            if (bodyEnd > bodyOpen) scopes.Add(new ParameterScope(bodyOpen, bodyEnd, new[] { tokens[index].Text }, IsFunctionScope: true));
        }
        return scopes;
    }

    private static bool IsGenericMethodParameterList(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int open,
        IReadOnlySet<int> methodDeclarationIndices)
    {
        if (open == 0 || tokens[open - 1].Text != ">") return false;
        for (var index = open - 2; index >= Math.Max(0, open - 16); index--)
        {
            if (methodDeclarationIndices.Contains(index)) return true;
            if (tokens[index].Text is ";" or "{" or "}" or "=>" or "=") break;
        }
        return false;
    }

    private static int FindCallableBodyMarker(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start, bool functionLike)
    {
        if (start >= tokens.Count) return -1;
        if (tokens[start].Text == "=>") return start;
        if (tokens[start].Text == "{") return functionLike ? start : -1;
        if (!functionLike && !tokens.Skip(start).Take(48).Any(item => item.Text == "=>")) return -1;
        var angles = 0;
        var limit = Math.Min(tokens.Count, start + 48);
        for (var index = start; index < limit; index++)
        {
            var text = tokens[index].Text;
            if (text == "<") angles++;
            else if (text == ">" && angles > 0) angles--;
            else if (angles == 0 && text is "=>" or "{") return index;
            else if (angles == 0 && text is ";" or "=" or "}") return -1;
        }
        return -1;
    }

    private static IReadOnlyList<ParameterScope> FindLocalBindingScopes(IReadOnlyList<JavaScriptTypeScriptToken> tokens,
        IReadOnlyList<ParameterScope> callableScopes)
    {
        var braces = FindDelimiterPairs(tokens, "{", "}");
        var opensByClose = braces.ToDictionary(pair => pair.Value, pair => pair.Key);
        var stack = new Stack<int>();
        var scopes = new List<ParameterScope>();
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Text == "}")
                while (stack.Count > 0 && opensByClose.TryGetValue(index, out var expected) && stack.Peek() == expected)
                    stack.Pop();
            if (tokens[index].Text is "const" or "let" or "var" && index + 1 < tokens.Count)
            {
                var names = ReadBindingNames(tokens, index + 1).Distinct(StringComparer.Ordinal).ToArray();
                if (names.Length > 0 && tokens[index].Text == "var")
                {
                    var functionScope = callableScopes
                        .Where(scope => scope.IsFunctionScope && scope.Start <= index && index < scope.End)
                        .OrderBy(scope => scope.End - scope.Start)
                        .FirstOrDefault();
                    scopes.Add(functionScope is null
                        ? new ParameterScope(0, tokens.Count, names)
                        : new ParameterScope(functionScope.Start, functionScope.End, names));
                }
                else if (names.Length > 0 && stack.Count > 0)
                {
                    var open = stack.Peek();
                    if (braces.TryGetValue(open, out var close))
                        scopes.Add(new ParameterScope(open + 1, close, names));
                }
            }
            if (tokens[index].Text == "{" && braces.ContainsKey(index)) stack.Push(index);
        }
        return scopes;
    }

    private static IEnumerable<string> ReadBindingNames(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start)
    {
        while (start < tokens.Count && tokens[start].Text is "." or "public" or "private" or "protected" or "readonly")
            start++;
        if (start >= tokens.Count) yield break;
        if (tokens[start].Kind == "identifier")
        {
            yield return tokens[start].Text;
            yield break;
        }
        if (tokens[start].Text is not "{" and not "[") yield break;
        var closeToken = tokens[start].Text == "{" ? "}" : "]";
        var close = FindMatching(tokens, start, tokens[start].Text, closeToken);
        if (close < 0) yield break;
        var segmentStart = start + 1;
        var round = 0; var square = 0; var curly = 0;
        for (var index = segmentStart; index <= close; index++)
        {
            var atEnd = index == close;
            var text = atEnd ? string.Empty : tokens[index].Text;
            if (!atEnd && text == "," && round == 0 && square == 0 && curly == 0)
            {
                foreach (var name in ReadBindingSegment(tokens, segmentStart, index, tokens[start].Text == "{"))
                    yield return name;
                segmentStart = index + 1;
                continue;
            }
            if (!atEnd)
            {
                if (text == "(") round++; else if (text == ")") round--;
                else if (text == "[") square++; else if (text == "]") square--;
                else if (text == "{") curly++; else if (text == "}") curly--;
            }
            else
                foreach (var name in ReadBindingSegment(tokens, segmentStart, index, tokens[start].Text == "{"))
                    yield return name;
        }
    }

    private static IEnumerable<string> ReadBindingSegment(IReadOnlyList<JavaScriptTypeScriptToken> tokens,
        int start, int end, bool objectPattern)
    {
        if (start >= end) yield break;
        var round = 0; var square = 0; var curly = 0;
        for (var index = start; index < end; index++)
        {
            var text = tokens[index].Text;
            if (text == "(") round++; else if (text == ")") round--;
            else if (text == "[") square++; else if (text == "]") square--;
            else if (text == "{") curly++; else if (text == "}") curly--;
            else if (objectPattern && text == ":" && round == 0 && square == 0 && curly == 0)
            {
                foreach (var name in ReadBindingNames(tokens, index + 1)) yield return name;
                yield break;
            }
        }
        if (tokens[start].Text is "{" or "[")
        {
            foreach (var name in ReadBindingNames(tokens, start)) yield return name;
            yield break;
        }
        var first = Enumerable.Range(start, end - start).Select(position => tokens[position])
            .FirstOrDefault(token => token.Kind == "identifier");
        if (first is not null) yield return first.Text;
    }

    private static Dictionary<int, int> FindDelimiterPairs(IReadOnlyList<JavaScriptTypeScriptToken> tokens,
        string open, string close)
    {
        var pairs = new Dictionary<int, int>();
        var stack = new Stack<int>();
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Text == open) stack.Push(index);
            else if (tokens[index].Text == close && stack.Count > 0) pairs[stack.Pop()] = index;
        }
        return pairs;
    }

    private static bool IsFunctionParameterList(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int open)
    {
        for (var index = open - 1; index >= Math.Max(0, open - 48); index--)
        {
            if (tokens[index].Text == "function") return true;
            if (tokens[index].Text is ";" or "{" or "}" or "=>" or "=") break;
        }
        return false;
    }

    private static IEnumerable<string> ReadParameterNames(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start, int end)
    {
        var segmentStart = start;
        var parens = 0; var brackets = 0; var braces = 0; var angles = 0;
        IEnumerable<string> ReadSegment(int segmentEnd)
        {
            var cursor = segmentStart;
            while (cursor < segmentEnd && tokens[cursor].Text is "public" or "private" or "protected" or "readonly") cursor++;
            return ReadBindingNames(tokens, cursor);
        }
        for (var index = start; index <= end; index++)
        {
            var atEnd = index == end;
            var text = atEnd ? string.Empty : tokens[index].Text;
            if (!atEnd && text == "," && parens == 0 && brackets == 0 && braces == 0 && angles == 0)
            {
                foreach (var name in ReadSegment(index)) yield return name;
                segmentStart = index + 1;
                continue;
            }
            if (!atEnd)
            {
                if (text == "(") parens++; else if (text == ")") parens--;
                else if (text == "[") brackets++; else if (text == "]") brackets--;
                else if (text == "{") braces++; else if (text == "}") braces--;
                else if (text == "<") angles++; else if (text == ">") angles--;
            }
            if (atEnd)
            {
                foreach (var name in ReadSegment(index)) yield return name;
            }
        }
    }

    private static int FindExpressionEnd(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int start)
    {
        var parens = 0; var brackets = 0; var braces = 0;
        for (var index = start; index < tokens.Count; index++)
        {
            var text = tokens[index].Text;
            if (parens == 0 && brackets == 0 && braces == 0 && text is ";" or "," or "}") return index;
            if (index > start && parens == 0 && brackets == 0 && braces == 0
                && tokens[index].Line > tokens[index - 1].Line
                && tokens[index - 1].Text is not "." and not "?." and not "=" and not ",") return index;
            if (text == "(") parens++; else if (text == ")") parens--;
            else if (text == "[") brackets++; else if (text == "]") brackets--;
            else if (text == "{") braces++; else if (text == "}") braces--;
        }
        return tokens.Count;
    }

    private static JavaScriptTypeScriptToken? FindAssignedArrowName(IReadOnlyList<JavaScriptTypeScriptToken> tokens, JavaScriptTypeScriptToken arrow)
    {
        var arrowIndex = -1;
        for (var index = 0; index < tokens.Count; index++) if (tokens[index].Offset == arrow.Offset) { arrowIndex = index; break; }
        if (arrowIndex < 2) return null;
        for (var index = arrowIndex - 1; index >= 0 && index >= arrowIndex - 40; index--)
            if (tokens[index].Text is "const" or "let" or "var")
                return index + 1 < tokens.Count && tokens[index + 1].Kind == "identifier" ? tokens[index + 1] : null;
        return null;
    }

    private static string FindEnclosingCallable(IReadOnlyList<JavaScriptTypeScriptToken> tokens, int index,
        IReadOnlyList<JavaScriptTypeScriptDeclaration> declarations)
    {
        for (var declarationIndex = declarations.Count - 1; declarationIndex >= 0; declarationIndex--)
            if (declarations[declarationIndex].Line <= tokens[index].Line)
                return declarations[declarationIndex].Name;
        return "<module>";
    }

    private static void Advance(char value, ref int line, ref int column)
    { if (value == '\n') { line++; column = 1; } else if (value != '\r') column++; }

    private sealed record SourceUnit(string Path, IReadOnlyList<JavaScriptTypeScriptDeclaration> Declarations,
        IReadOnlyList<ModuleSpec> Modules, IReadOnlyList<CallSite> Calls,
        IReadOnlyDictionary<(int Line, int Column, string Name), int> MethodContainerStarts, bool RecordLimitReached);
    private sealed class IssueCollector(int limit)
    {
        private readonly List<JavaScriptTypeScriptIssue> issues = new(Math.Min(limit, 1024));
        private bool limitReached;

        public void Add(JavaScriptTypeScriptIssue issue)
        {
            if (issues.Count >= limit) { limitReached = true; return; }
            issues.Add(issue);
        }

        public void AddRange(IEnumerable<JavaScriptTypeScriptIssue> values)
        {
            foreach (var issue in values) Add(issue);
        }

        public JavaScriptTypeScriptIssue[] ToOutput()
        {
            var ordered = issues.OrderBy(issue => issue.Path, RepositoryPathComparer.Instance)
                .ThenBy(issue => issue.Code, StringComparer.Ordinal).ToArray();
            if (!limitReached) return ordered;
            return ordered.Take(Math.Max(0, limit - 1))
                .Append(new JavaScriptTypeScriptIssue(string.Empty, "IssueLimit")).ToArray();
        }
    }
    private sealed class MetadataReadBudget(string root, int maxFileBytes, int maxTotalBytes,
        IssueCollector issues)
    {
        private readonly Dictionary<string, string?> cache = new(PathStringComparer);
        private int totalBytes;

        public bool TryRead(string relativePath, out string text)
        {
            if (cache.TryGetValue(relativePath, out var cached))
            {
                text = cached ?? string.Empty;
                return cached is not null;
            }
            var fullPath = Path.Combine(root, relativePath);
            try
            {
                var byteLength = new FileInfo(fullPath).Length;
                if (byteLength > maxFileBytes)
                {
                    issues.Add(new JavaScriptTypeScriptIssue(relativePath, "MetadataFileSizeLimit"));
                    cache[relativePath] = null;
                    text = string.Empty;
                    return false;
                }
                if (byteLength > maxTotalBytes - totalBytes)
                {
                    issues.Add(new JavaScriptTypeScriptIssue(relativePath, "TotalMetadataSizeLimit"));
                    cache[relativePath] = null;
                    text = string.Empty;
                    return false;
                }
                text = File.ReadAllText(fullPath);
                totalBytes += checked((int)byteLength);
                cache[relativePath] = text;
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                issues.Add(new JavaScriptTypeScriptIssue(relativePath, "MetadataReadFailed"));
                cache[relativePath] = null;
                text = string.Empty;
                return false;
            }
        }
    }
    private sealed record TokenizationResult(IReadOnlyList<JavaScriptTypeScriptToken> Tokens, IReadOnlyList<string> IssueCodes);
    private sealed record ModuleSpec(JavaScriptTypeScriptToken Token, string? Specifier, string Kind, string[]? Bindings, string? ForcedReason);
    private sealed record ParameterScope(int Start, int End, string[] Names, bool IsFunctionScope = false);
    private sealed record ImportedBinding(string TargetPath, string ImportedName);
    private sealed record CallSite(JavaScriptTypeScriptToken Token, string Caller, string Expression, string? ForcedReason,
        int? ContainerStart, string[] ShadowedNames, bool IsInsideOrdinaryFunction);
    private sealed record ModuleTargetResolution(IReadOnlyList<string> Targets, string? Reason);
    internal sealed record WorkspacePackage(string RootPath, string? EntryPath);

    private sealed record PathAlias(string ConfigDirectory, string Pattern, string Target, string BasePath)
    {
        public bool Matches(string specifier, string sourcePath)
        {
            var sourceDirectory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? string.Empty;
            if (ConfigDirectory.Length > 0 && !sourceDirectory.StartsWith(ConfigDirectory + "/", StringComparison.Ordinal)
                && sourceDirectory != ConfigDirectory) return false;
            return TryCapture(Pattern, specifier, out _);
        }

        public IEnumerable<string> Expand(string specifier)
        {
            if (!TryCapture(Pattern, specifier, out var capture)) yield break;
            var expanded = Target.Replace("*", capture, StringComparison.Ordinal);
            yield return Path.GetFullPath(Path.Combine(BasePath, expanded));
        }

        private static bool TryCapture(string pattern, string value, out string capture)
        {
            var star = pattern.IndexOf('*');
            if (star < 0) { capture = string.Empty; return string.Equals(pattern, value, StringComparison.Ordinal); }
            var prefix = pattern[..star]; var suffix = pattern[(star + 1)..];
            if (!value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal)
                || value.Length < prefix.Length + suffix.Length) { capture = string.Empty; return false; }
            capture = value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
            return !capture.Contains('*');
        }
    }
}
