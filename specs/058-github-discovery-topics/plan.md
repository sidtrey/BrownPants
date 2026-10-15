# Implementation Plan: GitHub Discovery Topics

**Branch**: `058-github-discovery-topics` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Set accurate public repository topics centered on BrownPants' brownfield and legacy-modernization value, then verify the saved public list. Keep the scope to repository topics; do not change source, visibility, description, or website metadata.

## Technical Context

**Language/Version**: N/A; repository metadata only
**Primary Dependencies**: GitHub repository settings and topic metadata endpoint
**Storage**: Public repository metadata
**Testing**: Validate names locally; read back saved topics and compare sets
**Target Platform**: `sidtrey/BrownPants` on GitHub.com
**Project Type**: Repository configuration
**Performance Goals**: One metadata update and one verification read
**Constraints**: At most 20 topics; lowercase letters, digits, and hyphens; each <=50 characters. Do not place credentials in command output or files. Write requires repository Administration permission; if absent, stop and report the exact permission needed.
**Scale/Scope**: 11 required topics, with any existing accurate topics preserved only if capacity permits.

## Constitution Check

- **Evidence First**: Pass. Every topic maps to a documented product purpose, language, or workflow. MFC is a target ecosystem label; README keeps its lexical-candidate limits explicit.
- **Broad, Engine-Neutral Core**: Pass. Topics identify the overall product and do not center a single SDD adapter.
- **Deterministic and Reviewable Output**: Pass. The intended list is fixed in the spec and verified by exact set comparison.
- **Safe Inspection by Default**: Pass. No analyzer behavior changes. The only remote operation is the explicitly authorized repository metadata update; no credentials are persisted.
- **Specified and Verified Changes**: Pass. Criteria are hashed, the topic set is source-reviewed, and public read-back evidence is recorded.

## Research and Design Decisions

- Use GitHub topics because they appear on the repository page and connect repositories to topic search. Follow GitHub's official limit and character rules.
- Select 11 reviewed topics: `brownfield`, `legacy-modernization`, `code-analysis`, `repository-analysis`, `spec-driven-development`, `spec-kit`, `dotnet`, `csharp`, `visual-basic`, `cpp`, and `mfc`.
- Exclude generic `ai`, model, and agent-host topics because BrownPants' core inventory does not require an LLM and host support is optional. Do not use `static-analysis`, which could overstate the product's evidence.
- The repo had no About description, website, or topics at planning time. Change topics only; description/website content is outside this queue item.
- If the current list differs at write time, preserve unrelated accurate topics where the 20-topic cap permits; remove only entries that are inaccurate or redundant with the reviewed core set.

## Project Structure

```text
specs/058-github-discovery-topics/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/topics.md
├── tasks.md
└── verification.md
IDEA-QUEUE.md
```

**Structure Decision**: This task changes repository-host metadata, not BrownPants source. Its durable specification, reviewed list, task mapping, and read-back evidence live in the feature directory; the idea queue records completion.

## Risks and Mitigations

- Missing metadata-write permission can block the API update. Check the response; do not claim success unless public read-back matches.
- Topics can be public and can overpromise support. Keep the exact list evidence-backed and state MFC/C++ limits in the README.
- Existing topics may be replaced by the API. Read the current list immediately before update and preserve accurate entries within GitHub's cap.

## Complexity Tracking

No constitution violations or new runtime dependencies.
