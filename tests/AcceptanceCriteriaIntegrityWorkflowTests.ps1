$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$workflow = Get-Content -LiteralPath (Join-Path $root '.github/workflows/ci.yml') -Raw
if ($workflow -notmatch '(?m)^name:\s*CI\s*$') { throw 'CI must retain its declared workflow name.' }
if ($workflow -notmatch '(?m)^  workflow_dispatch:\s*$') { throw 'CI must retain its manual verification trigger.' }
if ([regex]::Matches($workflow, '(?m)^jobs:\s*$').Count -ne 1) { throw 'CI must contain exactly one jobs section.' }
if ([regex]::Matches($workflow, '(?m)^      - name: Verify recorded acceptance baselines\s*$').Count -ne 1) { throw 'CI must contain exactly one acceptance-baseline verification step.' }
if ($workflow -notmatch '(?ms)pull_request:\s*\r?\n\s+types:\s*\r?\n(?:\s+-\s+\w+\s*\r?\n)*\s+-\s+labeled\s*\r?\n\s+-\s+unlabeled') {
    throw 'CI must rerun on pull request label additions and removals.'
}
if ($workflow -notmatch '(?m)fetch-depth:\s*0') { throw 'CI must fetch history for base/head comparisons.' }
if ($workflow -notmatch 'check-acceptance-criteria-review\.ps1') { throw 'CI does not run baseline review validation.' }
if ($workflow -notmatch 'check-acceptance-evidence\.ps1') { throw 'CI does not validate changed task evidence ledgers.' }
if ($workflow -notmatch [regex]::Escape("[char]39")) { throw 'CI must compare the closed disposition status.' }
if ($workflow -notmatch [regex]::Escape('statusMatch.Groups[''status''].Value -eq $closedStatus')) { throw 'CI must compare the full feature status before skipping its ledger.' }
$closedFeatureSkip = $workflow.IndexOf('continue', [System.StringComparison]::Ordinal)
$ledgerValidation = $workflow.IndexOf('Changed feature tasks require an acceptance evidence ledger', [System.StringComparison]::Ordinal)
if ($closedFeatureSkip -lt 0 -or $ledgerValidation -lt $closedFeatureSkip) { throw 'CI must skip closed features before requiring an acceptance evidence ledger.' }
if ($workflow -notmatch 'acceptance-criteria-reviewed') { throw 'CI does not pass the explicit review label to the baseline checker.' }
foreach ($requiredText in @('PR_BASE', 'PR_HEAD', 'github.event.pull_request.base.sha', 'github.event.pull_request.head.sha', "github.event_name == 'pull_request'", 'tasks.md', 'acceptance-evidence.json')) {
    if ($workflow -notmatch [regex]::Escape($requiredText)) { throw "CI is missing pull request evidence policy input '$requiredText'." }
}
if ($workflow -notmatch 'github\.event\.pull_request\.head\.sha \|\| github\.sha') { throw 'CI should test the pull request head commit and the pushed commit.' }
if ($workflow -notmatch 'get-changed-spec-artifacts\.ps1') { throw 'CI must use the tested changed-artifact selector.' }
if ($workflow -notmatch 'github\.ref_type') { throw 'CI must pass the Git ref type to changed-artifact selection.' }
if ($workflow -notmatch 'AcceptanceCriteriaIntegrityWorkflowTests\.ps1') { throw 'CI must run the workflow behavior test.' }

$selector = Join-Path $root 'scripts/get-changed-spec-artifacts.ps1'
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-git-range-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
try {
    Push-Location $fixture
    try {
        & git init --quiet
        if ($LASTEXITCODE -ne 0) { throw 'Could not initialize temporary Git history.' }
        & git config user.name 'BrownPants Workflow Test'
        & git config user.email 'workflow-test@example.invalid'
        New-Item -ItemType Directory -Path 'specs/legacy', 'specs/feature' -Force | Out-Null
        Set-Content -LiteralPath 'specs/legacy/tasks.md' -Value 'legacy unchanged' -Encoding utf8
        Set-Content -LiteralPath 'specs/feature/tasks.md' -Value 'initial feature' -Encoding utf8
        & git add -- specs
        & git commit --quiet -m 'Initial history'
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit temporary Git root.' }
        $rootCommit = (& git rev-parse HEAD).Trim()

        Set-Content -LiteralPath 'specs/feature/tasks.md' -Value 'changed feature' -Encoding utf8
        & git add -- specs/feature/tasks.md
        & git commit --quiet -m 'Change feature task'
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit temporary feature change.' }
        $headCommit = (& git rev-parse HEAD).Trim()

        $normalPush = @(& $selector -EventName push -RefType branch -Before $rootCommit -Head $headCommit)
        if ($LASTEXITCODE -ne 0 -or (Compare-Object @('specs/feature/tasks.md') $normalPush)) { throw 'Normal branch pushes must select only files changed since before.' }
        $tagPush = @(& $selector -EventName push -RefType tag -Before ('0' * 40) -Head $headCommit)
        if ($LASTEXITCODE -ne 0 -or (Compare-Object @('specs/feature/tasks.md') $tagPush)) { throw 'A new tag must select changes from its parent, not every historical task.' }
        $firstBranchPush = @(& $selector -EventName push -RefType branch -Before ('0' * 40) -Head $headCommit)
        $allTasks = @('specs/feature/tasks.md', 'specs/legacy/tasks.md')
        if ($LASTEXITCODE -ne 0 -or (Compare-Object $allTasks $firstBranchPush)) { throw 'An initial branch push must select the complete tree.' }
        $pullRequest = @(& $selector -EventName pull_request -RefType branch -Before ('0' * 40) -Head $headCommit -PullRequestBase $rootCommit -PullRequestHead $headCommit)
        if ($LASTEXITCODE -ne 0 -or (Compare-Object @('specs/feature/tasks.md') $pullRequest)) { throw 'Pull requests must retain their base/head comparison.' }
        $manualRun = @(& $selector -EventName workflow_dispatch -RefType branch -Head $headCommit)
        if ($manualRun.Count -ne 0) { throw 'Manual workflow dispatch must retain its empty changed-file set.' }
    }
    finally { Pop-Location }

    $rootFixture = Join-Path $fixture 'root-tag'
    New-Item -ItemType Directory -Path $rootFixture -Force | Out-Null
    Push-Location $rootFixture
    try {
        & git init --quiet
        & git config user.name 'BrownPants Workflow Test'
        & git config user.email 'workflow-test@example.invalid'
        New-Item -ItemType Directory -Path 'specs/root' -Force | Out-Null
        Set-Content -LiteralPath 'specs/root/tasks.md' -Value 'root feature' -Encoding utf8
        & git add -- specs
        & git commit --quiet -m 'Root commit'
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit temporary root-tag history.' }
        $rootHead = (& git rev-parse HEAD).Trim()
        $rootTag = @(& $selector -EventName push -RefType tag -Before ('0' * 40) -Head $rootHead)
        if ($LASTEXITCODE -ne 0 -or (Compare-Object @('specs/root/tasks.md') $rootTag)) { throw 'A root tag without a parent must fall back to complete-tree discovery.' }
    }
    finally { Pop-Location }
}
finally {
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixture)
    if (-not $resolvedFixture.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not (Split-Path -Leaf $resolvedFixture).StartsWith('brownpants-git-range-test-', [System.StringComparison]::Ordinal)) {
        throw 'Refusing to remove the Git test fixture outside its designated temporary directory.'
    }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
Write-Output 'Acceptance criteria integrity workflow tests passed.'
