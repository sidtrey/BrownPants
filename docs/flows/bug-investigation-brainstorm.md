# Bug investigation brainstorm

```brownpants-brainstorm
brainstorm brownfield-bug
idea root "Why does this bug happen?"
idea repro "Make it repeatable"
idea call "Trace who calls this"
idea test "Check related tests"
idea impact "Find affected features"
idea evidence "Keep source and line links"
idea criteria "Write exact acceptance checks"
idea fix "Compare safe repair options"
idea qa "Give QA a test handoff"
branch root -> repro
branch root -> call
branch root -> test
branch call -> impact
branch call -> evidence
branch repro -> criteria
branch impact -> fix
branch test -> qa
```
