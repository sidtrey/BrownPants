# Research: C# Consumption of C++/CLI Libraries

## Decision: use source-only reference evidence

- **Decision**: Read C# project references and legacy assembly `Reference`/`HintPath` XML, then associate only discovered CLR-enabled native projects and source declarations. Do not evaluate the projects or load their output binaries.
- **Rationale**: The repository's default inspect path is local/read-only and works with legacy projects. Evaluating imports or loading assemblies would add execution and platform/toolchain risk and could falsely imply a working build.
- **Alternatives considered**: MSBuildWorkspace project evaluation and C++/CLI compilation were rejected because they can execute imports/targets and require Visual Studio native toolchains unavailable on many review hosts.

## Decision: model consumer calls as a separate candidate direction

- **Decision**: Add a relation kind for C# consumer call -> C++/CLI wrapper method and retain the current C++/CLI handle call -> C#/VB relation as a separate direction.
- **Rationale**: The existing `cppcli-managed-call` edge describes C++/CLI-to-managed calls. Reusing that kind for the reverse direction would invert its meaning and make reports/impact ambiguous.
- **Alternatives considered**: Encoding the C# consumer as an existing native-flow edge without a distinct kind was rejected because clients could not tell which side was the source or interpret direction correctly.

## Decision: match references conservatively

- **Decision**: Match by explicit C# receiver/type and method evidence plus a static project/reference path or assembly identity that maps to a discovered CLR-enabled native project. Preserve multiple project/type/method locations as ambiguous.
- **Rationale**: A name-only match is too weak in repositories with duplicate APIs. Reference metadata narrows the candidate without claiming that configuration conditions, binary builds, or runtime loading succeeded.
- **Alternatives considered**: Match any same-named C++/CLI method across the repository was rejected as likely to create false links. Proving the target through an evaluated compiler graph was rejected for safety and portability.

## Pinned example evidence

At Microsoft `VCSamples` commit `9e1d4475555b76a17a3568369867f1d7b6cc6126`:

- `VC2010Samples/CLR/stlclr/StlClr Sample/StlClr Sample/Form1.cs` creates `StlClrLibrary.Tools` and calls `Shuffle`, `BubbleSort`, `BubbleStep`, and `BubbleSortNative`.
- `VC2010Samples/CLR/stlclr/StlClr Sample/StlClr Sample/StlClr Sample.csproj` declares `Reference Include="StlClrLibrary, ..."` and `HintPath>..\debug\StlClrLibrary.dll`.
- The input feature record identifies `StlClrLibrary.h/.cpp` as the wrapper and native-call implementation. The connector did not resolve the proposed header/implementation paths; those exact upstream filenames remain unverified here. Regression coverage will pin the observed forms locally and verification will state that the upstream native projects were not built.

The C# and project files were fetched from the pinned commit. This is source evidence, not a claim that the sample builds on the current host.

## Existing repository evidence

- `DotNetProjectAnalyzer` currently reports target frameworks, language versions, and only `ProjectReference` paths; it does not retain legacy assembly `Reference`/`HintPath` entries.
- `NativeProjectAnalyzer` reports CLR support and project references but not output assembly identity.
- `CSharpAnalyzer` parses C# with Roslyn syntax, retains declaration and invocation samples, and holds a bounded declaration index. Invocation samples currently omit receiver type and source column.
- `NativeFlowAnalyzer` extracts supported MFC, qualified native, and C++/CLI typed-handle-to-managed candidates. Its evidence limits explicitly state that C++/CLI-to-managed compiler binding and project references are not resolved.
- `CallImpactAnalyzer` currently traces incoming managed/native candidate paths; it does not start from a changed C# consumer and follow an outgoing C++/CLI wrapper path.
- Existing managed/native contract and fixture behavior are covered by features 054-056 and 064-067. New direction-specific tests must preserve those behaviors.
