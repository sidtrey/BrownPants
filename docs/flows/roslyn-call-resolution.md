# How BrownPants follows a C# call

This map summarizes `CSharpSemanticAnalyzer.AnalyzeCompilations` in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`. The analyzer uses Roslyn symbols and source locations to connect calls with declarations when projects load successfully.

```brownpants-flow
flowchart roslyn-call-resolution
node start start "Project compilation available"
node trees process "Visit each source file in the project"
node model process "Ask Roslyn for that file's semantic model"
node syntax process "Find declarations and call expressions"
node call decision "Does Roslyn bind this call to a method?"
node unresolved process "Record unresolved call and candidate reason"
node target process "Record target symbol and source location"
node unresolvedResult input-output "Unresolved call kept with reason"
node graph input-output "Resolved edge kept for impact review"
edge start -> trees
edge trees -> model
edge model -> syntax
edge syntax -> call
edge call -> unresolved label "No"
edge call -> target label "Yes"
edge unresolved -> unresolvedResult
edge target -> graph
```

The diagram shows the analyzer's declared processing path. It does not show runtime execution, and incomplete project loading or unsupported dynamic dispatch can leave relationships unresolved.
