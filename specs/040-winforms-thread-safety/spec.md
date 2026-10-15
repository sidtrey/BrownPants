# Feature Specification: WinForms Cross-Thread UI Access Candidates

**Feature Branch**: `040-winforms-thread-safety`

**Created**: 2026-10-15

**Status**: Draft

**Input**: FIFO item 15: find Windows Forms `Control` property writes that may run away from the display/UI thread, while recognizing marshaling patterns and avoiding false claims that a property assignment alone proves a defect.

## User Scenarios & Testing

### User Story 1 - Find suspicious control property writes (Priority: P1)

A .NET maintainer wants to locate source statements that set properties on Windows Forms controls, identify writes inside recognizable background callbacks, and see whether a likely UI-thread marshaling call surrounds the write.

**Why this priority**: Cross-thread UI errors are difficult to spot in brownfield projects and can be intermittent at runtime.

**Independent Test**: Analyze small C# and Visual Basic fixtures with controls, ordinary non-control objects, background callbacks, and marshaling calls; compare stable findings and evidence classifications.

**Acceptance Scenarios**:

1. **AC-001** Given a C# or Visual Basic assignment to a property on a syntax-recognized `System.Windows.Forms.Control` or derived control, when analysis runs, then it reports the relative source path, line/column, receiver, property, and type evidence.
2. **AC-002** Given an assignment within a recognized background callback such as `Task.Run`, `ThreadPool.QueueUserWorkItem`, `Thread.Start`, or `BackgroundWorker.DoWork`, when it targets a recognized Control, then the report identifies the observed callback construct and labels thread-safety as a candidate.
3. **AC-003** Given a control assignment within a recognizable marshaling call such as `Invoke`, `BeginInvoke`, `InvokeAsync`, or `SynchronizationContext.Post/Send`, when analysis runs, then it reports the observed marshaling context; an `InvokeRequired` reference alone is only a marker and is not treated as proof that the write is marshaled.
4. **AC-004** Given a control property write with no recognized background or marshaling context, when analysis runs, then it labels execution context unknown and does not call the write unsafe or a confirmed defect.
5. **AC-005** Given a property write to an ordinary non-Control object or text in a comment/string, when analysis runs, then it does not report that write as a WinForms control property assignment.
6. **AC-006** Given a repository with C# and Visual Basic files, when Markdown or JSON output is requested, then findings from both parsed languages use equivalent evidence levels and deterministic ordering.

### User Story 2 - Review candidate findings safely (Priority: P1)

A developer or QA reviewer wants source-located, bounded findings that describe observed syntax and its limits without requiring project builds, Windows execution, or repository changes.

**Why this priority**: Findings must be useful in legacy projects while remaining safe to run on developer and CI hosts.

**Independent Test**: Run against temporary mixed-language repositories and verify stable Markdown/JSON, no source text disclosure, path containment, no network/build activity, and explicit parser limitations.

**Acceptance Scenarios**:

1. **AC-007** Given repeated analysis of identical files, when the report is generated, then Markdown and JSON contain the same stably ordered finding identities, locations, execution-context labels, and marshal observations.
2. **AC-008** Given any analysis request, when it runs, then it reads repository files only, performs no network access, project evaluation, code execution, or file writes, and labels all results as syntax-based candidates rather than confirmed runtime defects.

## Edge Cases

- Aliases, partial classes, inherited control types, fields declared in another file, and chained member access may prevent local syntax from proving the control type.
- `InvokeRequired` can be false before the target handle exists; its presence does not prove safe marshaling.
- `Invoke`/`BeginInvoke` calls can still be misused or fail when a handle is unavailable or disposed.
- Background execution can be introduced by external callers or custom abstractions not visible in a local syntax tree.
- An assignment can occur inside code that is safe because of a captured UI `SynchronizationContext`, or unsafe despite a nearby marshal-related name.
- Native MFC/COM dispatch and C++/CLI thread behavior are outside this WinForms-focused analysis.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST inspect C# and Visual Basic syntax for property assignments on source-recognized WinForms `Control` types and derived types.
- **FR-002**: Each finding MUST include repository-relative path, line/column, receiver/property identity, and the evidence used to recognize a Control target.
- **FR-003**: BrownPants MUST classify recognizable background callback syntax and recognizable marshaling callback syntax separately; unknown execution context MUST remain unknown.
- **FR-004**: BrownPants MUST report an `InvokeRequired` marker without treating it alone as proof of a guard or safe access.
- **FR-005**: Reports MUST describe findings as candidates and MUST NOT label a cross-thread defect from a property assignment alone.
- **FR-006**: The command MUST emit deterministic bounded Markdown and JSON without copying source text into the report.
- **FR-007**: Default analysis MUST parse source only without building/evaluating projects, accessing the network, executing repository code, or modifying files.
- **FR-008**: C# and Visual Basic parse errors, unsupported constructs, and incomplete type evidence MUST be reflected as coverage limits rather than hidden.

### Key Entities

- **UI access candidate**: A source-located property assignment whose receiver is recognized by local syntax as a WinForms control or derived type.
- **Execution-context evidence**: A bounded syntax marker for a known background callback or unknown context; it is not a complete call graph.
- **Marshaling evidence**: A recognized dispatcher/invoke construct enclosing the assignment, or a weaker `InvokeRequired` marker, reported distinctly.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Fixture tests identify all expected Control property assignments in C# and Visual Basic and omit ordinary non-Control property assignments and comment/string text.
- **SC-002**: Every reported candidate states its execution-context category and any recognized marshaling evidence; no test expects an unqualified “thread-safe” or “unsafe” conclusion.
- **SC-003**: Equivalent repeated runs produce byte-stable Markdown and JSON finding records, bounded by the requested result limit.
- **SC-004**: The command completes without project evaluation or network calls and leaves the inspected repository unchanged.
- **SC-005**: Legacy .NET Framework source is analyzable on the .NET 10 host when it uses supported C#/VB syntax; findings remain source-level candidates.

## Assumptions

- V1 uses source syntax to recognize WinForms control types and callback/marshaling patterns. It does not resolve runtime threads, callers, custom dispatch abstractions, or control handle lifecycle.
- The command is useful as a review and test-planning aid; developers validate each candidate in its call context.
- `InvokeAsync` applies only to .NET 9+ WinForms; older frameworks may rely on `Invoke` or `BeginInvoke`.
