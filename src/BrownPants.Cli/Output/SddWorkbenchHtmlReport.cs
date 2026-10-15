using System.Net;
using System.Text;
using BrownPants.Cli.Analysis;

namespace BrownPants.Cli.Output;

public static class SddWorkbenchHtmlReport
{
    private const string Styles = """
        :root{color-scheme:light;--ink:#17242b;--muted:#52636d;--paper:#f4f6f5;--card:#fff;--line:#d5dfdc;--brand:#165d53;--good:#206c42;--todo:#8a5a00;--focus:#145dc5;font:16px/1.55 system-ui,-apple-system,"Segoe UI",sans-serif}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink)}a{color:var(--brand)}a:focus,button:focus,textarea:focus{outline:3px solid var(--focus);outline-offset:2px}.shell{max-width:1120px;margin:auto;padding:clamp(1rem,4vw,3rem)}header{background:#173a35;color:white;padding:2rem clamp(1rem,4vw,3rem)}header .shell{padding-top:0;padding-bottom:0}header p{max-width:72ch;color:#dce9e5}.eyebrow{text-transform:uppercase;letter-spacing:.09em;font-size:.78rem;font-weight:700;color:#a9d1c5}h1{line-height:1.15;margin:.3rem 0 1rem;font-size:clamp(2rem,5vw,3.5rem)}h2{margin-top:2.5rem;border-bottom:1px solid var(--line);padding-bottom:.45rem}h3{margin-top:.4rem}.summary-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:1rem;margin:1.5rem 0}.card,details{background:var(--card);border:1px solid var(--line);border-radius:.65rem;padding:1rem}.metric{font-size:1.55rem;font-weight:750;display:block}.muted{color:var(--muted)}.status{display:inline-block;padding:.15rem .55rem;border-radius:1rem;background:#e9f2ee;color:var(--good);font-weight:650;font-size:.88rem}.status.todo{background:#fff1d8;color:var(--todo)}.story-list,.criteria-list{display:grid;gap:1rem}.story-card,.criterion-card,.task-card{background:var(--card);border:1px solid var(--line);border-radius:.65rem;padding:1rem}.story-head{display:flex;gap:.8rem;align-items:baseline;flex-wrap:wrap}.story-head .muted{margin-left:auto}.criterion-card p,.story-card p{max-width:82ch}.feedback{margin-top:.8rem}.feedback label{font-weight:650;display:block}.feedback textarea{display:block;width:100%;min-height:5rem;margin-top:.35rem;border:1px solid #9aaca7;border-radius:.4rem;padding:.65rem;font:inherit}.task-list{display:grid;gap:.65rem}.task-card{display:grid;grid-template-columns:auto minmax(0,1fr);gap:.75rem;align-items:start}.task-card .feedback{grid-column:1/-1;width:100%}.task-card p{margin:.1rem 0}.evidence-list{display:grid;gap:.75rem}.evidence-card{border-left:4px solid #88a9a0}.evidence-card p{margin:.2rem 0}code{overflow-wrap:anywhere}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#f1f4f3;border-radius:.4rem;padding:1rem;max-height:42rem;overflow:auto;font: .9rem/1.5 ui-monospace,SFMono-Regular,Consolas,monospace}details{margin:.7rem 0}summary{cursor:pointer;font-weight:650}button{border:0;border-radius:.45rem;background:var(--brand);color:white;padding:.75rem 1rem;font:inherit;font-weight:700;cursor:pointer}button:hover{background:#0d473e}.actions{position:sticky;bottom:0;background:rgba(244,246,245,.96);border-top:1px solid var(--line);padding:1rem;display:flex;align-items:center;gap:1rem;flex-wrap:wrap}.skip{position:absolute;left:-10000px}.skip:focus{left:1rem;top:1rem;background:white;padding:.6rem;z-index:5}.empty{border:1px dashed #8ca09a;border-radius:.5rem;padding:1rem;color:var(--muted)}footer{margin-top:3rem;padding-top:1rem;border-top:1px solid var(--line);color:var(--muted);font-size:.9rem}@media(prefers-reduced-motion:reduce){*{scroll-behavior:auto!important;transition:none!important}}
        """;

    private const string Script = """
        (()=>{const button=document.getElementById('export-notes');const status=document.getElementById('note-status');button.addEventListener('click',()=>{const notes=Array.from(document.querySelectorAll('textarea[data-note-kind]')).map(field=>({kind:field.dataset.noteKind,id:field.dataset.noteId,text:field.value.trim()})).filter(note=>note.text.length>0);const payload={schemaVersion:1,featurePath:document.body.dataset.featurePath,notes};const blob=new Blob([JSON.stringify(payload,null,2)],{type:'application/json'});const url=URL.createObjectURL(blob);const link=document.createElement('a');link.href=url;link.download='brownpants-review-notes.json';link.click();window.setTimeout(()=>URL.revokeObjectURL(url),1000);status.textContent=notes.length===0?'No feedback was added; an empty notes file was downloaded.':`${notes.length} note${notes.length===1?'':'s'} downloaded for ${payload.featurePath}.`;});})();
        """;

    public static string Render(SddWorkbenchData data)
    {
        var html = new StringBuilder(24_000);
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<meta name=\"color-scheme\" content=\"light\"><title>SDD review — " + E(data.FeatureTitle) + "</title>");
        html.AppendLine("<style>"); html.AppendLine(Styles); html.AppendLine("</style></head>");
        html.AppendLine("<body data-feature-path=\"" + E(data.FeaturePath) + "\"><a class=\"skip\" href=\"#main\">Skip to feature review</a>");
        html.AppendLine("<header><div class=\"shell\"><div class=\"eyebrow\">BrownPants · " + E(data.Engine) + " · Local SDD Review Workbench</div><h1>" + E(data.FeatureTitle) + "</h1>");
        html.AppendLine("<p>A plain-language view of what the team plans to deliver, what has been checked off, and what verification was recorded. Add feedback and send the downloaded notes to the feature owner.</p>");
        html.AppendLine("<p>Feature: <code>" + E(data.FeaturePath) + "</code></p></div></header><main id=\"main\" class=\"shell\">");
        AppendSummary(html, data);
        if (data.Engine == "OpenSpec") AppendProposalSummary(html, data.ProposalSummary, data.FeaturePath);
        else AppendStories(html, data.Stories);
        AppendCriteria(html, data.Criteria);
        AppendTasks(html, data.Tasks);
        if (data.Engine == "Spec Kit") AppendEvidence(html, data.Evidence, data.EvidenceIssues);
        AppendArtifacts(html, data.Artifacts);
        html.AppendLine("<footer>These are the values currently written in the selected change files. A completed checkbox or recorded evidence entry is a review aid; it does not independently prove that behavior passed or was accepted. This page is read-only and works as a local file.</footer>");
        html.AppendLine("</main><div class=\"actions\"><button type=\"button\" id=\"export-notes\">Download review notes</button><span id=\"note-status\" role=\"status\" aria-live=\"polite\">Notes stay in this browser until you download them.</span></div>");
        html.AppendLine("<script>"); html.AppendLine(Script); html.AppendLine("</script></body></html>");
        return html.ToString();
    }

    private static void AppendSummary(StringBuilder html, SddWorkbenchData data)
    {
        var complete = data.Tasks.Count(task => task.IsComplete);
        var baselineClass = data.Engine == "OpenSpec" || data.AcceptanceBaselineStatus.Contains("match", StringComparison.OrdinalIgnoreCase) ? "status" : "status todo";
        html.AppendLine("<section aria-labelledby=\"overview-title\"><h2 id=\"overview-title\">At a glance</h2><div class=\"summary-grid\">");
        Metric(html, data.Engine == "OpenSpec" ? "Requirement and scenario headings" : "Stories", data.Engine == "OpenSpec" ? data.Criteria.Count.ToString() : data.Stories.Count.ToString());
        Metric(html, data.Engine == "OpenSpec" ? "Delta spec files" : "Review points", data.Engine == "OpenSpec" ? data.Artifacts.Count(artifact => artifact.Path.Contains("/specs/", StringComparison.Ordinal) && artifact.Path.EndsWith("/spec.md", StringComparison.Ordinal)).ToString() : data.Criteria.Count.ToString());
        Metric(html, "Tasks recorded complete", $"{complete} of {data.Tasks.Count}");
        html.AppendLine("<div class=\"card\"><strong>Acceptance baseline</strong><p><span class=\"" + baselineClass + "\">" + E(data.AcceptanceBaselineStatus) + "</span></p></div>");
        html.AppendLine("</div></section>");
    }

    private static void AppendProposalSummary(StringBuilder html, string summary, string featurePath)
    {
        html.AppendLine("<section aria-labelledby=\"proposal-summary-title\"><h2 id=\"proposal-summary-title\">Why this is changing</h2>");
        html.AppendLine("<p class=\"muted\">Source: <code>" + E(featurePath + "/proposal.md") + "</code></p>");
        if (string.IsNullOrWhiteSpace(summary)) html.AppendLine("<p class=\"empty\">No Why or What Changes summary was recognized. Read the original proposal below.</p>");
        else html.AppendLine("<p>" + E(summary).Replace("\n", "<br>", StringComparison.Ordinal) + "</p>");
        AppendFeedback(html, "proposal", "proposal.md", "Feedback for the proposal");
        html.AppendLine("</section>");
    }

    private static void Metric(StringBuilder html, string label, string value) =>
        html.AppendLine("<div class=\"card\"><span class=\"metric\">" + E(value) + "</span><span class=\"muted\">" + E(label) + "</span></div>");

    private static void AppendStories(StringBuilder html, IReadOnlyList<SddWorkbenchStory> stories)
    {
        html.AppendLine("<section aria-labelledby=\"stories-title\"><h2 id=\"stories-title\">What people need to do</h2>");
        if (stories.Count == 0) { html.AppendLine("<p class=\"empty\">No user story headings were recognized. Open the specification below to read its original text.</p></section>"); return; }
        html.AppendLine("<div class=\"story-list\">");
        foreach (var story in stories)
        {
            html.AppendLine("<article class=\"story-card\"><div class=\"story-head\"><h3>" + E(story.Title) + "</h3><span class=\"muted\">" + E(story.Id) + (string.IsNullOrWhiteSpace(story.Priority) ? string.Empty : " · " + E(story.Priority)) + "</span></div>");
            if (story.Description.Length > 0) html.AppendLine("<p>" + E(story.Description) + "</p>");
            AppendFeedback(html, "story", story.Id, "Feedback for " + story.Id);
            html.AppendLine("</article>");
        }
        html.AppendLine("</div></section>");
    }

    private static void AppendCriteria(StringBuilder html, IReadOnlyList<SddWorkbenchCriterion> criteria)
    {
        var openSpec = criteria.Any(item => item.Kind is "Requirement" or "Scenario");
        html.AppendLine("<section aria-labelledby=\"criteria-title\"><h2 id=\"criteria-title\">" + (openSpec ? "Requirements and scenarios" : "How the team will know") + "</h2>");
        if (criteria.Count == 0) { html.AppendLine("<p class=\"empty\">No review points were recognized. Read the original specification below.</p></section>"); return; }
        html.AppendLine("<div class=\"criteria-list\">");
        foreach (var item in criteria)
        {
            var heading = string.IsNullOrWhiteSpace(item.SourcePath) ? item.Id : item.Text;
            html.AppendLine("<article class=\"criterion-card\"><div class=\"story-head\"><h3>" + E(heading) + "</h3><span class=\"muted\">" + E(item.Kind) + "</span></div>");
            if (!string.IsNullOrWhiteSpace(item.SourcePath)) html.AppendLine("<p class=\"muted\">Source: <code>" + E(item.SourcePath) + "</code> · Reference: <code>" + E(item.Id) + "</code></p>");
            else html.AppendLine("<p>" + E(item.Text) + "</p>");
            AppendFeedback(html, openSpec ? item.Kind.ToLowerInvariant() : "criterion", item.Id, "Feedback for " + item.Kind + " " + item.Id);
            html.AppendLine("</article>");
        }
        html.AppendLine("</div></section>");
    }

    private static void AppendTasks(StringBuilder html, IReadOnlyList<SddWorkbenchTask> tasks)
    {
        html.AppendLine("<section aria-labelledby=\"tasks-title\"><h2 id=\"tasks-title\">Work in progress</h2>");
        if (tasks.Count == 0) { html.AppendLine("<p class=\"empty\">No task checklist was found for this feature.</p></section>"); return; }
        html.AppendLine("<div class=\"task-list\">");
        foreach (var task in tasks)
        {
            var status = task.IsComplete ? "Recorded complete" : "Not marked complete";
            var className = task.IsComplete ? "status" : "status todo";
            html.AppendLine("<article class=\"task-card\"><span class=\"" + className + "\">" + status + "</span><div><strong>" + E(task.Id) + "</strong><p>" + E(task.Text) + "</p>" + (string.IsNullOrWhiteSpace(task.SourcePath) ? string.Empty : "<p class=\"muted\">Source: <code>" + E(task.SourcePath) + "</code></p>") + "</div>");
            AppendFeedback(html, "task", task.Id, "Feedback for " + task.Id);
            html.AppendLine("</article>");
        }
        html.AppendLine("</div></section>");
    }

    private static void AppendEvidence(StringBuilder html, IReadOnlyList<SddWorkbenchEvidence> evidence, IReadOnlyList<string> issues)
    {
        html.AppendLine("<section aria-labelledby=\"evidence-title\"><h2 id=\"evidence-title\">Checks and handoff recorded</h2>");
        html.AppendLine("<p class=\"muted\">Evidence entries are recorded in the feature ledger. A result label records what the task author entered; review the linked evidence before relying on it.</p>");
        if (issues.Count > 0)
            foreach (var issue in issues) html.AppendLine("<p class=\"status todo\">" + E(issue) + "</p>");
        if (evidence.Count == 0) { html.AppendLine("<p class=\"empty\">No acceptance evidence entries were recorded.</p></section>"); return; }
        html.AppendLine("<div class=\"evidence-list\">");
        foreach (var item in evidence)
        {
            html.AppendLine("<article class=\"card evidence-card\"><h3>" + E(item.TaskId) + " · " + E(item.Result) + "</h3>");
            if (item.CriterionIds.Count > 0) html.AppendLine("<p>Criteria: " + E(string.Join(", ", item.CriterionIds)) + "</p>");
            if (item.EvidencePaths.Count > 0) html.AppendLine("<p>Evidence files: " + E(string.Join(", ", item.EvidencePaths)) + "</p>");
            html.AppendLine("</article>");
        }
        html.AppendLine("</div></section>");
    }

    private static void AppendArtifacts(StringBuilder html, IReadOnlyList<SddWorkbenchArtifact> artifacts)
    {
        html.AppendLine("<section aria-labelledby=\"artifacts-title\"><h2 id=\"artifacts-title\">Original feature documents</h2>");
        foreach (var artifact in artifacts)
        {
            html.AppendLine("<details><summary>" + E(artifact.Name) + " <span class=\"muted\">(" + E(artifact.Path) + ")</span></summary><pre>" + E(artifact.Content) + "</pre></details>");
        }
        html.AppendLine("</section>");
    }

    private static void AppendFeedback(StringBuilder html, string kind, string id, string label)
    {
        html.AppendLine("<div class=\"feedback\"><label>" + E(label) + "<textarea data-note-kind=\"" + E(kind) + "\" data-note-id=\"" + E(id) + "\" rows=\"2\" placeholder=\"Add a question or suggestion\"></textarea></label></div>");
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);
}
