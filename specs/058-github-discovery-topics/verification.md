# Verification: GitHub Discovery Topics

**Date:** 2026-10-15

## Reviewed set

`brownfield`, `legacy-modernization`, `code-analysis`, `repository-analysis`, `spec-driven-development`, `spec-kit`, `dotnet`, `csharp`, `visual-basic`, `cpp`, and `mfc`.

The evidence mapping is in [data-model.md](data-model.md). The primary brownfield/legacy terms match the README's product positioning. Language and workflow topics map to implemented support. `mfc` identifies a supported candidate-analysis ecosystem; it does not claim compiler-resolved native behavior. No generic AI or `static-analysis` topic was added.

The frozen acceptance hash verified as `88f8a2163f90e74bbb37d4907f35924effd794a9690efb960c61fbf474d5b085`; all 4 tasks map to the 7 acceptance criteria in the evidence ledger.

## Remote update and read-back

The repository had zero topics immediately before the update. The reviewed set was applied to `sidtrey/BrownPants`; no existing unrelated topics needed preservation. The GitHub credential was used only in memory and its value was not written to files or command output.

A cache-busted, unauthenticated public API read returned HTTP 200 and exactly the 11 reviewed names. The live [repository About page](https://github.com/sidtrey/BrownPants) renders all 11 topic links. Names satisfy GitHub's lowercase, character, and length rules; the total is within the 20-topic maximum.

An initial public API request returned a stale empty response. Adding a cache-busting query and `Cache-Control: no-cache` returned the updated set; the repository HTML independently showed each topic. Only the topic metadata changed; description, website, source, and repository visibility were left as they were.

## Limitations

Topics improve classification and search eligibility; they do not guarantee a ranking or a particular increase in visits. GitHub controls topic search and recommendation behavior.
