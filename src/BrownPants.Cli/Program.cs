using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Cli;

public static class Program
{
    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            output.WriteLine("BrownPants - deterministic brownfield evidence for spec-driven development");
            output.WriteLine("Usage: brownpants inspect [path] [--format markdown|json] [--limit 1..10000] [--semantic]");
            return args.Length == 0 ? 2 : 0;
        }

        if (!string.Equals(args[0], "inspect", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine($"Unknown command '{args[0]}'. Use --help for usage.");
            return 2;
        }

        var path = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sampleLimit = 200;
        var analyzeSemantics = false;
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index] == "--format")
            {
                if (++index >= args.Length)
                {
                    error.WriteLine("Missing value for --format. Expected markdown or json.");
                    return 2;
                }
                format = args[index].ToLowerInvariant();
            }
            else if (args[index] == "--limit")
            {
                if (++index >= args.Length || !int.TryParse(args[index], out sampleLimit) || sampleLimit is < 1 or > 10_000)
                {
                    error.WriteLine("Invalid --limit value. Expected an integer from 1 to 10000.");
                    return 2;
                }
            }
            else if (args[index] == "--semantic")
            {
                analyzeSemantics = true;
            }
            else if (args[index].StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown option '{args[index]}'.");
                return 2;
            }
            else if (!pathProvided)
            {
                path = args[index];
                pathProvided = true;
            }
            else
            {
                error.WriteLine("Only one repository path may be supplied.");
                return 2;
            }
        }

        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }

        try
        {
            var inventory = RepositoryScanner.Scan(path, sampleLimit, analyzeSemantics);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true })
                : MarkdownReport.Render(inventory));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to inspect repository: {exception.Message}");
            return 1;
        }
    }
}




