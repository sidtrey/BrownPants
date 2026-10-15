# Tasks: GitHub Discovery Topics

**Input**: Design documents from `specs/058-github-discovery-topics/`

## Phase 1: Review and apply metadata (User Story 1)

**Independent test**: Public repository topics match the reviewed set, comply with GitHub constraints, and are justified by product evidence.

- [x] T001 [US1] Validate the 11 required topic names against GitHub's constraints and map each to README or implemented support in `specs/058-github-discovery-topics/data-model.md`.
- [x] T002 [US1] Read existing public topics, preserve any accurate non-duplicate topics within GitHub's 20-topic limit, and apply the reviewed list to `sidtrey/BrownPants` without exposing credentials.
- [x] T003 [US1] Read back public topic metadata, compare the saved set to the reviewed set, inspect the repository About page, and record evidence and limits in `specs/058-github-discovery-topics/verification.md`.
- [x] T004 [US1] Mark queue item 19 as implemented in `IDEA-QUEUE.md`, then validate the unchanged acceptance hash and completed task ledger.

## Dependencies & Execution Order

- T001 precedes the remote write in T002.
- T002 must succeed before T003 can verify the published state.
- T004 follows successful read-back; if permissions block T002, leave the queue item open and record the blocker without claiming completion.

## Implementation Strategy

Complete the reviewed topic list first, make one authorized metadata update, verify it from the public endpoint and repository page, then record the queue status. No source-code or runtime behavior changes are in scope.
