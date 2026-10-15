# Evaluation Result Format

Each agent writes one JSON object with this shape. Use repository-relative paths. For managed calls, include the fully qualified caller and target signatures exactly as resolved from source, plus the call-site line. For framework signals, set `signal` to the reported BrownPants category and use `status: candidate`. Do not list an expected fact merely because the prompt mentions the technology.

```json
{
  "schemaVersion": 1,
  "findings": [
    {
      "category": "managed-call | architecture-candidate | framework-candidate | test-identification",
      "signal": "BrownPants category or resolved call",
      "path": "relative/source/path.cs",
      "line": 12,
      "callerSignature": "optional fully qualified caller",
      "targetSignature": "optional fully qualified target",
      "status": "resolved | candidate | unresolved",
      "note": "short evidence explanation"
    }
  ],
  "limitations": ["relationships not established by available evidence"]
}
```

For one repeated architecture or framework signal at multiple locations, submit one finding
with `lines` instead of `line` (never both). Include the exact reviewed line set. The scorer
rejects grouped findings with missing or unrelated lines. Example:

```json
{
  "category": "framework-candidate",
  "signal": "NHibernate session",
  "path": "legacy/Framework/OrderDataAccess.cs",
  "lines": [13, 15],
  "status": "candidate",
  "note": "Review both candidate locations and assess their relevance to the reported defect."
}
```
