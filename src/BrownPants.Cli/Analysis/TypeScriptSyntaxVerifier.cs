using System.Diagnostics;
using System.Text.Json;

namespace BrownPants.Cli.Analysis;

internal interface ITypeScriptSyntaxVerifier
{
    TypeScriptSyntaxVerification Verify(string root, IReadOnlyList<string> sourcePaths,
        IReadOnlyList<string> packageManifestPaths, IReadOnlyList<string> reactProjectManifestPaths, int sampleLimit);
}

internal sealed class TypeScriptSyntaxVerifier : ITypeScriptSyntaxVerifier
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(45);
    private const int MaximumPackageJsonBytes = 1_048_576;
    private const int MaximumDiagnostics = 10_000;
    private const string ParserAdapter = """
        const fs = require('node:fs');
        const path = require('node:path');
        const compilerPath = process.argv[1];
        const rootPath = process.argv[2];
        const request = JSON.parse(fs.readFileSync(0, 'utf8'));
        const root = fs.realpathSync(rootPath);
        const inside = (candidate) => {
          const relative = path.relative(root, candidate);
          return relative === '' || (!relative.startsWith('..' + path.sep) && relative !== '..' && !path.isAbsolute(relative));
        };
        const rejectLinks = (candidate) => {
          let current = path.resolve(candidate);
          const parts = [];
          while (current !== root && inside(current)) {
            parts.push(current);
            const parent = path.dirname(current);
            if (parent === current) break;
            current = parent;
          }
          if (current !== root) throw new Error('Path escapes the scan root');
          for (const part of parts) if (fs.lstatSync(part).isSymbolicLink()) throw new Error('Reparse or symbolic-link path is not allowed');
        };
        rejectLinks(compilerPath);
        const ts = require(compilerPath);
        const results = [];
        let diagnosticCount = 0;
        for (const fileEntry of request.files) {
          const relative = fileEntry.path;
          const absolute = path.resolve(root, relative);
          if (!inside(absolute) || absolute === root) throw new Error('Source path escapes the scan root');
          rejectLinks(absolute);
          const text = fs.readFileSync(absolute, 'utf8');
          const extension = path.extname(relative).toLowerCase();
          const kind = extension === '.tsx' ? ts.ScriptKind.TSX : extension === '.jsx' ? ts.ScriptKind.JSX
            : ['.ts', '.mts', '.cts'].includes(extension) ? ts.ScriptKind.TS : ts.ScriptKind.JS;
          const source = ts.createSourceFile(relative, text, ts.ScriptTarget.Latest, true, kind);
          const diagnostics = [];
          for (const item of source.parseDiagnostics) {
            if (diagnosticCount >= 10000) break;
            const position = item.file && item.start != null ? item.file.getLineAndCharacterOfPosition(item.start) : null;
            diagnostics.push({
              code: item.code,
              message: ts.flattenDiagnosticMessageText(item.messageText, '\n'),
              line: position ? position.line + 1 : null,
              column: position ? position.character + 1 : null
            });
            diagnosticCount++;
          }
          const reactComponents = [];
          const reactHookCalls = [];
          if (fileEntry.reactProject) {
            const locationOf = (node) => {
              const point = source.getLineAndCharacterOfPosition(node.getStart(source));
              return { line: point.line + 1, column: point.character + 1 };
            };
            const hasJsx = (rootNode) => {
              let found = false;
              const find = (node) => {
                if (found) return;
                if (node !== rootNode && ts.isFunctionLike(node)) return;
                if (ts.isJsxElement(node) || ts.isJsxSelfClosingElement(node) || ts.isJsxFragment(node)) {
                  found = true;
                  return;
                }
                ts.forEachChild(node, find);
              };
              find(rootNode);
              return found;
            };
            const addComponent = (identifier) => {
              if (!identifier || !/^[A-Z]/.test(identifier.text)) return;
              const location = locationOf(identifier);
              reactComponents.push({ name: identifier.text, line: location.line, column: location.column, kind: 'function-component-candidate' });
            };
            const hookName = (expression) => ts.isIdentifier(expression) ? expression.text
              : ts.isPropertyAccessExpression(expression) ? expression.name.text : null;
            const visitReact = (node) => {
              if (ts.isFunctionDeclaration(node) && node.body && hasJsx(node.body)) addComponent(node.name);
              if (ts.isVariableDeclaration(node) && ts.isIdentifier(node.name)
                && node.initializer && (ts.isArrowFunction(node.initializer) || ts.isFunctionExpression(node.initializer))
                && hasJsx(node.initializer.body)) addComponent(node.name);
              if (ts.isCallExpression(node)) {
                const name = hookName(node.expression);
                if (name && /^use[A-Z0-9]/.test(name)) {
                  const location = locationOf(node.expression);
                  reactHookCalls.push({ name, line: location.line, column: location.column, kind: 'hook-call-candidate' });
                }
              }
              ts.forEachChild(node, visitReact);
            };
            visitReact(source);
          }
          results.push({ path: relative.replaceAll('\\', '/'), status: 'verified', diagnostics, reactComponents, reactHookCalls });
        }
        process.stdout.write(JSON.stringify({ compilerVersion: ts.version, results, diagnosticsTruncated: diagnosticCount >= 10000 }));
        """;

    public TypeScriptSyntaxVerification Verify(string root, IReadOnlyList<string> sourcePaths,
        IReadOnlyList<string> packageManifestPaths, IReadOnlyList<string> reactProjectManifestPaths, int sampleLimit)
    {
        var canonicalRoot = Path.GetFullPath(root);
        var files = sourcePaths.Where(IsSupportedSource)
            .Distinct(PathComparer)
            .OrderBy(path => path, PathComparer)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var reactProjectDirectories = reactProjectManifestPaths.Select(path => Path.GetDirectoryName(
                Path.GetFullPath(Path.Combine(canonicalRoot, path))))
            .Where(path => path is not null).Cast<string>().Distinct(PathComparer).ToArray();
        if (files.Length == 0)
            return Empty("not-applicable", "No supported JavaScript or TypeScript source files were discovered.");

        var compilers = new Dictionary<string, CompilerGroup>(PathComparer);
        var fileResults = new List<TypeScriptSyntaxFileResult>(files.Length);
        var diagnostics = new List<TypeScriptSyntaxDiagnostic>();
        var reactComponents = new List<ReactSourceCandidate>();
        var reactHookCalls = new List<ReactSourceCandidate>();
        var parsers = new List<SyntaxParserInfo>();
        var messages = new SortedSet<string>(StringComparer.Ordinal);
        var candidates = FindCompilerCandidates(canonicalRoot, packageManifestPaths, messages);
        foreach (var relativePath in files)
        {
            var discovered = FindNearestCompiler(canonicalRoot, relativePath, candidates);
            if (discovered.CompilerPath is null)
            {
                fileResults.Add(new TypeScriptSyntaxFileResult(relativePath, "unavailable", discovered.Message));
                if (!string.IsNullOrWhiteSpace(discovered.Message)) messages.Add(discovered.Message);
                continue;
            }

            if (!compilers.TryGetValue(discovered.CompilerPath, out var group))
            {
                group = new CompilerGroup(discovered.Version!, discovered.CompilerPath);
                compilers.Add(discovered.CompilerPath, group);
            }
            group.Files.Add(relativePath);
        }

        var diagnosticsTruncated = false;
        foreach (var group in compilers.Values.OrderBy(item => item.Path, PathComparer).ThenBy(item => item.Path, StringComparer.Ordinal))
        {
            var relativeCompilerPath = Path.GetRelativePath(canonicalRoot, group.Path).Replace('\\', '/');
            var reactFiles = group.Files.ToDictionary(path => path,
                path => IsReactProjectFile(canonicalRoot, path, reactProjectDirectories), PathComparer);
            var processResult = RunParser(canonicalRoot, group.Path, group.Files, reactFiles);
            if (!processResult.Success)
            {
                messages.Add(processResult.Message!);
                fileResults.AddRange(group.Files.Select(path => new TypeScriptSyntaxFileResult(path,
                    processResult.Status, processResult.Message)));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(processResult.CompilerVersion))
                parsers.Add(new SyntaxParserInfo("TypeScript", processResult.CompilerVersion, relativeCompilerPath));
            diagnosticsTruncated |= processResult.DiagnosticsTruncated;
            foreach (var file in processResult.Files)
            {
                fileResults.Add(new TypeScriptSyntaxFileResult(file.Path, "verified", null));
                reactComponents.AddRange(file.ReactComponents);
                reactHookCalls.AddRange(file.ReactHookCalls);
                foreach (var diagnostic in file.Diagnostics)
                {
                    if (diagnostics.Count >= MaximumDiagnostics)
                    {
                        diagnosticsTruncated = true;
                        break;
                    }
                    diagnostics.Add(diagnostic);
                }
            }
        }

        var orderedFiles = fileResults.OrderBy(item => item.Path, PathComparer).ThenBy(item => item.Path, StringComparer.Ordinal).ToArray();
        var orderedDiagnostics = diagnostics.OrderBy(item => item.Path, PathComparer).ThenBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line).ThenBy(item => item.Column).ThenBy(item => item.Code).ToArray();
        var status = orderedFiles.All(item => item.Status == "verified") ? "verified"
            : orderedFiles.Any(item => item.Status == "verified") ? "partial"
            : orderedFiles.Any(item => item.Status == "failed") ? "failed" : "unavailable";
        if (diagnosticsTruncated) messages.Add("Parser diagnostics reached the 10,000-entry limit.");
        var orderedMessages = messages.ToArray();
        var selectedFiles = orderedFiles.Take(sampleLimit).ToArray();
        var selectedDiagnostics = orderedDiagnostics.Take(sampleLimit).ToArray();
        var selectedParsers = parsers.OrderBy(item => item.Path, PathComparer).ThenBy(item => item.Path, StringComparer.Ordinal)
            .Take(sampleLimit).ToArray();
        return new TypeScriptSyntaxVerification(status, orderedFiles.Length, Math.Max(0, orderedFiles.Length - selectedFiles.Length),
            orderedDiagnostics.Length, Math.Max(0, orderedDiagnostics.Length - selectedDiagnostics.Length),
            selectedParsers, selectedFiles, selectedDiagnostics, 0, 0, Array.Empty<JavaScriptTypeScriptIssue>(),
            orderedMessages.Length == 0 ? null : string.Join("; ", orderedMessages))
        {
            AllVerifiedSourcePaths = orderedFiles.Where(file => file.Status == "verified")
                .Select(file => file.Path).ToHashSet(PathComparer),
            AllReactComponents = reactComponents,
            AllReactHookCalls = reactHookCalls
        };
    }

    private static TypeScriptSyntaxVerification Empty(string status, string message) => new(status, 0, 0, 0, 0,
        Array.Empty<SyntaxParserInfo>(), Array.Empty<TypeScriptSyntaxFileResult>(), Array.Empty<TypeScriptSyntaxDiagnostic>(),
        0, 0, Array.Empty<JavaScriptTypeScriptIssue>(), message);

    internal static IReadOnlyList<CompilerCandidate> FindCompilerCandidates(string root,
        IReadOnlyList<string> packageManifestPaths, SortedSet<string> messages)
    {
        var candidates = new Dictionary<string, CompilerCandidate>(PathComparer);
        foreach (var manifestPath in packageManifestPaths.Where(path =>
                     Path.GetFileName(path).Equals("package.json", StringComparison.OrdinalIgnoreCase))
                 .OrderBy(path => path, PathComparer).ThenBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                var manifest = Path.GetFullPath(Path.Combine(root, manifestPath));
                if (!IsContained(root, manifest)) continue;
                var scope = Path.GetDirectoryName(manifest)!;
                var packageDirectory = Path.Combine(scope, "node_modules", "typescript");
                var compiler = Path.Combine(packageDirectory, "lib", "typescript.js");
                var compilerManifest = Path.Combine(packageDirectory, "package.json");
                if (!File.Exists(compiler) && !Directory.Exists(packageDirectory)) continue;
                if (!IsContained(root, compiler) || HasReparsePoint(root, compiler) || HasReparsePoint(root, compilerManifest))
                {
                    messages.Add($"Ignored a local TypeScript package reached through a path outside the scan root or a reparse point: {manifestPath}");
                    continue;
                }
                if (!File.Exists(compiler) || !File.Exists(compilerManifest))
                {
                    messages.Add($"The local TypeScript package declared near {manifestPath} has no readable parser entry and manifest.");
                    continue;
                }
                if (new FileInfo(compilerManifest).Length > MaximumPackageJsonBytes)
                {
                    messages.Add($"The TypeScript package manifest near {manifestPath} exceeds the 1 MiB limit.");
                    continue;
                }
                using var document = JsonDocument.Parse(File.ReadAllText(compilerManifest));
                var version = document.RootElement.TryGetProperty("version", out var value) ? value.GetString() : null;
                if (string.IsNullOrWhiteSpace(version))
                {
                    messages.Add($"The TypeScript package manifest near {manifestPath} has no version.");
                    continue;
                }
                var fullCompilerPath = Path.GetFullPath(compiler);
                candidates.TryAdd(fullCompilerPath, new CompilerCandidate(scope, fullCompilerPath, version));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException
                or JsonException or NotSupportedException)
            {
                messages.Add($"TypeScript compiler discovery failed near {manifestPath}: {exception.Message}");
            }
        }
        return candidates.Values.OrderBy(candidate => candidate.Path, PathComparer)
            .ThenBy(candidate => candidate.Path, StringComparer.Ordinal).ToArray();
    }

    internal static (string? CompilerPath, string? Version, string? Message) FindNearestCompiler(string root,
        string relativeSourcePath, IReadOnlyList<CompilerCandidate> candidates)
    {
        var sourcePath = Path.GetFullPath(Path.Combine(root, relativeSourcePath));
        if (!IsContained(root, sourcePath)) return (null, null, "Source path escapes the scan root.");
        if (candidates.Count == 0) return (null, null, "No repository-local TypeScript compiler package was found.");
        var sourceDirectory = Path.GetDirectoryName(sourcePath)!;
        var selected = candidates.Select(candidate => (Candidate: candidate,
                Distance: DirectoryDistance(root, sourceDirectory, candidate.ScopeDirectory)))
            .OrderBy(item => item.Distance).ThenBy(item => item.Candidate.Path, PathComparer)
            .ThenBy(item => item.Candidate.Path, StringComparer.Ordinal).First().Candidate;
        return (selected.Path, selected.Version, null);
    }

    private static int DirectoryDistance(string root, string left, string right)
    {
        var leftParts = Path.GetRelativePath(root, left).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        var rightParts = Path.GetRelativePath(root, right).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        var common = 0;
        while (common < leftParts.Length && common < rightParts.Length
            && PathComparer.Equals(leftParts[common], rightParts[common])) common++;
        return leftParts.Length + rightParts.Length - 2 * common;
    }

    private static bool HasReparsePoint(string root, string candidate)
    {
        var fullPath = Path.GetFullPath(candidate);
        if (!IsContained(root, fullPath)) return true;
        var relative = Path.GetRelativePath(root, fullPath);
        var current = root;
        foreach (var segment in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current)) continue;
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
        }
        return false;
    }

    private static bool IsContained(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, PathComparison)
            && !Path.IsPathRooted(relative);
    }

    private static bool IsSupportedSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".js" or ".jsx" or ".mjs" or ".cjs" or ".ts" or ".tsx" or ".mts" or ".cts";

    private static bool IsReactProjectFile(string root, string relativePath, IReadOnlyList<string> reactProjectDirectories)
    {
        var sourcePath = Path.GetFullPath(Path.Combine(root, relativePath));
        return reactProjectDirectories.Any(directory => IsContained(directory, sourcePath));
    }

    private static ParserProcessResult RunParser(string root, string compilerPath, IReadOnlyList<string> files,
        IReadOnlyDictionary<string, bool> reactFiles)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "node",
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--eval");
            startInfo.ArgumentList.Add(ParserAdapter);
            startInfo.ArgumentList.Add(compilerPath);
            startInfo.ArgumentList.Add(root);
            startInfo.Environment.Remove("NODE_OPTIONS");
            startInfo.Environment.Remove("NODE_PATH");
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start()) return ParserProcessResult.Failed("Node did not start the TypeScript parser.");

            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            var requestFiles = files.Select(path => new { path, reactProject = reactFiles[path] }).ToArray();
            process.StandardInput.Write(JsonSerializer.Serialize(new { files = requestFiles }));
            process.StandardInput.Close();
            if (!process.WaitForExit((int)ProcessTimeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                return ParserProcessResult.Failed("The local TypeScript parser exceeded the 45-second timeout.");
            }

            var output = standardOutput.GetAwaiter().GetResult();
            var error = standardError.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
                return ParserProcessResult.Failed(string.IsNullOrWhiteSpace(error)
                    ? $"The local TypeScript parser exited with code {process.ExitCode}."
                    : $"The local TypeScript parser failed: {error.Trim()}");
            var result = JsonSerializer.Deserialize<NodeParserResult>(output,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result is null || string.IsNullOrWhiteSpace(result.CompilerVersion) || result.Results is null)
                return ParserProcessResult.Failed("The local TypeScript parser returned an invalid response.");
            var expectedPaths = files.ToHashSet(PathComparer);
            if (result.Results.Length != expectedPaths.Count
                || result.Results.Any(item => !expectedPaths.Remove(item.Path)) || expectedPaths.Count != 0)
                return ParserProcessResult.Failed("The local TypeScript parser returned a missing, duplicate, or unexpected source path.");
            var parsedFiles = result.Results.Select(item => new ParsedFile(item.Path,
                (item.Diagnostics ?? Array.Empty<NodeDiagnostic>()).Select(diagnostic => new TypeScriptSyntaxDiagnostic(
                    item.Path, diagnostic.Code.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    diagnostic.Message ?? string.Empty, diagnostic.Line, diagnostic.Column)).ToArray(),
                (item.ReactComponents ?? Array.Empty<NodeCandidate>()).Select(candidate => new ReactSourceCandidate(
                    item.Path, candidate.Name, candidate.Line, candidate.Column, candidate.Kind)).ToArray(),
                (item.ReactHookCalls ?? Array.Empty<NodeCandidate>()).Select(candidate => new ReactSourceCandidate(
                    item.Path, candidate.Name, candidate.Line, candidate.Column, candidate.Kind)).ToArray())).ToArray();
            return new ParserProcessResult(true, "verified", parsedFiles, result.DiagnosticsTruncated, result.CompilerVersion, null);
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return ParserProcessResult.Unavailable($"Node.js is not available on PATH: {exception.Message}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException
            or JsonException or ArgumentException)
        {
            return ParserProcessResult.Failed($"Unable to run the local TypeScript parser: {exception.Message}");
        }
    }

    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private sealed class CompilerGroup(string version, string path)
    {
        public string Version { get; } = version;
        public string Path { get; } = path;
        public List<string> Files { get; } = new();
    }

    internal sealed record CompilerCandidate(string ScopeDirectory, string Path, string Version);
    private sealed record NodeParserResult(string? CompilerVersion, NodeParsedFile[]? Results, bool DiagnosticsTruncated);
    private sealed record NodeParsedFile(string Path, NodeDiagnostic[]? Diagnostics,
        NodeCandidate[]? ReactComponents, NodeCandidate[]? ReactHookCalls);
    private sealed record NodeDiagnostic(int Code, string? Message, int? Line, int? Column);
    private sealed record NodeCandidate(string Name, int Line, int Column, string Kind);
    private sealed record ParsedFile(string Path, IReadOnlyList<TypeScriptSyntaxDiagnostic> Diagnostics,
        IReadOnlyList<ReactSourceCandidate> ReactComponents, IReadOnlyList<ReactSourceCandidate> ReactHookCalls);
    private sealed record ParserProcessResult(bool Success, string Status, IReadOnlyList<ParsedFile> Files,
        bool DiagnosticsTruncated, string? CompilerVersion, string? Message)
    {
        public static ParserProcessResult Failed(string message) => new(false, "failed", Array.Empty<ParsedFile>(), false, null, message);
        public static ParserProcessResult Unavailable(string message) => new(false, "unavailable", Array.Empty<ParsedFile>(), false, null, message);
    }
}
