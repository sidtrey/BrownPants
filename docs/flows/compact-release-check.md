# Compact release check

This compact example walks a change from focused tests through human review. At its documented 1188 × 1208 pixel native display size, labels fit inside their shapes without overlap. BrownPants does not claim that passing tests alone proves release readiness.

Regenerate from the repository root with `dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/compact-release-check.md --output docs/flows/compact-release-check.png`.

```brownpants-flow
flowchart ReleaseCheck
node start start "Change ready"
node tests process "Run focused tests"
node green decision "Tests pass?"
node fix process "Fix the failing case"
node review process "Request human review"
node done end "Ready to merge"
edge start -> tests
edge tests -> green
edge green -> fix label "no"
edge fix -> tests label "retry"
edge green -> review label "yes"
edge review -> done
```
