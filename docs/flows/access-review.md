# Access request review

```brownpants-flow
flowchart AccessReview
node start start "Access request received"
node parse process "Read account and requested scope"
node valid decision "Request fields valid?"
node reject end "Return validation errors"
node policy process "Load applicable access policy"
node known decision "Known account?"
node register process "Create pending account record"
node risk decision "Risk review required?"
node manual process "Assign security reviewer"
node grant process "Grant least-privilege access"
node audit process "Write decision and evidence to audit log"
node notify process "Notify requester and service owner"
node done end "Request complete"
node retry process "Correct request and resubmit"
edge start -> parse
edge parse -> valid
edge valid -> reject label "no"
edge valid -> policy label "yes"
edge policy -> known
edge known -> register label "no"
edge known -> risk label "yes"
edge register -> risk
edge risk -> manual label "yes"
edge risk -> grant label "no"
edge manual -> grant label "approved"
edge manual -> retry label "denied"
edge retry -> parse label "retry"
edge grant -> audit
edge audit -> notify
edge notify -> done
```
