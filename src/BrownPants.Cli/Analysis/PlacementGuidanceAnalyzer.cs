namespace BrownPants.Cli.Analysis;

public static class PlacementGuidanceAnalyzer
{
    public const int MaximumRuleLength = 4000;

    public static PlacementGuidanceReport Analyze(string rule, string scope, string durability, string purpose)
    {
        if (string.IsNullOrWhiteSpace(rule)) throw new ArgumentException("A proposed rule is required.", nameof(rule));
        rule = rule.Trim();
        if (rule.Length > MaximumRuleLength)
            throw new ArgumentException($"Proposed rules must be {MaximumRuleLength} characters or fewer.", nameof(rule));

        scope = Normalize(scope, "scope", "repository", "feature", "unknown");
        durability = Normalize(durability, "durability", "enduring", "feature", "unknown");
        purpose = Normalize(purpose, "purpose", "principle", "outcome", "agent_workflow", "mixed", "unknown");
        var inputs = new PlacementInputs(scope, durability, purpose);
        var rationale = new List<string>
        {
            $"Submitted attributes: scope={scope}, durability={durability}, purpose={purpose}.",
            "Placement rubric: durable principles shared across repository features belong in the constitution; feature-specific outcomes and acceptance criteria belong in feature specifications; operational agent or contributor procedures belong in guidance."
        };
        var questions = new List<string>();
        string recommendation;
        string[] destinations;

        if (scope == "unknown" || durability == "unknown" || purpose == "unknown")
        {
            recommendation = "clarify";
            destinations = Array.Empty<string>();
            if (scope == "unknown") questions.Add("Does this rule apply across repository features (repository) or only to one feature (feature)?");
            if (durability == "unknown") questions.Add("Should it govern future work (enduring), or only this feature (feature)?");
            if (purpose == "unknown") questions.Add("Is it a project principle (principle), a user-visible feature result (outcome), agent/contributor procedure (agent-workflow), or a combination (mixed)?");
            rationale.Add("At least one required classification attribute is unknown, so no destination is asserted.");
        }
        else if (purpose == "mixed")
        {
            recommendation = "split";
            destinations = new[] { "constitution", "feature_spec", "agent_guidance" };
            rationale.Add("Separate the concerns: durable cross-feature principles go in the constitution; concrete feature outcomes go in feature specifications; operational procedures go in agent/contributor guidance.");
        }
        else if ((purpose is "principle" or "outcome") && scope == "repository" && durability == "feature")
        {
            recommendation = "clarify";
            destinations = Array.Empty<string>();
            questions.Add("Which boundary is intended: only the current feature (feature), or future repository features too (repository + enduring)?");
            rationale.Add("The selected repository-wide scope conflicts with feature-bounded durability.");
        }
        else if (purpose == "principle" && scope == "repository" && durability == "enduring")
        {
            recommendation = "constitution";
            destinations = new[] { "constitution" };
            rationale.Add("An enduring principle intended to govern repository-wide work belongs in the project constitution.");
        }
        else if (purpose == "principle" && scope == "feature")
        {
            recommendation = "feature_spec";
            destinations = new[] { "feature_spec" };
            rationale.Add("The rule is scoped to one feature, so record it with that feature's requirements; feature scope does not promote it to the constitution.");
        }
        else if (purpose == "outcome" && scope == "feature")
        {
            recommendation = "feature_spec";
            destinations = new[] { "feature_spec" };
            rationale.Add("A feature-specific user-visible outcome belongs in that feature's specification, where it can be made testable.");
        }
        else if (purpose == "outcome" && scope == "repository" && durability == "enduring")
        {
            recommendation = "split";
            destinations = new[] { "constitution", "feature_spec" };
            rationale.Add("Record the durable cross-feature invariant in the constitution, then state concrete user outcomes and acceptance criteria in the relevant feature specifications.");
        }
        else if (purpose == "agent_workflow")
        {
            recommendation = "agent_guidance";
            destinations = new[] { "agent_guidance" };
            rationale.Add("Operational directions for how an agent or contributor performs work belong in guidance, not in product outcomes or governance principles.");
        }
        else
        {
            recommendation = "clarify";
            destinations = Array.Empty<string>();
            questions.Add("Which is intended: a durable repository principle (repository + enduring + principle), or a feature-specific result (feature + outcome)?");
            rationale.Add("The combination does not match a single supported placement rule.");
        }

        return new PlacementGuidanceReport(
            rule,
            inputs,
            recommendation,
            destinations,
            rationale,
            questions,
            "The report uses the scope, durability, and purpose supplied by the requester. It does not infer intent from wording; review the recommendation before editing project artifacts.");
    }

    private static string Normalize(string value, string name, params string[] allowed)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_');
        if (!allowed.Contains(normalized, StringComparer.Ordinal))
            throw new ArgumentException($"Invalid {name} '{value}'. Expected: {string.Join(", ", allowed)}.", name);
        return normalized;
    }
}

public sealed record PlacementGuidanceReport(
    string Rule,
    PlacementInputs Inputs,
    string Recommendation,
    IReadOnlyList<string> Destinations,
    IReadOnlyList<string> Rationale,
    IReadOnlyList<string> Questions,
    string EvidenceLimits)
{
    [System.Text.Json.Serialization.JsonPropertyOrder(-1)]
    public int SchemaVersion => 1;
}

public sealed record PlacementInputs(string Scope, string Durability, string Purpose);
