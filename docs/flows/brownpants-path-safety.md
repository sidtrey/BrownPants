# BrownPants path safety

**Plain English:** `ResolveInside` rejects blank or rooted input, combines a relative path with the selected repository, and checks that the normalized result stays below the repository root. It selects a path-comparison rule for the current operating system, then returns the safe full path or throws an argument error.

This is a BrownPants-on-BrownPants Roslyn control-flow chart: 17 CFG blocks, five decision blocks, and two explicit throw paths. Roslyn does not model implicit runtime exceptions. The editable `.drawio` file keeps the full compiler operations and source-line details.

Regenerate locally from the repository root (project evaluation may run imported MSBuild targets):

```powershell
brownpants chart flow . --source src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs --method 'global::BrownPants.Cli.Analysis.LocalFlowchartCommand.ResolveInside(string, string, string)' --semantic --output docs/flows/brownpants-path-safety.png --overwrite
```
