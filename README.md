#           <img width="25%" height="25%" alt="image" src="https://github.com/user-attachments/assets/bc2bfb61-14b8-4c87-bd7f-74897cc50b4e" />

**Plain English:** BrownPants helps teams understand an existing system before they change it. It maps the code and project evidence already in the repository, gives developers and agents leads into unfamiliar behavior, and helps QA see what a change may touch and what was checked. Its core inspection runs locally without AI, builds, or source changes.

> **Looks like someone has the Right idea.**

## Start with the brownfield system

BrownPants is built for the code that teams already have: old frameworks, mixed languages, undocumented boundaries, and project files nobody wants to upgrade just to inspect. Run the .NET 10 tool against legacy .NET Framework 4.x source; Roslyn reads C# and Visual Basic without building those projects. It also inventories C/C++, MFC and C++/CLI markers, reports candidate evidence for factories, dependency injection, interop, and framework-specific patterns, and links supported MFC message-map entries to handler and explicitly qualified native/C++/CLI method definitions. Where compiler context is available, semantic mode resolves managed method calls and explicit constructor use—including target-typed and generic construction—and traces their callers.

That gives a team a reviewable starting point before it writes a spec or edits code: what languages and projects are present, which files contain relevant patterns, where calls lead, and what test paths may deserve attention. Candidate matches and incomplete analysis are labeled as such; BrownPants does not pretend to understand behavior it could not resolve.

## BrownPants in a day

- **You inherit a .NET Framework 4.x service with C# and MFC mixed together.** Run `brownpants inspect .` to see project markers, source coverage, and Roslyn structure without upgrading or building the old projects. Native C++ and MFC matches are labeled as candidate evidence; BrownPants does not claim it compiled or resolved native behavior.
- **A user reports a bug but does not know where it lives.** Run `brownpants investigate . --clue "timeout" --clue "ORD-42"` to find relevant source and test paths. After identifying changed C# or VB files, `brownpants impact . --changed <path>` follows resolved callers. For MFC/C++/CLI changes, it traces supported MFC dispatch and qualified native calls as separate lexical candidates. These leads help investigation; they do not claim root cause or test coverage.
- **An agent is about to implement a bug task.** Run `brownpants evidence examples/brownfield-bug-lab/managed --clue "OrderImportWorkflow" --changed Workflows/OrderImportWorkflow.cs --semantic` to give it findings selected by a literal clue and a changed source path. The JSON explains each selection and keeps candidates separate from resolved callers. Resolved findings include one-based source columns, so separate calls on the same line keep distinct IDs and remain navigable. `--semantic` loads MSBuild projects, so use it only when those project files are trusted. The full handoff remains available without selectors.
- **A team is using Spec Kit.** BrownPants supplies bounded repository context, checks the feature's hashed acceptance criteria, and maps tasks to observable evidence. The adapter refreshes context at workflow steps; maintainers still review any proposed criteria change.
- **QA needs to know what to verify and what shipped.** The local workbench presents feature scenarios and tasks. The Azure DevOps adapter can trace related work items, PRs, builds, and releases; the local PR preview carries QA guidance and evidence gaps. A user can prepare or create a draft PR, while review and merge stay human-controlled.
- **Your company restricts AI services or command-line agents.** BrownPants' default inspection runs locally without an LLM, network calls, project builds, or source modifications. IDE and SDD adapters are optional; an organization can use local reports or approved hosts without enabling online integrations.

## Spec Kit vs. Spec Kit + BrownPants: brownfield bug lab

The [Brownfield Bug Lab](examples/brownfield-bug-lab/) is a small .NET 10 order importer with a seeded retry-row bug, generic overloads, factory/DI wiring, and analysis-only .NET Framework, MFC/C++/CLI, and NHibernate artifacts. It keeps the old integrations visible for inspection without requiring those frameworks to build.

It also demonstrates constructor impact across a generic factory and a DI registration. Run `brownpants impact examples/brownfield-bug-lab/managed --changed Workflows/OrderImportWorkflow.cs --format markdown --limit 1000` to see the factory's target-typed `new(repository)` resolved to `OrderImportWorkflow<Order>..ctor(OrderRepository<Order>)`, then traced through the registration and test caller paths. On the checked-in fixture this produces **4 direct callers, 1 transitive caller, 2 affected projects, and 1 test-path candidate**. Those counts describe the fixture's loaded source graph; they do not prove the DI container activates that factory at runtime.

BrownPants' own reports identify the legacy project and source candidates, including **21 architecture signals** with paths and lines. Its semantic impact report resolves the four managed calls around the changed batch save: single-item `Save(T)`, batch `Save(IEnumerable<T>)`, `SaveMapped<TSource>`, and the generic forwarding call from `SaveMapped` to the batch overload. The forwarding path matters because both ordinary and mapped imports can reach the affected method. MFC and NHibernate findings remain candidates; the fixture does not prove native-to-managed runtime behavior or database behavior.

The matched agent run produced the same narrow repair in both conditions, and the same independent regression check passed after each fix. The structured finding scores were:

| Finding domain | Expected | Spec Kit hits / submitted | + BrownPants hits / submitted |
| --- | ---: | ---: | ---: |
| Managed call edges | 4 | 4 / 4 | 4 / 4 |
| Factory and DI candidates | 5 | 0 / 0 | 0 / 1 |
| MFC and NHibernate candidates | 10 | 0 / 2 | 2 / 3 |
| Test candidates | 1 | 1 / 1 | 0 / 1 |

Overall recall moved from **25% to 30%**, while precision moved from **71.4% to 66.7%**. The assisted run therefore **did not meet the frozen advantage criterion** of at least 20 percentage points more recall with no precision loss. This pair shows BrownPants can produce detailed source-linked brownfield evidence; it does not show a better repair or a net improvement in the agents' final structured findings. The agents used **299,072 input / 3,531 output tokens** and **389,425 input / 3,331 output tokens**, respectively (cached input is included in input totals).

Both pre-fix checks failed with “Batch import saved 1 of 2 orders.” The driver verified the frozen task hash and ran the same post-fix check in both copies; both passed. The [full protocol, scores, run details, and limits](docs/comparisons/brownfield-bug-lab-2026-10.md) include the machine-readable [results](docs/comparisons/brownfield-bug-lab-2026-10-results.json). This is a measured case study, not a general performance claim.

### Delegate-impact head-to-head

A separate frozen Codex CLI task seeded a persistence bug behind a local method-group delegate. Across three counterbalanced baseline/assisted pairs, baseline Codex found **1 of 2** expected managed-impact relationships in every run; the BrownPants-assisted runs found **2 of 2**. Pooled recall increased from **50% to 100%**, precision stayed at **100%**, all three paired trials favored BrownPants, and all six repairs passed the same independent check. This meets the pre-registered threshold for this delegate fixture; it does not establish an advantage across other tasks or codebases.

Codex reported **366,727 input / 2,190 output tokens** for the three baseline runs and **326,586 input / 2,236 output tokens** for the assisted runs. These are recorded usage counts, not a cost or efficiency claim. The [comparison report](docs/comparisons/delegate-impact-comparison.md), [scored JSON](docs/comparisons/delegate-impact-comparison.json), frozen [task and scoring protocol](examples/brownfield-bug-lab/evaluation/delegate-impact/), and [raw run artifacts](examples/brownfield-bug-lab/evaluation/delegate-impact/runs/codex-cli-0.162.1-gpt-6.1-sol-20261015/) include the counterbalanced order, input hashes, outputs, usage, and post-fix checks.

### Grouped evidence follow-up

The next matched run added deterministic `repeatedCandidateGroups` for repeated architecture/framework locations while preserving every raw finding. BrownPants grouped five sets in the fixture: C++/CLI bridge, MFC message map, and NHibernate session factory, session, and transaction. The [current head-to-head](docs/comparisons/brownfield-evidence-grouping-2026-10.md) scored **20/20 expected findings for both Spec Kit and Spec Kit + BrownPants** (100% recall, 86.96% precision each); both agents made the same fix. This run shows **no finding-quality advantage** for BrownPants on this prompt. The assisted run used **325,087 input / 5,010 output tokens**, while baseline used **704,193 input / 7,379 output tokens**; one pair does not establish cost or time savings. The [full results](docs/comparisons/brownfield-evidence-grouping-2026-10-results.json) include provider token details and a retained prompt-refinement pilot. Earlier v3 evidence-handoff results are preserved but superseded because their source commit and C++/CLI line could not be verified; see the [v4 report](docs/comparisons/brownfield-evidence-grouping-2026-10.md) for provenance limits.

BrownPants complements Spec Kit: Spec Kit structures the work; BrownPants adds evidence about the existing code and links completion claims to reviewable artifacts. Neither guarantees a correct fix or replaces human review.

A SHA-256 acceptance hash is a fingerprint of the agreed pass/fail conditions. If the criteria change during a task, the fingerprint changes and asks a maintainer to review the change. It keeps the goalposts visible; it does not prove the implementation is correct.

**Author's note:** Loves his job, wishes his employer would convert him to full-time. If you have a Trey in your meeting, you should hire him.

## Quick start

Requires the .NET 10 SDK.

```powershell
dotnet build BrownPants.sln
dotnet run --project src/BrownPants.Cli -- inspect .
dotnet run --project src/BrownPants.Cli -- inspect . --format json
dotnet run --project src/BrownPants.Cli -- inspect . --format json --limit 1000
dotnet run --project src/BrownPants.Cli -- inspect . --semantic
dotnet run --project src/BrownPants.Cli -- investigate . --clue "TimeoutException" --clue "ERR-42"
dotnet run --project src/BrownPants.Cli -- investigate . --clue "TimeoutException" --format json
dotnet run --project src/BrownPants.Cli -- impact . --changed src/OrderService.cs
dotnet run --project src/BrownPants.Cli -- workbench .
dotnet run --project src/BrownPants.Cli -- workbench . --engine openspec
dotnet run --project src/BrownPants.Cli -- instructions . --format markdown
dotnet run --project src/BrownPants.Cli -- instructions . --source .agents/custom-guidance.md --format json
dotnet run --project src/BrownPants.Cli -- instructions . --source AGENTS.md --section repository-guidelines --format markdown
dotnet run --project src/BrownPants.Cli -- placement --rule "All features follow this durable principle" --scope repository --durability enduring --purpose principle
dotnet run --project src/BrownPants.Cli -- models validate .
dotnet run --project src/BrownPants.Cli -- models report . --format markdown
dotnet pack src/BrownPants.Cli -c Release -o ./artifacts
dotnet tool install --tool-path ./.tools --add-source ./artifacts BrownPants.Cli --version 0.2.1
./.tools/brownpants inspect . # Windows: .\.tools\brownpants.exe inspect .
dotnet test BrownPants.sln
```

### Use an approved NuGet mirror

The installed CLI's default `inspect`, `investigate`, and local workbench paths work without network access. Initial restore or tool installation still needs the pinned dependencies from a package source. If your environment blocks nuget.org, use your organization's NuGet configuration and trusted mirror:

```powershell
dotnet restore BrownPants.sln --configfile <approved-nuget.config> --locked-mode
dotnet build BrownPants.sln -c Release --no-restore
dotnet tool install --tool-path ./.tools --configfile <approved-nuget.config> --add-source ./artifacts BrownPants.Cli --version 0.2.1
```

Keep package-signature validation aligned with your organization's trust policy. Once installed, default repository inspection does not call the package source, an AI service, or a remote API.

`investigate` accepts one or more literal clues from a bug report, such as an error phrase, symbol, or path fragment. It returns deterministic path and line matches, labels likely test paths, and supports Markdown or versioned JSON output. Results are bounded and omit matched source text; binary files and files over 10 MiB are skipped for content matching. These matches are investigation leads, not evidence of root cause or regression coverage. The command is read-only and does not run code, tests, builds, or network requests. See the [investigation CLI contract](specs/017-bug-investigation-context/contracts/investigate-cli.md).

`instructions` maps common agent/project instruction files and any explicitly selected repository-relative files. It reports Markdown headings, approximate scope, byte counts, heuristic token estimates, and SHA-256 identities without exposing instruction contents. Use `--source <path> --section <anchor>` to return one canonical section, including its source and line range; the host still decides whether other instruction files are loaded, so retrieval does not guarantee token savings. See the [section retrieval contract](specs/024-lazy-load-context/contracts/instruction-section.md). To draft a frame, prepare JSON with a `budget`, ordered `points` with `text` and cited `sources` (`path` and optional heading `anchor`), and optional `reviewItems`, then run `dotnet run --project src/BrownPants.Cli -- frame create . ./frame-draft.json docs/agent-context-frame.md`. Use `frame validate . docs/agent-context-frame.md` to detect changed or missing sources. Output stays inside the repository and existing files are protected unless `--overwrite` is explicit. Review semantic summaries and conflicts yourself. Frames do not guarantee model memory or token savings; the host must load the frame instead of continuing to inject duplicate source files. See the [feature contract](specs/019-instruction-context-framing/contracts/instruction-context-frame.schema.json).

`models` validates a repository-local `.brownpants/models.json` policy, reports task-specific model preferences, and can export selected profiles for VS Code/Copilot, Visual Studio 18.4+, Claude Code, or Codex. VS Code and Visual Studio share `.github/agents/`, so generated filenames distinguish the two. Copy and edit the [example policy](examples/model-routing-policy.json) first. Exports preserve conflicts and require explicit host selection. Generated agents set a model only when the host or user invokes them; BrownPants does not schedule tasks, authenticate, check model access, or recommend current model products. See the [model-routing guide](docs/model-routing.md).

`placement` recommends whether a proposed rule belongs in a constitution, feature spec, or agent/contributor guidance. Supply explicit `--scope`, `--durability`, and `--purpose` values; `unknown` produces focused clarification questions, and `mixed` recommends a split. Repository-wide durable outcomes are split between a lasting principle and feature-specific outcomes. The command emits deterministic Markdown or JSON, explains its rubric, and never edits project artifacts or infers intent from keywords. See the [placement contract](specs/020-requirement-placement-guidance/contracts/placement-guidance.schema.json) and [quickstart](specs/020-requirement-placement-guidance/quickstart.md).

Spec Kit implementation guidance freezes the active feature's acceptance scenarios and success criteria in `acceptance-criteria.sha256`. Run `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath <feature>/spec.md` before implementation and completion. A changed digest requires review; `-Refresh` intentionally replaces the baseline and should only follow explicit agreement. Map every task to `AC-###` and `SC-###` IDs in a feature-local `acceptance-evidence.json`, then validate it with `pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath <feature>/spec.md -TasksPath <feature>/tasks.md -EvidencePath <feature>/acceptance-evidence.json`. This structural check never executes recorded commands and requires completed tasks to cite existing evidence files; humans still judge whether tests prove the behavior. A changed existing baseline requires a feature-local `acceptance-criteria-review.md` and the `acceptance-criteria-reviewed` pull request label after maintainer review. A first baseline for a new feature is exempt; a late first baseline for an existing feature needs review. CI checks the baseline, reruns its review policy when the label changes, and validates ledgers when their task list or ledger changes. Configure the CI job as a required pull request check to block merges when these policies fail. See [the acceptance integrity procedure](specs/021-acceptance-criteria-integrity/quickstart.md).

`inspect` reports project markers and grouped ecosystems, source-language coverage, file type counts, likely test paths, top-level entries, and supported SDD workflow artifacts when present. Spec Kit feature directories and OpenSpec capability/active-change directories are reported separately, with artifact paths bounded by `--limit`. When evidence is unambiguous, each workflow can also report an active work item: Spec Kit uses the validated `.specify/feature.json` pointer, OpenSpec selects only a single non-archived change directory, and custom workflows can declare an explicit path under their artifact roots. Multiple OpenSpec changes remain unselected, and invalid custom selections are diagnosed without discarding other workflow inventory. Ecosystem markers cover common .NET, C/C++, CMake/Make, Python, JavaScript/TypeScript, Go, Rust, JVM, Ruby, PHP, Apple, Dart, Elixir, Scala, container, and infrastructure files. Extensionless files and unfamiliar suffixes remain in file counts. The scanner skips common VCS, build, package, and environment directories and avoids following directory symlinks. Markdown is reviewable context for any planning workflow; JSON is available for tools and other SDD engines and starts with root `SchemaVersion: 1`. JSON consumers should check this integer before reading fields; it versions the report contract independently of the package. Optional additive fields remain version 1, while breaking field changes require a new schema version. Test-path signals remain heuristic, and project references are reported as written in project files rather than resolved into a dependency graph.

Language coverage identifies what BrownPants actually analyzes: Roslyn syntax and candidate patterns for C# and VB.NET, lexical candidate patterns for Python, JavaScript/TypeScript, Java/Kotlin, Go, and native C/C++, and inventory-only coverage for other recognized source languages. Go candidates include `NewType` constructor naming and Fx/Dig provider and invocation calls. Python candidates include factory class/method/call naming, Dependency Injector provider declarations, and dependency markers such as FastAPI `Depends`, `Provide[...]`, and `@inject`. JavaScript/TypeScript candidates include factory class/method/call naming, Angular `useFactory`/`inject`/`@Injectable` cues, and container `bind`/`get` calls. Java/Kotlin candidates include factory class/method/call naming and Spring, Jakarta Inject/CDI, Dagger, and Guice annotations such as `@Bean`, `@Provides`, `@Binds`, `@Inject`, and `@Autowired`, plus Guice binding and injector-creation calls. These lexical analyzers filter comments and quoted strings, but do not parse source syntax or resolve runtime dependency graphs. Ecosystem detection comes from conventional manifest names; it does not prove that every listed project is active or buildable.

Roslyn parses C# and Visual Basic files without building the project, so legacy .NET Framework source can be inspected from a .NET 10 host. Reports include `.csproj` and `.vbproj` target framework values, project references, type/member declarations, import/using directives (C#), invocation syntax, inactive preprocessor text (C#), and parser diagnostics. Where a C# project has one explicit `LangVersion`, that version is used for its source files. Default mode reports invocation syntax without binding calls to target methods; `--semantic` opts into that resolution. Default mode also leaves MSBuild conditions/imports and preprocessor symbols unevaluated, and parser diagnostics are not build results. By default, structured symbol samples are capped at 200; raise the cap for JSON with `--limit 1000` (maximum 10000).

C# and VB.NET factory-method name candidates omit `void`/`Sub` methods and string-returning helpers. Other return types remain naming-based candidates and should be checked at their cited source locations.

Pass `--semantic` to opt in to MSBuildWorkspace project loading. When the required MSBuild installation and references are available, BrownPants resolves C# and Visual Basic invocation expressions and object construction to method and constructor symbols. Constructor declarations include source-backed implicit constructors and C# primary constructors, anchored to their type declarations when no explicit constructor syntax exists. Reports include caller-to-target edges, in-repository declaration locations, separate method and constructor counts, unresolved candidate reasons, workspace issues, and compiler errors. Calls inside C#/VB lambdas and anonymous methods retain their named enclosing method and a source-relative callable anchor; local functions retain their parent callable identity. Compiler errors are summarized by project and diagnostic code with exact counts; bounded source-located samples remain available beside the groups. Group frequency is evidence for triage, not a root-cause diagnosis. Workspace diagnostics preserve the failure reason, show repository paths relative to the scan root, and replace external absolute paths with `[external-path]`. Closed generic targets retain their constructed signatures and match open generic declarations for impact tracing. One stable solution context is loaded; discovered projects outside it are opened directly, and duplicate project paths are analyzed once. Coverage is marked incomplete when a project cannot be loaded or workspace issues occur. This mode may evaluate project imports and run design-time builds; review the repository before using it. Results reflect MSBuild's loaded configuration, and unresolved relationships or compiler errors do not by themselves prove a runtime defect.

`impact` accepts changed C#, Visual Basic, and C/C++ source paths. Managed paths use Roslyn-resolved method and constructor relationships; native paths use bounded lexical MFC dispatch, qualified-call, and supported typed C++/CLI-to-managed declaration candidates, clearly separated in Markdown and JSON. Managed `inspect --semantic` keeps its public resolved-call sample bounded by `--limit`; impact traversal independently retains up to 100,000 resolved method and constructor edges. Reaching that internal ceiling is reported with retained and omitted counts and makes impact incomplete. Other limits, including sampled declarations, unresolved calls, and project-loading issues, remain visible. A changed managed repository method can now trace through its impacted callers into exact matching C++/CLI, native-call, and MFC candidates. For example, `brownpants impact examples/brownfield-bug-lab --changed managed/Persistence/OrderRepository.cs` follows `OrderImportWorkflow<T>.ImportBatch` to the bridge and MFC command map. JSON exposes both native `Depth` and the `ManagedImpactDepth` that led to the native trace; native depth retains its native-only meaning. A changed managed workflow also traces directly through its typed bridge call. Native and interop candidates do not prove compiler binding, overload selection, runtime dispatch, or execution. Candidate test paths do not prove coverage, and graph size is not an effort estimate. Native-only impact reads source without loading MSBuild projects; requests that include managed paths explicitly load managed projects through MSBuildWorkspace, which may evaluate project imports and design-time targets. See the [managed impact report contract](specs/023-call-impact-report/contracts/impact-report.md), [native impact contract](specs/055-native-call-impact/contracts/impact-report.md), [managed interop contract](specs/056-managed-interop-impact/contracts/interop-evidence.md), and [constructor impact contract](specs/050-semantic-constructor-impact/contracts/semantic-impact.md).

`workbench` creates a self-contained HTML review page. By default it uses the active Spec Kit feature; add `--engine openspec` to review the unique active OpenSpec change, or select one with `--feature openspec/changes/<name>`. Open the generated `TestResults/brownpants-workbench.html` in a browser to review the proposal, requirements/scenarios, task status, and design. Reviewers can attach notes and download a JSON handoff; this does not edit feature files or send data over the network. See the [Spec Kit quickstart](specs/035-sdd-workbench/quickstart.md), [OpenSpec quickstart](specs/036-openspec-workbench/quickstart.md), and [OpenSpec contract](specs/036-openspec-workbench/contracts/workbench-openspec.md).

`diagram` renders a repository-local `brownpants-flow` Markdown block as a deterministic PNG, or as editable native diagrams.net XML with `--format drawio`. It uses a BrownPants-owned grammar rather than Mermaid, packages no graphics engine, and makes no network requests. Both source and output paths must stay inside the selected repository; output is preserved unless `--overwrite` is explicit. For example:

````markdown
```brownpants-flow
flowchart brownfield-review
node start start "Legacy code"
node inspect process "Inspect"
node impact process "Trace impacts"
node criteria process "Set criteria"
node verify process "Verify"
node review end "Review"
edge start -> inspect
edge inspect -> impact
edge impact -> criteria
edge criteria -> verify
edge verify -> review
```
````

```powershell
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/approval.md --output docs/flows/approval.png
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/approval.md --output docs/flows/approval.drawio --format drawio
```

The format supports `start`, `process`, `decision`, and `end` nodes, directed edges, and optional edge labels. Input is limited to 1 MiB, 200 nodes, 500 edges, and images up to 4096 pixels per side and 16 million pixels total. Labels must use printable ASCII. See `brownpants diagram --help` and the [flowchart contract](specs/032-local-text-flowcharts/contracts/local-flowchart.md).

Here is the rendered chart generated from the example above with BrownPants' local renderer:

![BrownPants flowchart showing inspection, impact tracing, acceptance criteria, verification, and review](docs/flows/approval.png)

The [source Markdown](docs/flows/approval.md) is checked in alongside the PNG so contributors can regenerate or edit the diagram. This makes the feature visible in the README without relying on a hosted chart renderer.

Native coverage includes `.vcxproj` and legacy `.vcproj` markers, MFC/CLR project settings, and source-located signals for MFC class inheritance from common framework types, runtime macros, object factories, message maps, COM activation and marshalling, COM interface metadata, C++/CLI, C#/VB interop attributes, factory types and calls, `Func<T>` factory delegates, constructor-injected interface/factory parameters, and DI APIs such as `AddTransient`, `RegisterType`, `RegisterInstance`, `Bind`, `GetRequiredService`, `GetInstance`, and container-scoped `Resolve`/`Get<T>`. Context checks on generic `Get`, `GetObject`, and `Resolve` names reduce matches from unrelated object APIs. Pattern findings carry `EvidenceKind: Candidate` in JSON and are labeled in Markdown; they are not proof of runtime relationships. Native flow output links recognized MFC command-map entries to a unique handler definition and explicitly class-qualified calls to a unique C++/C++CLI definition, with both source locations. Typed C++/CLI handle calls can also point to matching C# and VB method declarations, with native-to-managed and reverse-impact paths kept as lexical candidates. Missing and ambiguous targets remain visible. Analysis does not expand macros, prove overload selection, resolve runtime dispatch, or establish C++/CLI project references or compiler binding. C# and VB.NET patterns are syntax-based. Native C++ matches remain lexical candidates, not a full C++ parse. Inspect both sides of managed/native boundaries and the cited build configuration.

For .NET brownfield code, `inspect` also reports direct NHibernate package references, explicit mapping/configuration files, and C#/VB session factory, session, query, transaction, and DI API candidates. It parses metadata without MSBuild evaluation and omits XML values such as connection strings from NHibernate metadata findings. Candidate presence does not establish transitive packages, runtime mapping activation, session lifetime, or database behavior. See [NHibernate awareness](specs/041-nhibernate-awareness/quickstart.md) and the [official session configuration](https://nhibernate.info/doc/nhibernate-reference/session-configuration.html) and [transaction guidance](https://nhibernate.info/doc/nhibernate-reference/transactions.html).

Repository inventories skip generated `TestResults`, `test-results`, `testresults`, build outputs, coverage, and artifact directories. To inspect one of those folders intentionally, pass it directly as the repository root, for example `brownpants inspect TestResults`; fixtures and reports elsewhere remain included.

## Workflow integrations

### WinForms thread access review

For a legacy WinForms application, run `brownpants winforms analyze <repository>` to list candidate property writes on controls. It indexes C#/VB declarations across files, follows partial/derived controls and typed member chains, recognizes aliases, and respects local scopes so same-named ordinary objects do not inherit a Control's type. The report also counts unresolved receivers as coverage issues. It separates recognizable background callbacks from `Invoke`/`BeginInvoke`/`InvokeAsync` callback syntax; `InvokeRequired` is only a marker. Every result remains a candidate because source syntax cannot prove runtime thread identity or successful marshaling. The command parses C# and Visual Basic without building or evaluating the project, so it can inspect .NET Framework code from a .NET 10 host on any OS. See the [WinForms review guide](integrations/winforms/README.md).

### GitHub Copilot in Visual Studio and VS Code

For Visual Studio 2022 and VS Code's Local harness, install the repository-shareable Copilot context prompt with `pwsh -NoProfile -File ./integrations/copilot/Install.ps1 -ProjectRoot <target-repository>`, then invoke `/brownpants-context` in Copilot Chat. Current VS Code Copilot Agent mode and Visual Studio 2026 18.5+ use agent skills instead; install one with `pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent copilot`. The combined `-Agent all` option installs for Copilot, Codex, and Claude Code. Skill discovery does not guarantee activation. If Copilot cannot use the terminal, run BrownPants from an approved shell and provide its report. Host support and terminal access depend on version, settings, and organization policy. See the [Copilot prompt guide](integrations/copilot/README.md) and [shared agent-skill guide](integrations/agent-skills/README.md).

### Cursor Chat

Install the project command with `pwsh -NoProfile -File ./integrations/cursor/Install.ps1 -ProjectRoot <target-repository>`, then invoke `/brownpants-context` in Cursor Chat. Cursor documents project commands under `.cursor/commands/`; its command feature is beta. The adapter uses the local BrownPants tool when terminal access is allowed and asks for an approved-shell report when it is not. It does not install AI tools or bypass host policy. See the [Cursor integration guide](integrations/cursor/README.md).

### Codex and Claude Code local sessions

Install a project skill for Codex, Claude Code, or both with `pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent both`. Invoke `$brownpants-context` in Codex CLI/IDE or `/brownpants-context` in a local Claude Code session. The workflow uses the local BrownPants command and respects host and organization terminal policies; if commands are unavailable or disallowed, run BrownPants in an approved environment and provide the report. This adapter does not claim support for Claude Cowork/cloud sessions. See the [local agent skills guide](integrations/agent-skills/README.md).

### Azure DevOps delivery traceability

`brownpants devops trace` connects mapped feature work items to their iteration and returned PR/build/release references. Online access is explicit. Azure DevOps Services can use delegated Entra sign-in in the system browser; the user's existing Azure DevOps permissions still apply. Contributors can review a local PR preview and explicitly submit it as a draft; BrownPants does not approve or complete it. See the [Azure DevOps integration guide](integrations/azure-devops/README.md) for app consent, token setup, evidence limits, and command details.

### GitHub draft pull requests

Prepare and review a local evidence-backed preview without Azure DevOps configuration or network access, then explicitly create a GitHub draft PR. BrownPants requires both `--confirm` and `--online`, uses a user-supplied token, and never reviews, approves, or merges. See the [GitHub integration guide](integrations/github/README.md) for the command, token permission, and limitations.

The same preview can be submitted as a draft to [GitLab.com](integrations/gitlab/README.md) or [Bitbucket Cloud](integrations/bitbucket/README.md). Each adapter is pinned to its public service API and requires explicit `--confirm` and `--online`; self-managed GitLab and Bitbucket Data Center are not supported. Review, approval, and merge remain with the team.

### Register another SDD engine

Add `.brownpants/sdd-workflows.json` to describe an engine that does not have a built-in detector:

```json
{
  "schemaVersion": 1,
  "workflows": [
    {
      "name": "ExampleSDD",
      "artifactRoots": ["planning"],
      "specificationRoots": ["planning/specs"],
      "changeRoots": ["planning/changes"],
      "archivedChangeDirectoryNames": ["archive"],
      "activeWorkItemPath": "planning/changes/add-search"
    }
  ]
}
```

`artifactRoots` is required; the other root lists and `activeWorkItemPath` are optional. When provided, `activeWorkItemPath` must be a safe directory below an artifact root and contain a scanned file; BrownPants reports it with `CustomWorkflowDeclaration` evidence. Invalid or stale selections add `SddWorkflowIssues` in JSON and diagnostics in Markdown while preserving the workflow's other counts. BrownPants matches roots and selections against files already discovered inside the repository; it does not enumerate configured roots, read selected directories, or execute engine code. The declaration must use schema version 1, contain no unknown properties, include at most 100 workflows, and remain under 1 MiB. See the [configuration contract](specs/005-pluggable-sdd-discovery/contracts/custom-workflow-config.md).

The core inventory is workflow-neutral. Spec Kit is the first adapter and refreshes BrownPants context before constitution, specify, clarify, plan, checklist, tasks, analyze, implement, and task-to-issues phases, then again after implementation for completion review. For Codex CLI, initialize Spec Kit with `specify init --here --integration codex --integration-options="--skills"`. Its commands are installed as `.agents/skills`; the BrownPants context command runs automatically through extension hooks. To install the BrownPants adapter, build the extension manifest with `integrations/speckit/BuildExtension.ps1`, install the BrownPants .NET tool, then run `specify extension add <extension-path> --dev`.

For OpenSpec with Codex, initialize with `openspec init --tools codex`, install BrownPants, and run `integrations/openspec/Install.ps1 -ProjectRoot .`. Invoke `$openspec-brownpants-context` before `$openspec-propose` or `$openspec-apply-change`; OpenSpec does not provide the Spec Kit lifecycle hooks, so this adapter is explicit. See [the OpenSpec adapter guide](integrations/openspec/README.md). Other SDD engines can consume the same JSON/Markdown evidence; engine-specific adapters belong under `integrations/` and should not change scanner semantics.

## Development

See [AGENTS.md](AGENTS.md) for repository contribution guidance. Keep analysis deterministic, document evidence limits, and add fixture-based tests for new inventory behavior. GitHub Actions runs the Release build, xUnit and adapter checks, then packs and smoke-tests the .NET tool on Windows, Linux, and macOS. BrownPants inventories Windows-specific project metadata on every host; it does not build MFC projects as part of inspection.
