# Quickstart: Instruction Context Frames

## Map instruction sources

Run the CLI against a repository to list recognized instruction files, their observed or inferred scope, headings, size, and limitations:

```powershell
brownpants instructions . --format markdown
brownpants instructions . --format json --source .agents/custom-guidance.md
```

## Create and validate a frame

Draft concise response priorities using the map in JSON. The input shape is documented in `contracts/instruction-context-frame.schema.json`:

```json
{
  "budget": { "unit": "words", "requested": 120 },
  "points": [
    { "text": "Keep detailed rules available and consult them when relevant.",
      "sources": [{ "path": "AGENTS.md", "anchor": "repository-guidelines" }] }
  ],
  "reviewItems": []
}
```

Render it with `brownpants frame create . ./frame-draft.json docs/agent-context-frame.md`, then check freshness with `brownpants frame validate . docs/agent-context-frame.md`. BrownPants validates every source and heading, adds current source identities, and reports budget overflow. An existing output is protected unless `--overwrite` is explicitly requested.

Before relying on a frame, validate its references and check for stale sources. Read the full linked section whenever the task depends on details, exceptions, or scoped rules. Review conflicts and any budget shortfall manually.

## Interpret estimates

Byte counts are measured. Token counts are approximate unless a supported tokenizer is selected. A frame reduces prompt load only when the agent host is configured to load it in place of duplicated source content; BrownPants cannot guarantee host behavior or model compliance.
