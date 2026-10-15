# Quickstart: WinForms UI Access Candidates

Analyze a legacy WinForms repository without building it or running Windows Forms:

```powershell
brownpants winforms analyze C:\src\LegacyClient --format markdown
brownpants winforms analyze C:\src\LegacyClient --format json --limit 1000
```

Review each candidate at its source location. Findings identify the Control-type evidence, whether a known background callback contains the write, and which marshaling syntax was observed. `Unknown` means this scan could not determine the caller's thread. A report never declares a candidate safe or a confirmed defect. The command is read-only and works from any host with the .NET 10 tool.
