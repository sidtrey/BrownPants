using BrownPants.Cli.Analysis;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using CSharpInvocation = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
using VisualBasicInvocation = Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax;

namespace BrownPants.Cli.Analysis;

public static class CSharpSemanticAnalyzer
{
    public static CSharpSemanticAnalysis AnalyzeRepository(string root, IReadOnlyList<string> projectPaths,
        IReadOnlyList<string> solutionPaths, int sampleLimit)
    {
        var issues = new List<string>();
        var compilations = new List<(string Project, Compilation Compilation)>();
        var discovered = projectPaths.Count;

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
                    AddSample(issues, args.Diagnostic.Message, sampleLimit);
            });

            if (solutionPaths.Count > 0)
            {
                foreach (var solutionPath in solutionPaths)
                {
                    try
                    {
                        var solution = workspace.OpenSolutionAsync(Path.Combine(root, solutionPath)).GetAwaiter().GetResult();
                        foreach (var project in solution.Projects.Where(project =>
                            project.Language is LanguageNames.CSharp or LanguageNames.VisualBasic))
                            AddProject(project, compilations, issues, sampleLimit);
                    }
                    catch (Exception exception) when (IsLoadFailure(exception))
                    {
                        AddSample(issues, $"Solution '{solutionPath}' could not be loaded: {exception.Message}", sampleLimit);
                    }
                }
            }
            else
            {
                foreach (var projectPath in projectPaths)
                {
                    try
                    {
                        var project = workspace.OpenProjectAsync(Path.Combine(root, projectPath)).GetAwaiter().GetResult();
                        AddProject(project, compilations, issues, sampleLimit);
                    }
                    catch (Exception exception) when (IsLoadFailure(exception))
                    {
                        AddSample(issues, $"Project '{projectPath}' could not be loaded: {exception.Message}", sampleLimit);
                    }
                }
            }
        }
        catch (Exception exception) when (IsLoadFailure(exception))
        {
            AddSample(issues, $"MSBuild could not be initialized: {exception.Message}", sampleLimit);
        }

        var semantic = AnalyzeCompilations(compilations, discovered, sampleLimit, root);
        return semantic with { Issues = issues.Concat(semantic.Issues).Distinct(StringComparer.Ordinal).Take(sampleLimit).ToArray() };
    }

    public static CSharpSemanticAnalysis AnalyzeCompilations(
        IEnumerable<(string Project, Compilation Compilation)> compilations,
        int projectsDiscovered,
        int sampleLimit,
        string root)
    {
        var resolvedCalls = new List<CSharpResolvedCall>();
        var unresolvedCalls = new List<CSharpUnresolvedCall>();
        var issues = new List<string>();
        var projectsLoaded = 0;
        var resolvedInvocationCount = 0;
        var unresolvedInvocationCount = 0;

        foreach (var (projectPath, compilation) in compilations)
        {
            projectsLoaded++;
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var syntaxRoot = syntaxTree.GetRoot();
                foreach (var invocationNode in syntaxRoot.DescendantNodes()
                    .Where(node => node is CSharpInvocation or VisualBasicInvocation))
                {
                    var expressionText = invocationNode switch
                    {
                        CSharpInvocation csharp => csharp.Expression.ToString(),
                        VisualBasicInvocation visualBasic => visualBasic.Expression.ToString(),
                        _ => throw new InvalidOperationException("Unexpected invocation syntax.")
                    };
                    if (invocationNode is CSharpInvocation && expressionText == "nameof")
                        continue;

                    var symbolInfo = semanticModel.GetSymbolInfo(invocationNode);
                    var symbol = symbolInfo.Symbol;
                    if (symbol is IMethodSymbol method)
                    {
                        resolvedInvocationCount++;
                        AddSample(resolvedCalls, new CSharpResolvedCall(
                            RelativePath(root, projectPath),
                            RelativePath(root, syntaxTree.FilePath),
                            invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Normalize(expressionText),
                            EnclosingMethod(semanticModel.GetEnclosingSymbol(invocationNode.SpanStart)),
                            ResolvedTarget(method)), sampleLimit);
                    }
                    else
                    {
                        unresolvedInvocationCount++;
                        AddSample(unresolvedCalls, new CSharpUnresolvedCall(
                            RelativePath(root, projectPath),
                            RelativePath(root, syntaxTree.FilePath),
                            invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Normalize(expressionText),
                            symbolInfo.CandidateReason.ToString(),
                            symbolInfo.CandidateSymbols.Take(10)
                                .Select(candidate => candidate.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
                                .ToArray()), sampleLimit);
                    }
                }
            }
        }

        return new CSharpSemanticAnalysis(projectsDiscovered, projectsLoaded, resolvedInvocationCount,
            unresolvedInvocationCount, resolvedCalls, unresolvedCalls, issues);
    }

    private static void AddProject(Project project, List<(string Project, Compilation Compilation)> compilations,
        List<string> issues, int sampleLimit)
    {
        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
        if (compilation is null)
        {
            AddSample(issues, $"Project '{project.FilePath ?? project.Name}' produced no C# compilation.", sampleLimit);
            return;
        }
        compilations.Add((project.FilePath ?? project.Name, compilation));
    }

    private static bool IsLoadFailure(Exception exception) => exception is not OutOfMemoryException
        and not StackOverflowException
        and not AccessViolationException;

    private static string Normalize(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string ResolvedTarget(IMethodSymbol method)
    {
        var containingType = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var genericArguments = method.TypeArguments.Length == 0
            ? string.Empty
            : $"<{string.Join(", ", method.TypeArguments.Select(argument => argument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>";
        var parameters = string.Join(", ", method.Parameters.Select(parameter =>
            parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        return $"{containingType}.{method.Name}{genericArguments}({parameters})";
    }

    private static string EnclosingMethod(ISymbol? symbol) => symbol is IMethodSymbol method
        ? ResolvedTarget(method)
        : symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "<unknown>";

    private static string RelativePath(string root, string path) => Path.IsPathRooted(path)
        ? Path.GetRelativePath(root, path).Replace('\\', '/')
        : path.Replace('\\', '/');

    private static void AddSample<T>(List<T> values, T value, int limit)
    {
        if (values.Count < limit)
            values.Add(value);
    }
}
