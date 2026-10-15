using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BrownPants.Cli.Output;

namespace BrownPants.Cli.Analysis;

public static class MultiProviderPullRequestCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error,
        Func<string?>? tokenProvider = null, Func<HttpMessageHandler>? handlerFactory = null)
    {
        if (args.Length == 0 || !args[0].Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: brownpants pr create <repository> --provider gitlab|bitbucket --preview <path> --repository <project> --source <branch> --target <branch> --confirm --online");
            return 2;
        }
        string? provider = null, previewPath = null, repository = null, source = null, target = null;
        var root = Directory.GetCurrentDirectory();
        var rootSet = false; var online = false; var confirm = false;
        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--provider" or "--preview" or "--repository" or "--source" or "--target")
            {
                if (++i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal)) { error.WriteLine($"Missing value for {arg}."); return 2; }
                switch (arg) { case "--provider": provider = args[i].ToLowerInvariant(); break; case "--preview": previewPath = args[i]; break; case "--repository": repository = args[i]; break; case "--source": source = args[i]; break; case "--target": target = args[i]; break; }
            }
            else if (arg == "--online") online = true;
            else if (arg == "--confirm") confirm = true;
            else if (arg.StartsWith("-", StringComparison.Ordinal)) { error.WriteLine($"Unknown pull request create option '{arg}'."); return 2; }
            else if (!rootSet) { root = arg; rootSet = true; }
            else { error.WriteLine("Only one repository root may be supplied."); return 2; }
        }
        if (provider is not ("gitlab" or "bitbucket")) { error.WriteLine("--provider gitlab or --provider bitbucket is required."); return 2; }
        if (string.IsNullOrWhiteSpace(previewPath) || string.IsNullOrWhiteSpace(repository) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        { error.WriteLine("--preview, --repository, --source, and --target are required."); return 2; }
        if (!confirm || !online) { error.WriteLine($"{ProviderName(provider)} draft creation requires both --confirm and --online. No credentials were read and no request was sent."); return 2; }
        try
        {
            root = Path.GetFullPath(root);
            if (Path.IsPathRooted(previewPath)) throw new ArgumentException("Preview path must be repository-relative.");
            var file = Path.GetFullPath(Path.Combine(root, previewPath));
            var relative = Path.GetRelativePath(root, file);
            if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative)) throw new ArgumentException("Preview path must remain inside the repository.");
            if (!File.Exists(file)) throw new FileNotFoundException("Saved PR preview does not exist inside the repository.");
            for (var current = file; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current)!)
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Preview path cannot pass through a symbolic link or reparse point.");
                if (string.Equals(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) break;
            }
            var preview = PullRequestPreviewRenderer.Read(File.ReadAllText(file));
            if (!preview.IntegrityValid) throw new InvalidDataException("The saved PR preview was edited after it was generated.");
            if (!string.Equals(preview.Metadata.Repository, repository, StringComparison.OrdinalIgnoreCase) || preview.Metadata.SourceBranch != source || preview.Metadata.TargetBranch != target)
                throw new InvalidDataException("The explicit provider repository and branches must match the saved preview.");
            PullRequestPreviewAnalyzer.ValidateBranch(source, nameof(source)); PullRequestPreviewAnalyzer.ValidateBranch(target, nameof(target));
            if (source == target) throw new ArgumentException("Source and target branches must be different.");
            if (provider == "gitlab" && !ValidGitLabProject(repository)) throw new ArgumentException("GitLab project must use group/project path segments.");
            if (provider == "bitbucket" && !ValidBitbucketRepository(repository)) throw new ArgumentException("Bitbucket repository must use workspace/repository-slug format.");
            var token = (tokenProvider ?? (() => Environment.GetEnvironmentVariable(provider == "gitlab" ? "BROWNPANTS_GITLAB_TOKEN" : "BROWNPANTS_BITBUCKET_TOKEN")))();
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException($"Set { (provider == "gitlab" ? "BROWNPANTS_GITLAB_TOKEN" : "BROWNPANTS_BITBUCKET_TOKEN") } through your approved credential process.");
            using var client = new HttpClient(handlerFactory?.Invoke() ?? new HttpClientHandler { AllowAutoRedirect = false }, true)
            { BaseAddress = new Uri(provider == "gitlab" ? "https://gitlab.com/api/v4/" : "https://api.bitbucket.org/2.0/"), Timeout = TimeSpan.FromSeconds(30) };
            if (provider == "gitlab") client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
            else client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var body = new StringContent(provider == "gitlab"
                ? JsonSerializer.Serialize(new { title = "Draft: " + preview.Metadata.Title, description = preview.Body, source_branch = source, target_branch = target })
                : JsonSerializer.Serialize(new { title = preview.Metadata.Title, description = preview.Body, source = new { branch = new { name = source } }, destination = new { branch = new { name = target } }, draft = true }), Encoding.UTF8, "application/json");
            var path = provider == "gitlab"
                ? $"projects/{Uri.EscapeDataString(repository)}/merge_requests"
                : $"repositories/{Uri.EscapeDataString(repository.Split('/')[0])}/{Uri.EscapeDataString(repository.Split('/')[1])}/pullrequests";
            using var response = client.PostAsync(path, body).GetAwaiter().GetResult();
            if ((int)response.StatusCode is >= 300 and < 400) throw new InvalidOperationException("Provider redirected the draft create request; inspect the provider before retrying.");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new InvalidOperationException("Provider denied draft creation; check the repository permission.");
            if (response.StatusCode != HttpStatusCode.Created) throw new InvalidOperationException($"Provider returned HTTP {(int)response.StatusCode}; inspect before retrying.");
            using var json = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var node = json.RootElement;
            string? url = null; var draft = false;
            if (provider == "gitlab")
            {
                draft = node.TryGetProperty("draft", out var d) && d.ValueKind == JsonValueKind.True
                    && node.TryGetProperty("state", out var state) && state.GetString() == "opened";
                url = node.TryGetProperty("web_url", out var u) ? u.GetString() : null;
            }
            else
            {
                draft = node.TryGetProperty("draft", out var d) && d.ValueKind == JsonValueKind.True
                    && node.TryGetProperty("state", out var state) && state.GetString() == "OPEN";
                if (node.TryGetProperty("links", out var links) && links.TryGetProperty("html", out var html) && html.TryGetProperty("href", out var href)) url = href.GetString();
            }
            if (!draft || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                !string.Equals(uri.IdnHost, provider == "gitlab" ? "gitlab.com" : "bitbucket.org", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidDataException("Provider did not confirm an open draft with a valid provider URL; inspect before retrying.");
            output.WriteLine($"Created {ProviderName(provider)} draft: {uri.AbsoluteUri}"); return 0;
        }
        catch (HttpRequestException)
        { error.WriteLine("Provider could not confirm draft creation; inspect the provider before retrying."); return 1; }
        catch (TaskCanceledException)
        { error.WriteLine("Provider draft creation timed out; inspect the provider before retrying."); return 1; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException or InvalidOperationException or JsonException)
        { error.WriteLine($"Draft creation failed: {ex.Message}"); return 1; }
    }

    private static string ProviderName(string provider) => provider == "gitlab" ? "GitLab" : "Bitbucket";
    private static bool ValidGitLabProject(string value) => value.Split('/').Length >= 2 && value.Split('/').All(SafeSegment);
    private static bool ValidBitbucketRepository(string value) => value.Split('/') is [var workspace, var repo] && SafeSegment(workspace) && SafeSegment(repo);
    private static bool SafeSegment(string value) => value is not ("." or "..") && value.Length is > 0 and <= 100 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
