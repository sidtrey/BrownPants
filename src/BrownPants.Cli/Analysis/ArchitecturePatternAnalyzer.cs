using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
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
    private static readonly Regex JavaScriptFactoryType = new(
        @"\bclass\s+[A-Za-z_$][\w$]*Factory\b", RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptFactoryMethod = new(
        @"^\s*(?:(?:export|default|async|public|private|protected|static|readonly|override)\s+)*(?:function\s+)?(?:create|build|make|resolve)(?:_[A-Za-z_$][\w$]*|[A-Z_$][\w$]*)?\s*(?:<[^;\r\n]*>)?\s*\([^;\r\n]*\)\s*(?::[^\{;\r\n]+)?\s*(?:\{|;|=>)",
        RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptFactoryInvocation = new(
        @"\b(?:[A-Za-z_$][\w$]*(?:Factory|_factory)|factory)\s*(?:\.\s*(?:create|build|make|resolve)[A-Za-z_$][\w$]*)?\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptProviderRegistration = new(
        @"\buseFactory\s*:", RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptContainerBinding = new(
        @"\b[A-Za-z_$]*[Cc]ontainer\w*\s*\.\s*bind\s*(?:<[^;\r\n]*>)?\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptContainerResolution = new(
        @"\b[A-Za-z_$]*[Cc]ontainer\w*\s*\.\s*(?:get|resolve)\s*(?:<[^;\r\n]*>)?\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptDependencyInjection = new(
        @"@\s*(?:Injectable|Inject|injectable|inject)\b|(?<![.$\w])inject\s*\(", RegexOptions.CultureInvariant);
    private static readonly Regex JvmFactoryType = new(
        @"\b(?:class|interface|object|record|enum)\s+[A-Za-z_$][\w$]*Factory\b", RegexOptions.CultureInvariant);
    private static readonly Regex JvmFactoryMethod = new(
        @"^\s*(?:@[A-Za-z_$][\w$.]*(?:\s*\([^()\r\n]*\))?\s+)*(?:(?:public|protected|private|static|final|abstract|synchronized|override|open|internal|suspend|inline|tailrec|operator|fun)\s+)*(?:[A-Za-z_$][\w$<>?,.\[\] ]*\s+)?(?:fun\s+)?(?:create|build|make|resolve|provide)(?:_[A-Za-z_$][\w$]*|[A-Z_$][\w$]*)?\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex JvmFactoryInvocation = new(
        @"\b(?:[A-Za-z_$][\w$]*(?:Factory|_factory)|factory)\s*\.\s*(?:create|build|make|resolve|provide)[A-Za-z_$][\w$]*\s*\(|\b[A-Za-z_$][\w$]*Factory\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex JvmProviderRegistration = new(
        @"@\s*(?:Bean|Provides|Binds|BindsInstance|Produces|Module|Component|Configuration|Singleton|Service|Repository|Controller|RestController|Scope)\b",
        RegexOptions.CultureInvariant);
    private static readonly Regex JvmDependencyInjection = new(
        @"@\s*(?:Inject|Autowired|Resource|Injectable)\b", RegexOptions.CultureInvariant);
    private static readonly Regex JvmContainerBinding = new(
        @"\b(?:binder\s*\.\s*)?bind\s*(?:<[^;\r\n]*>)?\s*\(", RegexOptions.CultureInvariant);
    private static readonly Regex JvmContainerResolution = new(
        @"\bGuice\s*\.\s*createInjector\s*\(|\b(?:[A-Za-z_$][\w$]*Injector|injector)\s*\.\s*getInstance\s*(?:<[^;\r\n]*>)?\s*\(|\bDagger[A-Za-z_$][\w$]*\s*\.\s*(?:create|builder)\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex GoFactoryType = new(
        @"^\s*type\s+[A-Za-z_]\w*Factory\b", RegexOptions.CultureInvariant);
    private static readonly Regex GoFactoryFunction = new(
        @"^\s*func\s+(?:\([^)]*\)\s*)?(?:New(?:[A-Z][A-Za-z0-9_]*)?|(?:Create|Build|Make|Resolve)[A-Z][A-Za-z0-9_]*)\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex GoFactoryInvocation = new(
        @"\b(?:New[A-Z][A-Za-z0-9_]*|(?:Create|Build|Make|Resolve)[A-Z][A-Za-z0-9_]*)\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex GoProviderRegistration = new(
        @"\b[A-Za-z_]\w*\s*\.\s*Provide\s*\(", RegexOptions.CultureInvariant);
    private static readonly Regex GoDependencyResolution = new(
        @"\b(?:fx|dig)\s*\.\s*New\s*\(|\b[A-Za-z_]\w*\s*\.\s*Invoke\s*\(",
        RegexOptions.CultureInvariant);

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
                else if (IsNhMetadataPath(relativePath))
                    AnalyzeNhMetadata(source, relativePath, signals, sampleLimit, ref signalCount);
                else if (IsJavaScriptOrTypeScriptSource(relativePath))
                    AnalyzeJavaScript(source, relativePath, signals, sampleLimit, ref signalCount);
                else if (IsJvmSource(relativePath))
                    AnalyzeJvm(source, relativePath, signals, sampleLimit, ref signalCount);
                else if (IsGoSource(relativePath))
                    AnalyzeGo(source, relativePath, signals, sampleLimit, ref signalCount);
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

        var nhibernateObserved = signals.Any(signal => signal.Category.StartsWith("NHibernate ", StringComparison.Ordinal));
        var limits = nhibernateObserved
            ? "NHibernate candidates do not prove transitive package resolution, runtime configuration or mapping activation, session lifetime, or database behavior."
            : string.Empty;
        return new ArchitecturePatternAnalysis(signalCount, signals, limits,
            NativeFlowAnalyzer.Analyze(root, sourcePaths, sampleLimit));
    }

    private static void AnalyzeCSharp(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: path);
        var root = tree.GetRoot();
        var namespaces = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Select(directive => directive.Name?.ToString() ?? string.Empty).ToArray();
        var nhibernateContext = namespaces.Any(name => name.StartsWith("NHibernate", StringComparison.Ordinal)
                || name.StartsWith("FluentNHibernate", StringComparison.Ordinal))
            || root.DescendantNodes().OfType<NameSyntax>().Any(name => name.ToString().StartsWith("NHibernate.", StringComparison.Ordinal)
                || name.ToString().StartsWith("FluentNHibernate.", StringComparison.Ordinal));
        if (nhibernateContext)
        {
            foreach (var type in root.DescendantNodes().OfType<TypeSyntax>())
            {
                var simple = type.ToString().Split('.').Last().TrimEnd('?');
                var category = simple switch
                {
                    "ISessionFactory" => "NHibernate session factory",
                    "ISession" or "IStatelessSession" => "NHibernate session",
                    "ITransaction" => "NHibernate transaction",
                    _ => null
                };
                if (category is not null) AddNamedSignal(category, "C#", path, type, simple, signals, sampleLimit, ref signalCount);
            }
        }
        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            var name = TypeIdentifier(type);
            if (name.Contains("Factory", StringComparison.OrdinalIgnoreCase))
                AddSignal("Factory type", "C#", path, type, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!HasTestAttribute(method.AttributeLists.SelectMany(list => list.Attributes)
                    .Select(attribute => attribute.Name.ToString()))
                && IsFactoryMethod(method))
                AddMethodSignal("Factory method", "C#", path,
                    (method.Modifiers.Count > 0 ? method.Modifiers[0] : method.ReturnType.GetFirstToken()).GetLocation(),
                    method.WithAttributeLists(default).WithBody(null).WithExpressionBody(null)
                        .WithSemicolonToken(default).WithoutTrivia().ToString(), signals, sampleLimit, ref signalCount);
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
            if (nhibernateContext)
            {
                var nhCategory = name switch
                {
                    "BuildSessionFactory" or "OpenSessionFactory" => "NHibernate session factory",
                    "OpenSession" or "OpenStatelessSession" or "Get" or "Load" or "Save" or "SaveOrUpdate" or "Update" or "Delete" or "Flush" or "Clear" or "Close" => "NHibernate session",
                    "CreateQuery" or "CreateSQLQuery" or "GetNamedQuery" or "Query" => "NHibernate query",
                    "BeginTransaction" or "Commit" or "Rollback" => "NHibernate transaction",
                    "AddFromAssembly" or "AddFromAssemblyOf" or "AddClass" or "AddFile" or "AddAssembly" => "NHibernate mapping",
                    "Configure" or "SetProperty" or "SetProperties" => "NHibernate configuration",
                    _ => null
                };
                if (nhCategory is not null)
                    AddNamedSignal(nhCategory, "C#", path, invocation, name, signals, sampleLimit, ref signalCount);
                else if (IsRegistrationName(name) && invocation.ToString().Contains("ISession", StringComparison.Ordinal))
                    AddNamedSignal("NHibernate DI registration", "C#", path, invocation, name, signals, sampleLimit, ref signalCount);
            }
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
        var nhibernateContext = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.ImportsStatementSyntax>()
            .Any(import => import.ToString().Contains("NHibernate", StringComparison.OrdinalIgnoreCase))
            || root.DescendantNodes().Any(node => node.GetType().Name is "QualifiedNameSyntax" or "MemberAccessExpressionSyntax"
                && (node.ToString().StartsWith("NHibernate.", StringComparison.OrdinalIgnoreCase)
                    || node.ToString().StartsWith("FluentNHibernate.", StringComparison.OrdinalIgnoreCase)));
        foreach (var type in root.DescendantNodes().OfType<VisualBasicTypeStatement>())
        {
            if (type.Identifier.ValueText.Contains("Factory", StringComparison.OrdinalIgnoreCase))
                AddSignal("Factory type", "VB.NET", path, type, source, signals, sampleLimit, ref signalCount);
        }

        foreach (var method in root.DescendantNodes().OfType<VisualBasicMethodStatement>())
        {
            if (!HasTestAttribute(method.AttributeLists.SelectMany(list => list.Attributes)
                    .Select(attribute => attribute.Name.ToString()))
                && IsFactoryMethod(method))
                AddMethodSignal("Factory method", "VB.NET", path,
                    (method.Modifiers.Count > 0 ? method.Modifiers[0] : method.SubOrFunctionKeyword).GetLocation(),
                    method.WithAttributeLists(default).WithoutTrivia().ToString(), signals, sampleLimit, ref signalCount);
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
            if (nhibernateContext)
            {
                var nhCategory = name switch
                {
                    "BuildSessionFactory" or "OpenSessionFactory" => "NHibernate session factory",
                    "OpenSession" or "OpenStatelessSession" or "Get" or "Load" or "Save" or "SaveOrUpdate" or "Update" or "Delete" or "Flush" or "Clear" or "Close" => "NHibernate session",
                    "CreateQuery" or "CreateSQLQuery" or "GetNamedQuery" or "Query" => "NHibernate query",
                    "BeginTransaction" or "Commit" or "Rollback" => "NHibernate transaction",
                    "AddFromAssembly" or "AddFromAssemblyOf" or "AddClass" or "AddFile" or "AddAssembly" => "NHibernate mapping",
                    "Configure" or "SetProperty" or "SetProperties" => "NHibernate configuration",
                    _ => null
                };
                if (nhCategory is not null)
                    AddNamedSignal(nhCategory, "VB.NET", path, invocation, name, signals, sampleLimit, ref signalCount);
                else if (IsRegistrationName(name) && invocation.ToString().Contains("ISession", StringComparison.OrdinalIgnoreCase))
                    AddNamedSignal("NHibernate DI registration", "VB.NET", path, invocation, name, signals, sampleLimit, ref signalCount);
            }
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

    private static bool IsNhMetadataPath(string path)
    {
        var fileName = Path.GetFileName(path);
        return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("packages.config", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("hibernate.cfg.xml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".hbm.xml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".config", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
    }

    private static void AnalyzeNhMetadata(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.Equals("hibernate.cfg.xml", StringComparison.OrdinalIgnoreCase))
            AddLineSignal("NHibernate configuration", "XML", path, 1, "hibernate.cfg.xml configuration candidate",
                signals, sampleLimit, ref signalCount);
        if (path.EndsWith(".hbm.xml", StringComparison.OrdinalIgnoreCase))
            AddLineSignal("NHibernate mapping", "XML", path, 1, "*.hbm.xml mapping candidate",
                signals, sampleLimit, ref signalCount);

        try
        {
            using var reader = XmlReader.Create(new StringReader(source), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 4_000_000
            });
            var document = XDocument.Load(reader, LoadOptions.SetLineInfo);
            var extension = Path.GetExtension(path);
            var isPackageMetadata = extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("packages.config", StringComparison.OrdinalIgnoreCase);
            if (isPackageMetadata)
            {
                foreach (var dependency in document.Descendants().Where(element => element.Name.LocalName is "PackageReference" or "Reference" or "package"))
                {
                    var id = (string?)dependency.Attribute("Include") ?? (string?)dependency.Attribute("id") ?? "";
                    id = id.Split(',')[0].Trim();
                    if (!IsNhPackageId(id)) continue;
                    var line = (dependency as IXmlLineInfo)?.HasLineInfo() == true ? ((IXmlLineInfo)dependency).LineNumber : 0;
                    AddLineSignal("NHibernate dependency", "project metadata", path, line, $"Direct package/reference: {id}",
                        signals, sampleLimit, ref signalCount);
                }
            }

            foreach (var element in document.Root?.DescendantsAndSelf() ?? Enumerable.Empty<XElement>())
            {
                var name = element.Name.LocalName;
                if (name.Equals("hibernate-mapping", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".hbm.xml", StringComparison.OrdinalIgnoreCase))
                {
                    var line = (element as IXmlLineInfo)?.HasLineInfo() == true ? ((IXmlLineInfo)element).LineNumber : 0;
                    AddLineSignal("NHibernate mapping", "XML", path, line, "hibernate-mapping root element",
                        signals, sampleLimit, ref signalCount);
                }
                else if (name.Equals("hibernate-configuration", StringComparison.OrdinalIgnoreCase)
                    && !fileName.Equals("hibernate.cfg.xml", StringComparison.OrdinalIgnoreCase))
                {
                    var line = (element as IXmlLineInfo)?.HasLineInfo() == true ? ((IXmlLineInfo)element).LineNumber : 0;
                    AddLineSignal("NHibernate configuration", "XML", path, line, "hibernate-configuration root element",
                        signals, sampleLimit, ref signalCount);
                }
            }
        }
        catch (XmlException)
        {
            // Malformed metadata is not evidence of NHibernate use. Source read failures are handled by the caller.
        }
    }

    private static bool IsNhPackageId(string id) => id.Equals("NHibernate", StringComparison.OrdinalIgnoreCase)
        || id.Equals("FluentNHibernate", StringComparison.OrdinalIgnoreCase)
        || id.StartsWith("NHibernate.", StringComparison.OrdinalIgnoreCase);

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
        var normalizedSource = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalizedSource.Split('\n');
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

    private static void AnalyzeJavaScript(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var language = Path.GetExtension(path).ToLowerInvariant() is ".ts" or ".tsx" or ".mts" or ".cts"
            ? "TypeScript" : "JavaScript";
        var inBlockComment = false;
        var quote = '\0';
        var escaped = false;
        var normalizedSource = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalizedSource.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var code = RemoveJavaScriptCommentsAndStrings(lines[index], ref inBlockComment, ref quote, ref escaped);
            AddJavaScriptPattern(JavaScriptFactoryType, "Factory type", code, language, path, index + 1, signals,
                sampleLimit, ref signalCount);
            AddJavaScriptPattern(JavaScriptFactoryMethod, "Factory method", code, language, path, index + 1,
                signals, sampleLimit, ref signalCount);
            AddJavaScriptPattern(JavaScriptFactoryInvocation, "Factory invocation", code, language, path,
                index + 1, signals, sampleLimit, ref signalCount);
            AddJavaScriptPattern(JavaScriptProviderRegistration, "DI registration", code, language, path,
                index + 1, signals, sampleLimit, ref signalCount);
            AddJavaScriptPattern(JavaScriptContainerBinding, "DI registration", code, language, path,
                index + 1, signals, sampleLimit, ref signalCount);
            AddJavaScriptPattern(JavaScriptContainerResolution, "DI resolution", code, language, path,
                index + 1, signals, sampleLimit, ref signalCount);
            AddJavaScriptPattern(JavaScriptDependencyInjection, "Dependency injection", code, language, path,
                index + 1, signals, sampleLimit, ref signalCount);
        }
    }

    private static void AnalyzeJvm(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var language = extension == ".java" ? "Java" : "Kotlin";
        var code = MaskJvmCommentsAndStrings(source.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n'), language == "Kotlin");
        var lines = code.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            AddJvmPattern(JvmFactoryType, "Factory type", lines[index], language, path, index + 1, signals,
                sampleLimit, ref signalCount);
            AddJvmPattern(JvmFactoryMethod, "Factory method", lines[index], language, path, index + 1, signals,
                sampleLimit, ref signalCount);
            AddJvmPattern(JvmFactoryInvocation, "Factory invocation", lines[index], language, path, index + 1,
                signals, sampleLimit, ref signalCount);
            AddJvmPattern(JvmProviderRegistration, "DI registration", lines[index], language, path, index + 1,
                signals, sampleLimit, ref signalCount);
            AddJvmPattern(JvmDependencyInjection, "Dependency injection", lines[index], language, path, index + 1,
                signals, sampleLimit, ref signalCount);
            AddJvmPattern(JvmContainerBinding, "DI registration", lines[index], language, path, index + 1, signals,
                sampleLimit, ref signalCount);
            AddJvmPattern(JvmContainerResolution, "DI resolution", lines[index], language, path, index + 1, signals,
                sampleLimit, ref signalCount);
        }
    }

    private static void AddJvmPattern(Regex pattern, string category, string code, string language, string path,
        int line, List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        if (pattern.IsMatch(code))
            AddLineSignal(category, language, path, line, code, signals, sampleLimit, ref signalCount);
    }

    private static string MaskJvmCommentsAndStrings(string source, bool kotlin)
    {
        var result = source.ToCharArray();
        var blockCommentDepth = 0;
        var quote = '\0';
        var escaped = false;
        var inTripleQuotedString = false;
        for (var index = 0; index < source.Length; index++)
        {
            var current = source[index];
            var next = index + 1 < source.Length ? source[index + 1] : '\0';
            var startsTripleQuote = index + 2 < source.Length
                && source[index] == '"' && source[index + 1] == '"' && source[index + 2] == '"';
            if (inTripleQuotedString)
            {
                if (startsTripleQuote)
                {
                    result[index] = result[index + 1] = result[index + 2] = ' ';
                    index += 2;
                    inTripleQuotedString = false;
                }
                else if (current != '\n') result[index] = ' ';
                continue;
            }

            if (blockCommentDepth > 0)
            {
                if (current == '/' && next == '*')
                {
                    if (kotlin) blockCommentDepth++;
                    result[index] = result[index + 1] = ' ';
                    index++;
                }
                else if (current == '*' && next == '/')
                {
                    blockCommentDepth--;
                    result[index] = result[index + 1] = ' ';
                    index++;
                }
                else if (current != '\n') result[index] = ' ';
                continue;
            }

            if (quote != '\0')
            {
                if (current == '\n')
                {
                    quote = '\0';
                    escaped = false;
                }
                else
                {
                    result[index] = ' ';
                    if (escaped) escaped = false;
                    else if (current == '\\') escaped = true;
                    else if (current == quote) quote = '\0';
                }
                continue;
            }

            if (current == '/' && next == '/')
            {
                while (index < source.Length && source[index] != '\n') result[index++] = ' ';
                if (index < source.Length) index--;
            }
            else if (current == '/' && next == '*')
            {
                blockCommentDepth = 1;
                result[index] = result[index + 1] = ' ';
                index++;
            }
            else if (startsTripleQuote)
            {
                inTripleQuotedString = true;
                result[index] = result[index + 1] = result[index + 2] = ' ';
                index += 2;
            }
            else if (current is '\'' or '"')
            {
                quote = current;
                result[index] = ' ';
            }
        }

        return new string(result);
    }

    private static void AnalyzeGo(string source, string path, List<ArchitecturePatternSignal> signals,
        int sampleLimit, ref int signalCount)
    {
        var code = MaskGoCommentsAndStrings(source.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n'));
        var lines = code.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            AddGoPattern(GoFactoryType, "Factory type", line, path, index + 1, signals, sampleLimit,
                ref signalCount);
            var functionDeclaration = GoFactoryFunction.IsMatch(line);
            AddGoPattern(GoFactoryFunction, "Factory method", line, path, index + 1, signals, sampleLimit,
                ref signalCount);
            if (!functionDeclaration)
            {
                AddGoPattern(GoFactoryInvocation, "Factory invocation", line, path, index + 1, signals,
                    sampleLimit, ref signalCount);
            }
            else
            {
                var bodyStart = line.IndexOf('{');
                for (var invocation = GoFactoryInvocation.Match(line); invocation.Success;
                     invocation = invocation.NextMatch())
                {
                    if (bodyStart < 0 || invocation.Index <= bodyStart)
                        continue;
                    AddLineSignal("Factory invocation", "Go", path, index + 1, line, signals, sampleLimit,
                        ref signalCount);
                    break;
                }
            }

            AddGoPattern(GoProviderRegistration, "DI registration", line, path, index + 1, signals, sampleLimit,
                ref signalCount);
            AddGoPattern(GoDependencyResolution, "DI resolution", line, path, index + 1, signals, sampleLimit,
                ref signalCount);
        }
    }

    private static void AddGoPattern(Regex pattern, string category, string code, string path, int line,
        List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        if (pattern.IsMatch(code))
            AddLineSignal(category, "Go", path, line, code, signals, sampleLimit, ref signalCount);
    }

    private static string MaskGoCommentsAndStrings(string source)
    {
        var result = source.ToCharArray();
        var inBlockComment = false;
        var quote = '\0';
        var escaped = false;
        var inRawString = false;
        for (var index = 0; index < source.Length; index++)
        {
            var current = source[index];
            var next = index + 1 < source.Length ? source[index + 1] : '\0';
            if (inRawString)
            {
                if (current == '`')
                {
                    result[index] = ' ';
                    inRawString = false;
                }
                else if (current != '\n') result[index] = ' ';
                continue;
            }

            if (inBlockComment)
            {
                if (current == '*' && next == '/')
                {
                    inBlockComment = false;
                    result[index] = result[index + 1] = ' ';
                    index++;
                }
                else if (current != '\n') result[index] = ' ';
                continue;
            }

            if (quote != '\0')
            {
                if (current == '\n')
                {
                    quote = '\0';
                    escaped = false;
                }
                else
                {
                    result[index] = ' ';
                    if (escaped) escaped = false;
                    else if (current == '\\') escaped = true;
                    else if (current == quote) quote = '\0';
                }
                continue;
            }

            if (current == '/' && next == '/')
            {
                while (index < source.Length && source[index] != '\n') result[index++] = ' ';
                if (index < source.Length) index--;
            }
            else if (current == '/' && next == '*')
            {
                inBlockComment = true;
                result[index] = result[index + 1] = ' ';
                index++;
            }
            else if (current == '`')
            {
                inRawString = true;
                result[index] = ' ';
            }
            else if (current is '\'' or '"')
            {
                quote = current;
                result[index] = ' ';
            }
        }

        return new string(result);
    }

    private static void AddJavaScriptPattern(Regex pattern, string category, string code, string language,
        string path, int line, List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        if (pattern.IsMatch(code))
            AddLineSignal(category, language, path, line, code, signals, sampleLimit, ref signalCount);
    }

    private static string RemoveJavaScriptCommentsAndStrings(string line, ref bool inBlockComment, ref char quote,
        ref bool escaped)
    {
        var result = new StringBuilder(line.Length);
        for (var index = 0; index < line.Length; index++)
        {
            var current = line[index];
            var next = index + 1 < line.Length ? line[index + 1] : '\0';
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

            if (current == '/' && next == '/')
                break;
            if (current == '/' && next == '*')
            {
                inBlockComment = true;
                result.Append("  ");
                index++;
                continue;
            }
            if (current is '\'' or '"' or '`')
            {
                quote = current;
                result.Append(' ');
                continue;
            }

            result.Append(current);
        }

        return result.ToString();
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

    internal static string RemoveCommentsAndStrings(string line, ref bool inBlockComment, ref char quote,
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
        !method.ReturnType.ToString().Equals("void", StringComparison.Ordinal)
        && !IsStringReturnType(method.ReturnType.ToString())
        && IsFactoryMethod(method.Identifier.ValueText);

    private static bool IsFactoryMethod(VisualBasicMethodStatement method) =>
        string.Equals(method.SubOrFunctionKeyword.ValueText, "Function", StringComparison.OrdinalIgnoreCase)
        && !IsStringReturnType(method.AsClause?.Type().ToString() ?? string.Empty)
        && IsFactoryMethod(method.Identifier.ValueText, StringComparison.OrdinalIgnoreCase);

    private static bool HasTestAttribute(IEnumerable<string> attributeNames) =>
        attributeNames.Any(IsTestAttributeName);

    private static bool IsTestAttributeName(string attributeName)
    {
        var separator = Math.Max(attributeName.LastIndexOf('.'), attributeName.LastIndexOf(':'));
        var simpleName = attributeName[(separator + 1)..];
        if (simpleName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
            simpleName = simpleName[..^"Attribute".Length];

        return IsOneOf(simpleName, "Fact", "Theory", "Test", "TestCase", "TestCaseSource",
            "TestMethod", "DataTestMethod");
    }

    private static bool IsStringReturnType(string typeName)
    {
        var normalized = typeName
            .Replace("global::", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Global.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("?", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
        return normalized.Equals("string", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("System.String", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasFactoryPrefix(string name, string prefix, StringComparison comparison) =>
        name.StartsWith(prefix, comparison)
        && (name.Length == prefix.Length || comparison == StringComparison.OrdinalIgnoreCase || char.IsUpper(name[prefix.Length]));

    private static bool IsNativeSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".cpp" or ".cc" or ".cxx" or ".c" or ".h" or ".hpp" or ".hh" or ".hxx";

    private static bool IsPythonSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".py" or ".pyw" or ".pyi";

    private static bool IsJavaScriptOrTypeScriptSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".js" or ".jsx" or ".mjs" or ".cjs" or ".ts" or ".tsx" or ".mts" or ".cts";

    private static bool IsJvmSource(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".java" or ".kt" or ".kts";

    private static bool IsGoSource(string path) => Path.GetExtension(path).Equals(".go", StringComparison.OrdinalIgnoreCase);

    private static void AddSignal(string category, string language, string path, SyntaxNode node, string source,
        List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        var span = node.GetLocation().GetLineSpan().StartLinePosition;
        var line = source.Split('\n').ElementAtOrDefault(span.Line)?.Trim() ?? node.ToString();
        AddLineSignal(category, language, path, span.Line + 1, line, signals, sampleLimit, ref signalCount);
    }

    private static void AddNamedSignal(string category, string language, string path, SyntaxNode node, string evidence,
        List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        AddLineSignal(category, language, path, line, $"NHibernate API: {evidence}", signals, sampleLimit, ref signalCount);
    }

    private static void AddMethodSignal(string category, string language, string path, Location location,
        string signature, List<ArchitecturePatternSignal> signals, int sampleLimit, ref int signalCount)
    {
        var line = location.GetLineSpan().StartLinePosition.Line + 1;
        AddLineSignal(category, language, path, line, signature, signals, sampleLimit, ref signalCount);
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
