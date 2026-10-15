# Research: WinForms Cross-Thread UI Access Candidates

## Decisions

- **Use syntax-only analysis by default and in V1.** The command remains available on Windows, Linux, and macOS without loading MSBuild projects or a Windows Desktop targeting pack. C# and Visual Basic are parsed with the existing Roslyn dependencies.
- **Require Control type evidence.** Match explicit `System.Windows.Forms` control types and custom source types whose declared base chain reaches `Control`, `Form`, or `UserControl`. A member named `Text` or `Enabled` on an unrecognized object is not enough.
- **Classify context with narrow syntax evidence.** Recognize assignments nested in known background callbacks such as `Task.Run`, `ThreadPool.QueueUserWorkItem`, `Thread.Start`, and `BackgroundWorker.DoWork`. Everything else is `Unknown`; a local method name does not establish the caller thread.
- **Report marshaling observations without declaring safety.** A write nested in an `Invoke`/`BeginInvoke`/`InvokeAsync` or `SynchronizationContext.Post/Send` callback is labeled with that observed mechanism. `InvokeRequired` is a marker only. Do not infer a complete guard, successful dispatch, or handle lifetime.
- **Keep reports bounded and source-located.** Report paths and positions, receiver/property names, recognition evidence, context, and parse errors; never copy the matched source line into output.

## Evidence

Microsoft's WinForms guidance states that controls must be accessed from the thread that created them and documents `Control.Invoke`, `BeginInvoke`, and .NET 9+ `InvokeAsync` for marshaling. Its `InvokeRequired` documentation warns that it can return false when a handle has not yet been created, even if a cross-thread call would be unsafe.

- [How to handle cross-thread operations with controls](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-make-thread-safe-calls)
- [Control.InvokeRequired](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.invokerequired)
- [Control.Invoke](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.invoke)

## Alternatives considered

- **Treat every WinForms property write as unsafe**: rejected because ordinary UI event handlers are usually already on the UI thread and the assignment alone cannot establish thread identity.
- **Evaluate each project semantically by default**: rejected because MSBuild evaluation can execute imported targets, may require Windows Desktop targeting assets, and conflicts with safe offline-by-default inspection.
- **Suppress findings whenever `InvokeRequired` appears in the method**: rejected because the condition may not guard this write, invocation may fail, and `InvokeRequired` can be misleading before handle creation.
