# Requirements Quality Checklist: Impact Graph Coverage

**Purpose**: Review acceptance criteria for precise, testable caller coverage and honest truncation.
**Feature**: [spec.md](../spec.md)

## Testability

- [ ] CHK001 The display-sample limit and internal graph bound are described as separate conditions.
- [ ] CHK002 A caller beyond the public sample has a source path, call-site line, and depth outcome.
- [ ] CHK003 Internal graph truncation has a measurable retained/omitted outcome and incomplete status.

## Compatibility and Evidence

- [ ] CHK004 Method and constructor relationships are both covered.
- [ ] CHK005 Public semantic JSON and its caller-selected sample limit are explicitly protected.
- [ ] CHK006 Workspace issues and unresolved-call evidence remain distinct from both sampling limits.
