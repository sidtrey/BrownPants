using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

internal static class ManagedNativeConsumerAnalyzer
{
    private static readonly Regex CliClass = new(
        @"\bref\s+class\s+(?<name>[A-Za-z_]\w*)[^;{}]*\{", RegexOptions.CultureInvariant);
    private static readonly Regex InlineMethod = new(
        @"^\s*(?:(?:public|protected|private|internal|static|virtual|override|final|inline|friend|constexpr|explicit|extern|new)\s+)*(?:[A-Za-z_~][\w:<>,*&\s]*?)\s+(?<method>~?[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?:(?:const|noexcept|override|final)\s*)*\{",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);
    private static readonly Regex Namespace = new(
        @"\bnamespace\s+(?<name>[A-Za-z_]\w*(?:::\w+)*)\s*\{", RegexOptions.CultureInvariant);
    private static readonly Regex QualifiedCall = new(
        @"(?<qualified>(?<owner>(?:[A-Za-z_]\w*::)+)(?<method>[A-Za-z_]\w*))\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex NativeMethod = new(
        @"(?<qualified>(?<owner>(?:[A-Za-z_]\w*::)+)(?<method>[A-Za-z_]\w*))\s*\([^;{}]*\)\s*(?:(?:const|noexcept|override|final)\s*)*\{",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    public static IReadOnlyList<NativeFlowRelation> Analyze(string root,
        IReadOnlyList<DotNetProjectInfo> managedProjects,
        IReadOnlyList<NativeProjectInfo> nativeProjects,
        IReadOnlyList<CSharpConsumerCall> calls)
    {
        if (calls.Count == 0 || nativeProjects.Count == 0) return Array.Empty<NativeFlowRelation>();

        var inRootNativeProjects = nativeProjects.Where(project => project.HasClrSupport)
            .OrderBy(project => project.Path, RepositoryPathComparer.Instance).ToArray();
        if (inRootNativeProjects.Length == 0) return Array.Empty<NativeFlowRelation>();

        var projectFiles = RepositoryScanner.DiscoverFiles(root).ToHashSet(PathComparer());
        var sources = ReadNativeSources(root, projectFiles);
        var wrappers = sources.SelectMany(ParseWrappers).ToArray();
        var nativeDefinitions = sources.SelectMany(ParseNativeDefinitions)
            .GroupBy(item => item.Symbol, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Path, RepositoryPathComparer.Instance)
                .ThenBy(item => item.Line).ToArray(), StringComparer.Ordinal);
        var relations = new List<NativeFlowRelation>();
        var nativeCallRelations = new Dictionary<string, NativeFlowRelation>(StringComparer.Ordinal);
        foreach (var call in calls.OrderBy(call => call.Path, RepositoryPathComparer.Instance)
                     .ThenBy(call => call.Line).ThenBy(call => call.Column).ThenBy(call => call.MethodName, StringComparer.Ordinal))
        {
            var containingProject = FindManagedProject(call.Path, managedProjects);
            if (containingProject is null) continue;
            var references = ResolveReferences(root, containingProject, inRootNativeProjects, projectFiles);
            var typeName = NormalizeTypeName(call.ReceiverType);
            var typeParts = typeName.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var simpleTypeName = typeParts.LastOrDefault() ?? string.Empty;
            if (references.Length == 0)
            {
                var unsafeReference = containingProject.AssemblyReferences.FirstOrDefault(reference =>
                    string.Equals(reference.Include.Split(',')[0].Trim(), typeParts.FirstOrDefault(), StringComparison.OrdinalIgnoreCase)
                    && reference.HintPath is not null
                    && NormalizeInRootPath(root, Path.GetDirectoryName(containingProject.Path)?.Replace('\\', '/') ?? string.Empty,
                        reference.HintPath) is null);
                if (unsafeReference is null) continue;

                var unresolvedTarget = string.Join("::", typeParts.Append(call.MethodName));
                var unsafeIdentity = string.Join("\n", "csharp-cppcli-call", call.Path, call.Line, call.Column,
                    call.Caller, call.ReceiverType, call.MethodName, "unsafe-reference", unsafeReference.HintPath);
                var unsafeId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(unsafeIdentity)))[..16].ToLowerInvariant();
                relations.Add(new NativeFlowRelation(unsafeId, "csharp-cppcli-call", "unresolved", call.Path,
                    call.Line, call.Caller.Length == 0 ? call.ReceiverExpression : call.Caller, null, null,
                    unresolvedTarget, null, 0,
                    $"{call.ReceiverType} {call.ReceiverExpression}.{call.MethodName}(...) references unsafe or external HintPath '{unsafeReference.HintPath}', which was not read"));
                continue;
            }

            var matchingReferences = references.Where(reference =>
                    wrappers.Any(wrapper => IsProjectSourcePath(reference.Project.Path, wrapper.Path)
                        && WrapperTypeMatches(wrapper, typeName, simpleTypeName))
                    || typeParts.Length > 0 && string.Equals(reference.AssemblyName, typeParts[0], StringComparison.Ordinal))
                .ToArray();
            var matchingTypes = wrappers.Where(wrapper =>
                    matchingReferences.Any(reference => IsProjectSourcePath(reference.Project.Path, wrapper.Path))
                    && WrapperTypeMatches(wrapper, typeName, simpleTypeName))
                .ToArray();

            var candidates = matchingTypes.Where(wrapper => string.Equals(wrapper.Method, call.MethodName, StringComparison.Ordinal))
                .Select(wrapper => new NativeFlowTarget(wrapper.Path, wrapper.Line, wrapper.Symbol))
                .DistinctBy(target => $"{target.Path}\n{target.Line}\n{target.Symbol}", StringComparer.Ordinal)
                .OrderBy(target => target.Path, RepositoryPathComparer.Instance).ThenBy(target => target.Line)
                .ThenBy(target => target.Symbol, StringComparer.Ordinal).ToArray();

            var likelyReferencedType = matchingReferences.Length > 0
                || references.Any(reference => typeParts.Length > 0 &&
                    (string.Equals(reference.AssemblyName, typeParts[0], StringComparison.Ordinal)
                     || string.Equals(Path.GetFileNameWithoutExtension(reference.HintPath), typeParts[0], StringComparison.OrdinalIgnoreCase)));
            if (candidates.Length == 0 && !likelyReferencedType) continue;

            var status = candidates.Length switch
            {
                0 => "unresolved",
                1 => "candidate",
                _ => "ambiguous"
            };
            var targetSymbol = candidates.Length == 1 ? candidates[0].Symbol :
                string.Join("::", typeParts.Append(call.MethodName));
            var target = candidates.Length == 1 ? candidates[0] : null;
            var reasons = references.Select(reference => reference.Reason).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal);
            var evidence = $"{call.ReceiverType} {call.ReceiverExpression}.{call.MethodName}(...) via {string.Join("; ", reasons)}";
            var identity = string.Join("\n", "csharp-cppcli-call", call.Path, call.Line, call.Column,
                call.Caller, call.ReceiverType, call.MethodName, status, targetSymbol,
                string.Join("|", candidates.Select(item => $"{item.Path}:{item.Line}:{item.Symbol}")));
            var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16].ToLowerInvariant();
            relations.Add(new NativeFlowRelation(id, "csharp-cppcli-call", status, call.Path, call.Line,
                call.Caller.Length == 0 ? call.ReceiverExpression : call.Caller, target?.Path, target?.Line,
                targetSymbol, null, candidates.Length, evidence, candidates));

            if (candidates.Length != 1) continue;
            var wrapper = wrappers.FirstOrDefault(item => PathEquals(item.Path, target!.Path)
                && item.Line == target.Line && item.Symbol == target.Symbol);
            if (wrapper is null) continue;
            foreach (Match nativeCall in QualifiedCall.Matches(wrapper.Body))
            {
                var owner = nativeCall.Groups["owner"].Value.TrimEnd(':');
                var method = nativeCall.Groups["method"].Value;
                var qualified = owner + "::" + method;
                var definitions = nativeDefinitions.GetValueOrDefault(qualified) ?? Array.Empty<NativeDefinition>();
                var targetLocations = definitions.Select(definition => new NativeFlowTarget(definition.Path,
                        definition.Line, definition.Symbol))
                    .DistinctBy(item => $"{item.Path}\n{item.Line}\n{item.Symbol}", StringComparer.Ordinal)
                    .OrderBy(item => item.Path, RepositoryPathComparer.Instance).ThenBy(item => item.Line).ToArray();
                var nativeStatus = targetLocations.Length switch { 0 => "unresolved", 1 => "candidate", _ => "ambiguous" };
                var callLine = wrapper.LineAtBodyOffset(nativeCall.Index);
                var sourceSymbol = wrapper.Symbol;
                var nativeIdentity = string.Join("\n", "qualified-native-call", wrapper.Path, callLine,
                    sourceSymbol, qualified, string.Join("|", targetLocations.Select(item => $"{item.Path}:{item.Line}:{item.Symbol}")));
                var nativeId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nativeIdentity)))[..16].ToLowerInvariant();
                var nativeTarget = targetLocations.Length == 1 ? targetLocations[0] : null;
                nativeCallRelations.TryAdd(nativeId, new NativeFlowRelation(nativeId, "qualified-native-call",
                    nativeStatus, wrapper.Path, callLine, sourceSymbol, nativeTarget?.Path, nativeTarget?.Line,
                    qualified, null, targetLocations.Length,
                    $"{nativeCall.Groups["qualified"].Value}(...) inside C++/CLI wrapper candidate",
                    targetLocations));
            }
        }

        relations.AddRange(nativeCallRelations.Values);
        return relations.DistinctBy(item => item.Id, StringComparer.Ordinal)
            .OrderBy(item => item.SourcePath, RepositoryPathComparer.Instance).ThenBy(item => item.SourceLine)
            .ThenBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.TargetSymbol, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
    }

    private static NativeSource[] ReadNativeSources(string root, ISet<string> discovered)
    {
        var paths = discovered.Where(IsNativeSource).OrderBy(path => path, RepositoryPathComparer.Instance);
        var sources = new List<NativeSource>();
        foreach (var path in paths)
        {
            try
            {
                var lines = File.ReadAllLines(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                var offsets = new int[lines.Length];
                var offset = 0;
                var inBlockComment = false;
                var quote = '\0';
                var escaped = false;
                string? rawStringTerminator = null;
                var sanitized = new StringBuilder();
                for (var index = 0; index < lines.Length; index++)
                {
                    offsets[index] = offset;
                    var line = ArchitecturePatternAnalyzer.RemoveCommentsAndStrings(lines[index], ref inBlockComment,
                        ref quote, ref escaped, ref rawStringTerminator);
                    sanitized.AppendLine(line);
                    offset += lines[index].Length + Environment.NewLine.Length;
                }
                sources.Add(new NativeSource(path, sanitized.ToString(), offsets));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // Unreadable files are not candidates; the normal scanner reports source read issues.
            }
        }
        return sources.ToArray();
    }

    private static IEnumerable<WrapperMethod> ParseWrappers(NativeSource source)
    {
        foreach (Match classMatch in CliClass.Matches(source.Sanitized))
        {
            var classOpen = classMatch.Index + classMatch.Length - 1;
            var classClose = FindClosingBrace(source.Sanitized, classOpen);
            if (classClose < 0) continue;
            var namespaceName = NamespaceAt(source.Sanitized, classMatch.Index);
            var className = classMatch.Groups["name"].Value;
            var body = source.Sanitized[(classOpen + 1)..classClose];
            foreach (Match methodMatch in InlineMethod.Matches(body))
            {
                var absoluteStart = classOpen + 1 + methodMatch.Index;
                if (BraceDepth(source.Sanitized, classOpen + 1, absoluteStart) != 0) continue;
                var open = absoluteStart + methodMatch.Length - 1;
                var close = FindClosingBrace(source.Sanitized, open);
                if (close < 0 || close > classClose) continue;
                var method = methodMatch.Groups["method"].Value;
                var symbol = string.Join("::", new[] { namespaceName.Replace(".", "::", StringComparison.Ordinal), className, method }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
                yield return new WrapperMethod(source, namespaceName, className, method, symbol,
                    source.LineAt(absoluteStart + methodMatch.Groups["method"].Index), open + 1,
                    close - open - 1);
            }
        }
    }

    private static IEnumerable<NativeDefinition> ParseNativeDefinitions(NativeSource source)
    {
        foreach (Match match in NativeMethod.Matches(source.Sanitized))
        {
            var open = match.Index + match.Length - 1;
            var close = FindClosingBrace(source.Sanitized, open);
            if (close < 0) continue;
            var owner = match.Groups["owner"].Value.TrimEnd(':');
            var method = match.Groups["method"].Value;
            yield return new NativeDefinition(source.Path, source.LineAt(match.Index), owner + "::" + method);
        }
    }

    private static NativeConsumerReference[] ResolveReferences(string root, DotNetProjectInfo project,
        IReadOnlyList<NativeProjectInfo> nativeProjects, ISet<string> discovered)
    {
        var results = new List<NativeConsumerReference>();
        var projectDirectory = Path.GetDirectoryName(project.Path)?.Replace('\\', '/') ?? string.Empty;
        foreach (var reference in project.AssemblyReferences)
        {
            var assemblyName = reference.Include.Split(',')[0].Trim();
            var normalizedHint = NormalizeInRootPath(root, projectDirectory, reference.HintPath);
            if (reference.HintPath is not null && normalizedHint is null) continue;
            if (normalizedHint is not null && discovered.Contains(normalizedHint))
            {
                var exactProject = nativeProjects.FirstOrDefault(native => PathEquals(native.Path, normalizedHint));
                if (exactProject is not null && exactProject.HasClrSupport)
                    results.Add(new NativeConsumerReference(exactProject, assemblyName, normalizedHint,
                        $"assembly reference '{reference.Include}' and in-repository HintPath '{reference.HintPath}'"));
            }

            var hintAssemblyName = reference.HintPath is null ? null : Path.GetFileNameWithoutExtension(reference.HintPath);
            var matches = nativeProjects.Where(native => native.HasClrSupport
                    && (string.Equals(native.TargetName, assemblyName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(Path.GetFileNameWithoutExtension(native.Path), assemblyName, StringComparison.OrdinalIgnoreCase)
                        || hintAssemblyName is not null &&
                            (string.Equals(native.TargetName, hintAssemblyName, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(Path.GetFileNameWithoutExtension(native.Path), hintAssemblyName, StringComparison.OrdinalIgnoreCase))))
                .ToArray();
            foreach (var match in matches)
                results.Add(new NativeConsumerReference(match, assemblyName, normalizedHint,
                    $"assembly identity '{assemblyName}'" + (reference.HintPath is null ? "" : $" and HintPath '{reference.HintPath}'")));
        }

        foreach (var reference in project.ProjectReferences)
        {
            var normalizedPath = NormalizeInRootPath(root, projectDirectory, reference);
            if (normalizedPath is null) continue;
            var target = nativeProjects.FirstOrDefault(native => native.HasClrSupport && PathEquals(native.Path, normalizedPath));
            if (target is not null)
                results.Add(new NativeConsumerReference(target, target.TargetName ?? Path.GetFileNameWithoutExtension(target.Path),
                    normalizedPath, $"project reference '{reference}'"));
        }
        return results.DistinctBy(item => $"{item.Project.Path}\n{item.AssemblyName}\n{item.HintPath}", StringComparer.Ordinal)
            .OrderBy(item => item.Project.Path, RepositoryPathComparer.Instance).ThenBy(item => item.AssemblyName, StringComparer.Ordinal)
            .ToArray();
    }

    private static DotNetProjectInfo? FindManagedProject(string sourcePath, IReadOnlyList<DotNetProjectInfo> projects) => projects
        .Where(project => IsProjectSourcePath(project.Path, sourcePath))
        .OrderByDescending(project => project.Path.Length).ThenBy(project => project.Path, RepositoryPathComparer.Instance)
        .FirstOrDefault();

    private static bool IsProjectSourcePath(string projectPath, string sourcePath)
    {
        var directory = Path.GetDirectoryName(projectPath)?.Replace('\\', '/') ?? string.Empty;
        return directory.Length == 0 || sourcePath.StartsWith(directory + "/", PathComparison());
    }

    private static bool WrapperTypeMatches(WrapperMethod wrapper, string typeName, string simpleTypeName)
    {
        if (!string.Equals(wrapper.Name, simpleTypeName, StringComparison.Ordinal)) return false;
        var namespaceType = string.IsNullOrWhiteSpace(wrapper.Namespace)
            ? wrapper.Name : wrapper.Namespace + "." + wrapper.Name;
        return typeName.Contains(".", StringComparison.Ordinal)
            ? string.Equals(namespaceType, typeName, StringComparison.Ordinal)
            : string.Equals(wrapper.Name, simpleTypeName, StringComparison.Ordinal);
    }

    private static string NormalizeTypeName(string value) =>
        value.Split('<', 2)[0].Trim().Replace("global::", "", StringComparison.Ordinal);

    private static string? NormalizeInRootPath(string root, string projectDirectory, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) return null;
        try
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(fullRoot,
                projectDirectory.Replace('/', Path.DirectorySeparatorChar), path.Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)));
            var prefix = fullRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, PathComparison())) return null;
            return Path.GetRelativePath(fullRoot, fullPath).Replace('\\', '/');
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            return null;
        }
    }

    private static string NamespaceAt(string source, int position)
    {
        return Namespace.Matches(source).Cast<Match>()
            .Where(match => match.Index < position)
            .Select(match =>
            {
                var open = match.Index + match.Length - 1;
                var close = FindClosingBrace(source, open);
                return close >= position ? match.Groups["name"].Value.Replace("::", ".", StringComparison.Ordinal) : null;
            })
            .Where(value => value is not null)
            .OrderByDescending(value => value!.Length)
            .FirstOrDefault() ?? string.Empty;
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

    private static bool IsNativeSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".c" or ".cc" or ".cpp" or ".cxx" or ".h" or ".hh" or ".hpp" or ".hxx" or ".inl" or ".ipp";

    private static bool PathEquals(string left, string right) => PathComparer().Equals(left.Replace('\\', '/'), right.Replace('\\', '/'));
    private static StringComparer PathComparer() => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static StringComparison PathComparison() => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private sealed record NativeSource(string Path, string Sanitized, int[] LineOffsets)
    {
        public int LineAt(int position)
        {
            var index = Array.BinarySearch(LineOffsets, position);
            return (index >= 0 ? index : Math.Max(0, ~index - 1)) + 1;
        }
    }

    private sealed record WrapperMethod(NativeSource Source, string Namespace, string Name, string Method,
        string Symbol, int Line, int BodyStart, int BodyLength)
    {
        public string Path => Source.Path;
        public string Body => Source.Sanitized.Substring(BodyStart, BodyLength);
        public int LineAtBodyOffset(int offset) => Source.LineAt(BodyStart + offset);
    }

    private sealed record NativeDefinition(string Path, int Line, string Symbol);
    private sealed record NativeConsumerReference(NativeProjectInfo Project, string AssemblyName, string? HintPath, string Reason);
}
