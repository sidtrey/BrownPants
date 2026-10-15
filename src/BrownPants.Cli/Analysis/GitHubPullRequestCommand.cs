using System.Net.Http;
using BrownPants.Cli.Output;

namespace BrownPants.Cli.Analysis;

public static class GitHubPullRequestCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error,
        Func<string?>? tokenProvider = null, Func<HttpMessageHandler>? handlerFactory = null)
    {
        if (args.Length == 0 || !string.Equals(args[0], "create", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: brownpants pr create <repository> --provider github --preview <path> --repository <owner/name> --source <branch> --target <branch> --confirm --online");
            return 2;
        }

        var repositoryRoot = Directory.GetCurrentDirectory();
        var rootProvided = false;
        string? provider = null;
        string? previewPath = null;
        string? destinationRepository = null;
        string? source = null;
        string? target = null;
        var online = false;
        var confirm = false;

        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument is "--provider" or "--preview" or "--repository" or "--source" or "--target")
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine($"Missing value for {argument}.");
                    return 2;
                }
                var value = args[index];
                switch (argument)
                {
                    case "--provider": provider = value.ToLowerInvariant(); break;
                    case "--preview": previewPath = value; break;
                    case "--repository": destinationRepository = value; break;
                    case "--source": source = value; break;
                    case "--target": target = value; break;
                }
            }
            else if (argument == "--online") online = true;
            else if (argument == "--confirm") confirm = true;
            else if (argument.StartsWith("-", StringComparison.Ordinal))
            {
                error.WriteLine($"Unknown pull request create option '{argument}'.");
                return 2;
            }
            else if (!rootProvided)
            {
                repositoryRoot = argument;
                rootProvided = true;
            }
            else
            {
                error.WriteLine("Only one repository root may be supplied.");
                return 2;
            }
        }

        if (provider != "github")
        {
            error.WriteLine("--provider github is required; other providers are not supported by this command.");
            return 2;
        }
        if (string.IsNullOrWhiteSpace(previewPath) || string.IsNullOrWhiteSpace(destinationRepository)
            || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            error.WriteLine("--preview, --repository, --source, and --target are required.");
            return 2;
        }
        if (!confirm || !online)
        {
            error.WriteLine("GitHub draft PR creation requires both --confirm and --online. No credentials were read and no request was sent.");
            return 2;
        }

        try
        {
            var root = Path.GetFullPath(repositoryRoot);
            var previewFile = ResolvePreviewPath(root, previewPath);
            RejectReparsePointAncestors(previewFile, root);
            var savedPreview = PullRequestPreviewRenderer.Read(File.ReadAllText(previewFile));
            if (!savedPreview.IntegrityValid)
                throw new GitHubPullRequestException("invalid-preview", "The saved PR preview was edited after it was generated.");
            if (!GitHubPullRequestClient.IsValidRepository(destinationRepository))
                throw new GitHubPullRequestException("invalid-repository", "GitHub repository must use owner/name format with valid path segments.");
            if (!string.Equals(savedPreview.Metadata.Repository, destinationRepository, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(savedPreview.Metadata.SourceBranch, source, StringComparison.Ordinal)
                || !string.Equals(savedPreview.Metadata.TargetBranch, target, StringComparison.Ordinal))
                throw new GitHubPullRequestException("preview-mismatch", "The explicit GitHub repository and branches must match the saved preview.");
            PullRequestPreviewAnalyzer.ValidateBranch(source, nameof(source));
            PullRequestPreviewAnalyzer.ValidateBranch(target, nameof(target));
            if (string.Equals(source, target, StringComparison.Ordinal))
                throw new GitHubPullRequestException("invalid-branches", "Source and target branches must be different.");

            var token = (tokenProvider ?? ReadToken)();
            if (string.IsNullOrWhiteSpace(token))
                throw new GitHubPullRequestException("missing-token", "Set BROWNPANTS_GITHUB_TOKEN through your approved credential process.");
            var handler = handlerFactory?.Invoke() ?? new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new GitHubPullRequestClient(token, handler);
            var result = client.CreateDraftAsync(savedPreview, destinationRepository, source, target).GetAwaiter().GetResult();
            output.WriteLine($"Created GitHub draft PR #{result.Number}: {result.Url}");
            return 0;
        }
        catch (GitHubPullRequestException exception)
        {
            error.WriteLine($"GitHub draft PR creation failed ({exception.Code}): {exception.Message}");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"GitHub draft PR creation failed: {exception.Message}");
            return 1;
        }
    }

    private static string? ReadToken() => Environment.GetEnvironmentVariable("BROWNPANTS_GITHUB_TOKEN");

    private static string ResolvePreviewPath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Preview path must be repository-relative.");
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, fullPath);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new ArgumentException("Preview path must remain inside the repository.");
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Saved PR preview does not exist inside the repository.");
        return fullPath;
    }

    private static void RejectReparsePointAncestors(string path, string root)
    {
        var current = path;
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        while (!string.IsNullOrEmpty(current))
        {
            if (IsReparsePoint(current))
                throw new ArgumentException("Preview path cannot pass through a symbolic link or reparse point.");
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
