using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace BrownPants.Cli.Analysis;

public static class VisualBasicAnalyzer
{
    public static VisualBasicAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit)
    {
        var types = new List<VisualBasicTypeInfo>();
        var members = new List<VisualBasicMemberInfo>();
        var invocations = new List<VisualBasicInvocationInfo>();
        var issues = new List<VisualBasicParseIssue>();
        var sourceFileCount = 0;
        var typeCount = 0;
        var memberCount = 0;
        var invocationCount = 0;
        var parseErrorCount = 0;

        foreach (var relativePath in sourcePaths.OrderBy(path => path, RepositoryPathComparer.Instance))
        {
            sourceFileCount++;
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                var tree = VisualBasicSyntaxTree.ParseText(File.ReadAllText(fullPath), path: relativePath);
                var syntaxRoot = tree.GetRoot();
                foreach (var declaration in syntaxRoot.DescendantNodes().OfType<TypeBlockSyntax>())
                {
                    typeCount++;
                    var statement = declaration.BlockStatement;
                    AddSample(types, new VisualBasicTypeInfo(relativePath, Line(statement), NamespaceOf(statement),
                        declaration.Kind().ToString().Replace("Block", string.Empty, StringComparison.Ordinal),
                        statement.Identifier.ValueText), sampleLimit);
                }

                foreach (var member in syntaxRoot.DescendantNodes().Where(IsMemberDeclaration))
                {
                    memberCount++;
                    AddSample(members, DescribeMember(relativePath, member), sampleLimit);
                }

                foreach (var invocation in syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    invocationCount++;
                    AddSample(invocations, new VisualBasicInvocationInfo(relativePath, Line(invocation), Normalize(invocation.Expression.ToString())), sampleLimit);
                }

                foreach (var diagnostic in tree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    parseErrorCount++;
                    AddSample(issues, new VisualBasicParseIssue(relativePath,
                        diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                        diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture)), sampleLimit);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                parseErrorCount++;
                AddSample(issues, new VisualBasicParseIssue(relativePath, 0, "BP0001",
                    $"Source could not be read: {exception.Message}"), sampleLimit);
            }
        }

        return new VisualBasicAnalysis(sourceFileCount, typeCount, memberCount, invocationCount, parseErrorCount,
            types, members, invocations, issues);
    }

    private static bool IsMemberDeclaration(SyntaxNode node) => node is MethodStatementSyntax
        or PropertyStatementSyntax or EventStatementSyntax or FieldDeclarationSyntax;

    private static VisualBasicMemberInfo DescribeMember(string path, SyntaxNode member)
    {
        var containingType = string.Join(".", member.Ancestors().OfType<TypeBlockSyntax>().Reverse()
            .Select(block => block.BlockStatement.Identifier.ValueText));
        var currentNamespace = NamespaceOf(member);
        if (currentNamespace.Length > 0)
            containingType = $"{currentNamespace}.{containingType}";

        var (kind, signature) = member switch
        {
            MethodStatementSyntax method => ("method", method.ToString()),
            PropertyStatementSyntax property => ("property", property.ToString()),
            EventStatementSyntax eventDeclaration => ("event", eventDeclaration.ToString()),
            FieldDeclarationSyntax field => ("field", field.ToString()),
            _ => (member.Kind().ToString(), member.ToString())
        };
        return new VisualBasicMemberInfo(path, Line(member), containingType, kind, Normalize(signature));
    }

    private static string NamespaceOf(SyntaxNode node) => string.Join(".", node.Ancestors()
        .OfType<NamespaceBlockSyntax>().Reverse()
        .Select(block => block.NamespaceStatement.Name.ToString()));

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    private static string Normalize(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static void AddSample<T>(List<T> samples, T value, int sampleLimit)
    {
        if (samples.Count < sampleLimit)
            samples.Add(value);
    }
}
