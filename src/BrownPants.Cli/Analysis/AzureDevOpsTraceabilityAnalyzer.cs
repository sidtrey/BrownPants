namespace BrownPants.Cli.Analysis;

public sealed record AzureDevOpsTraceWorkItem(
    string Id,
    string? Title,
    string? Type,
    string? State,
    string? IterationPath,
    string SourceUrl,
    string Status);

public sealed record AzureDevOpsDeliveryReference(
    string ArtifactType,
    string Identifier,
    string? Title,
    string? Status,
    string? TargetUrl,
    string EvidenceSourceUrl,
    string EvidenceKind);

public sealed record AzureDevOpsTraceReport(
    int SchemaVersion,
    string FeaturePath,
    IReadOnlyList<AzureDevOpsTraceWorkItem> WorkItems,
    IReadOnlyList<AzureDevOpsDeliveryReference> DeliveryReferences,
    IReadOnlyList<string> Diagnostics,
    bool IsComplete);

public static class AzureDevOpsTraceabilityAnalyzer
{
    public static async Task<AzureDevOpsTraceReport> AnalyzeAsync(
        AzureDevOpsConfiguration configuration,
        string featurePath,
        AzureDevOpsClient client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(client);
        var normalizedFeature = NormalizeFeaturePath(featurePath);
        var items = new List<AzureDevOpsTraceWorkItem>();
        var references = new List<AzureDevOpsDeliveryReference>();
        var diagnostics = new List<string>();
        if (!configuration.Features.TryGetValue(normalizedFeature, out var mappedIds) || mappedIds.Count == 0)
        {
            diagnostics.Add($"No Azure DevOps work items are mapped to feature '{normalizedFeature}'.");
            return new AzureDevOpsTraceReport(1, normalizedFeature, items, references, diagnostics, false);
        }

        foreach (var id in mappedIds.OrderBy(value => int.TryParse(value, out var number) ? number : int.MaxValue)
                     .ThenBy(value => value, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AzureDevOpsWorkItem item;
            try
            {
                item = await client.GetWorkItemAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (AzureDevOpsApiException exception)
            {
                items.Add(new AzureDevOpsTraceWorkItem(id, null, null, null, null, WorkItemUrl(configuration, id), "Unavailable"));
                diagnostics.Add($"Work item {id}: {exception.Message}");
                continue;
            }

            var missingFields = new List<string>();
            if (item.Title == "Unknown title") missingFields.Add("title");
            if (item.Type == "Unknown type") missingFields.Add("type");
            if (item.State == "Unknown state") missingFields.Add("state");
            if (item.IterationPath == "Unknown iteration") missingFields.Add("iteration path");
            if (missingFields.Count > 0)
                diagnostics.Add($"Work item {id} did not return: {string.Join(", ", missingFields)}.");

            items.Add(new AzureDevOpsTraceWorkItem(item.Id, item.Title, item.Type, item.State, item.IterationPath, item.SourceUrl, "Observed"));
            foreach (var relation in item.Relations)
            {
                var artifactType = Classify(relation);
                if (artifactType is null)
                {
                    if (relation.RelationType.Contains("Artifact", StringComparison.OrdinalIgnoreCase))
                        diagnostics.Add($"Work item {id} has an unsupported artifact relation; its target is unknown.");
                    continue;
                }
                var target = ResolveTargetUrl(relation, artifactType, configuration, out var evidenceKind);
                var identifier = ExtractIdentifier(relation.Url, artifactType) ?? relation.Name ?? "Unknown identifier";
                references.Add(new AzureDevOpsDeliveryReference(artifactType, identifier, relation.Name, null, target,
                    item.SourceUrl, evidenceKind));
                if (target is null)
                    diagnostics.Add($"Work item {id} returned a {artifactType} relation without a validated clickable URL.");
            }
        }

        var sortedReferences = references
            .DistinctBy(reference => (reference.ArtifactType, reference.Identifier, reference.EvidenceSourceUrl))
            .OrderBy(reference => reference.ArtifactType, StringComparer.Ordinal)
            .ThenBy(reference => reference.Identifier, StringComparer.Ordinal)
            .ThenBy(reference => reference.EvidenceSourceUrl, StringComparer.Ordinal)
            .ToArray();
        return new AzureDevOpsTraceReport(1, normalizedFeature, items, sortedReferences,
            diagnostics.OrderBy(value => value, StringComparer.Ordinal).ToArray(), diagnostics.Count == 0);
    }

    private static string NormalizeFeaturePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.StartsWith('/') || Path.IsPathRooted(path)
            || path.Split('/').Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new ArgumentException("Feature path must be a normalized repository-relative path.", nameof(path));
        return path;
    }

    private static string WorkItemUrl(AzureDevOpsConfiguration configuration, string id) =>
        new Uri(configuration.OrganizationUrl, $"{Uri.EscapeDataString(configuration.Project)}/_apis/wit/workItems/{Uri.EscapeDataString(id)}").AbsoluteUri;

    private static string? Classify(AzureDevOpsRelation relation)
    {
        var hint = $"{relation.RelationType} {relation.Name} {relation.Url}";
        if (hint.Contains("pullrequest", StringComparison.OrdinalIgnoreCase) || hint.Contains("pull request", StringComparison.OrdinalIgnoreCase)) return "PullRequest";
        if (hint.Contains("build", StringComparison.OrdinalIgnoreCase)) return "Build";
        if (hint.Contains("release", StringComparison.OrdinalIgnoreCase)) return "Release";
        return null;
    }

    private static string? SafeSameHostUrl(string value, string configuredHost)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, configuredHost, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo)) return null;
        return uri.AbsoluteUri;
    }

    private static string? ResolveTargetUrl(AzureDevOpsRelation relation, string artifactType,
        AzureDevOpsConfiguration configuration, out string evidenceKind)
    {
        evidenceKind = "Azure DevOps work-item relation";
        var direct = SafeSameHostUrl(relation.Url, configuration.OrganizationUrl.Host);
        if (direct is not null) return direct;
        if (artifactType == "PullRequest" && TryGetMappedPullRequestId(relation.Url, configuration, out var id))
        {
            var project = Uri.EscapeDataString(configuration.Project);
            var repository = Uri.EscapeDataString(configuration.Repository);
            evidenceKind = "Azure DevOps work-item relation; portal URL derived from configured repository and returned pull-request ID";
            return new Uri(configuration.OrganizationUrl, $"{project}/_git/{repository}/pullrequest/{id}").AbsoluteUri;
        }
        return null;
    }

    private static bool TryGetMappedPullRequestId(string value, AzureDevOpsConfiguration configuration, out string id)
    {
        id = string.Empty;
        if (!value.StartsWith("vstfs:///Git/PullRequestId/", StringComparison.OrdinalIgnoreCase)) return false;
        var parts = Uri.UnescapeDataString(value).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var marker = Array.FindIndex(parts, part => part.Equals("PullRequestId", StringComparison.OrdinalIgnoreCase));
        if (marker < 0 || parts.Length != marker + 4
            || !string.Equals(parts[marker + 1], configuration.Project, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parts[marker + 2], configuration.Repository, StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(parts[marker + 3], out var numericId) || numericId < 1) return false;
        id = numericId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }

    private static string? ExtractIdentifier(string value, string artifactType)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in query)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] is "buildId" or "releaseId" or "pullRequestId") return Uri.UnescapeDataString(parts[1]);
        }
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var index = segments.Length - 1; index >= 0; index--)
        {
            if (int.TryParse(Uri.UnescapeDataString(segments[index]), out _)) return Uri.UnescapeDataString(segments[index]);
        }
        if (value.StartsWith("vstfs:///", StringComparison.OrdinalIgnoreCase))
        {
            var tail = Uri.UnescapeDataString(uri.AbsolutePath).Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (!string.IsNullOrWhiteSpace(tail)) return tail;
        }
        return null;
    }
}
