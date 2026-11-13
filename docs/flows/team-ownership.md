# Team ownership example

```brownpants-org
organization brownpants-team
person owner "Product Owner"
person lead "Engineering Lead"
person qa "QA Lead"
person dev "Developer"
person analyst "Business Analyst"
person tester "QA Analyst"
person automation "Test Automation"
reports owner -> lead
reports lead -> qa
reports lead -> dev
reports lead -> analyst
reports qa -> tester
reports qa -> automation
```
