using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Cs = Microsoft.CodeAnalysis.CSharp.Syntax;
using Vb = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace BrownPants.Cli.Analysis;

public static class WinFormsUiThreadingAnalyzer
{
    private static readonly HashSet<string> BuiltInControls = new(StringComparer.Ordinal)
    {
        "Control", "Form", "UserControl", "Button", "Label", "TextBox", "RichTextBox", "ComboBox",
        "ListBox", "CheckedListBox", "CheckBox", "RadioButton", "PictureBox", "Panel", "GroupBox",
        "TabControl", "TabPage", "DataGridView", "ListView", "TreeView", "ProgressBar", "StatusStrip",
        "MenuStrip", "ToolStrip", "ToolStripButton", "NumericUpDown", "DateTimePicker", "WebBrowser"
    };

    public static WinFormsUiThreadingReport Analyze(string repository, int limit)
    {
        if (limit is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(limit));
        var root = Path.GetFullPath(repository);
        var discovered = RepositoryScanner.DiscoverFiles(root);
        var files = discovered.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".vb", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ThenBy(path => path, StringComparer.Ordinal).ToArray();
        var projectMetadata = DotNetProjectAnalyzer.Analyze(root,
            discovered.Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)));
        var contexts = new List<SourceContext>();
        var findings = new List<WinFormsUiThreadingFinding>();
        var issues = new List<WinFormsUiThreadingIssue>();
        var parseErrorCount = 0;
        foreach (var path in files)
        {
            try
            {
                var source = File.ReadAllText(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                var isVisualBasic = path.EndsWith(".vb", StringComparison.OrdinalIgnoreCase);
                SyntaxTree tree = isVisualBasic
                    ? VisualBasicSyntaxTree.ParseText(source, path: path)
                    : CSharpSyntaxTree.ParseText(source, CSharpAnalyzer.ParseOptionsFor(path, projectMetadata), path);
                var context = new SourceContext(path, isVisualBasic, tree.GetRoot());
                context.IndexScopedDeclarations();
                AddDeclarations(context);
                contexts.Add(context);
                var diagnostics = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                parseErrorCount += diagnostics.Length;
                AddIssues(path, diagnostics, issues, 10_000);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AddIssue(issues, new(path, 0, "BP0001", "Source could not be read."), 10_000);
            }
        }

        var index = new SourceIndex(contexts);
        var unresolvedReceiverCount = 0;
        foreach (var context in contexts)
        {
            if (context.IsVisualBasic)
                AnalyzeVisualBasic(context, index, findings, issues, ref unresolvedReceiverCount);
            else
                AnalyzeCSharp(context, index, findings, issues, ref unresolvedReceiverCount);
        }
        var ordered = findings.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ThenBy(f => f.Path, StringComparer.Ordinal)
            .ThenBy(f => f.Line).ThenBy(f => f.Column).ToArray();
        return new(1, files.Length, ordered.Length, Math.Max(0, ordered.Length - limit), parseErrorCount,
            ordered.Take(limit).ToArray(), issues.OrderBy(i => i.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Path, StringComparer.Ordinal).ThenBy(i => i.Line).ThenBy(i => i.Id, StringComparer.Ordinal)
                .Take(limit).ToArray(), unresolvedReceiverCount, index.TypeCount);
    }

    private static void AddDeclarations(SourceContext context)
    {
        if (!context.IsVisualBasic)
        {
            foreach (var declaration in context.Root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var type = new SourceType(context, NamespaceOf(declaration), declaration.Identifier.ValueText) { DeclarationNode = declaration };
                if (declaration.BaseList is not null)
                    type.BaseTypeNames.AddRange(declaration.BaseList.Types.Select(baseType => baseType.Type.ToString()));
                foreach (var field in declaration.Members.OfType<Cs.FieldDeclarationSyntax>())
                    foreach (var variable in field.Declaration.Variables)
                        type.AddMember(variable.Identifier.ValueText, field.Declaration.Type.ToString(), field);
                foreach (var property in declaration.Members.OfType<PropertyDeclarationSyntax>())
                    type.AddMember(property.Identifier.ValueText, property.Type.ToString(), property);
                context.Types.Add(type);
            }
            return;
        }

        foreach (var declaration in context.Root.DescendantNodes().OfType<TypeBlockSyntax>()
                     .Where(block => block.BlockStatement.GetType().Name.Contains("Class", StringComparison.Ordinal)))
        {
            var type = new SourceType(context, NamespaceOf(declaration), declaration.BlockStatement.Identifier.ValueText) { DeclarationNode = declaration };
            foreach (var inherits in declaration.DescendantNodes().Where(node => node.GetType().Name == "InheritsStatementSyntax"))
                type.BaseTypeNames.Add(Regex.Replace(inherits.ToString(), @"^\s*Inherits\s+", "", RegexOptions.IgnoreCase).Trim());
            foreach (var member in declaration.Members)
            {
                if (member.GetType().Name == "FieldDeclarationSyntax")
                {
                    var match = Regex.Match(member.ToString(), @"^\s*(?:(?:Public|Private|Protected|Friend|Shared|ReadOnly|WithEvents|Dim)\s+)*(?<name>[A-Za-z_][\w]*)\s+As\s+(?<type>[\w.(),?]+)", RegexOptions.IgnoreCase);
                    if (match.Success) type.AddMember(match.Groups["name"].Value, match.Groups["type"].Value, member);
                }
                else if (member.GetType().Name is "PropertyStatementSyntax" or "PropertyBlockSyntax")
                {
                    var match = Regex.Match(member.ToString(), @"\bProperty\s+(?<name>[A-Za-z_][\w]*)\s+As\s+(?<type>[\w.(),?]+)", RegexOptions.IgnoreCase);
                    if (match.Success) type.AddMember(match.Groups["name"].Value, match.Groups["type"].Value, member);
                }
            }
            context.Types.Add(type);
        }
    }

    private static void AnalyzeCSharp(SourceContext context, SourceIndex index,
        List<WinFormsUiThreadingFinding> output, List<WinFormsUiThreadingIssue> issues, ref int unresolvedCount)
    {
        foreach (var assignment in context.Root.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                     .Where(a => a.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression)))
        {
            if (assignment.Left is not Cs.MemberAccessExpressionSyntax member)
            {
                if (assignment.Left is Cs.IdentifierNameSyntax)
                {
                    var current = index.ContainingType(context, assignment);
                    if (current is not null && index.IsControl(current.Key))
                        AddFinding(context, assignment, "this", assignment.Left.ToString(), current.Identity, output);
                }
                continue;
            }
            var receiver = member.Expression.ToString();
            var type = index.ResolveCSharpReceiver(context, assignment, member.Expression);
            if (type is null)
            {
                if (index.MayBeWinFormsSource(context, assignment))
                {
                    unresolvedCount++;
                    AddIssue(issues, new(context.Path, Line(assignment), "BPWF100", "Assignment receiver type could not be resolved from source declarations."), 10_000);
                }
                continue;
            }
            if (index.IsControl(type)) AddFinding(context, assignment, receiver, member.Name.Identifier.ValueText, type, output);
        }
    }

    private static void AnalyzeVisualBasic(SourceContext context, SourceIndex index,
        List<WinFormsUiThreadingFinding> output, List<WinFormsUiThreadingIssue> issues, ref int unresolvedCount)
    {
        foreach (var assignment in context.Root.DescendantNodes().OfType<AssignmentStatementSyntax>())
        {
            string receiver;
            string propertyName;
            string? type;
            if (assignment.Left is Vb.MemberAccessExpressionSyntax member)
            {
                receiver = member.Expression.ToString(); propertyName = member.Name.ToString();
                type = index.ResolveVisualBasicReceiver(context, assignment, member.Expression);
                if (type is null)
                {
                    if (index.MayBeWinFormsSource(context, assignment))
                    {
                        unresolvedCount++;
                        AddIssue(issues, new(context.Path, Line(assignment), "BPWF100", "Assignment receiver type could not be resolved from source declarations."), 10_000);
                    }
                    continue;
                }
            }
            else if (assignment.Left is Vb.IdentifierNameSyntax identifier)
            {
                receiver = "Me"; propertyName = identifier.Identifier.ValueText;
                type = index.ContainingType(context, assignment)?.Identity;
            }
            else continue;
            if (type is not null && index.IsControl(type)) AddFinding(context, assignment, receiver, propertyName, type, output);
        }
    }

    private static void AddFinding(SourceContext context, SyntaxNode assignment,
        string receiver, string property, string type, List<WinFormsUiThreadingFinding> output)
    {
        var ancestors = assignment.Ancestors().ToArray();
        var callback = FindCallback(ancestors);
        var marshal = FindMarshal(ancestors);
        var methodText = string.Join(" ", ancestors.Where(node => node.GetType().Name.Contains("Method", StringComparison.Ordinal)
            || node is BaseMethodDeclarationSyntax).Select(node => node.ToString()));
        if (methodText.Contains("DoWorkEventArgs", StringComparison.OrdinalIgnoreCase)) callback ??= "DoWorkEventArgs handler signature";
        if (methodText.Contains("InvokeRequired", StringComparison.OrdinalIgnoreCase)) marshal ??= "InvokeRequired marker observed";
        var location = assignment.GetLocation().GetLineSpan().StartLinePosition;
        output.Add(new(context.Path, location.Line + 1, location.Character + 1, receiver, property,
            $"Source declaration: {DisplayType(type)}", callback is null ? "Unknown" : "Background callback observed", callback, marshal));
    }

    private static string DisplayType(string key)
    {
        if (key.StartsWith("@WF:", StringComparison.Ordinal)) return "System.Windows.Forms." + key[4..];
        if (key.StartsWith("CS:", StringComparison.Ordinal) || key.StartsWith("VB:", StringComparison.Ordinal)) return key[3..];
        return key;
    }

    private static string? FindCallback(IEnumerable<SyntaxNode> ancestors)
    {
        foreach (var lambda in ancestors.Where(n => n.GetType().Name.Contains("Lambda", StringComparison.Ordinal)
                     || n.GetType().Name.Contains("AnonymousMethod", StringComparison.Ordinal)))
        {
            var context = lambda.Ancestors().FirstOrDefault(n => n.GetType().Name.Contains("InvocationExpression", StringComparison.Ordinal)
                || n.GetType().Name.Contains("ObjectCreationExpression", StringComparison.Ordinal));
            var text = context?.ToString() ?? string.Empty;
            foreach (var marker in new[] { "Task.Run", "TaskFactory.StartNew", "ThreadPool.QueueUserWorkItem", "QueueUserWorkItem", "new Thread", "Thread.Start", "DoWork" })
                if (text.Contains(marker, StringComparison.Ordinal)) return marker;
        }
        return null;
    }

    private static string? FindMarshal(IEnumerable<SyntaxNode> ancestors)
    {
        foreach (var lambda in ancestors.Where(n => n.GetType().Name.Contains("Lambda", StringComparison.Ordinal)
                     || n.GetType().Name.Contains("AnonymousMethod", StringComparison.Ordinal)))
        {
            var context = lambda.Ancestors().FirstOrDefault(n => n.GetType().Name.Contains("InvocationExpression", StringComparison.Ordinal));
            var text = context?.ToString() ?? string.Empty;
            foreach (var marker in new[] { "InvokeAsync", "BeginInvoke", "Invoke", "SynchronizationContext.Post", "SynchronizationContext.Send", ".Post(", ".Send(" })
                if (text.Contains(marker, StringComparison.Ordinal)) return $"{marker} callback syntax observed";
        }
        return null;
    }

    private static string NamespaceOf(SyntaxNode node)
    {
        if (node.Language == LanguageNames.CSharp)
            return string.Join(".", node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(ns => ns.Name.ToString()));
        return string.Join(".", node.Ancestors().Where(ancestor => ancestor.GetType().Name == "NamespaceBlockSyntax")
            .Reverse().Select(ns => Regex.Replace(ns.ChildNodes().FirstOrDefault()?.ToString() ?? "", @"^\s*Namespace\s+", "", RegexOptions.IgnoreCase).Trim()));
    }

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static void AddIssues(string path, IEnumerable<Diagnostic> diagnostics, List<WinFormsUiThreadingIssue> output, int limit)
    {
        foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            AddIssue(output, new(path, diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1, diagnostic.Id,
                diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)), limit);
    }

    private static void AddIssue(List<WinFormsUiThreadingIssue> output, WinFormsUiThreadingIssue issue, int limit)
    {
        if (output.Count < limit) output.Add(issue);
    }

    private sealed class SourceContext(string path, bool isVisualBasic, SyntaxNode root)
    {
        public string Path { get; } = path;
        public bool IsVisualBasic { get; } = isVisualBasic;
        public SyntaxNode Root { get; } = root;
        public List<SourceType> Types { get; } = [];
        public List<Cs.VariableDeclaratorSyntax> CSharpVariables { get; } = [];
        public List<Cs.ParameterSyntax> CSharpParameters { get; } = [];
        public List<Vb.VariableDeclaratorSyntax> VisualBasicVariables { get; } = [];
        public List<Vb.ParameterSyntax> VisualBasicParameters { get; } = [];
        private readonly Dictionary<string, (HashSet<string> Imports, Dictionary<string, string> Aliases)> _scopeCache =
            new(isVisualBasic ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        public void IndexScopedDeclarations()
        {
            if (!IsVisualBasic)
            {
                CSharpVariables.AddRange(Root.DescendantNodes().OfType<Cs.VariableDeclaratorSyntax>());
                CSharpParameters.AddRange(Root.DescendantNodes().OfType<Cs.ParameterSyntax>());
                return;
            }
            VisualBasicVariables.AddRange(Root.DescendantNodes().OfType<Vb.VariableDeclaratorSyntax>());
            VisualBasicParameters.AddRange(Root.DescendantNodes().OfType<Vb.ParameterSyntax>());
        }

        public (HashSet<string> Imports, Dictionary<string, string> Aliases) ImportsAt(SyntaxNode location)
        {
            var locationNamespace = NamespaceOf(location);
            if (_scopeCache.TryGetValue(locationNamespace, out var cached)) return cached;
            var comparer = IsVisualBasic ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var imports = new HashSet<string>(comparer);
            var aliases = new Dictionary<string, string>(comparer);
            if (!IsVisualBasic)
            {
                foreach (var directive in Root.DescendantNodes().OfType<UsingDirectiveSyntax>())
                {
                    var directiveNamespace = NamespaceOf(directive);
                    if (directiveNamespace.Length > 0 && !string.Equals(directiveNamespace, locationNamespace,
                            IsVisualBasic ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) continue;
                    var name = directive.Name?.ToString();
                    if (name is null) continue;
                    if (directive.Alias is null) imports.Add(name);
                    else aliases[directive.Alias.Name.Identifier.ValueText] = name;
                }
            }
            else
            {
                foreach (var directive in Root.DescendantNodes().OfType<ImportsStatementSyntax>())
                {
                    var directiveNamespace = NamespaceOf(directive);
                    if (directiveNamespace.Length > 0 && !string.Equals(directiveNamespace, locationNamespace, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var clause in directive.ImportsClauses)
                    {
                        var text = clause.ToString();
                        var alias = Regex.Match(text, @"^(?<alias>[A-Za-z_]\w*)\s*=\s*(?<name>[\w.]+)$");
                        if (alias.Success) aliases[alias.Groups["alias"].Value] = alias.Groups["name"].Value;
                        else imports.Add(text);
                    }
                }
            }
            var result = (imports, aliases);
            _scopeCache[locationNamespace] = result;
            return result;
        }
    }

    private sealed class SourceType(SourceContext context, string ns, string name)
    {
        public SourceContext Context { get; } = context;
        public string Namespace { get; } = ns;
        public string Name { get; } = name;
        public string Identity => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
        public string Key => $"{(Context.IsVisualBasic ? "VB" : "CS")}:{Identity}";
        public List<string> BaseTypeNames { get; } = [];
        public List<SourceMember> Members { get; } = [];
        public bool IsControl { get; set; }
        public SyntaxNode? DeclarationNode { get; set; }

        public void AddMember(string member, string type, SyntaxNode source) => Members.Add(new(member, type, source));
    }

    private sealed record SourceMember(string Name, string TypeName, SyntaxNode Source);

    private sealed class SourceIndex
    {
        private readonly List<SourceType> _types;
        private readonly Dictionary<string, List<SourceType>> _byKey = new(StringComparer.Ordinal);

        public SourceIndex(IEnumerable<SourceContext> contexts)
        {
            _types = contexts.SelectMany(context => context.Types).ToList();
            foreach (var type in _types)
            {
                if (!_byKey.TryGetValue(type.Key, out var parts)) _byKey[type.Key] = parts = [];
                parts.Add(type);
            }
            foreach (var type in _types)
                type.IsControl = type.BaseTypeNames.Any(baseName => IsWinFormsBase(baseName, type.Context, type.Namespace, type.DeclarationNode));
            bool changed;
            do
            {
                changed = false;
                foreach (var type in _types.Where(type => !type.IsControl))
                    if (type.BaseTypeNames.Any(baseName => ResolveSourceType(baseName, type.Context, type.Namespace, type.DeclarationNode)?.IsControl == true))
                    { type.IsControl = true; changed = true; }
            } while (changed);
        }

        public int TypeCount => _byKey.Count;

        public bool IsControl(string key) => key.StartsWith("@WF:", StringComparison.Ordinal)
            || (_byKey.TryGetValue(key, out var parts) && parts.Any(part => part.IsControl));

        public bool MayBeWinFormsSource(SourceContext context, SyntaxNode assignment)
        {
            var scope = context.ImportsAt(assignment);
            return scope.Imports.Contains("System.Windows.Forms")
                || scope.Aliases.Values.Any(value => value.StartsWith("System.Windows.Forms", StringComparison.OrdinalIgnoreCase))
                || (ContainingType(context, assignment) is { } type && IsControl(type.Key));
        }

        public SourceType? ContainingType(SourceContext context, SyntaxNode node)
        {
            if (!context.IsVisualBasic)
            {
                var declaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (declaration is null) return null;
                var ns = NamespaceOf(declaration);
                return FindType(context.IsVisualBasic, ns, declaration.Identifier.ValueText);
            }
            var vbDeclaration = node.Ancestors().OfType<TypeBlockSyntax>().FirstOrDefault();
            return vbDeclaration is null ? null : FindType(true, NamespaceOf(vbDeclaration), vbDeclaration.BlockStatement.Identifier.ValueText);
        }

        public string? ResolveCSharpReceiver(SourceContext context, SyntaxNode assignment, SyntaxNode receiver)
        {
            if (receiver is Cs.ThisExpressionSyntax or Cs.BaseExpressionSyntax)
                return ContainingType(context, assignment)?.Key;
            if (receiver is Cs.ParenthesizedExpressionSyntax parenthesized)
                return ResolveCSharpReceiver(context, assignment, parenthesized.Expression);
            if (receiver is Cs.IdentifierNameSyntax identifier)
            {
                var name = identifier.Identifier.ValueText;
                if (name is "this" or "base") return ContainingType(context, assignment)?.Key;
                var containing = ContainingType(context, assignment);
                var local = ResolveCSharpLocal(context, assignment, name);
                if (local is not null) return ResolveTypeName(local, context, containing?.Namespace, assignment);
                return containing is null ? null : ResolveMember(containing, name);
            }
            if (receiver is Cs.MemberAccessExpressionSyntax member)
            {
                var parentType = ResolveCSharpReceiver(context, assignment, member.Expression);
                if (parentType is null) return null;
                return _byKey.TryGetValue(parentType, out var parts) ? ResolveMember(parts[0], member.Name.Identifier.ValueText) : null;
            }
            return null;
        }

        public string? ResolveVisualBasicReceiver(SourceContext context, SyntaxNode assignment, SyntaxNode receiver)
        {
            var text = receiver.ToString();
            if (text.Equals("Me", StringComparison.OrdinalIgnoreCase) || text.Equals("MyBase", StringComparison.OrdinalIgnoreCase))
                return ContainingType(context, assignment)?.Key;
            if (receiver is Vb.ParenthesizedExpressionSyntax parenthesized)
                return ResolveVisualBasicReceiver(context, assignment, parenthesized.Expression);
            if (receiver is Vb.IdentifierNameSyntax identifier)
            {
                var containing = ContainingType(context, assignment);
                var local = ResolveVisualBasicLocal(context, assignment, identifier.Identifier.ValueText);
                if (local is not null) return ResolveTypeName(local, context, containing?.Namespace, assignment);
                return containing is null ? null : ResolveMember(containing, identifier.Identifier.ValueText);
            }
            if (receiver is Vb.MemberAccessExpressionSyntax member)
            {
                var parentType = ResolveVisualBasicReceiver(context, assignment, member.Expression);
                if (parentType is null) return null;
                return _byKey.TryGetValue(parentType, out var parts) ? ResolveMember(parts[0], member.Name.ToString()) : null;
            }
            return null;
        }

        private string? ResolveMember(SourceType type, string name) => ResolveMember(type, name, []);

        private string? ResolveMember(SourceType type, string name, HashSet<string> visited)
        {
            if (!visited.Add(type.Key) || !_byKey.TryGetValue(type.Key, out var parts)) return null;
            var declarations = parts.SelectMany(part => part.Members.Where(member => NameEquals(part.Context, member.Name, name))
                .Select(member => (Part: part, Member: member))).ToArray();
            if (declarations.Length > 0)
            {
                var candidates = declarations.Select(item => ResolveTypeName(item.Member.TypeName, item.Part.Context,
                        item.Part.Namespace, item.Member.Source))
                    .Where(value => value is not null).Distinct(StringComparer.Ordinal).ToArray();
                return candidates.Length == 1 ? candidates[0] : null;
            }
            foreach (var part in parts)
            foreach (var baseName in part.BaseTypeNames)
            {
                var baseKey = ResolveTypeName(baseName, part.Context, part.Namespace, part.DeclarationNode);
                if (baseKey is not null && _byKey.TryGetValue(baseKey, out var baseParts) && baseParts.Count > 0)
                {
                    var inherited = ResolveMember(baseParts[0], name, visited);
                    if (inherited is not null) return inherited;
                }
            }
            return null;
        }

        private string? ResolveTypeName(string typeName, SourceContext context, string? namespaceHint = null, SyntaxNode? sourceLocation = null)
        {
            var normalized = typeName.Trim().Replace("global::", "", StringComparison.Ordinal)
                .Replace("Global.", "", StringComparison.OrdinalIgnoreCase).TrimEnd('?');
            var generic = normalized.IndexOf('<');
            if (generic >= 0) normalized = normalized[..generic];
            if (normalized.EndsWith("[]", StringComparison.Ordinal)) normalized = normalized[..^2];
            var scope = context.ImportsAt(sourceLocation ?? context.Root);
            var first = normalized.Split('.')[0];
            if (scope.Aliases.TryGetValue(first, out var alias))
                normalized = alias + normalized[first.Length..];
            if (normalized.StartsWith("System.Windows.Forms.", StringComparison.OrdinalIgnoreCase))
            {
                var simple = normalized[(normalized.LastIndexOf('.') + 1)..];
                return BuiltInControls.Contains(simple) ? "@WF:" + simple : null;
            }
            if (normalized.Contains('.'))
            {
                var source = _types.Where(type => type.Context.IsVisualBasic == context.IsVisualBasic
                    && NameEquals(context, type.Identity, normalized)).Select(type => type.Key).Distinct(StringComparer.Ordinal).ToArray();
                if (source.Length == 1) return source[0];
                return null;
            }
            if (BuiltInControls.Contains(normalized) && scope.Imports.Contains("System.Windows.Forms")) return "@WF:" + normalized;
            var sameNamespace = _types.Where(type => type.Context.IsVisualBasic == context.IsVisualBasic
                && NameEquals(context, type.Namespace, namespaceHint ?? "")
                && NameEquals(context, type.Name, normalized)).Select(type => type.Key).Distinct(StringComparer.Ordinal).ToArray();
            if (sameNamespace.Length == 1) return sameNamespace[0];
            var imported = _types.Where(type => type.Context.IsVisualBasic == context.IsVisualBasic
                && scope.Imports.Any(import => NameEquals(context, type.Identity, import + "." + normalized)))
                .Select(type => type.Key).Distinct(StringComparer.Ordinal).ToArray();
            return imported.Length == 1 ? imported[0] : null;
        }

        private bool IsWinFormsBase(string baseName, SourceContext context, string ns, SyntaxNode? sourceLocation) =>
            ResolveTypeName(baseName, context, ns, sourceLocation)?.StartsWith("@WF:", StringComparison.Ordinal) == true;

        private SourceType? ResolveSourceType(string name, SourceContext context, string ns, SyntaxNode? sourceLocation)
        {
            var key = ResolveTypeName(name, context, ns, sourceLocation);
            return key is not null && _byKey.TryGetValue(key, out var types) ? types.FirstOrDefault(type => type.IsControl) ?? types.FirstOrDefault() : null;
        }

        private SourceType? FindType(bool visualBasic, string ns, string name) => _types.FirstOrDefault(type =>
            type.Context.IsVisualBasic == visualBasic && NameEquals(type.Context, type.Namespace, ns) && NameEquals(type.Context, type.Name, name));

        private string? ResolveCSharpLocal(SourceContext context, SyntaxNode assignment, string name)
        {
            var scopes = assignment.AncestorsAndSelf().Where(IsCSharpLocalScope).ToArray();
            foreach (var scope in scopes)
            {
                var declarations = context.CSharpVariables
                    .Where(variable => variable.Identifier.ValueText == name && variable.SpanStart < assignment.SpanStart
                        && ReferenceEquals(CSharpLocalScope(variable), scope))
                    .OrderByDescending(variable => variable.SpanStart).ToArray();
                if (declarations.Length > 0 && declarations[0].Parent is Cs.VariableDeclarationSyntax declaration)
                    return declaration.Type.ToString() == "var"
                        ? InferredCSharpType(declarations[0]) ?? "var"
                        : declaration.Type.ToString();
            }
            var executable = assignment.AncestorsAndSelf().FirstOrDefault(IsCSharpExecutable);
            if (executable is not null)
            {
                var parameter = context.CSharpParameters
                    .Where(candidate => candidate.AncestorsAndSelf().Any(node => ReferenceEquals(node, executable)))
                    .FirstOrDefault(candidate => candidate.Identifier.ValueText == name);
                if (parameter is not null) return parameter.Type?.ToString();
            }
            return null;
        }

        private static string? InferredCSharpType(Cs.VariableDeclaratorSyntax declaration) => declaration.Initializer?.Value switch
        {
            Cs.ObjectCreationExpressionSyntax creation => creation.Type.ToString(),
            Cs.ImplicitObjectCreationExpressionSyntax creation => null,
            Cs.CastExpressionSyntax cast => cast.Type.ToString(),
            Cs.DefaultExpressionSyntax expression => expression.Type.ToString(),
            _ => null
        };

        private string? ResolveVisualBasicLocal(SourceContext context, SyntaxNode assignment, string name)
        {
            var method = assignment.AncestorsAndSelf().FirstOrDefault(node => node is MethodBlockSyntax);
            if (method is null) return null;
            var scopes = assignment.AncestorsAndSelf().Where(IsVisualBasicLocalScope).ToArray();
            foreach (var scope in scopes)
            {
                var declarations = context.VisualBasicVariables
                    .Where(variable => method.FullSpan.Contains(variable.FullSpan))
                    .Where(variable => variable.Names.Any(candidate => candidate.Identifier.ValueText.Equals(name, StringComparison.OrdinalIgnoreCase))
                        && variable.SpanStart < assignment.SpanStart && ReferenceEquals(VisualBasicLocalScope(variable), scope))
                    .OrderByDescending(variable => variable.SpanStart).ToArray();
                if (declarations.Length > 0) return VbDeclaredType(declarations[0]);
            }
            var parameter = context.VisualBasicParameters
                .Where(candidate => candidate.AncestorsAndSelf().Any(node => ReferenceEquals(node, method)))
                .FirstOrDefault(candidate => candidate.Identifier.Identifier.ValueText.Equals(name, StringComparison.OrdinalIgnoreCase));
            return parameter?.AsClause is null ? null : Regex.Replace(parameter.AsClause.ToString(), @"^\s*As\s+", "", RegexOptions.IgnoreCase);
        }

        private static string? VbDeclaredType(Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax declaration)
        {
            var clause = declaration.AsClause?.ToString();
            if (clause is null) return null;
            var match = Regex.Match(clause, @"\bAs\s+(?<type>[\w.(),?]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["type"].Value : null;
        }

        private static bool NameEquals(SourceContext context, string left, string right) =>
            string.Equals(left, right, context.IsVisualBasic ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        private static bool IsCSharpLocalScope(SyntaxNode node) => node is Cs.BlockSyntax or Cs.SwitchSectionSyntax
            or Cs.ForStatementSyntax or Cs.ForEachStatementSyntax or Cs.UsingStatementSyntax or Cs.CatchClauseSyntax
            or Cs.BaseMethodDeclarationSyntax or Cs.LocalFunctionStatementSyntax or Cs.AnonymousFunctionExpressionSyntax;

        private static SyntaxNode? CSharpLocalScope(SyntaxNode node) => node.AncestorsAndSelf().FirstOrDefault(IsCSharpLocalScope);

        private static bool IsVisualBasicLocalScope(SyntaxNode node)
        {
            var name = node.GetType().Name;
            return name.EndsWith("BlockSyntax", StringComparison.Ordinal)
                && name is not "TypeBlockSyntax" and not "NamespaceBlockSyntax";
        }

        private static SyntaxNode? VisualBasicLocalScope(SyntaxNode node) => node.AncestorsAndSelf().FirstOrDefault(IsVisualBasicLocalScope);

        private static bool IsCSharpExecutable(SyntaxNode node) => node is BaseMethodDeclarationSyntax
            or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax or AccessorDeclarationSyntax;
    }
}
