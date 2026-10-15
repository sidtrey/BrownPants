using BrownPants.Cli.Analysis;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using CSharpInvocation = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
using CSharpObjectCreation = Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax;
using CSharpImplicitObjectCreation = Microsoft.CodeAnalysis.CSharp.Syntax.ImplicitObjectCreationExpressionSyntax;
using CSharpConstructorInitializer = Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorInitializerSyntax;
using VisualBasicInvocation = Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax;
using VisualBasicObjectCreation = Microsoft.CodeAnalysis.VisualBasic.Syntax.ObjectCreationExpressionSyntax;

namespace BrownPants.Cli.Analysis;

public static class CSharpSemanticAnalyzer
{
    private const int ImpactGraphEdgeLimit = 100_000;

    public static CSharpSemanticAnalysis AnalyzeRepository(string root, IReadOnlyList<string> projectPaths,
        IReadOnlyList<string> solutionPaths, int sampleLimit)
    {
        var issues = new List<string>();
        var issueCount = 0;
        var compilations = new List<(string Project, Compilation Compilation)>();
        var discoveredPaths = projectPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var discovered = discoveredPaths.Length;
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var discoveredFullPaths = discoveredPaths.Select(path => NormalizeProjectPath(Path.Combine(root, path)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        void RecordIssue(string message)
        {
            issueCount++;
            if (issues.Count < sampleLimit)
                issues.Add(WorkspaceDiagnosticNormalizer.Normalize(message, root));
        }

        try
        {
            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();

            using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                ["RestoreIgnoreFailedSources"] = "true",
                ["RestoreDuringBuild"] = "false"
            });
            workspace.RegisterWorkspaceFailedHandler(args =>
            {
                if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                    RecordIssue(args.Diagnostic.Message);
            });

            if (solutionPaths.Count > 0)
            {
                // Use one stable solution context. Every discovered project omitted from it
                // is opened directly below, preventing duplicate evaluation across solutions.
                foreach (var solutionPath in solutionPaths.OrderBy(path => path, StringComparer.Ordinal).Take(1))
                {
                    try
                    {
                        var solution = workspace.OpenSolutionAsync(Path.Combine(root, solutionPath)).GetAwaiter().GetResult();
                        foreach (var project in solution.Projects.Where(project =>
                            project.Language is LanguageNames.CSharp or LanguageNames.VisualBasic))
                            AddProjectOnce(project, compilations, loaded, discoveredFullPaths, RecordIssue, root);
                    }
                    catch (Exception exception) when (IsLoadFailure(exception))
                    {
                        RecordIssue($"Solution '{solutionPath}' could not be loaded: {exception.Message}");
                    }
                }
            }
            foreach (var projectPath in discoveredPaths.Where(path => !loaded.Contains(NormalizeProjectPath(Path.Combine(root, path)))))
            {
                try
                {
                    var project = workspace.OpenProjectAsync(Path.Combine(root, projectPath)).GetAwaiter().GetResult();
                    AddProjectOnce(project, compilations, loaded, discoveredFullPaths, RecordIssue, root);
                }
                catch (Exception exception) when (IsLoadFailure(exception))
                { RecordIssue($"Project '{projectPath}' could not be loaded: {exception.Message}"); }
            }
        }
        catch (Exception exception) when (IsLoadFailure(exception))
        {
            RecordIssue($"MSBuild could not be initialized: {exception.Message}");
        }

        var semantic = AnalyzeCompilations(compilations, discovered, sampleLimit, root);
        var failed = Math.Max(0, discovered - loaded.Count);
        return semantic with { ProjectsLoaded = loaded.Count, ProjectsFailed = failed,
            ProjectCoverageComplete = failed == 0 && issueCount == 0, Issues = issues,
            WorkspaceIssueCount = issueCount, WorkspaceIssueOmittedCount = Math.Max(0, issueCount - issues.Count) };
    }

    public static CSharpSemanticAnalysis AnalyzeCompilations(
        IEnumerable<(string Project, Compilation Compilation)> compilations,
        int projectsDiscovered,
        int sampleLimit,
        string root)
        => AnalyzeCompilations(compilations, projectsDiscovered, sampleLimit, root, ImpactGraphEdgeLimit);

    internal static CSharpSemanticAnalysis AnalyzeCompilations(
        IEnumerable<(string Project, Compilation Compilation)> compilations,
        int projectsDiscovered,
        int sampleLimit,
        string root,
        int impactGraphEdgeLimit)
    {
        if (impactGraphEdgeLimit is < 0 or > ImpactGraphEdgeLimit)
            throw new ArgumentOutOfRangeException(nameof(impactGraphEdgeLimit));

        var resolvedCalls = new List<CSharpResolvedCall>();
        var impactGraphCalls = new List<CSharpResolvedCall>(Math.Min(impactGraphEdgeLimit, 4096));
        var impactGraphOmittedCount = 0;
        var unresolvedCalls = new List<CSharpUnresolvedCall>();
        var declarations = new List<CSharpSemanticDeclaration>();
        var issues = new List<string>();
        var projectsLoaded = 0;
        var resolvedInvocationCount = 0;
        var unresolvedInvocationCount = 0;
        var constructorSyntaxCount = 0;
        var resolvedConstructorCount = 0;
        var unresolvedConstructorCount = 0;
        var declarationCount = 0;
        var invocationSyntaxCount = 0;
        var skippedInvocationCount = 0;
        var reasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var compilerDiagnostics = new List<SemanticCompilerDiagnostic>();
        var compilerDiagnosticGroupCounts = new Dictionary<(string Project, string Code), int>();
        var compilerErrorCount = 0;
        var seenProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var repositoryRoot = Path.GetFullPath(root);

        void AddResolvedCall(CSharpResolvedCall call)
        {
            AddSample(resolvedCalls, call, sampleLimit);
            if (impactGraphCalls.Count < impactGraphEdgeLimit)
                impactGraphCalls.Add(call);
            else
                impactGraphOmittedCount++;
        }

        foreach (var (projectPath, compilation) in compilations)
        {
            if (!TryGetRepositoryRelativePath(repositoryRoot, projectPath, out var relativeProjectPath))
                continue;

            if (!seenProjects.Add(NormalizeProjectPath(projectPath))) continue;

            projectsLoaded++;
            foreach (var diagnostic in compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error))
            {
                compilerErrorCount++;
                var groupKey = (relativeProjectPath, diagnostic.Id);
                compilerDiagnosticGroupCounts[groupKey] = compilerDiagnosticGroupCounts.GetValueOrDefault(groupKey) + 1;
                if (compilerDiagnostics.Count >= sampleLimit) continue;
                string? path = null; int? line = null;
                if (diagnostic.Location.IsInSource && diagnostic.Location.SourceTree is not null
                    && TryGetRepositoryRelativePath(repositoryRoot, diagnostic.Location.SourceTree.FilePath, out var diagnosticPath))
                { path = diagnosticPath; line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1; }
                compilerDiagnostics.Add(new SemanticCompilerDiagnostic(relativeProjectPath, path, line,
                    diagnostic.Id, Normalize(diagnostic.GetMessage())));
            }
            var synthesizedConstructorDeclarations = new Dictionary<string, CSharpSemanticDeclaration>(StringComparer.Ordinal);
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                if (!TryGetRepositoryRelativePath(repositoryRoot, syntaxTree.FilePath, out var relativeSourcePath))
                    continue;

                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var syntaxRoot = syntaxTree.GetRoot();
                foreach (var declarationNode in syntaxRoot.DescendantNodes()
                    .Where(node => node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax
                        or Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax
                        or Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax
                        or Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax
                        or Microsoft.CodeAnalysis.VisualBasic.Syntax.SubNewStatementSyntax))
                {
                    if (semanticModel.GetDeclaredSymbol(declarationNode) is not IMethodSymbol method)
                        continue;
                    declarationCount++;
                    AddSample(declarations, new CSharpSemanticDeclaration(
                        relativeProjectPath,
                        relativeSourcePath,
                        declarationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        ResolvedTarget(method, repositoryRoot)), sampleLimit);
                }
                foreach (var typeDeclarationNode in syntaxRoot.DescendantNodes().Where(node =>
                    node is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax
                        or Microsoft.CodeAnalysis.CSharp.Syntax.RecordDeclarationSyntax
                        or Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassStatementSyntax
                        or Microsoft.CodeAnalysis.VisualBasic.Syntax.StructureStatementSyntax))
                {
                    if (semanticModel.GetDeclaredSymbol(typeDeclarationNode) is not INamedTypeSymbol
                        { TypeKind: TypeKind.Class or TypeKind.Struct } typeSymbol)
                        continue;

                    foreach (var constructor in typeSymbol.InstanceConstructors)
                    {
                        if (constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is
                            Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax
                                or Microsoft.CodeAnalysis.VisualBasic.Syntax.SubNewStatementSyntax))
                            continue;

                        var sourceLocation = ConstructorSourceLocation(constructor, repositoryRoot);
                        if (sourceLocation.Location is null && constructor.IsImplicitlyDeclared)
                            sourceLocation = (typeDeclarationNode.GetLocation(), relativeSourcePath);
                        if (sourceLocation.Location is null || sourceLocation.Path is null)
                            continue;

                        var symbol = ResolvedTarget(constructor, repositoryRoot);
                        var key = string.Join("\n", relativeProjectPath, symbol);
                        var declaration = new CSharpSemanticDeclaration(relativeProjectPath, sourceLocation.Path,
                            sourceLocation.Location.GetLineSpan().StartLinePosition.Line + 1, symbol);
                        if (!synthesizedConstructorDeclarations.TryGetValue(key, out var prior)
                            || string.CompareOrdinal(declaration.Path, prior.Path) < 0
                            || (string.Equals(declaration.Path, prior.Path, StringComparison.Ordinal)
                                && declaration.Line < prior.Line))
                            synthesizedConstructorDeclarations[key] = declaration;
                    }
                }
                foreach (var invocationNode in syntaxRoot.DescendantNodes()
                    .Where(node => node is CSharpInvocation or VisualBasicInvocation))
                {
                    invocationSyntaxCount++;
                    var expressionText = invocationNode switch
                    {
                        CSharpInvocation csharp => csharp.Expression.ToString(),
                        VisualBasicInvocation visualBasic => visualBasic.Expression.ToString(),
                        _ => throw new InvalidOperationException("Unexpected invocation syntax.")
                    };
                    if (invocationNode is CSharpInvocation && expressionText == "nameof")
                    {
                        skippedInvocationCount++;
                        continue;
                    }

                    var symbolInfo = semanticModel.GetSymbolInfo(invocationNode);
                    var symbol = symbolInfo.Symbol;
                    if (symbol is IMethodSymbol method)
                    {
                        resolvedInvocationCount++;
                        var sourceLocation = method.Locations
                            .Where(location => location.IsInSource && location.SourceTree is not null)
                            .Select(location => TryGetRepositoryRelativePath(repositoryRoot, location.SourceTree!.FilePath,
                                out var targetPath) ? (Location: location, Path: targetPath) : default)
                            .Where(candidate => candidate.Location is not null)
                            .OrderBy(candidate => candidate.Path, StringComparer.Ordinal)
                            .FirstOrDefault();
                        AddResolvedCall(new CSharpResolvedCall(
                            relativeProjectPath,
                            relativeSourcePath,
                            invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Normalize(expressionText),
                            EnclosingMethod(semanticModel.GetEnclosingSymbol(invocationNode.SpanStart), repositoryRoot),
                            ResolvedTarget(method, repositoryRoot),
                            sourceLocation.Location is not null ? sourceLocation.Path : null,
                            sourceLocation.Location?.GetLineSpan().StartLinePosition.Line + 1, "method",
                            ResolvedTarget(method.OriginalDefinition, repositoryRoot),
                            invocationNode.GetLocation().GetLineSpan().StartLinePosition.Character + 1));
                    }
                    else
                    {
                        unresolvedInvocationCount++;
                        var reason = symbolInfo.CandidateReason.ToString();
                        reasonCounts[reason] = reasonCounts.GetValueOrDefault(reason) + 1;
                        AddSample(unresolvedCalls, new CSharpUnresolvedCall(
                            relativeProjectPath,
                            relativeSourcePath,
                            invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Normalize(expressionText),
                            reason,
                            symbolInfo.CandidateSymbols.Take(10)
                                .Select(candidate => candidate.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
                                .ToArray()), sampleLimit);
                    }
                }

                foreach (var constructionNode in syntaxRoot.DescendantNodes()
                    .Where(node => node is CSharpObjectCreation or CSharpImplicitObjectCreation
                        or CSharpConstructorInitializer or VisualBasicObjectCreation))
                {
                    constructorSyntaxCount++;
                    var expression = constructionNode.ToString();
                    var symbolInfo = semanticModel.GetSymbolInfo(constructionNode);
                    if (symbolInfo.Symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } method)
                    {
                        resolvedConstructorCount++;
                        var sourceLocation = ConstructorSourceLocation(method, repositoryRoot);
                        AddResolvedCall(new CSharpResolvedCall(relativeProjectPath, relativeSourcePath,
                            constructionNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Normalize(expression),
                            EnclosingMethod(semanticModel.GetEnclosingSymbol(constructionNode.SpanStart), repositoryRoot),
                            ResolvedTarget(method, repositoryRoot), sourceLocation.Path,
                            sourceLocation.Location?.GetLineSpan().StartLinePosition.Line + 1, "constructor",
                            ResolvedTarget(method.OriginalDefinition, repositoryRoot),
                            constructionNode.GetLocation().GetLineSpan().StartLinePosition.Character + 1));
                    }
                    else
                    {
                        unresolvedConstructorCount++;
                        var reason = symbolInfo.CandidateReason.ToString();
                        reasonCounts[reason] = reasonCounts.GetValueOrDefault(reason) + 1;
                        AddSample(unresolvedCalls, new CSharpUnresolvedCall(relativeProjectPath, relativeSourcePath,
                            constructionNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Normalize(expression), reason,
                            symbolInfo.CandidateSymbols.Take(10)
                                .Select(candidate => candidate.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
                                .ToArray(), "constructor"), sampleLimit);
                    }
                }
            }
            foreach (var declaration in synthesizedConstructorDeclarations.Values
                .OrderBy(item => item.Path, StringComparer.Ordinal).ThenBy(item => item.Line)
                .ThenBy(item => item.Symbol, StringComparer.Ordinal))
            {
                declarationCount++;
                AddSample(declarations, declaration, sampleLimit);
            }
        }

        var compilerDiagnosticGroups = compilerDiagnosticGroupCounts
            .OrderBy(pair => pair.Key.Project, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Code, StringComparer.Ordinal)
            .Take(sampleLimit)
            .Select(pair => new SemanticCompilerDiagnosticGroup(pair.Key.Project, pair.Key.Code, pair.Value))
            .ToArray();

        return new CSharpSemanticAnalysis(projectsDiscovered, projectsLoaded, resolvedInvocationCount,
            unresolvedInvocationCount, resolvedCalls, unresolvedCalls, issues, declarationCount, declarations,
            invocationSyntaxCount, skippedInvocationCount, resolvedInvocationCount + unresolvedInvocationCount,
            reasonCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new SemanticReasonCount(pair.Key, pair.Value)).ToArray(),
            Math.Max(0, projectsDiscovered - projectsLoaded), projectsDiscovered == projectsLoaded, 0, 0,
            compilerErrorCount, Math.Max(0, compilerErrorCount - compilerDiagnostics.Count), compilerDiagnostics,
            constructorSyntaxCount, resolvedConstructorCount, unresolvedConstructorCount)
        {
            ImpactGraphCalls = impactGraphCalls,
            ImpactGraphOmittedCount = impactGraphOmittedCount,
            CompilerDiagnosticGroupCount = compilerDiagnosticGroupCounts.Count,
            CompilerDiagnosticGroupOmittedCount = Math.Max(0, compilerDiagnosticGroupCounts.Count - compilerDiagnosticGroups.Length),
            CompilerDiagnosticGroups = compilerDiagnosticGroups
        };
    }

    private static void AddProjectOnce(Project project, List<(string Project, Compilation Compilation)> compilations,
        HashSet<string> loaded, HashSet<string> discoveredPaths, Action<string> recordIssue, string root)
    {
        var path = project.FilePath ?? project.Name;
        var normalizedPath = NormalizeProjectPath(path, root);
        if (!discoveredPaths.Contains(normalizedPath)) return;
        if (!loaded.Add(normalizedPath)) return;
        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
        if (compilation is null)
        {
            loaded.Remove(normalizedPath);
            recordIssue($"Project '{path}' produced no compilation.");
            return;
        }
        compilations.Add((path, compilation));
    }

    private static bool IsLoadFailure(Exception exception) => exception is not OutOfMemoryException
        and not StackOverflowException
        and not AccessViolationException;

    private static string Normalize(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeProjectPath(string path, string? root = null) =>
        Path.GetFullPath(path, root ?? Environment.CurrentDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string ResolvedTarget(IMethodSymbol method, string repositoryRoot) =>
        CallableIdentity(method, repositoryRoot);

    private static (Location? Location, string? Path) ConstructorSourceLocation(IMethodSymbol method, string repositoryRoot)
    {
        var locations = method.Locations
            .Concat(method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax().GetLocation()))
            .Concat(method.IsImplicitlyDeclared ? method.ContainingType.Locations : Array.Empty<Location>());
        return locations
            .Where(location => location.IsInSource && location.SourceTree is not null)
            .Select(location => TryGetRepositoryRelativePath(repositoryRoot, location.SourceTree!.FilePath,
                out var path) ? (Location: location, Path: path) : default)
            .Where(candidate => candidate.Location is not null)
            .OrderBy(candidate => candidate.Path, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Location!.SourceSpan.Start)
            .FirstOrDefault();
    }

    private static string CallableIdentity(ISymbol? symbol, string repositoryRoot)
    {
        if (symbol is not IMethodSymbol method)
            return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "<unknown>";

        if (method.MethodKind == MethodKind.AnonymousFunction)
        {
            var kind = method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
                .Any(node => node is Microsoft.CodeAnalysis.CSharp.Syntax.AnonymousMethodExpressionSyntax)
                ? "anonymous"
                : "lambda";
            return $"{CallableIdentity(method.ContainingSymbol, repositoryRoot)}::<{kind}@{CallableSourceAnchor(method, repositoryRoot)}>";
        }

        if (method.MethodKind == MethodKind.LocalFunction)
            return $"{CallableIdentity(method.ContainingSymbol, repositoryRoot)}::<local {MethodSignature(method)}>";

        return MethodSignature(method);
    }

    private static string MethodSignature(IMethodSymbol method)
    {
        var containingType = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var genericArguments = method.TypeArguments.Length == 0
            ? string.Empty
            : $"<{string.Join(", ", method.TypeArguments.Select(argument => argument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>";
        var parameters = string.Join(", ", method.Parameters.Select(parameter =>
            parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        return $"{containingType}.{method.Name}{genericArguments}({parameters})";
    }

    private static string CallableSourceAnchor(IMethodSymbol method, string repositoryRoot)
    {
        var location = method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax().GetLocation())
            .Where(candidate => candidate.IsInSource && candidate.SourceTree is not null)
            .OrderBy(candidate => candidate.SourceTree!.FilePath, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.SourceSpan.Start).FirstOrDefault();
        if (location is null) return "<external>:1:1";

        var linePosition = location.GetLineSpan().StartLinePosition;
        var path = TryGetRepositoryRelativePath(repositoryRoot, location.SourceTree!.FilePath, out var relativePath)
            ? relativePath
            : "<external>";
        return $"{path}:{linePosition.Line + 1}:{linePosition.Character + 1}";
    }

    private static string EnclosingMethod(ISymbol? symbol, string repositoryRoot) =>
        CallableIdentity(symbol, repositoryRoot);

    private static bool TryGetRepositoryRelativePath(string root, string path, out string relativePath)
    {
        relativePath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(path, root);
            var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
            if (Path.IsPathRooted(relative) || relative == "." || relative == ".."
                || relative.StartsWith("../", StringComparison.Ordinal))
                return false;

            relativePath = relative;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            return false;
        }
    }

    private static void AddSample<T>(List<T> values, T value, int limit)
    {
        if (values.Count < limit)
            values.Add(value);
    }
}
