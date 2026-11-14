# Acceptance Criteria Review: Indirect Placeholder Accessors

**Evidence refreshed**: 2026-11-13

**Status**: Approved under the user's one-maintainer waiver on 2026-11-13; SC-003 and baseline updated.

## Previous digest

`397995bc55aa20b229dfc23070f0875a85be6eb1a71339b615a87601facbae2d`

## Proposed digest

`769703724f3c7cca32a44be09d59e7c6fb4efd2b434416e8f201224cbe6c0e21`

## Reason

The current FullMetalFile source assigns `IndexingSessions` in the `PostgreSqlUnitOfWork` constructor. A fresh parser-backed report identifies four indirect placeholder candidates on that class: `Templates`, `FieldDefinitions`, `TemplateRelationships`, and `DocumentVersions`. It does not classify that class's constructor-initialized `IndexingSessions` property as a placeholder. The full report also contains similarly named throwing accessors in test fixtures, so the criterion must scope the exclusion to `PostgreSqlUnitOfWork`.

## Changed criteria

Replace SC-003 with:

> A parser-backed FullMetalFile scan reports `Templates`, `FieldDefinitions`, `TemplateRelationships`, and `DocumentVersions` as indirect placeholder candidates on `PostgreSqlUnitOfWork` and does not classify that class's constructor-initialized `IndexingSessions` property as a placeholder.

## Approval state

The user directed continuation under a one-maintainer waiver after relaxing PR requirements. This approval applies to the evidence-backed SC-003 wording above. The baseline was refreshed to match the revised criterion; no PR review or repository label gate was required under that waiver.
