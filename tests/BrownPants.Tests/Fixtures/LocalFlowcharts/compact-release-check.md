# Compact release check

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
