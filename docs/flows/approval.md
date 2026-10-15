# BrownPants local flowchart example

```brownpants-flow
flowchart brownfield-review
node start start "Legacy code"
node inspect process "Inspect"
node impact process "Trace impacts"
node criteria process "Set criteria"
node verify process "Verify"
node review end "Review"
edge start -> inspect
edge inspect -> impact
edge impact -> criteria
edge criteria -> verify
edge verify -> review
```

