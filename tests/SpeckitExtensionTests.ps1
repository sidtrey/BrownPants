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
$beforeEvents = @($events | Where-Object { $_ -like 'before_*' })
Assert-True ($beforeEvents.Count -eq 9 -and @($events | Where-Object { $_ -like 'after_*' }).Count -eq 1) 'Expected nine pre-phase hooks and one post-implementation hook.'
$integrationIndex = Get-Content -LiteralPath (Join-Path $repositoryRoot 'integrations/README.md') -Raw
Assert-True ($integrationIndex.Contains('before constitution, specification, clarification, planning, checklist, task, analysis, implementation, and issue-sync phases, and after implementation')) 'integrations/README.md must document pre- and post-implementation coverage.'

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
    Assert-True ($manifestHookCount -eq $events.Count) "$relativePath should register all ten lifecycle hooks."
    foreach ($event in $events) {
        $block = Get-HookBlock -Yaml $manifest -Event $event
        Assert-True ($block -match '(?m)^\s+command:\s*speckit\.brownpants\.context\s*$') "$relativePath/$event targets the wrong command."
        Assert-True ($block -match '(?m)^\s+optional:\s*false\s*$') "$relativePath/$event must be mandatory."
    }
}

$projectHooks = Get-Content -LiteralPath (Join-Path $repositoryRoot '.specify/extensions.yml') -Raw
$projectHookCount = [regex]::Matches($projectHooks, '(?m)^  (?:before|after)_[a-z_]+:').Count
Assert-True ($projectHookCount -eq $events.Count) '.specify/extensions.yml should contain all ten BrownPants lifecycle hooks.'
foreach ($event in $events) {
    $block = Get-HookBlock -Yaml $projectHooks -Event $event
    Assert-True ($block -match '(?m)^\s+- extension:\s*brownpants\s*$') ".specify/extensions.yml/$event must belong to BrownPants."
    Assert-True ($block -match '(?m)^\s+command:\s*speckit\.brownpants\.context\s*$') ".specify/extensions.yml/$event targets the wrong command."
    Assert-True ($block -match '(?m)^\s+optional:\s*false\s*$') ".specify/extensions.yml/$event must be mandatory."
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
}

Write-Output 'Spec Kit extension tests passed: nine pre-hooks, one post-hook, and synchronized phase guidance.'
