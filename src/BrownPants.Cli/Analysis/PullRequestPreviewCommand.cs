using BrownPants.Cli.Output;

namespace BrownPants.Cli.Analysis;

public static class PullRequestPreviewCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var repositoryRoot = Directory.GetCurrentDirectory();
        var repositoryRootProvided = false;
        string? feature = null;
        string? destinationRepository = null;
        string? source = null;
        string? target = null;
        string? outputPath = null;
        var overwrite = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument is "--feature" or "--repository" or "--source" or "--target" or "--output")
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine($"Missing value for {argument}.");
                    return 2;
                }
                var value = args[index];
                switch (argument)
                {
                    case "--feature": feature = value; break;
                    case "--repository": destinationRepository = value; break;
                    case "--source": source = value; break;
                    case "--target": target = value; break;
                    case "--output": outputPath = value; break;
                }
            }
            else if (argument == "--overwrite") overwrite = true;
            else if (argument.StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown pull request preview option '{argument}'.");
                return 2;
            }
            else if (!repositoryRootProvided)
            {
                repositoryRoot = argument;
                repositoryRootProvided = true;
            }
            else
            {
                error.WriteLine("Only one repository root may be supplied.");
                return 2;
            }
        }

        if (string.IsNullOrWhiteSpace(feature) || string.IsNullOrWhiteSpace(destinationRepository)
            || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            error.WriteLine("--feature, --repository, --source, and --target are required.");
            return 2;
        }
        if (overwrite && outputPath is null)
        {
            error.WriteLine("--overwrite requires --output.");
            return 2;
        }

        try
        {
            if (string.Equals(source, target, StringComparison.Ordinal))
                throw new ArgumentException("Source and target branches must be different.");
            var root = Path.GetFullPath(repositoryRoot);
            var preview = PullRequestPreviewAnalyzer.Analyze(root, feature, source, target, destinationRepository);
            var rendered = PullRequestPreviewRenderer.Render(preview);
            if (outputPath is null) output.Write(rendered);
            else
            {
                var destination = ResolveOutputPath(root, outputPath);
                RejectReparsePointAncestors(destination, root);
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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"Unable to create PR preview: {exception.Message}");
            return 1;
        }
    }

    private static string ResolveOutputPath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Preview output must be a repository-relative path.");
        var destination = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, destination);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new ArgumentException("Preview output must remain inside the repository.");
        return destination;
    }

    private static void RejectReparsePointAncestors(string path, string root)
    {
        var current = path;
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        while (!string.IsNullOrEmpty(current))
        {
            if (IsReparsePoint(current))
                throw new ArgumentException("Preview output cannot pass through a symbolic link or reparse point.");
            if (string.Equals(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), normalizedRoot,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) break;
            current = Path.GetDirectoryName(current);
        }
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return true;
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        return new FileInfo(path).LinkTarget is not null || new DirectoryInfo(path).LinkTarget is not null;
    }
}
