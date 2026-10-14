namespace BrownPants.Cli.Analysis;

public static class SourceLanguageCoverageAnalyzer
{
    private const string InventoryOnly = "Inventory only";
    private const string RoslynCoverage = "Roslyn syntax and candidate patterns";
    private const string LexicalCoverage = "Lexical candidate patterns";

    private static readonly IReadOnlyDictionary<string, (string Language, string AnalysisLevel)> Languages =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = ("C#", RoslynCoverage),
            [".vb"] = ("VB.NET", RoslynCoverage),
            [".c"] = ("C/C++", LexicalCoverage), [".cc"] = ("C/C++", LexicalCoverage),
            [".cpp"] = ("C/C++", LexicalCoverage), [".cxx"] = ("C/C++", LexicalCoverage),
            [".h"] = ("C/C++", LexicalCoverage), [".hh"] = ("C/C++", LexicalCoverage),
            [".hpp"] = ("C/C++", LexicalCoverage), [".hxx"] = ("C/C++", LexicalCoverage),
            [".js"] = ("JavaScript", InventoryOnly), [".jsx"] = ("JavaScript", InventoryOnly),
            [".mjs"] = ("JavaScript", InventoryOnly), [".cjs"] = ("JavaScript", InventoryOnly),
            [".ts"] = ("TypeScript", InventoryOnly), [".tsx"] = ("TypeScript", InventoryOnly),
            [".mts"] = ("TypeScript", InventoryOnly), [".cts"] = ("TypeScript", InventoryOnly),
            [".py"] = ("Python", LexicalCoverage), [".pyw"] = ("Python", LexicalCoverage), [".pyi"] = ("Python", LexicalCoverage),
            [".java"] = ("Java", InventoryOnly), [".kt"] = ("Kotlin", InventoryOnly),
            [".go"] = ("Go", InventoryOnly), [".rs"] = ("Rust", InventoryOnly),
            [".rb"] = ("Ruby", InventoryOnly), [".php"] = ("PHP", InventoryOnly),
            [".swift"] = ("Swift", InventoryOnly), [".m"] = ("Objective-C", InventoryOnly), [".mm"] = ("Objective-C++", InventoryOnly),
            [".dart"] = ("Dart", InventoryOnly), [".ex"] = ("Elixir", InventoryOnly), [".exs"] = ("Elixir", InventoryOnly),
            [".scala"] = ("Scala", InventoryOnly), [".sc"] = ("Scala", InventoryOnly),
            [".fs"] = ("F#", InventoryOnly), [".fsi"] = ("F#", InventoryOnly), [".fsx"] = ("F#", InventoryOnly),
            [".erl"] = ("Erlang", InventoryOnly), [".hrl"] = ("Erlang", InventoryOnly),
            [".groovy"] = ("Groovy", InventoryOnly), [".gvy"] = ("Groovy", InventoryOnly),
            [".sh"] = ("Shell", InventoryOnly), [".bash"] = ("Shell", InventoryOnly), [".zsh"] = ("Shell", InventoryOnly),
            [".ps1"] = ("PowerShell", InventoryOnly), [".psm1"] = ("PowerShell", InventoryOnly),
            [".lua"] = ("Lua", InventoryOnly), [".pl"] = ("Perl", InventoryOnly), [".pm"] = ("Perl", InventoryOnly),
            [".hs"] = ("Haskell", InventoryOnly), [".clj"] = ("Clojure", InventoryOnly),
            [".cljs"] = ("Clojure", InventoryOnly), [".cljc"] = ("Clojure", InventoryOnly),
            [".r"] = ("R", InventoryOnly), [".sql"] = ("SQL", InventoryOnly),
            [".html"] = ("HTML", InventoryOnly), [".htm"] = ("HTML", InventoryOnly),
            [".css"] = ("CSS", InventoryOnly), [".scss"] = ("CSS", InventoryOnly),
            [".sass"] = ("CSS", InventoryOnly), [".less"] = ("CSS", InventoryOnly)
        };

    public static IReadOnlyList<LanguageCoverageInfo> Analyze(IEnumerable<string> paths)
    {
        var counts = new Dictionary<string, (int Count, string AnalysisLevel)>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            if (!Languages.TryGetValue(Path.GetExtension(path), out var language))
                continue;
            var current = counts.TryGetValue(language.Language, out var existing)
                ? existing : (Count: 0, AnalysisLevel: language.AnalysisLevel);
            counts[language.Language] = (current.Count + 1, current.AnalysisLevel);
        }

        return counts
            .Select(item => new LanguageCoverageInfo(item.Key, item.Value.Count, item.Value.AnalysisLevel))
            .OrderBy(language => language.Language, StringComparer.Ordinal)
            .ToArray();
    }
}
