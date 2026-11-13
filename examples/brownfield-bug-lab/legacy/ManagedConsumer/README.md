# C# to C++/CLI consumer fixture

This source-only fixture reproduces the evidence shape recorded from Microsoft `VCSamples` at commit `9e1d4475555b76a17a3568369867f1d7b6cc6126`:

- The legacy C# project names `StlClrLibrary` in a `Reference` and points `HintPath` at a relative DLL.
- The C# consumer constructs `StlClrLibrary.Tools` and calls `BubbleSortNative`.
- The discovered CLR-enabled native project names the same output assembly; its C++/CLI wrapper calls a qualified native method.

The fixture is an analyzer test fixture, not a buildable replacement for the upstream WinForms/native sample. BrownPants reads the source and project XML as candidates. It does not evaluate MSBuild conditions, create the DLL, compile C++/CLI, prove compiler binding, or claim runtime execution.
