using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BrownPants.Cli.Analysis;

/// <summary>Finds syntax-only calls that may drop source data into an empty collection argument.</summary>
public static class CSharpDroppedCollectionMappingAnalyzer
{
    private const long MaximumSourceFileBytes = 1024 * 1024;
    private const int MaximumEvidenceLength = 180;

    public static DroppedCollectionMappingAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit)
    {
        if (sampleLimit is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "Sample limit must be between 1 and 10000.");

        var candidates = new List<DroppedCollectionMappingCandidate>();
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
                foreach (var invocation in syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var method = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if (method == null)
                        continue;

                    var emptyArguments = invocation.ArgumentList.Arguments
                        .Select((argument, index) => (argument, index))
                        .Where(item => IsEmptyCollection(item.argument.Expression))
                        .ToArray();
                    if (emptyArguments.Length == 0)
                        continue;

                    var sourceObjects = invocation.ArgumentList.Arguments
                        .Where(argument => !IsEmptyCollection(argument.Expression))
                        .SelectMany(argument => argument.Expression.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
                        .Where(IsMemberValueAccess)
                        .Select(access => GetRootIdentifier(access.Expression))
                        .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(identifier => identifier, StringComparer.Ordinal)
                        .ToArray();
                    if (sourceObjects.Length == 0)
                        continue;

                    candidateCount++;
                    if (candidates.Count >= sampleLimit)
                        continue;

                    var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var emptyIndexes = string.Join(",", emptyArguments.Select(item => item.index + 1));
                    candidates.Add(new DroppedCollectionMappingCandidate(
                        relativePath.Replace('\\', '/'),
                        line,
                        IsTestSource(relativePath) ? "test" : "production",
                        method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? "<top-level>",
                        FormatSignature(method),
                        GetTargetName(invocation.Expression),
                        sourceObjects[0]!,
                        emptyIndexes,
                        Bound(invocation.ToString())));
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                filesSkipped++;
            }
        }

        return new DroppedCollectionMappingAnalysis(
            sourceFilesScanned,
            bytesScanned,
            candidateCount,
            candidateCount - candidates.Count,
            filesSkipped,
            candidates);
    }

    private static bool IsEmptyCollection(ExpressionSyntax expression)
    {
        if (expression is CollectionExpressionSyntax collection)
            return collection.Elements.Count == 0;
        if (expression is ObjectCreationExpressionSyntax creation)
            return creation.ArgumentList?.Arguments.Count is 0
                && creation.Initializer is null
                && IsCollectionType(creation.Type.ToString());
        if (expression is ArrayCreationExpressionSyntax arrayCreation)
            return arrayCreation.Initializer?.Expressions.Count == 0;
        if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
            return implicitArray.Initializer.Expressions.Count == 0;
        if (expression is not InvocationExpressionSyntax invocation
            || invocation.Expression is not MemberAccessExpressionSyntax access)
            return false;

        var methodName = access.Name switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => string.Empty
        };
        var owner = access.Expression.ToString().Split('.').Last();
        return methodName == "Empty" && owner is "Enumerable" or "Array";
    }

    private static bool IsCollectionType(string typeName) =>
        typeName.Split('<')[0].Split('.').Last() is "List" or "HashSet" or "Dictionary" or "Collection";

    private static string? GetRootIdentifier(ExpressionSyntax expression)
    {
        while (expression is MemberAccessExpressionSyntax memberAccess)
            expression = memberAccess.Expression;
        return expression is IdentifierNameSyntax identifier ? identifier.Identifier.ValueText : null;
    }

    private static bool IsMemberValueAccess(MemberAccessExpressionSyntax access) =>
        access.Parent is not InvocationExpressionSyntax invocation || invocation.Expression != access;

    private static string GetTargetName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        _ => Bound(expression.ToString())
    };

    private static string FormatSignature(MethodDeclarationSyntax method)
    {
        var parameters = string.Join(", ", method.ParameterList.Parameters.Select(parameter =>
            parameter.Type is null ? parameter.Identifier.ValueText : $"{parameter.Type} {parameter.Identifier}"));
        return Bound($"{method.ReturnType} {method.Identifier}{method.TypeParameterList}({parameters})");
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
