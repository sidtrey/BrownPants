# Release pipeline review

This large example follows a proposed change through compilation, tests, security review, human pull-request approval, deployment, and recovery paths. BrownPants renders it as four ordered 1800-pixel tiles with 128-pixel shared edges. The manifest identifies exact origins and overlap. The renderer reports two remaining crossings where retry routes return upstream and prints their canvas coordinates; review those locations alongside the editable source.

Regenerate from the repository root with `dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/tiled-release-pipeline.md --output docs/flows/tiled-release-pipeline.png --tile-size 1800 --overlap 128 --overwrite`.

```brownpants-flow
flowchart ReleasePipeline
node start start "Change proposed"
node intake process "Record scope and acceptance criteria"
node compile process "Compile affected projects"
node compileOk decision "Compilation succeeds?"
node compileFix process "Correct compile errors"
node unit process "Run focused unit tests"
node unitOk decision "Focused tests pass?"
node unitFix process "Investigate failed assertions"
node full process "Run complete test suite"
node fullOk decision "Full suite passes?"
node fullFix process "Resolve regressions"
node audit process "Review dependency and security changes"
node auditOk decision "Review approved?"
node revise process "Update implementation or evidence"
node package process "Create release candidate"
node smoke process "Run smoke checks"
node smokeOk decision "Smoke checks pass?"
node smokeFix process "Repair release packaging"
node notes process "Prepare release notes and QA handoff"
node pr process "Open pull request"
node approve decision "Human approval granted?"
node revisePr process "Address reviewer feedback"
node merge process "Merge approved change"
node deploy process "Deploy release"
node monitor process "Monitor production signals"
node healthy decision "Release healthy?"
node rollback process "Roll back and investigate"
node done end "Release complete"
edge start -> intake
edge intake -> compile
edge compile -> compileOk
edge compileOk -> compileFix label "no"
edge compileFix -> compile label "retry"
edge compileOk -> unit label "yes"
edge unit -> unitOk
edge unitOk -> unitFix label "no"
edge unitFix -> unit label "retry"
edge unitOk -> full label "yes"
edge full -> fullOk
edge fullOk -> fullFix label "no"
edge fullFix -> full label "retry"
edge fullOk -> audit label "yes"
edge audit -> auditOk
edge auditOk -> revise label "no"
edge revise -> compile label "retry"
edge auditOk -> package label "yes"
edge package -> smoke
edge smoke -> smokeOk
edge smokeOk -> smokeFix label "no"
edge smokeFix -> package label "retry"
edge smokeOk -> notes label "yes"
edge notes -> pr
edge pr -> approve
edge approve -> revisePr label "no"
edge revisePr -> compile label "update"
edge approve -> merge label "yes"
edge merge -> deploy
edge deploy -> monitor
edge monitor -> healthy
edge healthy -> rollback label "no"
edge rollback -> intake label "recovery"
edge healthy -> done label "yes"
```
