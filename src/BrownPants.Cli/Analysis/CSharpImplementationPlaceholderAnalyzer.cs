using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BrownPants.Cli.Analysis;

/// <summary>Finds syntax-only candidates for C# methods whose whole implementation is a placeholder result.</summary>
public static class CSharpImplementationPlaceholderAnalyzer
{
    private const long MaximumSourceFileBytes = 1024 * 1024;
    private const int MaximumEvidenceLength = 160;

    public static ImplementationPlaceholderAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit)
    {
        if (sampleLimit is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "Sample limit must be between 1 and 10000.");

        var candidates = new List<ImplementationPlaceholderCandidate>();
        var sourceFilesScanned = 0;
        var filesSkipped = 0;
        var candidateCount = 0;
        long bytesScanned = 0;

        foreach (var relativePath in sourcePaths
                     .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(path => path, RepositoryPathComparer.Instance))
        {
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                var fileLength = new FileInfo(fullPath).Length;
                if (fileLength > MaximumSourceFileBytes)
                {
                    filesSkipped++;
                    continue;
                }

                var source = File.ReadAllText(fullPath);
                sourceFilesScanned++;
                bytesScanned += fileLength;
                var syntaxRoot = CSharpSyntaxTree.ParseText(source, path: relativePath).GetRoot();
                foreach (var type in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
                    {
                        if (TryClassify(method, out var kind, out var evidence))
                        {
                            AddCandidate(relativePath, type, method.Identifier, FormatSignature(method), kind, evidence);
                        }
                    }

                    void AddCandidate(string path, TypeDeclarationSyntax containingType, SyntaxToken identifier,
                        string signature, string kind, string evidence)
                    {
                        candidateCount++;
                        if (candidates.Count >= sampleLimit)
                            return;

                        var line = identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        candidates.Add(new ImplementationPlaceholderCandidate(
                            path.Replace('\\', '/'),
                            line,
                            IsTestSource(path) ? "test" : "production",
                            containingType.Identifier.ValueText,
                            signature,
                            kind,
                            Bound(evidence)));
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                filesSkipped++;
            }
        }

        return new ImplementationPlaceholderAnalysis(
            sourceFilesScanned,
            bytesScanned,
            candidateCount,
            candidateCount - candidates.Count,
            filesSkipped,
            candidates);
    }

    private static bool TryClassify(MethodDeclarationSyntax method, out string kind, out string evidence)
    {
        kind = string.Empty;
        evidence = string.Empty;
        ExpressionSyntax? result = method.ExpressionBody?.Expression;
        if (method.Body is { } body)
        {
            if (body.Statements.Count == 0)
            {
                kind = "Empty body";
                evidence = "{}";
                return true;
            }

            if (body.Statements.Count != 1)
                return false;

            switch (body.Statements[0])
            {
                case ThrowStatementSyntax throwStatement:
                    result = throwStatement.Expression;
                    break;
                case ReturnStatementSyntax returnStatement:
                    result = returnStatement.Expression;
                    break;
                default:
                    return false;
            }
        }

        if (result is ThrowExpressionSyntax throwExpression)
            result = throwExpression.Expression;

        if (result is ObjectCreationExpressionSyntax exceptionCreation && IsNotImplementedException(exceptionCreation.Type))
        {
            kind = "NotImplementedException";
            evidence = "new NotImplementedException()";
            return true;
        }

        if (result is null)
            return false;

        if (IsCompletedTask(result))
        {
            kind = "Completed task";
            evidence = result.ToString();
            return true;
        }

        if (TryGetTaskResultArgument(result, out var taskResult))
        {
            if (IsEmptyResult(taskResult))
            {
                kind = "Empty result";
                evidence = result.ToString();
                return true;
            }
            if (IsDefaultResult(taskResult))
            {
                kind = "Default result";
                evidence = result.ToString();
                return true;
            }
        }

        if (IsEmptyResult(result))
        {
            kind = "Empty result";
            evidence = result.ToString();
            return true;
        }

        if (IsDefaultResult(result))
        {
            kind = "Default result";
            evidence = result.ToString();
            return true;
        }

        return false;
    }

    private static bool IsCompletedTask(ExpressionSyntax expression) =>
        expression is MemberAccessExpressionSyntax memberAccess
        && memberAccess.Name.Identifier.ValueText == "CompletedTask"
        && memberAccess.Expression.ToString().EndsWith("Task", StringComparison.Ordinal);

    private static bool IsEmptyResult(ExpressionSyntax expression)
    {
        if (expression is ObjectCreationExpressionSyntax creation)
            return creation.ArgumentList?.Arguments.Count is 0
                && creation.Initializer is null
                && creation.Type.ToString().Split('<')[0].Split('.').Last() is "List" or "HashSet" or "Dictionary";

        if (expression is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is MemberAccessExpressionSyntax access)
        {
            var methodName = access.Name.Identifier.ValueText;
            var owner = access.Expression.ToString().Split('.').Last();
            if (methodName == "Empty" && owner is "Enumerable" or "Array")
                return true;
        }

        return false;
    }

    private static bool TryGetTaskResultArgument(ExpressionSyntax expression, out ExpressionSyntax result)
    {
        if (expression is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax access
            && access.Name.Identifier.ValueText == "FromResult"
            && access.Expression.ToString().Split('.').Last() == "Task"
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            result = invocation.ArgumentList.Arguments[0].Expression;
            return true;
        }

        result = null!;
        return false;
    }

    private static bool IsDefaultResult(ExpressionSyntax expression)
    {
        if (expression is DefaultExpressionSyntax
            || expression is LiteralExpressionSyntax defaultLiteral && defaultLiteral.IsKind(SyntaxKind.DefaultLiteralExpression))
            return true;
        if (expression is not LiteralExpressionSyntax literal)
            return false;

        return literal.IsKind(SyntaxKind.NullLiteralExpression)
            || literal.IsKind(SyntaxKind.FalseLiteralExpression)
            || literal.Token.Value is byte byteValue && byteValue == 0
            || literal.Token.Value is short shortValue && shortValue == 0
            || literal.Token.Value is int intValue && intValue == 0
            || literal.Token.Value is long longValue && longValue == 0
            || literal.Token.Value is uint uintValue && uintValue == 0
            || literal.Token.Value is ulong ulongValue && ulongValue == 0
            || literal.Token.Value is decimal decimalValue && decimalValue == 0
            || literal.Token.Value is double doubleValue && doubleValue == 0
            || literal.Token.Value is float floatValue && floatValue == 0;
    }

    private static bool IsNotImplementedException(TypeSyntax type) =>
        type.ToString().Split('.').Last() == "NotImplementedException";

    private static string FormatSignature(MethodDeclarationSyntax method)
    {
        var parameters = string.Join(", ", method.ParameterList.Parameters.Select(parameter =>
            parameter.Type is null ? parameter.Identifier.ValueText : $"{parameter.Type} {parameter.Identifier}"));
        var signature = $"{method.ReturnType} {method.Identifier}{method.TypeParameterList}({parameters})";
        return Bound(signature);
    }

    private static bool IsTestSource(string relativePath) =>
        RepositoryScanner.IsTestPath(relativePath)
        || relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => part.Contains(".Tests.", StringComparison.OrdinalIgnoreCase)
                || part.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
        || Path.GetFileName(relativePath).EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase);

    private static string Bound(string value)
    {
        var normalized = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= MaximumEvidenceLength ? normalized : normalized[..MaximumEvidenceLength];
    }
}
