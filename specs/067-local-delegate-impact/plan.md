# Implementation Plan: Local Delegate Impact

**Branch**: `067-local-delegate-impact` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Extend the existing Roslyn semantic graph with source-backed edges from a local method-group or lambda delegate target to each safe local invocation. Preserve `Delegate.Invoke` invocation accounting, identify delegate edges separately, and surface unsupported flows as bounded unresolved evidence that makes impact incomplete. Use the Brownfield Bug Lab to test whether the resulting evidence improves a matched SDD task; the proposed comparative acceptance gate remains under maintainer review.

## Technical Context

**Language/Version**: C# and Visual Basic analyzed by Roslyn; host targets .NET 10  
**Primary Dependencies**: Existing Roslyn compiler APIs; no new package  
**Storage**: N/A  
**Testing**: xUnit semantic compilation fixtures and the managed Brownfield Bug Lab  
**Target Platform**: .NET 10 supported platforms; fixtures include legacy .NET Framework source  
**Project Type**: CLI analyzer  
**Performance Goals**: One bounded local-reference scan per syntax tree, with no interprocedural points-to analysis  
**Constraints**: Preserve existing invocation/constructor totals; use compiler-selected symbols; bounded samples; repository-relative paths; no runtime claims  
**Scale/Scope**: Local single-assignment C#/VB delegate variables and their invocations; separate source-derived benchmark artifacts

## Constitution Check

- **Evidence First**: Emit delegate relationships only when Roslyn identifies one source-level local binding and all local uses are safe; unresolved flow stays explicit and does not claim execution.
- **Broad, Engine-Neutral Core**: Add the relationship to the shared managed graph used by CLI and SDD integrations; do not add Spec Kit-specific scanner behavior.
- **Deterministic and Reviewable Output**: Sort by path, line, symbol, and relationship kind; bound samples and redact external paths.
- **Safe Inspection by Default**: Delegate analysis remains under existing opt-in semantic/MSBuild loading; no network or source mutation is added to inspect.
- **Specified and Verified Changes**: Acceptance hash review is pending. Implementation must wait for maintainer disposition, then add fixture tests, run the release gates, and record exact evidence.

No package, project, or persistence exception is needed. The maintainer approved the comparison gate; the reviewed criteria are frozen in [acceptance-criteria.sha256](acceptance-criteria.sha256) and the review record is [acceptance-criteria-review.md](acceptance-criteria-review.md). Apply the required PR review label when this change is opened for review.

## Design

During each C#/VB syntax-tree pass, identify delegate-typed local declarations and the compiler-bound method reference or anonymous-function target in the initializer. Resolve a binding only when the local has one initializer in the same callable, is not captured, reassigned, passed, returned, stored, or otherwise referenced except as the receiver of local delegate-invocation syntax. A safe binding produces one `delegate` relationship per invocation, from the resolved target callable to the enclosing caller. Lambda body calls already use canonical anonymous-callable identities, allowing impact traversal to follow the chain through the lambda to its invoking method.

Keep existing resolved `Delegate.Invoke` invocation counts and rows unchanged. Track delegate target counts and unresolved samples separately. An unsafe or ambiguous target is never guessed; its presence makes managed impact incomplete. Extend Markdown and JSON additively and test output determinism, overload binding, impact traversal, and compatibility.

## Project Structure

```text
src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs
src/BrownPants.Cli/Analysis/RepositoryInventory.cs
src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs
src/BrownPants.Cli/Analysis/EvidenceReport.cs
src/BrownPants.Cli/Output/MarkdownReport.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
tests/BrownPants.Tests/CallImpactAnalyzerTests.cs
tests/BrownPants.Tests/EvidenceCommandTests.cs
examples/brownfield-bug-lab/evaluation/delegate-impact/
specs/067-local-delegate-impact/
```

**Structure Decision**: Extend the existing semantic analyzer and impact graph. Keep the comparative trial, scorer, frozen task, and raw results under the existing Bug Lab evaluation tree.

## Risks

- C# and VB represent method-group conversions and delegate invocations differently; operation-based symbol binding must be verified with both compiler fixtures.
- Conservatively rejecting any non-invocation local use may leave valid but complicated flows unresolved; that is preferable to inventing a target.
- A repeated benchmark on one seeded task proves only that tested scenario/model/host; it does not establish general superiority across repositories or providers.

## Research and Validation Artifacts

See [research.md](research.md), [data-model.md](data-model.md), [contracts/delegate-impact.md](contracts/delegate-impact.md), and [quickstart.md](quickstart.md). The task ledger maps each implementation and benchmark artifact to the frozen acceptance criteria.
