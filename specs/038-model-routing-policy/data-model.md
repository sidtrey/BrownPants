# Data Model: Task-Based Model Guidance

## Policy document

The policy is UTF-8 JSON, schema version `1`, at `.brownpants/models.json` by default. An explicit relative policy path can be used for validation/reporting, but it must remain inside the selected repository.

```json
{
  "schemaVersion": 1,
  "profiles": [
    {
      "id": "implementation",
      "categories": ["implementation", "complex-debugging"],
      "qualityTier": "high",
      "rationale": "Changes require code understanding and regression analysis.",
      "models": {
        "vscode": "Team selected model name",
        "visual-studio": "Team selected model name",
        "claude": "Team selected model name",
        "codex": "Team selected model name"
      },
      "instructions": "Use the approved plan and preserve its acceptance criteria."
    }
  ]
}
```

## Validation rules

- Reject schema versions other than `1`, unknown JSON properties, duplicate profile IDs, duplicate task categories, empty required strings, and categories outside the supported fixed category set.
- Supported categories: `analysis`, `complex-debugging`, `specification`, `planning`, `task-breakdown`, `implementation`, `testing`, `review`, `documentation`, `routine-maintenance`.
- `qualityTier` is exactly `economy`, `balanced`, or `high`; it is a team label, not a cost or quality claim.
- `models` is a non-empty map from lowercase host slugs to opaque non-empty model identifiers. Known slugs are `vscode`, `visual-studio`, `claude`, and `codex`; unknown slugs remain reportable as manual/unverified preferences but cannot be exported by BrownPants.
- Model names are opaque non-empty strings. Do not compare them to a live provider catalog or trim/normalize their internal spelling.
- A host export requires a non-empty model mapping for that host for every exported profile.
- Bound policy to 128 KiB, 64 profiles, 32 total categories, 80 characters for IDs/categories, 120 characters for model names, 1000 characters for rationale, and 4000 characters for instructions.

## Export targets

One generated file per profile. Profile IDs are validated as lowercase kebab-case before being used in paths. The exporter records no secrets or host-global configuration.

- VS Code/Copilot writes `.github/agents/brownpants-<id>.agent.md`.
- Visual Studio 18.4+ writes `.github/agents/brownpants-visual-studio-<id>.agent.md`; the display name also identifies Visual Studio. This avoids overwriting VS Code output in the shared directory.
- Visual Studio versions below 18.4 are not a supported export target.
