# BrownPants Spec Kit extension

This extension adds `speckit.brownpants.context` and runs it automatically before Spec Kit's constitution, specify, clarify, plan, checklist, tasks, analyze, implement, and task-to-issues commands, and after implementation for completion review. It uses BrownPants' deterministic report as a starting map, then instructs the agent to verify claims against source files and test results.

## Build the manifest

Spec Kit requires the extension repository URL and license identifier in `extension.yml`. Generate that file from the checked-in template:

```powershell
.\integrations\speckit\BuildExtension.ps1 -RepositoryUrl <https-url> -License <identifier>
```

Pack and install BrownPants in the target repository. A local tool path works with the context command’s `.tools` fallback:

```powershell
dotnet pack src/BrownPants.Cli -c Release -o ./artifacts
dotnet tool install --tool-path ./.tools --add-source ./artifacts BrownPants.Cli --version 0.1.0
```

Initialize the project with Spec Kit, then install this local extension:

```powershell
specify init --here --integration <agent>
specify extension add .\integrations\speckit --dev
specify extension list
```

For Codex CLI, use `specify init --here --integration codex --integration-options="--skills"`. Spec Kit 1.0+ installs command skills in `.agents/skills`; the BrownPants extension refreshes context before nine lifecycle commands.

The active agent must be restarted or refreshed if its command list does not update. The `--semantic` scan is not automatic because MSBuild project evaluation may execute imported targets.
