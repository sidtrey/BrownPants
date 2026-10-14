using System.Globalization;
using BrownPants.Cli.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BrownPants.Cli.Analysis;

public static class CSharpAnalyzer
{
    public static CSharpAnalysis Analyze(string root, IEnumerable<string> sourcePaths,
        IReadOnlyList<DotNetProjectInfo> projects, int sampleLimit)
    {
        var types = new List<CSharpTypeInfo>();
        var members = new List<CSharpMemberInfo>();
        var usings = new List<CSharpUsingInfo>();
        var invocations = new List<CSharpInvocationInfo>();
        var issues = new List<CSharpParseIssue>();
        var sourceFileCount = 0;
        var typeCount = 0;
        var memberCount = 0;
        var invocationCount = 0;
        var parseErrorCount = 0;
        var inactiveCodeRegionCount = 0;

        foreach (var relativePath in sourcePaths.OrderBy(path => path, RepositoryPathComparer.Instance))
        {
            sourceFileCount++;
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                var source = File.ReadAllText(fullPath);
                var tree = CSharpSyntaxTree.ParseText(
                    source,
                    ParseOptionsFor(relativePath, projects),
                    relativePath);
                var syntaxRoot = tree.GetRoot();

                foreach (var declaration in syntaxRoot.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    typeCount++;
                    AddSample(types, new CSharpTypeInfo(
                        relativePath,
                        Line(declaration),
                        NamespaceOf(declaration),
                        TypeKindName(declaration),
                        TypeName(declaration)), sampleLimit);
                }

                foreach (var member in syntaxRoot.DescendantNodes().OfType<MemberDeclarationSyntax>())
                {
                    foreach (var info in DescribeMember(relativePath, member))
                    {
                        memberCount++;
                        AddSample(members, info, sampleLimit);
                    }
                }

                foreach (var directive in syntaxRoot.DescendantNodes().OfType<UsingDirectiveSyntax>())
                {
                    AddSample(usings, new CSharpUsingInfo(
                        relativePath,
                        Line(directive),
                        Normalize(directive.Name?.ToString() ?? string.Empty),
                        directive.Alias?.Name.Identifier.ValueText,
                        !directive.StaticKeyword.IsKind(SyntaxKind.None),
                        !directive.GlobalKeyword.IsKind(SyntaxKind.None)), sampleLimit);
                }

                foreach (var invocation in syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    invocationCount++;
                    AddSample(invocations, new CSharpInvocationInfo(relativePath, Line(invocation), Normalize(invocation.Expression.ToString())), sampleLimit);
                }

                inactiveCodeRegionCount += syntaxRoot.DescendantTrivia(descendIntoTrivia: true)
                    .Count(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia));

                foreach (var diagnostic in tree.GetDiagnostics()
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    parseErrorCount++;
                    AddSample(issues, new CSharpParseIssue(
                        relativePath,
                        diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                        diagnostic.Id,
                        diagnostic.GetMessage(CultureInfo.InvariantCulture)), sampleLimit);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                parseErrorCount++;
                AddSample(issues, new CSharpParseIssue(relativePath, 0, "BP0001", $"Source could not be read: {exception.Message}"), sampleLimit);
            }
        }

        return new CSharpAnalysis(
            sourceFileCount,
            typeCount,
            memberCount,
            invocationCount,
            parseErrorCount,
            inactiveCodeRegionCount,
            types,
            members,
            usings,
            invocations,
            issues);
    }

    private static IEnumerable<CSharpMemberInfo> DescribeMember(string path, MemberDeclarationSyntax member)
    {
        var containingType = ContainingTypeOf(member);
        var line = Line(member);
        switch (member)
        {
            case MethodDeclarationSyntax method:
                yield return new CSharpMemberInfo(path, line, containingType, "method",
                    Normalize($"{method.ReturnType} {method.Identifier}{method.TypeParameterList}{method.ParameterList}"));
                break;
            case ConstructorDeclarationSyntax constructor:
                yield return new CSharpMemberInfo(path, line, containingType, "constructor",
                    Normalize($"{constructor.Identifier}{constructor.ParameterList}"));
                break;
            case DestructorDeclarationSyntax destructor:
                yield return new CSharpMemberInfo(path, line, containingType, "destructor",
                    Normalize($"~{destructor.Identifier}{destructor.ParameterList}"));
                break;
            case PropertyDeclarationSyntax property:
                yield return new CSharpMemberInfo(path, line, containingType, "property",
                    Normalize($"{property.Type} {property.Identifier}"));
                break;
            case IndexerDeclarationSyntax indexer:
                yield return new CSharpMemberInfo(path, line, containingType, "indexer",
                    Normalize($"{indexer.Type} this{indexer.ParameterList}"));
                break;
            case EventDeclarationSyntax eventDeclaration:
                yield return new CSharpMemberInfo(path, line, containingType, "event",
                    Normalize($"event {eventDeclaration.Type} {eventDeclaration.Identifier}"));
                break;
            case FieldDeclarationSyntax field:
                foreach (var variable in field.Declaration.Variables)
                    yield return new CSharpMemberInfo(path, line, containingType, "field",
                        Normalize($"{field.Declaration.Type} {variable.Identifier}"));
                break;
            case EventFieldDeclarationSyntax eventField:
                foreach (var variable in eventField.Declaration.Variables)
                    yield return new CSharpMemberInfo(path, line, containingType, "event field",
                        Normalize($"event {eventField.Declaration.Type} {variable.Identifier}"));
                break;
            case OperatorDeclarationSyntax op:
                yield return new CSharpMemberInfo(path, line, containingType, "operator",
                    Normalize($"{op.ReturnType} operator {op.OperatorToken}{op.ParameterList}"));
                break;
        }
    }

    private static string NamespaceOf(SyntaxNode node) => string.Join(".", node.Ancestors()
        .OfType<BaseNamespaceDeclarationSyntax>()
        .Reverse()
        .Select(declaration => declaration.Name.ToString()));

    private static string ContainingTypeOf(SyntaxNode node)
    {
        var names = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse()
            .Select(TypeName).ToArray();
        var currentNamespace = NamespaceOf(node);
        var typePath = string.Join(".", names);
        return currentNamespace.Length == 0 ? typePath : $"{currentNamespace}.{typePath}";
    }

    private static string TypeName(BaseTypeDeclarationSyntax declaration) => declaration switch
    {
        TypeDeclarationSyntax type => type.Identifier.ValueText + type.TypeParameterList,
        EnumDeclarationSyntax enumeration => enumeration.Identifier.ValueText,
        _ => declaration.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? declaration.Kind().ToString()
    };

    private static string TypeKindName(BaseTypeDeclarationSyntax declaration) => declaration.Kind() switch
    {
        SyntaxKind.ClassDeclaration => "class",
        SyntaxKind.StructDeclaration => "struct",
        SyntaxKind.InterfaceDeclaration => "interface",
        SyntaxKind.EnumDeclaration => "enum",
        SyntaxKind.RecordDeclaration => "record",
        SyntaxKind.RecordStructDeclaration => "record struct",
        _ => declaration.Kind().ToString()
    };

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static string Normalize(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static CSharpParseOptions ParseOptionsFor(string sourcePath, IReadOnlyList<DotNetProjectInfo> projects)
    {
        var project = projects
            .Select(projectInfo => (Project: projectInfo, Directory: Path.GetDirectoryName(projectInfo.Path)?.Replace('\\', '/') ?? string.Empty))
            .Where(candidate => candidate.Directory.Length == 0
                || sourcePath.StartsWith(candidate.Directory + "/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.Directory.Length)
            .FirstOrDefault();

        if (project.Project is not null
            && project.Project.LanguageVersions.Count == 1
            && LanguageVersionFacts.TryParse(project.Project.LanguageVersions[0], out var languageVersion))
        {
            return new CSharpParseOptions(languageVersion);
        }

        return new CSharpParseOptions(LanguageVersion.Latest);
    }

    private static void AddSample<T>(List<T> samples, T value, int sampleLimit)
    {
        if (samples.Count < sampleLimit)
            samples.Add(value);
    }
}

internal sealed class RepositoryPathComparer : IComparer<string>
{
    public static RepositoryPathComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        var result = StringComparer.OrdinalIgnoreCase.Compare(left, right);
        return result != 0 ? result : StringComparer.Ordinal.Compare(left, right);
    }
}
