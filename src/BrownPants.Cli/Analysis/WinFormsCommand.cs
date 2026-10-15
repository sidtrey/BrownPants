using BrownPants.Cli.Output;

namespace BrownPants.Cli.Analysis;

public static class WinFormsCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length > 0 && args[0] is "--help" or "-h")
        {
            output.WriteLine("Usage: brownpants winforms analyze <repository> [--format markdown|json] [--limit 1..10000]");
            output.WriteLine("Reports syntax-based WinForms property-write candidates; no project evaluation or source execution.");
            return 0;
        }
        if (args.Length < 2 || !args[0].Equals("analyze", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: brownpants winforms analyze <repository> [--format markdown|json] [--limit 1..10000]");
            return 2;
        }
        var path = args[1]; var format = "markdown"; var limit = 200;
        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] == "--format" && i + 1 < args.Length) format = args[++i].ToLowerInvariant();
            else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[++i], out var parsed) && parsed is >= 1 and <= 10_000) limit = parsed;
            else { error.WriteLine($"Invalid or unsupported option '{args[i]}'."); return 2; }
        }
        if (format is not ("markdown" or "json")) { error.WriteLine("Expected --format markdown or json."); return 2; }
        try
        {
            var report = WinFormsUiThreadingAnalyzer.Analyze(path, limit);
            output.WriteLine(WinFormsUiThreadingReportRenderer.Render(report, format));
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or DirectoryNotFoundException)
        {
            error.WriteLine($"Unable to analyze repository: {ex.Message}"); return 1;
        }
    }
}
