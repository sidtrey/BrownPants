# BrownPants OpenSpec adapter

This adapter adds repository evidence to OpenSpec's Codex workflow using the separate `$openspec-brownpants-context` skill. OpenSpec stores current behavior specs under `openspec/specs/` and proposed changes under `openspec/changes/<change>/`; the skill uses the paths present in the target repository.

## Install

Initialize OpenSpec with Codex, install BrownPants as a .NET tool, and run the installer from the BrownPants checkout:

```powershell
openspec init --tools codex
./integrations/openspec/Install.ps1 -ProjectRoot .
```

The script creates `.agents/skills/openspec-brownpants-context/SKILL.md`. It is safe to rerun when the installed file is unchanged. If the destination differs, the installer preserves it and reports a conflict. It does not modify OpenSpec-managed skills or access the network.

## Use

Invoke `$openspec-brownpants-context` before `$openspec-propose` or `$openspec-apply-change`. The adapter is explicit; OpenSpec does not provide the Spec Kit lifecycle hooks used by `integrations/speckit/`.

The skill reads BrownPants' Markdown inventory, then asks the agent to verify relevant claims in the repository. Roslyn analysis covers C# and VB.NET syntax and candidate patterns; C/C++ matching is lexical; other listed languages remain inventory-only unless a report says otherwise. Candidate signals are leads, not proof of runtime relationships.

Run the installer regression checks from the BrownPants repository with `pwsh -NoProfile -File tests/OpenSpecInstallerTests.ps1`.
