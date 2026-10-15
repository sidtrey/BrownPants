using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

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
        var files = discovered
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".vb", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ThenBy(path => path, StringComparer.Ordinal).ToArray();
        var projectMetadata = DotNetProjectAnalyzer.Analyze(root,
            discovered.Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)));
        var findings = new List<WinFormsUiThreadingFinding>();
        var issues = new List<WinFormsUiThreadingIssue>();
        foreach (var path in files)
        {
            try
            {
                var source = File.ReadAllText(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) AnalyzeCSharp(path, source, projectMetadata, findings, issues);
                else AnalyzeVisualBasic(path, source, findings, issues);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                issues.Add(new(path, 0, "BP0001", "Source could not be read."));
            }
        }
        var ordered = findings.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ThenBy(f => f.Path, StringComparer.Ordinal)
            .ThenBy(f => f.Line).ThenBy(f => f.Column).ToArray();
        return new(1, files.Length, ordered.Length, Math.Max(0, ordered.Length - limit), issues.Count,
            ordered.Take(limit).ToArray(), issues.OrderBy(i => i.Path, StringComparer.Ordinal).ThenBy(i => i.Line).Take(limit).ToArray());
    }

    private static void AnalyzeCSharp(string path, string source, IReadOnlyList<DotNetProjectInfo> projects,
        List<WinFormsUiThreadingFinding> output, List<WinFormsUiThreadingIssue> issues)
    {
        var tree = CSharpSyntaxTree.ParseText(source, CSharpAnalyzer.ParseOptionsFor(path, projects), path);
        var root = tree.GetRoot();
        var imports = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.Name?.ToString()).Where(n => n is not null).Select(n => n!).ToHashSet(StringComparer.Ordinal);
        var aliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(u => u.Alias is not null)
            .ToDictionary(u => u.Alias!.Name.Identifier.ValueText, u => u.Name?.ToString() ?? "", StringComparer.Ordinal);
        var derived = new HashSet<string>(StringComparer.Ordinal);
        var declarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
        bool changed;
        do
        {
            changed = false;
            foreach (var declaration in declarations)
            {
                if (derived.Contains(declaration.Identifier.ValueText)) continue;
                if (declaration.BaseList?.Types.Any(t => IsControlType(t.Type.ToString(), imports, aliases, derived)) == true)
                    changed |= derived.Add(declaration.Identifier.ValueText);
            }
        } while (changed);
        var variableTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in root.DescendantNodes().OfType<VariableDeclarationSyntax>())
            foreach (var declarator in variable.Variables) variableTypes[declarator.Identifier.ValueText] = variable.Type.ToString();
        foreach (var property in root.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => a.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression)))
        {
            string receiver;
            string propertyName;
            string? type;
            if (property.Left is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax member)
            {
                receiver = member.Expression.ToString();
                propertyName = member.Name.Identifier.ValueText;
                type = ResolveReceiver(receiver, variableTypes, declarations, imports, aliases, derived, property);
            }
            else if (property.Left is Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax identifier)
            {
                receiver = "this"; propertyName = identifier.Identifier.ValueText;
                type = property.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText;
            }
            else continue;
            if (type is null || !IsControlType(type, imports, aliases, derived)) continue;
            var evidence = IsControlType(type, imports, aliases, derived) ? $"Control type syntax: {type}" : $"WinForms namespace/type syntax: {type}";
            var ancestors = property.Ancestors().ToArray();
            var callback = FindCallback(ancestors);
            var marshal = FindMarshal(ancestors);
            var method = ancestors.OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            var methodText = method?.ToString() ?? "";
            if (methodText.Contains("DoWorkEventArgs", StringComparison.Ordinal)) callback ??= "DoWorkEventArgs handler signature";
            if (methodText.Contains("InvokeRequired", StringComparison.Ordinal)) marshal ??= "InvokeRequired marker observed";
            var loc = property.GetLocation().GetLineSpan().StartLinePosition;
            output.Add(new(path, loc.Line + 1, loc.Character + 1, receiver, propertyName, evidence,
                callback is null ? "Unknown" : "Background callback observed", callback, marshal));
        }
        AddIssues(path, tree.GetDiagnostics(), issues);
    }

    private static string? ResolveReceiver(string receiver, Dictionary<string, string> vars, ClassDeclarationSyntax[] classes,
        HashSet<string> imports, Dictionary<string, string> aliases, HashSet<string> derived, SyntaxNode assignment)
    {
        if (receiver is "this" or "base")
        {
            var cls = assignment.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return cls?.Identifier.ValueText;
        }
        if (vars.TryGetValue(receiver, out var type)) return type;
        if (receiver.Contains('.'))
        {
            var pieces = receiver.Split('.');
            if (vars.TryGetValue(pieces[0], out type)) return type;
            if (vars.TryGetValue(pieces[^1], out type)) return type;
            if (IsControlType(receiver, imports, aliases, derived)) return receiver;
        }
        return null;
    }

    private static void AnalyzeVisualBasic(string path, string source, List<WinFormsUiThreadingFinding> output, List<WinFormsUiThreadingIssue> issues)
    {
        var tree = VisualBasicSyntaxTree.ParseText(source, path: path);
        var root = tree.GetRoot();
        var imports = root.DescendantNodes().OfType<ImportsStatementSyntax>()
            .Select(i => i.ToString().Replace("Imports", "", StringComparison.OrdinalIgnoreCase).Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var typeBlocks = root.DescendantNodes().OfType<TypeBlockSyntax>().ToArray();
        var derived = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool added;
        do
        {
            added = false;
            foreach (var statement in root.DescendantNodes().Where(n => n.GetType().Name == "InheritsStatementSyntax"))
            {
                var parentType = statement.Ancestors().OfType<TypeBlockSyntax>().FirstOrDefault();
                if (parentType is null) continue;
                var baseName = statement.ToString().Split(' ').Last().Split('.').Last();
                if (IsControlType(baseName, imports, new(), derived)) added |= derived.Add(parentType.BlockStatement.Identifier.ValueText);
            }
        } while (added);
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var declaration in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax>())
        {
            var type = declaration.AsClause?.ToString();
            if (type is null) continue;
            foreach (var name in declaration.Names) variables[name.Identifier.ValueText] = type.Split(' ').Last().Trim();
        }
        foreach (var assignment in root.DescendantNodes().OfType<AssignmentStatementSyntax>())
        {
            string receiver;
            string propertyName;
            string? type;
            if (assignment.Left is Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax member)
            {
                receiver = member.Expression.ToString(); propertyName = member.Name.ToString();
                type = variables.GetValueOrDefault(receiver);
            }
            else if (assignment.Left is Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax identifier)
            {
                receiver = "Me"; propertyName = identifier.Identifier.ValueText;
                type = assignment.Ancestors().OfType<TypeBlockSyntax>().FirstOrDefault()?.BlockStatement.Identifier.ValueText;
            }
            else continue;
            if (receiver.Equals("Me", StringComparison.OrdinalIgnoreCase))
                type = assignment.Ancestors().OfType<TypeBlockSyntax>().FirstOrDefault()?.BlockStatement.Identifier.ValueText;
            if (type is null && IsControlType(receiver, imports, new(), derived)) type = receiver;
            if (type is null || !IsControlType(type, imports, new(), derived)) continue;
            var ancestors = assignment.Ancestors().ToArray();
            var callback = FindCallback(ancestors);
            var marshal = FindMarshal(ancestors);
            var methodText = ancestors.OfType<MethodBlockSyntax>().FirstOrDefault()?.ToString() ?? "";
            if (methodText.Contains("DoWorkEventArgs", StringComparison.OrdinalIgnoreCase)) callback ??= "DoWorkEventArgs handler signature";
            if (methodText.Contains("InvokeRequired", StringComparison.OrdinalIgnoreCase)) marshal ??= "InvokeRequired marker observed";
            var loc = assignment.GetLocation().GetLineSpan().StartLinePosition;
            output.Add(new(path, loc.Line + 1, loc.Character + 1, receiver, propertyName, $"WinForms type syntax: {type}",
                callback is null ? "Unknown" : "Background callback observed", callback, marshal));
        }
        AddIssues(path, tree.GetDiagnostics(), issues);
    }

    private static bool IsControlType(string type, HashSet<string> imports, Dictionary<string, string> aliases, HashSet<string> derived)
    {
        var simple = type.Split('.').Last().TrimEnd('?');
        if (aliases.TryGetValue(simple, out var aliasTarget)) type = aliasTarget;
        simple = type.Split('.').Last().TrimEnd('?');
        return type.StartsWith("System.Windows.Forms.", StringComparison.Ordinal)
            || (imports.Contains("System.Windows.Forms") && (BuiltInControls.Contains(simple) || derived.Contains(simple)))
            || derived.Contains(simple);
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

    private static void AddIssues(string path, IEnumerable<Diagnostic> diagnostics, List<WinFormsUiThreadingIssue> output)
    {
        foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
            output.Add(new(path, line, diagnostic.Id, diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }
}
