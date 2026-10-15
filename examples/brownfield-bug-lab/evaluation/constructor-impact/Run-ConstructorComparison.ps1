param(
    [Parameter(Mandatory=$true)][string] $RunRoot,
    [Parameter(Mandatory=$true)][string] $Model
)
$ErrorActionPreference = 'Stop'

function Invoke-Agent([string] $condition, [string] $run, [string] $modelName, [string] $codexVersion) {
    $resultDirectory = Join-Path $run 'agent-results'
    $eventsPath = Join-Path $resultDirectory "$condition-events.jsonl"
    $messagePath = Join-Path $resultDirectory "$condition-result.json"
    $sourceRoot = Join-Path $run "$condition-source"
    $promptPath = Join-Path $sourceRoot 'control/task-prompt.md'
    $schemaPath = Join-Path $sourceRoot 'control/result.schema.json'
    $prompt = [IO.File]::ReadAllText($promptPath)
    $started = [DateTimeOffset]::UtcNow
    Push-Location $sourceRoot
    try {
        $events = $prompt | & codex exec --json --ephemeral --skip-git-repo-check --cd $sourceRoot `
            --model $modelName --sandbox workspace-write --output-schema $schemaPath `
            --output-last-message $messagePath - 2>&1
        $exitCode = $LASTEXITCODE
    } finally { Pop-Location }
    $events | Set-Content -LiteralPath $eventsPath -Encoding utf8
    $completed = [DateTimeOffset]::UtcNow

    $usage = $null
    foreach ($line in Get-Content -LiteralPath $eventsPath) {
        try { $event = $line | ConvertFrom-Json -AsHashtable -ErrorAction Stop }
        catch { continue }
        if ($event.type -eq 'turn.completed' -and $event.usage) { $usage = $event.usage }
    }
    $postcheckPath = Join-Path $resultDirectory "$condition-postcheck.txt"
    Push-Location $sourceRoot
    try {
        $postcheck = & dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release 2>&1
        $postcheckCode = $LASTEXITCODE
    } finally { Pop-Location }
    $postcheck | Set-Content -LiteralPath $postcheckPath -Encoding utf8

    [pscustomobject]@{
        condition = $condition
        codexVersion = $codexVersion
        model = $modelName
        sandbox = 'workspace-write'
        startedUtc = $started.ToString('O')
        completedUtc = $completed.ToString('O')
        codexExitCode = $exitCode
        resultPath = "agent-results/$condition-result.json"
        eventLogPath = "agent-results/$condition-events.jsonl"
        postcheckExitCode = $postcheckCode
        postcheckLogPath = "agent-results/$condition-postcheck.txt"
        usage = $usage
    }
}

$run = (Resolve-Path -LiteralPath $RunRoot).Path
$codexCommand = Get-Command codex -ErrorAction Stop
$codexVersion = (& codex --version 2>&1 | Out-String).Trim()
$preparationPath = Join-Path $run 'preparation.json'
$preparation = Get-Content -LiteralPath $preparationPath -Raw | ConvertFrom-Json
if (-not $preparation.cleanChecksPassed -or -not $preparation.seededChecksFailedAsExpected -or -not $preparation.identicalSeededCode) {
    throw 'Preparation gate is not complete; refusing to run agents.'
}
foreach ($condition in @('baseline', 'assisted')) {
    foreach ($path in @("$condition-source/control/task-prompt.md", "$condition-source/control/result.schema.json")) {
        if (-not (Test-Path -LiteralPath (Join-Path $run $path) -PathType Leaf)) { throw "Prepared input is missing: $path" }
    }
}

$runs = @()
foreach ($condition in @('baseline', 'assisted')) {
    Write-Output "Running $condition Codex condition with model '$Model'."
    $runs += Invoke-Agent $condition $run $Model $codexVersion
}
$metadata = [pscustomobject]@{
    schemaVersion = 1
    codexVersion = $codexVersion
    model = $Model
    sandbox = 'workspace-write'
    promptDigest = (Get-FileHash -LiteralPath (Join-Path $run 'baseline-source/control/task-prompt.md') -Algorithm SHA256).Hash.ToLowerInvariant()
    resultSchemaDigest = (Get-FileHash -LiteralPath (Join-Path $run 'baseline-source/control/result.schema.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    taskSpecDigest = (Get-Content -LiteralPath (Join-Path $run 'baseline-source/control/acceptance-criteria.sha256') -Raw).Trim().Split(' ')[0]
    preparation = $preparation
    runs = $runs
}
$metadata | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $run 'codex-runs.json') -Encoding utf8
Write-Output "Saved Codex events, final responses, and postcheck logs under $(Join-Path $run 'agent-results')."
Write-Output "Run metadata: $(Join-Path $run 'codex-runs.json')"
