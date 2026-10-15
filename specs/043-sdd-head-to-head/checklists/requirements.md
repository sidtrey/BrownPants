# Specification Quality Checklist: Vanilla SDD and BrownPants Comparison

**Purpose**: Validate experiment fairness and reporting quality
**Created**: 2026-10-15
**Feature**: [spec.md](../spec.md)

## Experiment Quality

- [x] Same source commit and controlled bug state for each condition
- [x] Same task, model, and acceptance criteria
- [x] BrownPants evidence is the defined treatment difference
- [x] Token counts require observed provider-reported usage events

## Reporting Quality

- [x] Host, model, CLI version, outcomes, and limitations are reportable
- [x] One-run uncertainty is explicit
- [x] Raw traces stay temporary
- [x] README has plain-English and acceptance-hash requirements

## Notes

- Use the existing false-positive heuristic in `RepositoryScanner.IsTestPath` as the defect; do not fabricate a different bug to favor either condition.
