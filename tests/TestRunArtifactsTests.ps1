$ErrorActionPreference = 'Stop'
$module = Join-Path (Split-Path -Parent $PSScriptRoot) 'scripts/TestRunArtifacts.psm1'
Import-Module $module -Force
$root = Join-Path ([IO.Path]::GetTempPath()) ('brownpants-cleanup-' + [guid]::NewGuid().ToString('N'))
$countRoot = Join-Path ([IO.Path]::GetTempPath()) ('brownpants-cleanup-count-' + [guid]::NewGuid().ToString('N'))
$outside = Join-Path ([IO.Path]::GetTempPath()) ('brownpants-cleanup-outside-' + [guid]::NewGuid().ToString('N'))
$failedQaRoot = Join-Path ([IO.Path]::GetTempPath()) ('brownpants-cleanup-qa-failure-' + [guid]::NewGuid().ToString('N'))

function Assert($condition, [string] $message) { if (-not $condition) { throw $message } }
function NewFixtureRun([string] $kind, [string] $state, [int] $ageDays) {
    $dir = New-BrownPantsRun -RepositoryRoot $root -Kind $kind
    Set-BrownPantsRunState -RunDirectory $dir -State $state
    $marker = Join-Path $dir '.brownpants-run.json'
    $json = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
    $json.completedUtc = [DateTime]::UtcNow.AddDays(-$ageDays).ToString('o')
    $json | ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding utf8
    return $dir
}

try {
    New-Item -ItemType Directory -Path $root | Out-Null
    $newest = NewFixtureRun 'unit-tests' 'succeeded' 0
    $old = NewFixtureRun 'unit-tests' 'succeeded' 20
    $failedRecent = NewFixtureRun 'unit-tests' 'failed' 2
    $failedOld = NewFixtureRun 'unit-tests' 'failed' 8
    $legacy = Join-Path $root 'TestResults/legacy-qa'; New-Item -ItemType Directory -Path $legacy -Force | Out-Null

    $review = @(Invoke-BrownPantsRunCleanup -RepositoryRoot $root -MaxSuccessfulRuns 5)
    Assert (Test-Path -LiteralPath $old) 'Dry-run deleted an old successful run.'
    Assert (($review -join "`n") -match 'WOULD REMOVE succeeded') 'Dry-run did not report a successful candidate.'
    Assert (($review -join "`n") -match 'UNMANAGED \(preserved\)') 'Legacy output was not reported.'

    Invoke-BrownPantsRunCleanup -RepositoryRoot $root -MaxSuccessfulRuns 5 -Apply | Out-Null
    Assert (Test-Path -LiteralPath $newest) 'Recent successful run was removed.'
    Assert (-not (Test-Path -LiteralPath $old)) 'Expired successful run was retained.'
    Assert (Test-Path -LiteralPath $failedRecent) 'Recent failed run was removed.'
    Assert (-not (Test-Path -LiteralPath $failedOld)) 'Expired failed run was retained.'
    Assert (Test-Path -LiteralPath $legacy) 'Unmanaged legacy output was removed.'

    $runsRoot = Get-BrownPantsRunRoot $root
    $malformed = Join-Path $runsRoot 'malformed-run'
    New-Item -ItemType Directory -Path $malformed -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $malformed '.brownpants-run.json') -Value '{broken json'
    $malformedReport = @(Invoke-BrownPantsRunCleanup -RepositoryRoot $root -Apply)
    Assert (Test-Path -LiteralPath $malformed) 'Malformed run metadata was deleted.'
    Assert (($malformedReport -join "`n") -match 'SKIP invalid manifest') 'Malformed run metadata was not reported.'

    New-Item -ItemType Directory -Path $countRoot | Out-Null
    1..6 | ForEach-Object {
        $dir = New-BrownPantsRun -RepositoryRoot $countRoot -Kind 'count-check'
        Set-BrownPantsRunState -RunDirectory $dir -State succeeded
        Start-Sleep -Milliseconds 10
    }
    Invoke-BrownPantsRunCleanup -RepositoryRoot $countRoot -MaxSuccessfulRuns 2 -SuccessfulMaxAgeDays 14 -Apply | Out-Null
    $retained = @(Get-ChildItem -LiteralPath (Get-BrownPantsRunRoot $countRoot) -Directory)
    Assert ($retained.Count -eq 2) "Count retention kept $($retained.Count) successful runs instead of 2."

    $unsafeRun = NewFixtureRun 'link-check' 'succeeded' 20
    New-Item -ItemType Directory -Path $outside | Out-Null
    $link = Join-Path $unsafeRun 'linked-outside'
    $linkCreated = $false
    try { [IO.Directory]::CreateSymbolicLink($link, $outside); $linkCreated = $true } catch { }
    Invoke-BrownPantsRunCleanup -RepositoryRoot $root -Apply | Out-Null
    Assert (Test-Path -LiteralPath $outside) 'Cleanup touched an external directory.'
    if ($linkCreated) { Assert (Test-Path -LiteralPath $unsafeRun) 'Run containing a reparse point was removed.' }

    $linkedRepo = Join-Path ([IO.Path]::GetTempPath()) ('brownpants-cleanup-root-link-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $linkedRepo | Out-Null
    $linkedResults = Join-Path $linkedRepo 'TestResults'
    $rootLinkCreated = $false
    try { [IO.Directory]::CreateSymbolicLink($linkedResults, $outside); $rootLinkCreated = $true } catch { }
    if ($rootLinkCreated) {
        $rejected = $false
        try { Get-BrownPantsRunRoot $linkedRepo | Out-Null } catch { $rejected = $true }
        Assert $rejected 'A TestResults root link was accepted.'
    }
    if (Test-Path -LiteralPath $linkedRepo) { Remove-Item -LiteralPath $linkedRepo -Recurse -Force }
    Assert (Test-Path -LiteralPath $outside) 'Root-link safety check touched the external target.'

    New-Item -ItemType Directory -Path $failedQaRoot | Out-Null
    $qaScript = Join-Path (Split-Path -Parent $PSScriptRoot) 'scripts/qa-chart-engine.ps1'
    & pwsh -NoProfile -File $qaScript -RepositoryRoot $failedQaRoot 2>&1 | Out-Null
    Assert ($LASTEXITCODE -ne 0) 'QA failure fixture unexpectedly succeeded.'
    $failedQaRunsRoot = Get-BrownPantsRunRoot $failedQaRoot
    $failedQaRun = Get-ChildItem -LiteralPath $failedQaRunsRoot -Directory | Select-Object -First 1
    Assert ($null -ne $failedQaRun) 'Failed QA did not keep its run directory.'
    $failedQaManifest = Get-Content -LiteralPath (Join-Path $failedQaRun.FullName '.brownpants-run.json') -Raw | ConvertFrom-Json
    Assert ($failedQaManifest.state -eq 'failed') 'Failed QA run was not marked failed.'
    Invoke-BrownPantsRunCleanup -RepositoryRoot $failedQaRoot -Apply | Out-Null
    Assert (Test-Path -LiteralPath $failedQaRun.FullName) 'Cleanup removed a failed QA run.'
    Write-Output 'Test run artifact cleanup checks passed.'
} finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
    if (Test-Path -LiteralPath $countRoot) { Remove-Item -LiteralPath $countRoot -Recurse -Force }
    if (Test-Path -LiteralPath $outside) { Remove-Item -LiteralPath $outside -Recurse -Force }
    if (Test-Path -LiteralPath $failedQaRoot) { Remove-Item -LiteralPath $failedQaRoot -Recurse -Force }
}
