param(
    [Parameter(Mandatory=$true)][string] $RunRoot,
    [Parameter(Mandatory=$true)][string] $Model
)
$ErrorActionPreference = 'Stop'
function Get-TreeHashes([string] $source) {
    $base = (Resolve-Path -LiteralPath $source).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    @(Get-ChildItem -LiteralPath $base -Recurse -File -Force | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|BrownPantsEvidence)[\\/]'
    } | ForEach-Object {
        $relative = $_.FullName.Substring($base.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)).Replace([IO.Path]::DirectorySeparatorChar, '/')
        [pscustomobject]@{ path=$relative; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    } | Sort-Object -Property path)
}
function Get-ChangedPaths([object[]] $before, [object[]] $after) {
    $old = @{}; foreach ($item in $before) { $old[$item.path] = $item.sha256 }
    $new = @{}; foreach ($item in $after) { $new[$item.path] = $item.sha256 }
    @(@($old.Keys) + @($new.Keys) | Sort-Object -Unique | Where-Object { $old[$_] -cne $new[$_] })
}
function Invoke-Agent([int] $pair, [string] $condition, [string] $modelName, [string] $codexVersion, [string] $run) {
    $source = Join-Path $run "$condition-source"
    $resultDirectory = Join-Path $run 'agent-results'
    New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
    $before = Get-TreeHashes $source
    $eventPath = Join-Path $resultDirectory "$condition-events.jsonl"
    $resultPath = Join-Path $resultDirectory "$condition-result.json"
    $prompt = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'task-prompt.md'))
    $schema = Join-Path $PSScriptRoot 'result.schema.json'
    $started = [DateTimeOffset]::UtcNow
    Push-Location $source
    try {
        $events = $prompt | & codex exec --json --ephemeral --skip-git-repo-check --cd $source `
            --model $modelName --sandbox workspace-write --output-schema $schema `
            --output-last-message $resultPath - 2>&1
        $exitCode = $LASTEXITCODE
    } finally { Pop-Location }
    $events | Set-Content -LiteralPath $eventPath -Encoding utf8
    $completed = [DateTimeOffset]::UtcNow
    $usage = $null
    foreach ($line in Get-Content -LiteralPath $eventPath) {
        try { $event = $line | ConvertFrom-Json -AsHashtable -ErrorAction Stop } catch { continue }
        if ($event.type -eq 'turn.completed' -and $event.usage) { $usage = $event.usage }
    }
    $after = Get-TreeHashes $source
    $changed = @(Get-ChangedPaths $before $after)
    $expectedPath = 'src/WorkRepository.cs'
    $allowedChange = @($changed).Count -eq 1 -and $changed[0] -ceq $expectedPath
    $postcheckPath = Join-Path $resultDirectory "$condition-postcheck.txt"
    $postcheck = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Verify-Inputs.ps1') -FixtureRoot $source 2>&1
    $postcheckExit = $LASTEXITCODE
    $postcheck | Set-Content -LiteralPath $postcheckPath -Encoding utf8
    [pscustomobject]@{
        pair=$pair; condition=$condition; codexVersion=$codexVersion; model=$modelName; sandbox='workspace-write'
        startedUtc=$started.ToString('O'); completedUtc=$completed.ToString('O'); codexExitCode=$exitCode
        resultPath=("pair-{0:D2}/agent-results/{1}-result.json" -f $pair,$condition)
        eventLogPath=("pair-{0:D2}/agent-results/{1}-events.jsonl" -f $pair,$condition)
        postcheckExitCode=$postcheckExit; postcheckLogPath=("pair-{0:D2}/agent-results/{1}-postcheck.txt" -f $pair,$condition)
        changedPaths=$changed; changedPathsAllowed=$allowedChange; usage=$usage
    }
}
$runRoot = (Resolve-Path -LiteralPath $RunRoot).Path
$null = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Verify-FrozenInputs.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Frozen protocol verification failed.' }
$preparationPath = Join-Path $runRoot 'preparation.json'
$preparation = Get-Content -LiteralPath $preparationPath -Raw | ConvertFrom-Json
if (-not $preparation.cleanChecksPassed -or -not $preparation.seededChecksFailedAsExpected -or -not $preparation.identicalSeededCode) {
    throw 'Preparation gate is incomplete; refusing agent execution.'
}
if (-not (Get-Command codex -ErrorAction SilentlyContinue)) { throw 'Codex CLI is not available on PATH.' }
$codexVersion = (& codex --version 2>&1 | Out-String).Trim()
$order = @(@('baseline','assisted'), @('assisted','baseline'), @('baseline','assisted'))
$runs = @()
foreach ($pair in 1..3) {
    $pairRoot = Join-Path $runRoot ('pair-{0:D2}' -f $pair)
    foreach ($condition in $order[$pair - 1]) {
        Write-Output "Pair ${pair}: running $condition condition with $Model."
        $runs += Invoke-Agent $pair $condition $Model $codexVersion $pairRoot
    }
}
$metadata = [ordered]@{
    schemaVersion=1; codexVersion=$codexVersion; model=$Model; sandbox='workspace-write'
    conditionOrder=@('baseline/assisted','assisted/baseline','baseline/assisted')
    taskSpecDigest=$preparation.taskSpecDigest; answerKeyDigest=$preparation.answerKeyDigest
    promptDigest=$preparation.promptDigest; schemaDigest=$preparation.schemaDigest
    preparation=$preparation; runs=$runs
}
$metadata | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath (Join-Path $runRoot 'codex-runs.json') -Encoding utf8
Write-Output "Saved three matched pairs, six raw event logs, structured responses, and post-fix checks under $runRoot."
