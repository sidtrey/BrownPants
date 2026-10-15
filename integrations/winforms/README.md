# WinForms UI Thread Access Review

Run `brownpants winforms analyze <repository>` to find source-level property assignments on recognizable Windows Forms controls. Use `--format json` for automation and `--limit 1..10000` to bound displayed findings.

```powershell
dotnet run --project src/BrownPants.Cli -- winforms analyze .
dotnet run --project src/BrownPants.Cli -- winforms analyze C:\src\legacy-app --format json
```

BrownPants parses C# and Visual Basic with Roslyn on Windows, macOS, or Linux; the inspected project may target .NET Framework. It does not build the project, load Windows Desktop, evaluate MSBuild, run source code, write into the inspected repository, or use the network.

Each finding is a **candidate**, with a path, line, property and type evidence. Recognized callback syntax such as `Task.Run` is reported separately from dispatcher callback syntax such as `Control.Invoke`, `BeginInvoke`, or `InvokeAsync`. An `InvokeRequired` occurrence is only a marker. Unknown callers, captured synchronization contexts, custom dispatchers, aliases, handle lifecycle, and runtime thread identity are outside syntax-only evidence. Review each candidate in context; no finding proves that code is unsafe or safe.

Reports omit source-line excerpts. Parse diagnostics and result limits show coverage gaps. No additional packages or Windows-only dependencies are required. See the [feature specification](../../specs/040-winforms-thread-safety/spec.md).
