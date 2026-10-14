# Quickstart: OpenSpec BrownPants Context Adapter

1. Initialize OpenSpec for Codex with `openspec init --tools codex`.
2. Install BrownPants as a .NET tool or repository-local tool.
3. From the project root, run `pwsh ./integrations/openspec/Install.ps1` (Windows PowerShell: `./integrations/openspec/Install.ps1`). The project must contain `openspec/`.
4. Invoke `$openspec-brownpants-context` before `$openspec-propose` or `$openspec-apply-change`.
5. Review the report's analysis levels, then inspect the referenced OpenSpec artifacts and source before making claims.

The skill is explicit and manual. This integration does not add automatic OpenSpec lifecycle hooks.
