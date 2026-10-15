namespace BrownPants.Cli.Analysis;

public static class ModelRoutingCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h") return WriteHelp(output);
        var operation = args[0].ToLowerInvariant();
        if (operation is not ("validate" or "report" or "export"))
            return Invalid(error, "Expected 'validate', 'report', or 'export'.");

        string? repository = null;
        string? policyPath = null;
        string? host = null;
        var format = "markdown";
        var formatSupplied = false;
        var policySupplied = false;
        var hostSupplied = false;

        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--policy":
                    if (policySupplied || ++index >= args.Length || args[index].StartsWith("-", StringComparison.Ordinal))
                        return Invalid(error, "--policy requires one repository-relative JSON path and may be supplied once.");
                    policyPath = args[index];
                    policySupplied = true;
                    break;
                case "--format":
                    if (formatSupplied || ++index >= args.Length || operation != "report")
                        return Invalid(error, "--format is supported only for report and requires markdown or json.");
                    format = args[index].ToLowerInvariant();
                    if (format is not ("markdown" or "json")) return Invalid(error, "Unsupported --format. Expected markdown or json.");
                    formatSupplied = true;
                    break;
                case "--host":
                    if (hostSupplied || ++index >= args.Length || operation != "export")
                        return Invalid(error, "--host is required once for export and is supported only for that operation.");
                    host = args[index].ToLowerInvariant();
                    hostSupplied = true;
                    break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal))
                        return Invalid(error, $"Unknown models option '{args[index]}'.");
                    if (repository is not null) return Invalid(error, "Provide only one repository path.");
                    repository = args[index];
                    break;
            }
        }

        if (operation == "export" && !hostSupplied)
            return Invalid(error, "Export requires an explicit --host (vscode, visual-studio, claude, or codex). No files were written.");
        if (operation != "export" && hostSupplied)
            return Invalid(error, "--host is supported only for export.");

        var root = Path.GetFullPath(repository ?? Directory.GetCurrentDirectory());
        var selectedPolicy = policyPath ?? ModelRoutingPolicyReader.DefaultPolicyPath;
        try
        {
            ModelRoutingPolicy policy;
            try
            {
                policy = ModelRoutingPolicyReader.Load(root, policyPath);
            }
            catch (FileNotFoundException) when (operation is "validate" or "report")
            {
                var relative = selectedPolicy.Replace('\\', '/');
                policy = new ModelRoutingPolicy(relative, Array.Empty<ModelRoutingProfile>());
            }

            switch (operation)
            {
                case "validate":
                    if (policy.Profiles.Count == 0)
                    {
                        output.WriteLine($"No model assignments are configured ({policy.RelativePath} is absent).");
                        return 0;
                    }
                    output.WriteLine($"Model policy is valid: {policy.RelativePath} ({policy.Profiles.Count} profiles).");
                    return 0;
                case "report":
                    output.Write(format == "json"
                        ? ModelRoutingReportRenderer.RenderJson(policy)
                        : ModelRoutingReportRenderer.RenderMarkdown(policy));
                    return 0;
                case "export":
                    var result = ModelRoutingProfileExporter.Export(root, policy, host!);
                    output.WriteLine($"Model profiles exported for '{result.Host}': {result.Created} created, {result.Unchanged} unchanged.");
                    foreach (var path in result.Paths) output.WriteLine($"- {path}");
                    return 0;
                default:
                    throw new InvalidOperationException("Unreachable models operation.");
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException
            or System.Security.SecurityException)
        {
            error.WriteLine($"Unable to {operation} model policy: {exception.Message}");
            return 1;
        }
    }

    public static int WriteHelp(TextWriter output)
    {
        output.WriteLine("Usage: brownpants models <validate|report|export> [repository] [--policy <relative-json-path>] [options]");
        output.WriteLine("  validate  Validate the local model policy; read-only and no provider calls.");
        output.WriteLine("  report    Show policy and host support as Markdown or JSON.");
        output.WriteLine("  export    Write native project agent profiles for one explicitly selected host.");
        output.WriteLine("  --policy  Policy path relative to the repository (default: .brownpants/models.json).");
        output.WriteLine("  --format  report only: markdown (default) or json.");
        output.WriteLine("  --host    export only: vscode, visual-studio, claude, or codex; required to prevent guessing.");
        output.WriteLine("  Host files are project-local, idempotent, and conflict-preserving; hosts control invocation and access.");
        return 0;
    }

    private static int Invalid(TextWriter error, string message)
    {
        error.WriteLine(message);
        return 2;
    }
}
