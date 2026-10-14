$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$workflow = Get-Content -LiteralPath (Join-Path $root '.github/workflows/ci.yml') -Raw
if ($workflow -notmatch '(?ms)pull_request:\s*\r?\n\s+types:\s*\r?\n(?:\s+-\s+\w+\s*\r?\n)*\s+-\s+labeled\s*\r?\n\s+-\s+unlabeled') {
    throw 'CI must rerun on pull request label additions and removals.'
}
if ($workflow -notmatch '(?m)fetch-depth:\s*0') { throw 'CI must fetch history for base/head comparisons.' }
if ($workflow -notmatch 'check-acceptance-criteria-review\.ps1') { throw 'CI does not run baseline review validation.' }
if ($workflow -notmatch 'check-acceptance-evidence\.ps1') { throw 'CI does not validate changed task evidence ledgers.' }
if ($workflow -notmatch 'acceptance-criteria-reviewed') { throw 'CI does not pass the explicit review label to the baseline checker.' }
foreach ($requiredText in @('PR_BASE', 'PR_HEAD', 'github.event.pull_request.base.sha', 'github.event.pull_request.head.sha', "github.event_name == 'pull_request'", 'tasks.md', 'acceptance-evidence.json')) {
    if ($workflow -notmatch [regex]::Escape($requiredText)) { throw "CI is missing pull request evidence policy input '$requiredText'." }
}
if ($workflow -notmatch 'github\.event\.pull_request\.head\.sha \|\| github\.sha') { throw 'CI should test the pull request head commit and the pushed commit.' }
Write-Output 'Acceptance criteria integrity workflow tests passed.'
