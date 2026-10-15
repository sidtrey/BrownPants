using System.Text.Json;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli;

internal static class EvidenceCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length > 0 && (args[0] is "--help" or "-h"))
        {
            WriteHelp(output);
            return 0;
        }

        var repository = Directory.GetCurrentDirectory();
        var repositoryProvided = false;
        var format = "json";
        var limit = 200;
        var semantic = false;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--format":
                    if (++index >= args.Length) { error.WriteLine("Missing value for --format. Expected json."); return 2; }
                    format = args[index].ToLowerInvariant();
                    break;
                case "--limit":
                    if (++index >= args.Length || !int.TryParse(args[index], out limit) || limit is < 1 or > 10_000)
                    {
                        error.WriteLine("Invalid --limit value. Expected an integer from 1 to 10000.");
                        return 2;
                    }
                    break;
                case "--semantic":
                    semantic = true;
                    break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal))
                    {
                        error.WriteLine($"Unknown evidence option '{args[index]}'.");
                        return 2;
                    }
                    if (repositoryProvided)
                    {
                        error.WriteLine("Only one repository path may be supplied.");
                        return 2;
                    }
                    repository = args[index];
                    repositoryProvided = true;
                    break;
            }
        }

        if (format != "json")
        {
            error.WriteLine("Unsupported evidence format. Expected json.");
            return 2;
        }

        try
        {
            var inventory = RepositoryScanner.Scan(repository, limit, semantic);
            var report = EvidenceReportAnalyzer.Create(inventory, semantic, limit);
            output.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to produce evidence report: {exception.Message}");
            return 1;
        }
    }

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine("Usage: brownpants evidence [repository] [--format json] [--limit 1..10000] [--semantic]");
        output.WriteLine("Produces a structured, source-located evidence handoff for any SDD engine.");
        output.WriteLine("Architecture, framework, and test-path findings are candidates; only Roslyn-resolved calls are resolved.");
        output.WriteLine("--semantic loads MSBuild projects; imported build targets may execute.");
    }
}
