# Quickstart: Call-Trace Change Impact

Use explicit changed source paths from a reviewed diff:

```powershell
brownpants impact . --changed src/Services/OrderService.cs --format markdown
brownpants impact . --changed src/Services/OrderService.cs --changed src/Data/OrderRepository.cs --format json --limit 10000
```

The command loads managed projects through MSBuildWorkspace. Review the repository first: project imports and design-time targets may execute. BrownPants does not run tests or edit files.

Review `IsComplete`, load/unresolved issues, caller depth, affected paths/projects, and test-candidate call sites together. Candidate tests are navigation leads only. Graph counts do not estimate engineering effort. See [the impact report contract](contracts/impact-report.md).