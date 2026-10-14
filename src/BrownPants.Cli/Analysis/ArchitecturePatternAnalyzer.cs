using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using CSharpInvocation = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
using VisualBasicAttribute = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeSyntax;
using VisualBasicInvocation = Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax;
using VisualBasicConstructorStatement = Microsoft.CodeAnalysis.VisualBasic.Syntax.SubNewStatementSyntax;
using VisualBasicMethodStatement = Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax;
using VisualBasicTypeStatement = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeStatementSyntax;

namespace BrownPants.Cli.Analysis;

public static class ArchitecturePatternAnalyzer
{
    private static readonly Regex[] NativePatterns =
    {
        new(@"\b(?:DECLARE|IMPLEMENT)_(?:DYNAMIC|DYNCREATE|SERIAL)\s*\(|\bRUNTIME_CLASS\s*\(", Options),
        new(@"\bCreateObject\s*\(", Options),
        new(@"\b(?:BEGIN|END)_MESSAGE_MAP\s*\(", Options),
        new(@"\bCoCreateInstance\s*\(", Options),
        new(@"\b(?:ref\s+class|value\s+class|gcnew|gcroot)\b|^\s*#using\b", Options),
        new(@"\b[A-Za-z_]\w*::(?:Create|Build|Resolve)[A-Z_]\w*\s*\([^;]*\)\s*(?:const\s*)?(?:\{|$)", Options),
        new(@"\bstd::make_(?:shared|unique)\s*<", Options)
    };

    private static readonly string[] NativeCategories =
    {
        "MFC runtime type", "MFC object factory", "MFC message map", "COM activation",
        "C++/CLI bridge", "C++ factory method", "C++ smart-pointer construction"
    };

    private static readonly Regex NativeRawStringStart = new("R\\\"(?<delimiter>[^ ()\\\\\\t\\r\\n]{0,16})\\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex NativeClassDeclarationStart = new(@"\b(?:class|struct)\b", Options);
    private static readonly Regex MfcInheritanceDeclaration = new(
        @"\b(?:class|struct)\b[^;{]*:\s*(?:[^,;{]*,\s*)*(?:(?:public|protected|private)\s+)?(?:virtual\s+)?(?:CObject|CCmdTarget|CWinThread|CWinAppEx?|CWnd|CFrameWndEx?|CMDIFrameWndEx?|CMDIChildWnd|CMiniFrameWnd|CView|CScrollView|CFormView|CListView|CTreeView|CDocument|CDialogEx?|CPropertyPage|CPropertySheet|CControlBar|CToolBar|CStatusBar|CRecordView|CRecordset)\b",
        Options | RegexOptions.Singleline);
    private static readonly Regex PythonFactoryType = new(@"^\s*class\s+[A-Za-z_]\w*Factory\b", RegexOptions.CultureInvariant);
    private static readonly Regex PythonFactoryMethod = new(
        @"^\s*(?:async\s+)?def\s+(?:create|build|make|resolve)(?:_[A-Za-z_]\w*|[A-Z]\w*)?\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex PythonFactoryInvocation = new(
        @"\b(?:[A-Za-z_]\w*(?:Factory|_factory)|factory)\s*\.\s*(?:create|build|make|resolve)(?:_[A-Za-z_]\w*|[A-Z]\w*)?\s*\(|\b[A-Za-z_]\w*Factory\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex PythonProviderRegistration = new(
        @"\bproviders\.(?:Factory|Singleton|ThreadSafeSingleton|Resource|Callable|Coroutine|AbstractFactory)\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex PythonDependencyInjection = new(
        @"\bDepends\s*\(|\bProvide\s*\[|^\s*@inject(?:\s*\(|\s*$)", RegexOptions.CultureInvariant);

    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const int MaximumMfcDeclarationLines = 32;
    private const int MaximumMfcDeclarationCharacters = 4096;

    public static ArchitecturePatternAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit)
    {
        var signals = new List<ArchitecturePatternSignal>();
        var signalCount = 0;
        foreach (var relativePath in sourcePaths.OrderBy(path => path, RepositoryPathComparer.Instance))
        {
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                var source = File.ReadAllText(fullPath);
                if (Path.GetExtension(relativePath).Equals(".cs", StringComparison.OrdinalIgnoreCase))
                    AnalyzeCSharp(source, relativePath, signals, sampleLimit, ref signalCount);
                else if (Path.GetExtension(relativePath).Equals(".vb", StringComparison.OrdinalIgnoreCase))
                    AnalyzeVisualBasic(source, relativePath, signals, sampleLimit, ref signalCount);
                else if (IsPythonSource(relativePath))
                    AnalyzePython(source, relativePath, signals, sampleLimit, ref signalCount);
                else if (IsNativeSource(relativePath))
                    AnalyzeNative(source, relativePath, signals, sampleLimit, ref signalCount);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // Source read failures are already reported by the language-specific analyzer.
            }
        }

        return new ArchitecturePatternAnalysis(signalCount, signals);
    }

    private static void AnalyzeCSharp(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: path);
        var root = tree.GetRoot();
        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            var name = TypeIdentifier(type);
            if (name.Contains("Factory", StringComparison.OrdinalIgnoreCase))
                AddSignal("Factory type", "C#", path, type, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (IsFactoryMethod(method))
                AddSignal("Factory method", "C#", path, method, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var constructor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            var parameterTypes = constructor.ParameterList.Parameters
                .Select(parameter => parameter.Type?.ToString() ?? string.Empty)
                .ToArray();
            if (parameterTypes.Any(type => IsDependencyTypeName(type, visualBasic: false)))
                AddSignal("Constructor injection", "C#", path, constructor, source, signals, sampleLimit, ref signalCount);
            if (parameterTypes.Any(IsFactoryDelegateType))
                AddSignal("Factory delegate", "C#", path, constructor, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var invocation in root.DescendantNodes().OfType<CSharpInvocation>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;
            var name = memberAccess.Name switch
            {
                GenericNameSyntax generic => generic.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => string.Empty
            };
            var receiver = memberAccess.Expression.ToString();
            if (IsComActivationInvocation(name, receiver))
                AddSignal("COM activation", "C#", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsComMarshallingInvocation(name, receiver))
                AddSignal("COM marshalling", "C#", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsRegistrationName(name))
                AddSignal("DI registration", "C#", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsResolutionInvocation(name, receiver))
                AddSignal("DI resolution", "C#", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsFactoryInvocation(name, receiver))
                AddSignal("Factory invocation", "C#", path, invocation, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            var name = attribute.Name.ToString().Split('.').Last().Replace("Attribute", string.Empty, StringComparison.Ordinal);
            if (name is "DllImport" or "LibraryImport" or "ComImport" or "ComVisible" or "ProgId"
                or "Guid" or "InterfaceType" or "ClassInterface" or "ComDefaultInterface" or "ComSourceInterfaces" or "CoClass")
                AddSignal("Native interop", "C#", path, attribute, source, signals, sampleLimit, ref signalCount);
        }
    }

    private static void AnalyzeVisualBasic(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var tree = VisualBasicSyntaxTree.ParseText(source, path: path);
        var root = tree.GetRoot();
        foreach (var type in root.DescendantNodes().OfType<VisualBasicTypeStatement>())
        {
            if (type.Identifier.ValueText.Contains("Factory", StringComparison.OrdinalIgnoreCase))
                AddSignal("Factory type", "VB.NET", path, type, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var method in root.DescendantNodes().OfType<VisualBasicMethodStatement>())
        {
            if (IsFactoryMethod(method))
                AddSignal("Factory method", "VB.NET", path, method, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var constructor in root.DescendantNodes().OfType<VisualBasicConstructorStatement>())
        {
            var parameterTypes = constructor.ParameterList?.Parameters
                .Select(parameter => parameter.AsClause?.Type().ToString() ?? string.Empty)
                .ToArray() ?? Array.Empty<string>();
            if (parameterTypes.Any(type => IsDependencyTypeName(type, visualBasic: true)))
                AddSignal("Constructor injection", "VB.NET", path, constructor, source, signals, sampleLimit, ref signalCount);
            if (parameterTypes.Any(IsFactoryDelegateType))
                AddSignal("Factory delegate", "VB.NET", path, constructor, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var invocation in root.DescendantNodes().OfType<VisualBasicInvocation>())
        {
            var expression = invocation.Expression.ToString();
            var name = invocation.Expression switch
            {
                Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                Microsoft.CodeAnalysis.VisualBasic.Syntax.GenericNameSyntax generic => generic.Identifier.ValueText,
                Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => expression.Split('.').Last().Split('(')[0]
            };
            var receiver = invocation.Expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Expression.ToString() : string.Empty;
            if (IsOneOf(name, "CreateObject", "GetObject") || IsComActivationInvocation(name, receiver,
                    StringComparison.OrdinalIgnoreCase))
                AddSignal("COM activation", "VB.NET", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsComMarshallingInvocation(name, receiver, StringComparison.OrdinalIgnoreCase))
                AddSignal("COM marshalling", "VB.NET", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsRegistrationName(name))
                AddSignal("DI registration", "VB.NET", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsResolutionInvocation(name, receiver))
                AddSignal("DI resolution", "VB.NET", path, invocation, source, signals, sampleLimit, ref signalCount);
            else if (IsFactoryInvocation(name, receiver, StringComparison.OrdinalIgnoreCase))
                AddSignal("Factory invocation", "VB.NET", path, invocation, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var attribute in root.DescendantNodes().OfType<VisualBasicAttribute>())
        {
            var name = attribute.Name.ToString().Split('.').Last().Replace("Attribute", string.Empty, StringComparison.Ordinal);
            if (IsOneOf(name, "DllImport", "LibraryImport", "ComImport", "ComVisible", "ProgId", "Guid",
                    "InterfaceType", "ClassInterface", "ComDefaultInterface", "ComSourceInterfaces", "CoClass"))
                AddSignal("Native interop", "VB.NET", path, attribute, source, signals, sampleLimit, ref signalCount);
        }
    }

    private static bool IsRegistrationName(string name) => IsOneOf(name, "AddTransient", "AddScoped", "AddSingleton",
        "RegisterType", "RegisterInstance", "Register", "Bind");

    private static bool IsResolutionInvocation(string name, string receiver)
    {
        if (IsOneOf(name, "GetService", "GetRequiredService", "GetInstance"))
            return true;

        return IsOneOf(name, "Get", "GetObject", "Resolve") && IsContainerReceiver(receiver);
    }

    private static bool IsContainerReceiver(string receiver) =>
        receiver.Contains("Container", StringComparison.OrdinalIgnoreCase)
        || receiver.Contains("Resolver", StringComparison.OrdinalIgnoreCase)
        || receiver.Contains("Provider", StringComparison.OrdinalIgnoreCase)
        || receiver.Contains("Locator", StringComparison.OrdinalIgnoreCase)
        || receiver.Contains("Kernel", StringComparison.OrdinalIgnoreCase)
        || receiver.Contains("Scope", StringComparison.OrdinalIgnoreCase);

    private static bool IsOneOf(string value, params string[] choices) =>
        choices.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static bool IsDependencyTypeName(string typeName, bool visualBasic)
    {
        var simpleName = typeName.Split('<', '(')[0].Split('.').Last().TrimEnd('?');
        if (simpleName.Length < 2 || IsOneOf(simpleName, "Integer", "Int16", "Int32", "Int64", "IntPtr"))
            return simpleName.EndsWith("Factory", StringComparison.OrdinalIgnoreCase);

        var hasInterfacePrefix = visualBasic
            ? simpleName.StartsWith("I", StringComparison.OrdinalIgnoreCase)
            : simpleName.StartsWith("I", StringComparison.Ordinal) && char.IsUpper(simpleName[1]);
        return hasInterfacePrefix || simpleName.EndsWith("Factory", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFactoryDelegateType(string typeName) =>
        typeName.Split('<', '(')[0].Split('.').Last().Equals("Func", StringComparison.OrdinalIgnoreCase);

    private static bool IsComActivationInvocation(string name, string receiver,
        StringComparison comparison = StringComparison.Ordinal) =>
        ((string.Equals(name, "GetTypeFromProgID", comparison)
            || string.Equals(name, "GetTypeFromCLSID", comparison)) && receiver.EndsWith("Type", comparison))
        || (string.Equals(name, "GetActiveObject", comparison) && receiver.EndsWith("Marshal", comparison));

    private static bool IsComMarshallingInvocation(string name, string receiver,
        StringComparison comparison = StringComparison.Ordinal) =>
        receiver.EndsWith("Marshal", comparison)
        && IsOneOf(name, "GetObjectForIUnknown", "GetTypedObjectForIUnknown", "GetComInterfaceForObject",
            "GetIUnknownForObject", "GetIDispatchForObject", "GetObjectForNativeVariant", "GetNativeVariantForObject",
            "ReleaseComObject", "FinalReleaseComObject");

    private static bool IsFactoryInvocation(string name, string receiver,
        StringComparison comparison = StringComparison.Ordinal) =>
        receiver.Contains("Factory", StringComparison.OrdinalIgnoreCase)
        && (string.Equals(name, "Create", comparison) || IsFactoryMethod(name, comparison));

    private static void AnalyzeNative(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var language = Path.GetExtension(path).Equals(".c", StringComparison.OrdinalIgnoreCase) ? "C" : "C++";
        var inBlockComment = false;
        var quote = '\0';
        var escaped = false;
        string? rawStringTerminator = null;
        var lines = source.Split('\n');
        var declaration = new StringBuilder();
        var declarationStartLine = 0;
        for (var index = 0; index < lines.Length; index++)
        {
            var code = RemoveCommentsAndStrings(lines[index], ref inBlockComment, ref quote, ref escaped,
                ref rawStringTerminator);
            for (var patternIndex = 0; patternIndex < NativePatterns.Length; patternIndex++)
            {
                if (NativePatterns[patternIndex].IsMatch(code))
                    AddLineSignal(NativeCategories[patternIndex], language, path, index + 1, lines[index], signals,
                        sampleLimit, ref signalCount);
            }

            if (declaration.Length == 0 && NativeClassDeclarationStart.IsMatch(code))
                declarationStartLine = index + 1;
            if (declarationStartLine == 0)
                continue;

            declaration.AppendLine(code);
            if (MfcInheritanceDeclaration.IsMatch(declaration.ToString()))
            {
                AddLineSignal("MFC class inheritance", language, path, declarationStartLine, declaration.ToString(),
                    signals, sampleLimit, ref signalCount);
                declaration.Clear();
                declarationStartLine = 0;
            }
            else if (code.Contains('{') || code.Contains(';')
                || index + 1 - declarationStartLine + 1 > MaximumMfcDeclarationLines
                || declaration.Length > MaximumMfcDeclarationCharacters)
            {
                declaration.Clear();
                declarationStartLine = 0;
            }
        }
    }

    private static void AnalyzePython(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        string? tripleQuoteDelimiter = null;
        var continuedQuote = '\0';
        var lines = source.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var code = RemovePythonCommentsAndStrings(lines[index], ref tripleQuoteDelimiter, ref continuedQuote);
            AddPythonPattern(PythonFactoryType, "Factory type", code, path, index + 1, signals, sampleLimit,
                ref signalCount);
            AddPythonPattern(PythonFactoryMethod, "Factory method", code, path, index + 1, signals, sampleLimit,
                ref signalCount);
            AddPythonPattern(PythonFactoryInvocation, "Factory invocation", code, path, index + 1, signals,
                sampleLimit, ref signalCount);
            AddPythonPattern(PythonProviderRegistration, "DI registration", code, path, index + 1, signals,
                sampleLimit, ref signalCount);
            AddPythonPattern(PythonDependencyInjection, "Dependency injection", code, path, index + 1, signals,
                sampleLimit, ref signalCount);
        }
    }

    private static void AddPythonPattern(Regex pattern, string category, string code, string path, int line,
        List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        if (pattern.IsMatch(code))
            AddLineSignal(category, "Python", path, line, code, signals, sampleLimit, ref signalCount);
    }

    private static string RemovePythonCommentsAndStrings(string line, ref string? tripleQuoteDelimiter,
        ref char continuedQuote)
    {
        var result = new StringBuilder(line.Length);
        for (var index = 0; index < line.Length;)
        {
            if (continuedQuote != '\0')
            {
                var closingIndex = FindPythonStringTerminator(line, continuedQuote.ToString(), index);
                if (closingIndex < 0)
                {
                    result.Append(' ', line.Length - index);
                    break;
                }

                var continuedLiteralLength = closingIndex + 1 - index;
                result.Append(' ', continuedLiteralLength);
                index += continuedLiteralLength;
                continuedQuote = '\0';
                continue;
            }

            if (tripleQuoteDelimiter is not null)
            {
                var closingIndex = FindPythonStringTerminator(line, tripleQuoteDelimiter, index);
                if (closingIndex < 0)
                {
                    result.Append(' ', line.Length - index);
                    break;
                }

                var stringLength = closingIndex + tripleQuoteDelimiter.Length - index;
                result.Append(' ', stringLength);
                index += stringLength;
                tripleQuoteDelimiter = null;
                continue;
            }

            var current = line[index];
            if (current == '#')
                break;
            if (current is not ('\'' or '"'))
            {
                result.Append(current);
                index++;
                continue;
            }

            var delimiter = current.ToString();
            var isTripleQuoted = index + 2 < line.Length
                && line[index + 1] == current && line[index + 2] == current;
            if (isTripleQuoted)
                delimiter += delimiter + delimiter;
            var contentStart = index + delimiter.Length;
            var closingQuote = FindPythonStringTerminator(line, delimiter, contentStart);
            if (closingQuote < 0)
            {
                result.Append(' ', line.Length - index);
                if (isTripleQuoted)
                    tripleQuoteDelimiter = delimiter;
                else if (HasLineContinuation(line, index))
                    continuedQuote = current;
                break;
            }

            var literalLength = closingQuote + delimiter.Length - index;
            result.Append(' ', literalLength);
            index += literalLength;
        }

        return result.ToString();
    }

    private static bool HasLineContinuation(string line, int startIndex)
    {
        var lastIndex = line.EndsWith('\r') ? line.Length - 2 : line.Length - 1;
        var trailingBackslashes = 0;
        for (var index = lastIndex; index >= startIndex && line[index] == '\\'; index--)
            trailingBackslashes++;
        return trailingBackslashes % 2 == 1;
    }

    private static int FindPythonStringTerminator(string line, string delimiter, int startIndex)
    {
        var searchIndex = startIndex;
        while (searchIndex <= line.Length - delimiter.Length)
        {
            var matchIndex = line.IndexOf(delimiter, searchIndex, StringComparison.Ordinal);
            if (matchIndex < 0)
                return -1;

            var precedingBackslashes = 0;
            for (var index = matchIndex - 1; index >= 0 && line[index] == '\\'; index--)
                precedingBackslashes++;
            if (precedingBackslashes % 2 == 0)
                return matchIndex;
            searchIndex = matchIndex + delimiter.Length;
        }

        return -1;
    }

    private static string RemoveCommentsAndStrings(string line, ref bool inBlockComment, ref char quote,
        ref bool escaped, ref string? rawStringTerminator)
    {
        var result = new StringBuilder(line.Length);
        for (var index = 0; index < line.Length; index++)
        {
            var current = line[index];
            var next = index + 1 < line.Length ? line[index + 1] : '\0';
            if (rawStringTerminator is not null)
            {
                var closingIndex = line.IndexOf(rawStringTerminator, index, StringComparison.Ordinal);
                if (closingIndex < 0)
                {
                    result.Append(' ', line.Length - index);
                    break;
                }
                result.Append(' ', closingIndex - index + rawStringTerminator.Length);
                index = closingIndex + rawStringTerminator.Length - 1;
                rawStringTerminator = null;
                continue;
            }
            if (inBlockComment)
            {
                if (current == '*' && next == '/')
                {
                    inBlockComment = false;
                    result.Append("  ");
                    index++;
                }
                else result.Append(' ');
                continue;
            }
            if (quote != '\0')
            {
                if (escaped)
                {
                    result.Append(' ');
                    escaped = false;
                }
                else if (current == '\\')
                {
                    result.Append(' ');
                    escaped = true;
                }
                else
                {
                    if (current == quote) quote = '\0';
                    result.Append(' ');
                }
                continue;
            }
            if (current == 'R' && next == '"')
            {
                var rawStart = NativeRawStringStart.Match(line, index);
                if (rawStart.Success)
                {
                    rawStringTerminator = ")" + rawStart.Groups["delimiter"].Value + "\"";
                    var rawStartLength = rawStart.Length;
                    result.Append(' ', rawStartLength);
                    index += rawStartLength - 1;
                    continue;
                }
            }
            if (current == '/' && next == '/')
                break;
            if (current == '/' && next == '*')
            {
                inBlockComment = true;
                result.Append("  ");
                index++;
                continue;
            }
            if (current is '\"' or '\'')
            {
                quote = current;
                result.Append(' ');
                continue;
            }
            result.Append(current);
        }
        return result.ToString();
    }

    private static string TypeIdentifier(BaseTypeDeclarationSyntax type) => type switch
    {
        TypeDeclarationSyntax declaration => declaration.Identifier.ValueText,
        EnumDeclarationSyntax declaration => declaration.Identifier.ValueText,
        _ => string.Empty
    };

    private static bool IsFactoryMethod(string name, StringComparison comparison = StringComparison.Ordinal) =>
        HasFactoryPrefix(name, "Create", comparison)
        || HasFactoryPrefix(name, "Build", comparison)
        || HasFactoryPrefix(name, "Resolve", comparison);

    private static bool IsFactoryMethod(MethodDeclarationSyntax method) =>
        (method.ReturnType is not PredefinedTypeSyntax predefinedType
            || !predefinedType.Keyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.VoidKeyword))
        && IsFactoryMethod(method.Identifier.ValueText);

    private static bool IsFactoryMethod(VisualBasicMethodStatement method) =>
        string.Equals(method.SubOrFunctionKeyword.ValueText, "Function", StringComparison.OrdinalIgnoreCase)
        && IsFactoryMethod(method.Identifier.ValueText, StringComparison.OrdinalIgnoreCase);

    private static bool HasFactoryPrefix(string name, string prefix, StringComparison comparison) =>
        name.StartsWith(prefix, comparison)
        && (name.Length == prefix.Length || comparison == StringComparison.OrdinalIgnoreCase || char.IsUpper(name[prefix.Length]));

    private static bool IsNativeSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".cpp" or ".cc" or ".cxx" or ".c" or ".h" or ".hpp" or ".hh" or ".hxx";

    private static bool IsPythonSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".py" or ".pyw" or ".pyi";

    private static void AddSignal(string category, string language, string path, SyntaxNode node, string source,
        List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        var span = node.GetLocation().GetLineSpan().StartLinePosition;
        var line = source.Split('\n').ElementAtOrDefault(span.Line)?.Trim() ?? node.ToString();
        AddLineSignal(category, language, path, span.Line + 1, line, signals, sampleLimit, ref signalCount);
    }

    private static void AddLineSignal(string category, string language, string path, int lineNumber, string evidence,
        List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        signalCount++;
        if (signals.Count < sampleLimit)
            signals.Add(new ArchitecturePatternSignal(category, "Candidate", language, path, lineNumber,
                string.Join(" ", evidence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));
    }
}
