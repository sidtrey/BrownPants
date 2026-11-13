# Quickstart: C#-First C++/CLI Evidence

Build and run the feature checks:

```powershell
dotnet build BrownPants.sln -c Release
dotnet test BrownPants.sln -c Release --no-build
```

Inspect the source-only fixture without evaluating its project files:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab --format markdown --limit 1000
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab --changed legacy/ManagedConsumer/LegacyConsumer.cs --format json --limit 1000
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab --changed legacy/ManagedConsumer/LegacyConsumer.cs --format markdown --limit 1000
```

Expected output shows C# consumer locations, static project/reference evidence, a candidate C++/CLI wrapper location, and supported qualified native calls. Missing or ambiguous targets and binaries remain explicit. No build or runtime claim is made about the legacy interop chain.

The focused JSON relation summaries for the checked-in fixture are:

```text
candidate csharp-cppcli-call
  legacy/ManagedConsumer/LegacyConsumer.cs:8 -> legacy/Bridge/StlClrLibrary.cpp:11
candidate qualified-native-call
  legacy/Bridge/StlClrLibrary.cpp:13 -> legacy/Bridge/StlClrLibrary.cpp:2 (Native::BubbleSort)
```

The first edge is matched from the legacy `Reference` identity and `HintPath`. Focused impact reports both links as source candidates and remains incomplete for managed callers because semantic analysis was not requested.
