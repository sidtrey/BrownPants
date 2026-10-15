using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public sealed record PullRequestPreviewMetadata(
    int SchemaVersion,
    string FeaturePath,
    string Title,
    string Description,
    string Repository,
    string SourceBranch,
    string TargetBranch,
    IReadOnlyList<string> WorkItemIds,
    string ContentDigest);

public sealed record SavedPullRequestPreview(PullRequestPreviewMetadata Metadata, string Body, bool IntegrityValid);

public static class PullRequestPreviewRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Regex MetadataPattern = new("^<!-- brownpants-pr-preview-v1:(?<metadata>[A-Za-z0-9+/=]+) -->\\r?\\n", RegexOptions.CultureInvariant);

    public static string Render(PullRequestPreview preview)
    {
        var body = RenderBody(preview);
        var metadata = new PullRequestPreviewMetadata(1, preview.FeaturePath, preview.Title, preview.Description,
            preview.Repository, preview.SourceBranch, preview.TargetBranch, preview.WorkItemIds, "");
        var digest = ComputeDigest(metadata, body);
        metadata = metadata with { ContentDigest = digest };
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata, JsonOptions)));
        return $"<!-- brownpants-pr-preview-v1:{payload} -->\n{body}";
    }

    public static SavedPullRequestPreview Read(string content)
    {
        var match = MetadataPattern.Match(content);
        if (!match.Success) throw new InvalidDataException("File is not a BrownPants PR preview version 1.");
        PullRequestPreviewMetadata? metadata;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups["metadata"].Value));
            metadata = JsonSerializer.Deserialize<PullRequestPreviewMetadata>(json, JsonOptions);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new InvalidDataException("BrownPants PR preview metadata is invalid.", exception);
        }
        if (metadata is null || metadata.SchemaVersion != 1) throw new InvalidDataException("BrownPants PR preview metadata version is unsupported.");
        if (string.IsNullOrWhiteSpace(metadata.FeaturePath) || string.IsNullOrWhiteSpace(metadata.Title)
            || string.IsNullOrWhiteSpace(metadata.Description) || string.IsNullOrWhiteSpace(metadata.Repository)
            || string.IsNullOrWhiteSpace(metadata.SourceBranch) || string.IsNullOrWhiteSpace(metadata.TargetBranch)
            || metadata.WorkItemIds is null || string.IsNullOrWhiteSpace(metadata.ContentDigest) || metadata.ContentDigest.Length != 64
            || !metadata.ContentDigest.All(Uri.IsHexDigit))
            throw new InvalidDataException("BrownPants PR preview metadata is incomplete or malformed.");
        var body = content[match.Length..];
        var expected = ComputeDigest(metadata with { ContentDigest = "" }, body);
        return new SavedPullRequestPreview(metadata, body, CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(metadata.ContentDigest.ToLowerInvariant())));
    }

    private static string RenderBody(PullRequestPreview preview)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Draft PR Preview: {Escape(preview.Title)}");
        builder.AppendLine();
        builder.AppendLine($"**Review state:** {(preview.IsComplete ? "Complete" : "Incomplete; do not describe as verified")}");
        builder.AppendLine($"**Feature:** `{Escape(preview.FeaturePath)}`  ");
        builder.AppendLine($"**Repository:** `{Escape(preview.Repository)}`  ");
        builder.AppendLine($"**Branches:** `{Escape(preview.SourceBranch)}` → `{Escape(preview.TargetBranch)}`  ");
        builder.AppendLine($"**Acceptance digest:** `{Escape(preview.CriteriaDigest)}` ({(preview.CriteriaBaselineValid ? "matches baseline" : "missing or changed")})");
        builder.AppendLine();
        builder.AppendLine("## Proposed PR description");
        builder.AppendLine();
        builder.AppendLine(Escape(preview.Description));
        builder.AppendLine();
        builder.AppendLine("## Task evidence");
        builder.AppendLine();
        builder.AppendLine("| Task | Status | Verification command | Evidence | Gaps |");
        builder.AppendLine("|---|---|---|---|---|");
        foreach (var task in preview.Tasks)
            builder.Append('|').Append(EscapeCell(task.TaskId)).Append('|').Append(EscapeCell(task.Status)).Append('|')
                .Append(EscapeCell(task.VerificationCommand)).Append('|').Append(EscapeCell(string.Join(", ", task.EvidencePaths)))
                .Append('|').Append(EscapeCell(string.Join("; ", task.Gaps))).AppendLine("|");
        builder.AppendLine();
        builder.AppendLine("## QA knowledge transfer");
        builder.AppendLine();
        foreach (var criterion in preview.QaHandoff)
        {
            builder.AppendLine($"### {Escape(criterion.CriterionId)}");
            builder.AppendLine();
            builder.AppendLine(Escape(criterion.Text));
            builder.AppendLine();
            if (criterion.VerificationCommands.Count > 0)
            {
                builder.AppendLine("Documented verification commands:");
                foreach (var command in criterion.VerificationCommands) builder.AppendLine($"- `{EscapeCode(command)}`");
            }
            if (criterion.EvidencePaths.Count > 0)
            {
                builder.AppendLine("Existing evidence files:");
                foreach (var evidencePath in criterion.EvidencePaths) builder.AppendLine($"- `{EscapeCode(evidencePath)}`");
            }
            foreach (var gap in criterion.Gaps) builder.AppendLine($"- **Guidance gap:** {Escape(gap)}");
            builder.AppendLine();
        }
        builder.AppendLine("## Azure DevOps delivery links");
        builder.AppendLine();
        if (preview.DeliveryReferences.Count == 0) builder.AppendLine("No delivery links were available in the selected traceability report.");
        foreach (var item in preview.DeliveryReferences.OrderBy(value => value.ArtifactType, StringComparer.Ordinal).ThenBy(value => value.Identifier, StringComparer.Ordinal))
        {
            var url = item.TargetUrl ?? item.EvidenceSourceUrl;
            builder.Append("- ").Append(Escape(item.ArtifactType)).Append(' ').Append(Escape(item.Identifier));
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                builder.Append(": [").Append(Escape(item.Title ?? item.Identifier)).Append("](").Append(uri.AbsoluteUri.Replace(")", "%29", StringComparison.Ordinal)).Append(')');
            else builder.Append(": link unavailable; source evidence: ").Append(Escape(item.EvidenceSourceUrl));
            builder.AppendLine();
        }
        builder.AppendLine();
        builder.AppendLine("## Gaps and limits");
        builder.AppendLine();
        if (preview.Gaps.Count == 0) builder.AppendLine("No known evidence gaps.");
        else foreach (var gap in preview.Gaps) builder.Append("- ").AppendLine(Escape(gap));
        builder.AppendLine();
        builder.AppendLine("BrownPants does not invent QA procedures or assert delivery relationships it did not observe.");
        return builder.ToString();
    }

    private static string ComputeDigest(PullRequestPreviewMetadata metadata, string body)
    {
        var canonical = string.Join('\n', metadata.SchemaVersion, metadata.FeaturePath, metadata.Title, metadata.Description,
            metadata.Repository, metadata.SourceBranch, metadata.TargetBranch, string.Join(',', metadata.WorkItemIds), body);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string EscapeCell(string? value) => Escape(value).Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    private static string EscapeCode(string value) => value.Replace("`", "", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    private static string Escape(string? value) => (value ?? "Unknown").Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
}
