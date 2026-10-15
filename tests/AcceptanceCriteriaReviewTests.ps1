$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$checker = Join-Path $root 'scripts/check-acceptance-criteria-review.ps1'
if (-not (Test-Path -LiteralPath $checker)) { throw 'Acceptance criteria review checker does not exist yet.' }
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-review-' + [guid]::NewGuid().ToString('N'))
$oldDigest = 'a' * 64
$newDigest = 'b' * 64

function Invoke-Git {
    param([string] $Repo, [string[]] $Arguments)
    $result = & git -C $Repo @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed: $result" }
    return ($result | Out-String).Trim()
}

function New-Repository {
    param([bool] $WithBaseline = $true)
    $repo = Join-Path $tempRoot ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $repo -Force | Out-Null
    Invoke-Git $repo @('init') | Out-Null
    Invoke-Git $repo @('config', 'user.email', 'tests@example.invalid') | Out-Null
    Invoke-Git $repo @('config', 'user.name', 'Acceptance Tests') | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repo 'specs/feature') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $repo 'README.md') -Value 'fixture' -Encoding utf8
    if ($WithBaseline) {
        Set-Content -LiteralPath (Join-Path $repo 'specs/feature/spec.md') -Value "## User Scenarios & Testing`n1. **AC-001** — original`n`n## Success Criteria`n- **SC-001**: original" -Encoding utf8
        Set-Content -LiteralPath (Join-Path $repo 'specs/feature/acceptance-criteria.sha256') -Value "$oldDigest  spec.md" -Encoding ascii
    }
    Invoke-Git $repo @('add', '.') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'base') | Out-Null
    return $repo
}

function Commit-ChangedBaseline {
    param([string] $Repo, [bool] $WithRecord, [string] $RecordOld = $oldDigest, [string] $RecordNew = $newDigest, [string] $ChangedCriterion = 'AC-001')
    Set-Content -LiteralPath (Join-Path $Repo 'specs/feature/spec.md') -Value "## User Scenarios & Testing`n1. **AC-001** — updated`n`n## Success Criteria`n- **SC-001**: original" -Encoding utf8
    Set-Content -LiteralPath (Join-Path $Repo 'specs/feature/acceptance-criteria.sha256') -Value "$newDigest  spec.md" -Encoding ascii
    if ($WithRecord) {
        Set-Content -LiteralPath (Join-Path $Repo 'specs/feature/acceptance-criteria-review.md') -Encoding utf8 -Value @"
# Acceptance Criteria Change Review

## Previous digest
$RecordOld

## Proposed digest
$RecordNew

## Reason
The acceptance boundary changed after product review.

## Changed criteria
- ${ChangedCriterion}: Updated acceptance scenario.
"@
    }
    Invoke-Git $Repo @('add', '.') | Out-Null
    Invoke-Git $Repo @('commit', '-m', 'change acceptance criteria') | Out-Null
    return Invoke-Git $Repo @('rev-parse', 'HEAD')
}

function Assert-ReviewFails {
    param([string] $Repo, [string] $Base, [string] $Head, [bool] $Reviewed, [string] $Name)
    $failed = $false
    Push-Location $Repo
    try { & $checker -BaseRef $Base -HeadRef $Head -Reviewed:$Reviewed -RepositoryRoot $Repo | Out-Null } catch { $failed = $true }
    finally { Pop-Location }
    if (-not $failed) { throw "Expected review validation to reject '$Name'." }
}

function Assert-ReviewPasses {
    param([string] $Repo, [string] $Base, [string] $Head, [bool] $Reviewed, [string] $Name)
    Push-Location $Repo
    try {
        & $checker -BaseRef $Base -HeadRef $Head -Reviewed:$Reviewed -RepositoryRoot $Repo | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Successful review validation left native exit code $LASTEXITCODE." }
    }
    catch { throw "Expected review validation to accept '$Name': $_" }
    finally { Pop-Location }
}

try {
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

    $repo = New-Repository
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    $head = Commit-ChangedBaseline $repo -WithRecord:$true
    Assert-ReviewFails $repo $base $head $false 'missing approval label'

    $repo = New-Repository
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    $head = Commit-ChangedBaseline $repo -WithRecord:$true
    Assert-ReviewPasses $repo $base $head $true 'reviewed baseline amendment'

    $repo = New-Repository
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    $head = Commit-ChangedBaseline $repo -WithRecord:$false
    Assert-ReviewFails $repo $base $head $true 'missing review record'

    $repo = New-Repository
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    $head = Commit-ChangedBaseline $repo -WithRecord:$true -RecordOld:('c' * 64)
    Assert-ReviewFails $repo $base $head $true 'stale previous digest'

    $repo = New-Repository
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    $head = Commit-ChangedBaseline $repo -WithRecord:$true -ChangedCriterion:'AC-999'
    Assert-ReviewFails $repo $base $head $true 'unknown changed criterion ID'

    $repo = New-Repository -WithBaseline:$false
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    New-Item -ItemType Directory -Path (Join-Path $repo 'specs/feature') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $repo 'specs/feature/spec.md') -Value "## User Scenarios & Testing`n1. **AC-001** — initial`n`n## Success Criteria`n- **SC-001**: initial" -Encoding utf8
    Set-Content -LiteralPath (Join-Path $repo 'specs/feature/acceptance-criteria.sha256') -Value "$newDigest  spec.md" -Encoding ascii
    Invoke-Git $repo @('add', '.') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'add initial feature baseline') | Out-Null
    $head = Invoke-Git $repo @('rev-parse', 'HEAD')
    Assert-ReviewPasses $repo $base $head $false 'new feature baseline'

    $repo = New-Repository -WithBaseline:$false
    New-Item -ItemType Directory -Path (Join-Path $repo 'specs/feature') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $repo 'specs/feature/spec.md') -Value "## User Scenarios & Testing`n1. **AC-001** — initial`n`n## Success Criteria`n- **SC-001**: initial" -Encoding utf8
    Invoke-Git $repo @('add', '.') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'add feature without baseline') | Out-Null
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    Set-Content -LiteralPath (Join-Path $repo 'specs/feature/acceptance-criteria.sha256') -Value "$newDigest  spec.md" -Encoding ascii
    Invoke-Git $repo @('add', '.') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'add late baseline') | Out-Null
    $head = Invoke-Git $repo @('rev-parse', 'HEAD')
    Assert-ReviewFails $repo $base $head $false 'baseline added to an existing feature'

    $repo = New-Repository -WithBaseline:$false
    New-Item -ItemType Directory -Path (Join-Path $repo 'specs/feature') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $repo 'specs/feature/spec.md') -Value "## User Scenarios & Testing`n1. **AC-001** — initial`n`n## Success Criteria`n- **SC-001**: initial" -Encoding utf8
    Invoke-Git $repo @('add', '.') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'add feature without baseline') | Out-Null
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    Set-Content -LiteralPath (Join-Path $repo 'specs/feature/acceptance-criteria.sha256') -Value "$newDigest  spec.md" -Encoding ascii
    Set-Content -LiteralPath (Join-Path $repo 'specs/feature/acceptance-criteria-review.md') -Encoding utf8 -Value @"
## Previous digest
none

## Proposed digest
$newDigest

## Reason
The existing feature is adopting its initial frozen acceptance boundary.

## Changed criteria
- AC-001: Initial baseline.
"@
    Invoke-Git $repo @('add', '.') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'add reviewed late baseline') | Out-Null
    $head = Invoke-Git $repo @('rev-parse', 'HEAD')
    Assert-ReviewPasses $repo $base $head $true 'reviewed late first baseline'

    $repo = New-Repository
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    $oldPath = Join-Path $repo 'specs/feature/acceptance-criteria.sha256'
    New-Item -ItemType Directory -Path (Join-Path $repo 'specs/renamed') -Force | Out-Null
    Move-Item -LiteralPath $oldPath -Destination (Join-Path $repo 'specs/renamed/acceptance-criteria.sha256')
    Invoke-Git $repo @('add', '-A') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'rename acceptance baseline') | Out-Null
    $head = Invoke-Git $repo @('rev-parse', 'HEAD')
    Assert-ReviewFails $repo $base $head $true 'renamed existing baseline'

    $repo = New-Repository
    $base = Invoke-Git $repo @('rev-parse', 'HEAD')
    Remove-Item -LiteralPath (Join-Path $repo 'specs/feature/acceptance-criteria.sha256')
    Invoke-Git $repo @('add', '-A') | Out-Null
    Invoke-Git $repo @('commit', '-m', 'delete baseline') | Out-Null
    $head = Invoke-Git $repo @('rev-parse', 'HEAD')
    Assert-ReviewFails $repo $base $head $true 'deleted existing baseline'

    Write-Output 'Acceptance criteria review tests passed.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
