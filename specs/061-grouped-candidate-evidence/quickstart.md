# Quickstart: Grouped Candidate Evidence

Run the evidence analyzer against the bug lab:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab --format json --limit 1000
dotnet test BrownPants.sln -c Release --filter "FullyQualifiedName~RepeatedCandidateGroup"
```

Inspect `repeatedCandidateGroups`, then follow each `findingId` into `findings` to review its evidence. The fixture should group repeated C++/CLI bridge, MFC message-map, NHibernate session-factory/session/transaction locations while preserving every original finding. Grouping does not establish bug relevance. The updated controlled comparison uses `task-prompt-v4.md`; see the [contract](contracts/repeated-candidate-groups.md).
