$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-HookBlock {
    param([string]$Yaml, [string]$Event)
    $pattern = "(?ims)^  $([regex]::Escape($Event)):\r?\n(?<body>.*?)(?=^  [a-z_]+:\r?\n|^tags:|\z)"
    $match = [regex]::Match($Yaml, $pattern)
    if (-not $match.Success) { throw "Hook '$Event' is missing." }
    return $match.Groups['body'].Value
}

$events = @('before_constitution', 'before_specify', 'before_clarify', 'before_plan', 'before_checklist', 'before_tasks', 'before_analyze', 'before_implement', 'before_taskstoissues', 'after_implement')
$bugEvent = 'before_bug_assess'
$beforeEvents = @($events | Where-Object { $_ -like 'before_*' })
Assert-True ($beforeEvents.Count -eq 9 -and @($events | Where-Object { $_ -like 'after_*' }).Count -eq 1) 'Expected nine required lifecycle pre-hooks and one post-implementation hook, plus the optional bug-assessment pre-hook.'
$integrationIndex = Get-Content -LiteralPath (Join-Path $repositoryRoot 'integrations/README.md') -Raw
Assert-True ($integrationIndex.Contains('before constitution, specification, clarification, planning, checklist, task, analysis, implementation, and issue-sync phases, and after implementation')) 'integrations/README.md must document pre- and post-implementation coverage.'
Assert-True ($integrationIndex.Contains('optional bug-assessment evidence hook')) 'integrations/README.md must document the optional bug-assessment evidence hook.'

$manifestPaths = @(
    'integrations/speckit/extension.yml',
    'integrations/speckit/extension.yml.template',
    '.specify/extensions/brownpants/extension.yml',
    '.specify/extensions/brownpants/extension.yml.template'
)
foreach ($relativePath in $manifestPaths) {
    $manifest = Get-Content -LiteralPath (Join-Path $repositoryRoot $relativePath) -Raw
    Assert-True ($manifest -match '(?m)^  speckit_version:\s*">=1\.0\.0"\s*$') "$relativePath must require a Spec Kit version that supports all lifecycle events."
    $manifestHookCount = [regex]::Matches($manifest, '(?m)^  (?:before|after)_[a-z_]+:').Count
    Assert-True ($manifestHookCount -eq ($events.Count + 1)) "$relativePath should register eleven hooks, including the optional bug-assessment hook."
    foreach ($event in $events) {
        $block = Get-HookBlock -Yaml $manifest -Event $event
        Assert-True ($block -match '(?m)^\s+command:\s*speckit\.brownpants\.context\s*$') "$relativePath/$event targets the wrong command."
        Assert-True ($block -match '(?m)^\s+optional:\s*false\s*$') "$relativePath/$event must be mandatory."
    }
    $bugBlock = Get-HookBlock -Yaml $manifest -Event $bugEvent
    Assert-True ($bugBlock -match '(?m)^\s+command:\s*speckit\.brownpants\.bug-evidence\s*$') "$relativePath/$bugEvent targets the wrong command."
    Assert-True ($bugBlock -match '(?m)^\s+optional:\s*true\s*$') "$relativePath/$bugEvent must remain optional."
    Assert-True ($manifest -match '(?m)^\s+- name:\s*speckit\.brownpants\.bug-evidence\s*$') "$relativePath must provide the bug-evidence command."
    $commandCount = [regex]::Matches($manifest, '(?m)^\s+- name:\s*speckit\.brownpants\.').Count
    Assert-True ($commandCount -eq 2) "$relativePath should provide only the context and bug-evidence commands."
    Assert-True ($manifest -notmatch '(?m)^\s+- name:\s*speckit\.brownpants\.bug-(?:assess|fix|test)\s*$') "$relativePath must not introduce competing BrownPants bug workflow commands."
}

$projectHooks = Get-Content -LiteralPath (Join-Path $repositoryRoot '.specify/extensions.yml') -Raw
$projectHookCount = [regex]::Matches($projectHooks, '(?m)^  (?:before|after)_[a-z_]+:').Count
Assert-True ($projectHookCount -eq ($events.Count + 1)) '.specify/extensions.yml should contain all eleven BrownPants hooks.'
foreach ($event in $events) {
    $block = Get-HookBlock -Yaml $projectHooks -Event $event
    Assert-True ($block -match '(?m)^\s+- extension:\s*brownpants\s*$') ".specify/extensions.yml/$event must belong to BrownPants."
    Assert-True ($block -match '(?m)^\s+command:\s*speckit\.brownpants\.context\s*$') ".specify/extensions.yml/$event targets the wrong command."
    Assert-True ($block -match '(?m)^\s+optional:\s*false\s*$') ".specify/extensions.yml/$event must be mandatory."
}
$projectBugBlock = Get-HookBlock -Yaml $projectHooks -Event $bugEvent
Assert-True ($projectBugBlock -match '(?m)^\s+- extension:\s*brownpants\s*$') ".specify/extensions.yml/$bugEvent must belong to BrownPants."
Assert-True ($projectBugBlock -match '(?m)^\s+command:\s*speckit\.brownpants\.bug-evidence\s*$') ".specify/extensions.yml/$bugEvent targets the wrong command."
Assert-True ($projectBugBlock -match '(?m)^\s+optional:\s*true\s*$') ".specify/extensions.yml/$bugEvent must remain optional."

$bugCommandPaths = @(
    'integrations/speckit/commands/bug-evidence.md',
    '.specify/extensions/brownpants/commands/bug-evidence.md'
)
foreach ($relativePath in $bugCommandPaths) {
    $bugCommand = Get-Content -LiteralPath (Join-Path $repositoryRoot $relativePath) -Raw
    foreach ($requiredText in @('BUG_DIR', 'If either value is missing', 'brownpants investigate', 'brownpants-evidence.md', 'Do not fetch', 'up to 20', '1000 characters', 'secrets', 'credentials', 'Do not interpolate report text into a shell string', 'If the command fails', 'Do not silently overwrite', 'skip', 'navigation leads', 'no causal')) {
        Assert-True ($bugCommand -match [regex]::Escape($requiredText)) "$relativePath must include '$requiredText' guidance."
    }
}

$contextPaths = @(
    'integrations/speckit/commands/context.md',
    '.specify/extensions/brownpants/commands/context.md',
    '.agents/skills/speckit-brownpants-context/SKILL.md'
)
foreach ($relativePath in $contextPaths) {
    $context = Get-Content -LiteralPath (Join-Path $repositoryRoot $relativePath) -Raw
    Assert-True ($context -match 'inspect \. --format markdown --limit 10') "$relativePath must request bounded initial context."
    Assert-True ($context.Contains('./.tools/brownpants.exe inspect . --format markdown --limit 10')) "$relativePath must use the repository-relative Windows shim path."
    Assert-True ($context -match '(?i)higher `--limit`') "$relativePath must explain how to expand evidence when needed."
    foreach ($phase in @('constitution', 'specify', 'clarify', 'plan', 'checklist', 'tasks', 'analyze', 'implement', 'taskstoissues')) {
        Assert-True ($context -match "(?i)For ``$phase``") "$relativePath lacks phase-specific '$phase' guidance."
    }
    Assert-True ($context.Contains('After `implement`')) "$relativePath lacks post-implementation review guidance."
    Assert-True ($context.Contains('brownpants instructions')) "$relativePath lacks instruction-source mapping guidance."
    Assert-True ($context.Contains('brownpants frame create') -and $context.Contains('brownpants frame validate')) "$relativePath lacks response-frame creation and freshness guidance."
    Assert-True ($context.Contains('host is configured')) "$relativePath must explain host-dependent context savings."
    Assert-True ($context.Contains('acceptance-criteria.sha256') -and $context.Contains('scripts/acceptance-criteria-hash.ps1')) "$relativePath must verify frozen acceptance criteria before implementation."
    Assert-True ($context.Contains('At implementation completion, verify')) "$relativePath must recheck acceptance criteria before declaring completion."
    foreach ($requiredText in @('acceptance-evidence.json', 'AC-###', 'SC-###', 'scripts/check-acceptance-evidence.ps1', 'acceptance-criteria-review.md', 'acceptance-criteria-reviewed', 'never claim it ran unless the output was observed', 'does not execute commands')) {
        Assert-True ($context.Contains($requiredText)) "$relativePath must include acceptance evidence integrity guidance '$requiredText'."
    }
    Assert-True ($context.Contains('brownpants placement')) "$relativePath must route placement questions through the engine-neutral decision command."
    Assert-True ($context.Contains('distinct process argument')) "$relativePath must not interpolate untrusted candidate text into shell syntax."
}

$specify = Get-Command specify -ErrorAction SilentlyContinue
$liveIntegrationResult = 'skipped (specify CLI not installed)'
if (-not $specify -and $env:BROWNPANTS_REQUIRE_SPECKIT -eq 'true') {
    throw 'The Spec Kit CLI is required for this integration run but is not available on PATH.'
}
if ($specify) {
    $tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $liveProject = [System.IO.Path]::GetFullPath((Join-Path $tempBase ('brownpants-speckit-integration-' + [guid]::NewGuid().ToString('N'))))
    $pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
        [System.StringComparison]::OrdinalIgnoreCase
    } else {
        [System.StringComparison]::Ordinal
    }
    $tempPrefix = $tempBase + [System.IO.Path]::DirectorySeparatorChar
    Assert-True ($liveProject.StartsWith($tempPrefix, $pathComparison)) 'Refusing to create a Spec Kit fixture outside the temporary directory.'

    New-Item -ItemType Directory -Path $liveProject -Force | Out-Null
    $originalLocation = Get-Location
    try {
        Set-Location -LiteralPath $liveProject
        $extensionPath = Join-Path $repositoryRoot 'integrations/speckit'

        $initOutput = & $specify.Source init --here --integration codex --integration-options='--skills' --non-interactive --ignore-agent-tools 2>&1 | Out-String
        $initExitCode = $LASTEXITCODE
        Assert-True ($initExitCode -eq 0) "Spec Kit Codex initialization failed (exit $initExitCode): $initOutput"

        $codexSkillsPath = Join-Path $liveProject '.agents/skills'
        Assert-True (Test-Path -LiteralPath $codexSkillsPath -PathType Container) 'Spec Kit did not create the Codex skills directory.'
        foreach ($skill in @('speckit-specify', 'speckit-plan', 'speckit-implement')) {
            Assert-True (Test-Path -LiteralPath (Join-Path $codexSkillsPath $skill) -PathType Container) "Spec Kit did not install the '$skill' Codex skill."
        }

        $installOutput = & $specify.Source extension add $extensionPath --dev 2>&1 | Out-String
        $installExitCode = $LASTEXITCODE
        Assert-True ($installExitCode -eq 0) "Spec Kit extension installation failed (exit $installExitCode): $installOutput"

        $listOutput = & $specify.Source extension list 2>&1 | Out-String
        $listExitCode = $LASTEXITCODE
        Assert-True ($listExitCode -eq 0) "Spec Kit extension listing failed (exit $listExitCode): $listOutput"
        Assert-True ($listOutput -match '(?is)BrownPants.*Hooks:\s*11.*Status:\s*Enabled') 'Spec Kit did not report BrownPants as enabled with eleven hooks.'
        $bugAssessSkillPath = Join-Path $codexSkillsPath 'speckit-bug-assess/SKILL.md'
        Assert-True (-not (Test-Path -LiteralPath $bugAssessSkillPath -PathType Leaf)) 'The optional Spec Kit bug workflow unexpectedly exists before its extension is installed.'

        $bugInstallOutput = & $specify.Source extension add bug 2>&1 | Out-String
        $bugInstallExitCode = $LASTEXITCODE
        Assert-True ($bugInstallExitCode -eq 0) "Spec Kit bug extension installation failed (exit $bugInstallExitCode): $bugInstallOutput"
        Assert-True (Test-Path -LiteralPath $bugAssessSkillPath -PathType Leaf) 'Spec Kit did not install its bug assessment skill.'
        $bugAssessSkill = Get-Content -LiteralPath $bugAssessSkillPath -Raw
        Assert-True ($bugAssessSkill.Contains('before_bug_assess')) 'Installed Spec Kit bug assessment skill does not dispatch the pre-assessment hook.'
        Assert-True ($bugAssessSkill.Contains('By this point `BUG_SLUG` and `BUG_DIR` have been resolved')) 'Spec Kit bug assessment does not resolve its output directory before the pre-hook.'
        $preHookIndex = $bugAssessSkill.IndexOf('before_bug_assess', [System.StringComparison]::Ordinal)
        $ingestIndex = $bugAssessSkill.IndexOf('Ingest the bug report', $preHookIndex, [System.StringComparison]::Ordinal)
        Assert-True ($preHookIndex -ge 0 -and $ingestIndex -gt $preHookIndex) 'Spec Kit bug assessment must dispatch BrownPants before ingesting the report.'

        $projectSpecDirectory = Join-Path $liveProject '.specify'
        $installedRoot = Join-Path $projectSpecDirectory 'extensions/brownpants'
        $installedManifestPath = Join-Path $installedRoot 'extension.yml'
        Assert-True (Test-Path -LiteralPath $installedManifestPath -PathType Leaf) 'Spec Kit did not install the BrownPants extension manifest.'
        $installedManifest = Get-Content -LiteralPath $installedManifestPath -Raw
        $commandMatch = [regex]::Match($installedManifest, '(?m)^\s+file:\s*(?<path>commands/context\.md)\s*$')
        Assert-True ($commandMatch.Success) 'Installed manifest does not declare the expected context command file.'
        $commandPath = Join-Path $installedRoot ($commandMatch.Groups['path'].Value.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        Assert-True (Test-Path -LiteralPath $commandPath -PathType Leaf) 'Installed context command file is missing.'

        $bugCommandMatch = [regex]::Match($installedManifest, '(?m)^\s+file:\s*(?<path>commands/bug-evidence\.md)\s*$')
        Assert-True ($bugCommandMatch.Success) 'Installed manifest does not declare the expected bug-evidence command file.'
        $bugCommandPath = Join-Path $installedRoot ($bugCommandMatch.Groups['path'].Value.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        Assert-True (Test-Path -LiteralPath $bugCommandPath -PathType Leaf) 'Installed bug-evidence command file is missing.'
        $bugSkillPath = Join-Path $codexSkillsPath 'speckit-brownpants-bug-evidence/SKILL.md'
        Assert-True (Test-Path -LiteralPath $bugSkillPath -PathType Leaf) 'Spec Kit did not install the runnable Codex bug-evidence skill.'
        $bugSkill = Get-Content -LiteralPath $bugSkillPath -Raw
        Assert-True ($bugSkill.Contains('brownpants investigate')) 'Generated bug-evidence skill does not contain the investigation command instructions.'

        $projectHooks = Get-Content -LiteralPath (Join-Path $projectSpecDirectory 'extensions.yml') -Raw
        foreach ($event in $events) {
            $block = Get-HookBlock -Yaml $projectHooks -Event $event
            Assert-True ($block -match '(?m)^\s+- extension:\s*brownpants\s*$') "Installed project registration/$event has the wrong extension id."
            Assert-True ($block -match '(?m)^\s+command:\s*speckit\.brownpants\.context\s*$') "Installed project registration/$event targets the wrong command."
            Assert-True ($block -match '(?m)^\s+enabled:\s*true\s*$') "Installed project registration/$event is disabled."
            Assert-True ($block -match '(?m)^\s+optional:\s*false\s*$') "Installed project registration/$event is not mandatory."
        }
        $installedBugBlock = Get-HookBlock -Yaml $projectHooks -Event $bugEvent
        Assert-True ($installedBugBlock -match '(?m)^\s+- extension:\s*brownpants\s*$') 'Installed bug hook has the wrong extension id.'
        Assert-True ($installedBugBlock -match '(?m)^\s+command:\s*speckit\.brownpants\.bug-evidence\s*$') 'Installed bug hook targets the wrong command.'
        Assert-True ($installedBugBlock -match '(?m)^\s+enabled:\s*true\s*$') 'Installed bug hook is disabled.'
        Assert-True ($installedBugBlock -match '(?m)^\s+optional:\s*true\s*$') 'Installed bug hook is not optional.'
        $registeredHookCount = [regex]::Matches($projectHooks, '(?m)^  (?:before|after)_[a-z_]+:').Count
        Assert-True ($registeredHookCount -eq ($events.Count + 1)) "Spec Kit installed $registeredHookCount hooks; expected $($events.Count + 1)."
        $liveIntegrationResult = 'passed with Spec Kit CLI'
    } finally {
        Set-Location -LiteralPath $originalLocation
        if (Test-Path -LiteralPath $liveProject) {
            Remove-Item -LiteralPath $liveProject -Recurse -Force
        }
    }
}

Write-Output "Spec Kit extension tests passed: nine required lifecycle pre-hooks, one post-hook, one optional bug-assessment hook, synchronized guidance; live Codex setup and extension check $liveIntegrationResult."
