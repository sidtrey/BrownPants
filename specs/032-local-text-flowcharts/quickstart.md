# Quickstart: Create a Local Flowchart

Create `docs/flows/account-approval.md`:

````markdown
# Account approval

```brownpants-flow
flowchart account-approval
node start start "Start request"
node review decision "Approved?"
node grant process "Grant access"
node deny end "Show rejection"
edge start -> review
edge review -> grant label "yes"
edge review -> deny label "no"
```
````

Generate a portable PNG or an editable diagrams.net file:

```powershell
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/account-approval.md --output docs/flows/account-approval.png
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/account-approval.md --output docs/flows/account-approval.drawio --format drawio
```

The command is local-only. PNG is rasterized by BrownPants' managed .NET code. The `.drawio` option writes native XML that diagrams.net Desktop can import when that app is allowed by local policy. BrownPants does not invoke the diagrams.net application or send source to it. Existing outputs are preserved unless `--overwrite` is supplied. See [the input/output contract](contracts/local-flowchart.md) for the supported syntax, path checks, and limits.

After implementation, validate with:

```powershell
dotnet test BrownPants.sln -c Release --filter-class BrownPants.Tests.LocalFlowchartTests
dotnet test BrownPants.sln -c Release
```
