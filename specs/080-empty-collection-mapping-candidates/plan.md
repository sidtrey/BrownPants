# Implementation Plan: Empty Collection Mapping Candidates

1. Add additive inventory models for bounded mapping candidates.
2. Implement a Roslyn syntax analyzer that finds invocations with an empty collection argument and a direct member access from the same identifier in another argument. Keep evidence syntax-only and label it a review candidate.
3. Wire analysis into repository scans and render JSON/Markdown output with explicit limitations.
4. Add fixture tests for detection, meaningful/non-source mappings, comments and strings, deterministic limits, and report output.
5. Run BrownPants build and tests, then scan FullMetalFile source to verify the repaired OCR mapper is clean.

## Constraints

- Do not claim source-to-target dataflow or prove a defect without semantic/type analysis.
- Keep the scan bounded per file and by the global sample limit.
- Exclude comments and string literal text by using Roslyn syntax nodes.
