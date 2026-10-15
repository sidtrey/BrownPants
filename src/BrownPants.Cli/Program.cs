using System.Text.Json;
using BrownPants.Cli.Analysis;
using BrownPants.Cli.Output;

namespace BrownPants.Cli;

public static class Program
{
    private const int MaximumInvestigationClues = 20;
    private const int MaximumClueLength = 1000;

    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            WriteGlobalHelp(output);
            return 2;
        }

        if (IsHelpToken(args[0]) || string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length == 1) { WriteGlobalHelp(output); return 0; }
            return WriteTopicHelp(args.Skip(1).ToArray(), output, error);
        }

        if (args.Skip(1).Any(IsHelpToken))
            return WriteTopicHelp(args.TakeWhile(argument => !IsHelpToken(argument)).ToArray(), output, error);

        if (string.Equals(args[0], "instructions", StringComparison.OrdinalIgnoreCase))
            return RunInstructions(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "frame", StringComparison.OrdinalIgnoreCase))
            return RunFrame(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "placement", StringComparison.OrdinalIgnoreCase))
            return RunPlacement(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "investigate", StringComparison.OrdinalIgnoreCase))
            return RunInvestigation(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "impact", StringComparison.OrdinalIgnoreCase))
            return RunImpact(args.Skip(1).ToArray(), output, error);

        if (string.Equals(args[0], "devops", StringComparison.OrdinalIgnoreCase))
            return RunDevOps(args.Skip(1).ToArray(), output, error);

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

    private static bool IsHelpToken(string argument) => argument is "--help" or "-h";

    private static int RunDevOps(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || IsHelpToken(args[0]))
        {
            output.WriteLine("Usage: brownpants devops <trace|pr> --help");
            return 0;
        }
        if (args[0].Equals("pr", StringComparison.OrdinalIgnoreCase))
            return RunDevOpsPr(args.Skip(1).ToArray(), output, error);
        if (!args[0].Equals("trace", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Expected 'devops trace' or 'devops pr preview'.");
            return 2;
        }

        string? repository = null;
        string? feature = null;
        string? configPath = null;
        var online = false;
        var allowCustomHost = false;
        var format = "markdown";
        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--feature":
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index])) { error.WriteLine("Missing value for --feature."); return 2; }
                    feature = args[index];
                    break;
                case "--config":
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index])) { error.WriteLine("Missing value for --config."); return 2; }
                    configPath = args[index];
                    break;
                case "--format":
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index])) { error.WriteLine("Missing value for --format."); return 2; }
                    format = args[index].ToLowerInvariant();
                    break;
                case "--online": online = true; break;
                case "--allow-custom-host": allowCustomHost = true; break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal)) { error.WriteLine($"Unknown devops option '{args[index]}'."); return 2; }
                    if (repository is not null) { error.WriteLine("Only one repository path may be supplied."); return 2; }
                    repository = args[index];
                    break;
            }
        }
        if (!online) { error.WriteLine("Azure DevOps access requires explicit --online opt-in."); return 2; }
        if (string.IsNullOrWhiteSpace(feature)) { error.WriteLine("A --feature relative path is required."); return 2; }
        if (format is not ("markdown" or "json")) { error.WriteLine("Unsupported format. Expected markdown or json."); return 2; }

        try
        {
            var root = Path.GetFullPath(repository ?? Directory.GetCurrentDirectory());
            var configuration = AzureDevOpsConfiguration.Load(root, configPath, allowCustomHost);
            var token = AzureDevOpsAuthentication.AcquireTokenAsync(configuration).GetAwaiter().GetResult();
            using var client = new AzureDevOpsClient(configuration, new HttpClientHandler { AllowAutoRedirect = false }, token);
            var report = AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(configuration, feature, client).GetAwaiter().GetResult();
            output.WriteLine(format == "json" ? AzureDevOpsReportRenderer.RenderJson(report) : AzureDevOpsReportRenderer.RenderMarkdown(report));
            return report.IsComplete ? 0 : 1;
        }
        catch (AzureDevOpsAuthenticationException exception)
        {
            error.WriteLine($"Azure DevOps authentication failed: {exception.Message}");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or AzureDevOpsApiException)
        {
            error.WriteLine($"Azure DevOps trace failed: {exception.Message}");
            return 1;
        }
    }

    private static int RunDevOpsPr(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length > 0 && args[0].Equals("create", StringComparison.OrdinalIgnoreCase))
            return RunDevOpsPrCreate(args.Skip(1).ToArray(), output, error);
        return RunDevOpsPreview(args, output, error);
    }

    private static int RunDevOpsPrCreate(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || IsHelpToken(args[0]))
        {
            output.WriteLine("Usage: brownpants devops pr create <repository> --preview <file> --online --confirm [--allow-custom-host] [--config <path>]");
            output.WriteLine("Creates exactly one draft PR and associates only work items listed in the integrity-checked preview.");
            return 0;
        }
        string? repository = null;
        string? previewPath = null;
        string? configPath = null;
        var online = false;
        var confirmed = false;
        var allowCustomHost = false;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--preview":
                case "--config":
                    var option = args[index];
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index])) { error.WriteLine($"Missing value for {option}."); return 2; }
                    if (option == "--preview") previewPath = args[index]; else configPath = args[index];
                    break;
                case "--online": online = true; break;
                case "--confirm": confirmed = true; break;
                case "--allow-custom-host": allowCustomHost = true; break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal)) { error.WriteLine($"Unknown devops option '{args[index]}'."); return 2; }
                    if (repository is not null) { error.WriteLine("Only one repository path may be supplied."); return 2; }
                    repository = args[index];
                    break;
            }
        }
        if (!online || !confirmed) { error.WriteLine("Draft PR creation requires explicit --online and --confirm opt-ins."); return 2; }
        if (string.IsNullOrWhiteSpace(previewPath)) { error.WriteLine("A saved --preview file is required."); return 2; }
        try
        {
            var root = Path.GetFullPath(repository ?? Directory.GetCurrentDirectory());
            var configuration = AzureDevOpsConfiguration.Load(root, configPath, allowCustomHost);
            var content = File.ReadAllText(Path.GetFullPath(Path.IsPathRooted(previewPath) ? previewPath : Path.Combine(root, previewPath)));
            var token = AzureDevOpsAuthentication.AcquireTokenAsync(configuration).GetAwaiter().GetResult();
            using var submitter = new AzureDevOpsPullRequestSubmitter(configuration, new HttpClientHandler { AllowAutoRedirect = false }, token);
            var result = submitter.CreateDraftAsync(content).GetAwaiter().GetResult();
            output.WriteLine($"Created draft PR {result.Id}: {result.Url}");
            output.WriteLine("The PR remains a draft; BrownPants does not approve, complete, or merge it.");
            return 0;
        }
        catch (AzureDevOpsAuthenticationException exception)
        {
            error.WriteLine($"Azure DevOps authentication failed: {exception.Message}");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or AzureDevOpsApiException)
        {
            error.WriteLine($"Unable to create draft PR: {exception.Message}");
            return 1;
        }
    }

    private static int RunDevOpsPreview(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: brownpants devops pr preview <repository> --feature <relative-feature-path> --source <branch> --target <branch> [--online] [--output <relative-path>] [--overwrite]");
            return 0;
        }
        if (!args[0].Equals("preview", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Only 'devops pr preview' is available in this build.");
            return 2;
        }
        string? repository = null;
        string? feature = null;
        string? source = null;
        string? target = null;
        string? configPath = null;
        string? outputPath = null;
        var online = false;
        var allowCustomHost = false;
        var overwrite = false;
        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--feature":
                case "--source":
                case "--target":
                case "--config":
                case "--output":
                    var option = args[index];
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index])) { error.WriteLine($"Missing value for {option}."); return 2; }
                    switch (option)
                    {
                        case "--feature": feature = args[index]; break;
                        case "--source": source = args[index]; break;
                        case "--target": target = args[index]; break;
                        case "--config": configPath = args[index]; break;
                        case "--output": outputPath = args[index]; break;
                    }
                    break;
                case "--online": online = true; break;
                case "--allow-custom-host": allowCustomHost = true; break;
                case "--overwrite": overwrite = true; break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal)) { error.WriteLine($"Unknown devops option '{args[index]}'."); return 2; }
                    if (repository is not null) { error.WriteLine("Only one repository path may be supplied."); return 2; }
                    repository = args[index];
                    break;
            }
        }
        if (string.IsNullOrWhiteSpace(feature) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        { error.WriteLine("--feature, --source, and --target are required."); return 2; }
        if (overwrite && outputPath is null) { error.WriteLine("--overwrite requires --output."); return 2; }

        try
        {
            var root = Path.GetFullPath(repository ?? Directory.GetCurrentDirectory());
            var configuration = AzureDevOpsConfiguration.Load(root, configPath, allowCustomHost);
            AzureDevOpsTraceReport? trace = null;
            if (online)
            {
                var token = AzureDevOpsAuthentication.AcquireTokenAsync(configuration).GetAwaiter().GetResult();
                using var client = new AzureDevOpsClient(configuration, new HttpClientHandler { AllowAutoRedirect = false }, token);
                trace = AzureDevOpsTraceabilityAnalyzer.AnalyzeAsync(configuration, feature, client).GetAwaiter().GetResult();
            }
            var preview = PullRequestPreviewAnalyzer.Analyze(root, feature, source, target, configuration.Repository, trace);
            var rendered = PullRequestPreviewRenderer.Render(preview);
            if (outputPath is null) output.Write(rendered);
            else
            {
                var destination = ResolveRepositoryOutput(root, outputPath);
                if (HasReparsePointAncestor(destination, root)) throw new ArgumentException("Preview output cannot pass through a symbolic link or reparse point.");
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if (overwrite) File.WriteAllText(destination, rendered);
                else
                {
                    using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    using var writer = new StreamWriter(stream);
                    writer.Write(rendered);
                }
                output.WriteLine($"Created PR preview: {Path.GetRelativePath(root, destination).Replace('\\', '/')}");
            }
            return preview.IsComplete ? 0 : 1;
        }
        catch (AzureDevOpsAuthenticationException exception)
        {
            error.WriteLine($"Azure DevOps authentication failed: {exception.Message}");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or AzureDevOpsApiException)
        {
            error.WriteLine($"Unable to create PR preview: {exception.Message}");
            return 1;
        }
    }

    private static string ResolveRepositoryOutput(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Preview output must be a repository-relative path.");
        var destination = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, destination);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new ArgumentException("Preview output must remain inside the repository.");
        return destination;
    }

    private static int WriteTopicHelp(string[] topicParts, TextWriter output, TextWriter error)
    {
        var topic = string.Join(' ', topicParts);
        var command = topicParts.FirstOrDefault() ?? string.Empty;
        if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
            topic = string.Join(' ', topicParts.Skip(1));
        command = topic.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var frameOperation = topic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(command)) { WriteGlobalHelp(output); return 0; }
        if (command.Equals("devops", StringComparison.OrdinalIgnoreCase))
        {
            if (frameOperation.Equals("trace", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine("Usage: brownpants devops trace <repository> --feature <relative-feature-path> --online [--allow-custom-host] [--config <path>] [--format markdown|json]");
                output.WriteLine("  --online             Required before credentials are read or HTTP requests are made.");
                output.WriteLine("  Configure authenticationScheme as entra-browser or use BROWNPANTS_AZURE_DEVOPS_TOKEN for token authentication.");
                return 0;
            }
            if (frameOperation.Equals("pr", StringComparison.OrdinalIgnoreCase))
            {
                var prOperation = topic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2).FirstOrDefault() ?? string.Empty;
                if (prOperation.Equals("create", StringComparison.OrdinalIgnoreCase))
                {
                    output.WriteLine("Usage: brownpants devops pr create <repository> --preview <file> --online --confirm [--allow-custom-host] [--config <path>]");
                    output.WriteLine("  --confirm            Required before creating one draft PR from the saved preview.");
                    output.WriteLine("  --online             Required before credentials are read or HTTP requests are made.");
                    return 0;
                }
                output.WriteLine("Usage: brownpants devops pr preview <repository> --feature <relative-feature-path> --source <branch> --target <branch> [--online] [--output <relative-path>] [--overwrite]");
                return 0;
            }
            output.WriteLine("Usage: brownpants devops <trace|pr> --help");
            return 0;
        }
        if (command.Equals("inspect", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: brownpants inspect [path] [--format markdown|json] [--limit 1..10000] [--semantic]");
            output.WriteLine("  path                 Repository to inspect (default: current directory).");
            output.WriteLine("  --format             Report as markdown (default) or json.");
            output.WriteLine("  --limit              Maximum sampled entries, from 1 to 10000 (default: 200).");
            output.WriteLine("  --semantic           Opt in to MSBuild project loading and semantic call-target resolution.");
            output.WriteLine("Without --semantic, invocation names are syntax-only and targets are not resolved.");
            output.WriteLine("Semantic project evaluation may execute imported MSBuild targets.");
            return 0;
        }
        if (command.Equals("investigate", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: brownpants investigate [path] --clue <text> [--clue <text> ...] [--format markdown|json] [--limit 1..10000]");
            output.WriteLine("  --clue               Literal identifier, error phrase, or path fragment; repeatable (up to 20).");
            output.WriteLine("  --format             Report as markdown (default) or json.");
            output.WriteLine("  --limit              Maximum sampled entries, from 1 to 10000 (default: 200).");
            return 0;
        }
        if (command.Equals("impact", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: brownpants impact <repository> --changed <relative-source-path> [--changed <relative-source-path> ...] [--format markdown|json] [--limit 1..10000]");
            output.WriteLine("  --changed            Repository-relative C# or Visual Basic source path; repeatable.");
            output.WriteLine("  --format             Report as markdown (default) or json.");
            output.WriteLine("  --limit              Maximum sampled entries, from 1 to 10000 (default: 200).");
            return 0;
        }
        if (command.Equals("instructions", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: brownpants instructions [path] [--source <relative-path>] [--section <anchor>] [--format markdown|json]");
            output.WriteLine("  --source             Include one repository-relative instruction file; repeatable.");
            output.WriteLine("  --section            Retrieve one Markdown section from the single selected --source.");
            output.WriteLine("  --format             Report as markdown (default) or json.");
            return 0;
        }
        if (command.Equals("placement", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: brownpants placement --rule <text> [--scope repository|feature|unknown] [--durability enduring|feature|unknown] [--purpose principle|outcome|agent-workflow|mixed|unknown] [--format markdown|json]");
            output.WriteLine("  --rule               Requirement or instruction to classify (required).");
            output.WriteLine("  --scope, --durability, --purpose  Optional context; each defaults to unknown.");
            output.WriteLine("  --format             Report as markdown (default) or json.");
            return 0;
        }
        if (command.Equals("frame", StringComparison.OrdinalIgnoreCase))
        {
            if (frameOperation.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine("Usage: brownpants frame create <repository> <draft.json> <output.md> [--overwrite]");
                output.WriteLine("  --overwrite          Replace an existing output frame.");
                return 0;
            }
            if (frameOperation.Equals("validate", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine("Usage: brownpants frame validate <repository> <frame.md>");
                output.WriteLine("Checks whether a saved context frame still matches its source files.");
                return 0;
            }
            if (string.IsNullOrEmpty(frameOperation))
            {
                output.WriteLine("Usage: brownpants frame <create|validate> --help");
                output.WriteLine("  create              Create a context frame from a draft.");
                output.WriteLine("  validate            Check a frame against its source files.");
                return 0;
            }
        }

        error.WriteLine($"Unknown help topic '{topic}'. Use 'brownpants --help' to list supported commands.");
        return 2;
    }

    private static void WriteGlobalHelp(TextWriter output)
    {
        output.WriteLine("BrownPants - deterministic brownfield evidence for spec-driven development");
        output.WriteLine("Usage: brownpants <command> [options]");
        output.WriteLine("Commands:");
        output.WriteLine("  inspect       Inventory a repository and its source evidence.");
        output.WriteLine("  investigate   Search repository evidence using clues from a bug report.");
        output.WriteLine("  impact        Trace semantic callers affected by changed source files.");
        output.WriteLine("  instructions  Discover instruction files or retrieve a selected section.");
        output.WriteLine("  frame         Create or validate a context frame.");
        output.WriteLine("  placement     Classify whether guidance belongs in a constitution or a spec.");
        output.WriteLine("  devops        Trace Azure DevOps work items and delivery references.");
        output.WriteLine("Usage: brownpants inspect [path] [--format markdown|json] [--limit 1..10000] [--semantic]");
        output.WriteLine("       brownpants investigate [path] --clue <text> [--clue <text> ...] [--format markdown|json] [--limit 1..10000]");
        output.WriteLine("       brownpants impact <repository> --changed <relative-source-path> [--changed <relative-source-path> ...] [--format markdown|json] [--limit 1..10000]");
        output.WriteLine("       brownpants instructions [path] [--source <relative-path>] [--section <anchor>] [--format markdown|json]");
        output.WriteLine("       brownpants frame create <repository> <draft.json> <output.md> [--overwrite]");
        output.WriteLine("       brownpants frame validate <repository> <frame.md>");
        output.WriteLine("       brownpants placement --rule <text> [--scope repository|feature|unknown] [--durability enduring|feature|unknown] [--purpose principle|outcome|agent-workflow|mixed|unknown] [--format markdown|json]");
        output.WriteLine("       brownpants devops trace <repository> --feature <relative-feature-path> --online [--format markdown|json]");
        output.WriteLine("Run 'brownpants <command> --help' for command options. Use -h as an alias.");
        output.WriteLine("Use 'brownpants inspect --help' to understand syntax-only inventory and semantic target resolution.");
    }

    private static int RunPlacement(string[] args, TextWriter output, TextWriter error)
    {
        string? rule = null;
        var scope = "unknown";
        var durability = "unknown";
        var purpose = "unknown";
        var format = "markdown";

        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (option is not ("--rule" or "--scope" or "--durability" or "--purpose" or "--format"))
            {
                error.WriteLine($"Unknown placement option '{option}'. Use --help for usage.");
                return 2;
            }
            if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
            {
                error.WriteLine($"Missing value for {option}.");
                return 2;
            }
            var value = args[index];
            switch (option)
            {
                case "--rule": rule = value; break;
                case "--scope": scope = value; break;
                case "--durability": durability = value; break;
                case "--purpose": purpose = value; break;
                case "--format": format = value.ToLowerInvariant(); break;
            }
        }

        if (string.IsNullOrWhiteSpace(rule))
        {
            error.WriteLine("A non-empty --rule value is required.");
            return 2;
        }
        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }

        try
        {
            var report = PlacementGuidanceAnalyzer.Analyze(rule, scope, durability, purpose);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : PlacementGuidanceRenderer.Render(report));
            return 0;
        }
        catch (ArgumentException exception)
        {
            error.WriteLine($"Invalid placement input: {exception.Message}");
            return 2;
        }
    }

    private static int RunFrame(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 3 && string.Equals(args[0], "validate", StringComparison.OrdinalIgnoreCase))
        {
            var validationRoot = Path.GetFullPath(args[1]);
            var validationPath = Path.GetFullPath(Path.Combine(validationRoot, args[2]));
            var relativePath = Path.GetRelativePath(validationRoot, validationPath);
            if (relativePath == ".." || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
            {
                error.WriteLine("Frame path must be inside the repository.");
                return 2;
            }
            if (HasReparsePointAncestor(validationPath, validationRoot))
            {
                error.WriteLine("Frame path cannot pass through a symbolic link or reparse point.");
                return 2;
            }
            try
            {
                var result = InstructionContextFrameAnalyzer.Validate(validationRoot, relativePath);
                output.WriteLine(result);
                return result.StartsWith("Frame is stale", StringComparison.Ordinal) ? 1 : 0;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                error.WriteLine($"Unable to validate frame: {exception.Message}");
                return 1;
            }
        }
        if (args.Length < 4 || !string.Equals(args[0], "create", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: brownpants frame create <repository> <draft.json> <output.md> [--overwrite]");
            return 2;
        }
        var repository = Path.GetFullPath(args[1]);
        var draft = Path.GetFullPath(args[2]);
        var destination = Path.GetFullPath(Path.Combine(repository, args[3]));
        var overwrite = args.Length == 5 && args[4] == "--overwrite";
        if (args.Length > 5 || args.Length == 5 && !overwrite)
        {
            error.WriteLine("The only supported option is --overwrite.");
            return 2;
        }
        var relative = Path.GetRelativePath(repository, destination);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            error.WriteLine("Frame output must be inside the repository.");
            return 2;
        }
        if (HasReparsePointAncestor(destination, repository))
        {
            error.WriteLine("Frame output cannot pass through a symbolic link or reparse point.");
            return 2;
        }
        try
        {
            var frame = InstructionContextFrameAnalyzer.RenderDraft(repository, draft);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (overwrite) File.WriteAllText(destination, frame);
            else
            {
                using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                writer.Write(frame);
            }
            output.WriteLine($"Created frame: {relative.Replace('\\', '/')}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException)
        {
            error.WriteLine($"Unable to create frame: {exception.Message}");
            return 1;
        }
    }

    private static bool HasReparsePointAncestor(string fullPath, string root)
    {
        var current = fullPath;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        while (!string.Equals(current, root, comparison))
        {
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
            current = Path.GetDirectoryName(current) ?? root;
        }
        return false;
    }

    private static int RunInstructions(string[] args, TextWriter output, TextWriter error)
    {
        var path = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sources = new List<string>();
        string? sectionAnchor = null;
        for (var index = 0; index < args.Length; index++)
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
            else if (args[index] == "--source")
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine("Missing value for --source. Provide a repository-relative file path.");
                    return 2;
                }
                sources.Add(args[index]);
            }
            else if (args[index] == "--section")
            {
                if (sectionAnchor is not null || ++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(args[index]))
                {
                    error.WriteLine("Missing or repeated value for --section. Provide one section anchor.");
                    return 2;
                }
                sectionAnchor = args[index];
            }
            else if (args[index].StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown or incomplete option '{args[index]}'.");
                return 2;
            }
            else if (!pathProvided) { path = args[index]; pathProvided = true; }
            else { error.WriteLine("Only one repository path may be supplied."); return 2; }
        }
        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }
        if (sectionAnchor is not null && sources.Count != 1)
        {
            error.WriteLine("A section request requires exactly one --source <relative-path>.");
            return 2;
        }
        try
        {
            if (sectionAnchor is not null)
            {
                var section = InstructionSourceAnalyzer.ReadSection(path, sources[0], sectionAnchor);
                output.WriteLine(format == "json"
                    ? JsonSerializer.Serialize(section, new JsonSerializerOptions { WriteIndented = true })
                    : InstructionSourceReportRenderer.RenderSection(section));
                return 0;
            }

            var report = InstructionSourceAnalyzer.Analyze(path, sources);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : InstructionSourceReportRenderer.Render(report));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to retrieve instruction source or section: {exception.Message}");
            return 1;
        }
    }

    private static int RunInvestigation(string[] args, TextWriter output, TextWriter error)
    {
        var path = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sampleLimit = 200;
        var clues = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--clue")
            {
                if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index])
                    || args[index] is "--clue" or "--format" or "--limit")
                {
                    error.WriteLine("Missing or empty value for --clue. Provide a literal identifier, error phrase, or path fragment.");
                    return 2;
                }
                if (args[index].Length > MaximumClueLength)
                {
                    error.WriteLine($"Clues must be {MaximumClueLength} characters or fewer.");
                    return 2;
                }
                clues.Add(args[index]);
                if (clues.Count > MaximumInvestigationClues)
                {
                    error.WriteLine($"Provide no more than {MaximumInvestigationClues} --clue values.");
                    return 2;
                }
            }
            else if (args[index] == "--format")
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

        if (clues.Count == 0)
        {
            error.WriteLine("At least one --clue value is required. Use an identifier, error phrase, or path fragment from the bug report.");
            return 2;
        }

        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }

        try
        {
            var report = RepositoryInvestigator.Investigate(path, clues, sampleLimit);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : InvestigationReportRenderer.Render(report));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to investigate repository: {exception.Message}");
            return 1;
        }
    }

    private static int RunImpact(string[] args, TextWriter output, TextWriter error)
    {
        var root = Directory.GetCurrentDirectory();
        var pathProvided = false;
        var format = "markdown";
        var sampleLimit = 200;
        var changedPaths = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--changed")
            {
                if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]) || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine("Missing value for --changed. Provide a repository-relative C# or Visual Basic source path.");
                    return 2;
                }
                changedPaths.Add(args[index]);
            }
            else if (args[index] == "--format")
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
            else if (args[index].StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown option '{args[index]}'.");
                return 2;
            }
            else if (!pathProvided)
            {
                root = args[index];
                pathProvided = true;
            }
            else
            {
                error.WriteLine("Only one repository path may be supplied.");
                return 2;
            }
        }
        if (changedPaths.Count == 0)
        {
            error.WriteLine("At least one --changed path is required.");
            return 2;
        }
        if (format is not ("markdown" or "json"))
        {
            error.WriteLine($"Unsupported format '{format}'. Expected markdown or json.");
            return 2;
        }
        try
        {
            var inventory = RepositoryScanner.Scan(root, sampleLimit, analyzeSemantics: true);
            var report = CallImpactAnalyzer.Analyze(inventory.Root, changedPaths, RepositoryScanner.DiscoverFiles(inventory.Root),
                inventory.CSharp.Semantic!, sampleLimit);
            output.WriteLine(format == "json"
                ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                : CallImpactReportRenderer.Render(report));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to analyze call impact: {exception.Message}");
            return 1;
        }
    }
}




