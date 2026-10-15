param(
    [Parameter(Mandatory=$true)][string] $FixtureRoot,
    [Parameter(Mandatory=$true)][string] $OutputRoot,
    [string] $BrownPantsProject = (Join-Path $PSScriptRoot '../../../../src/BrownPants.Cli')
)
$ErrorActionPreference = 'Stop'
function Copy-Fixture([string] $source, [string] $destination) {
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $source -Recurse -File -Force | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object {
        $relative = $_.FullName.Substring($source.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar))
        $target = Join-Path $destination $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $target
    }
}
function Get-TreeHashes([string] $source) {
    $base = (Resolve-Path -LiteralPath $source).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    @(Get-ChildItem -LiteralPath $base -Recurse -File -Force | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|BrownPantsEvidence)[\\/]'
    } | ForEach-Object {
        $relative = $_.FullName.Substring($base.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)).Replace([IO.Path]::DirectorySeparatorChar, '/')
        [pscustomobject]@{ path=$relative; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    } | Sort-Object -Property path)
}
function Get-TreeDigest([string] $source) {
    $rows = Get-TreeHashes $source | ForEach-Object { "$($_.path)|$($_.sha256)" }
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($rows -join "`n")))).ToLowerInvariant()
}
function Invoke-Check([string] $root, [bool] $expectSuccess, [string] $log) {
    $output = & dotnet run --project (Join-Path $root 'DelegateImpact.Fixture.csproj') -c Release 2>&1
    $code = $LASTEXITCODE
    $output | Set-Content -LiteralPath $log -Encoding utf8
    $text = $output -join "`n"
    if ($expectSuccess -and ($code -ne 0 -or $text -notmatch 'Delegate impact fixture checks passed\.')) { throw "Clean fixture check failed: $root" }
    if (-not $expectSuccess -and ($code -eq 0 -or $text -notmatch 'Imported item was not persisted exactly once\.')) { throw "Seeded fixture did not fail as expected: $root" }
}
$fixture = (Resolve-Path -LiteralPath $FixtureRoot).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw "Output root already exists: $output" }
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../../..')).Path
$project = (Resolve-Path -LiteralPath $BrownPantsProject).Path
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Verify-FrozenInputs.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Frozen protocol verification failed.' }
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Verify-Inputs.ps1') -FixtureRoot $fixture
if ($LASTEXITCODE -ne 0) { throw 'The clean reference fixture must pass before preparation.' }
New-Item -ItemType Directory -Path $output | Out-Null
$sourceFiles = @()
$pairInfo = @()
foreach ($pair in 1..3) {
    $pairRoot = Join-Path $output ('pair-{0:D2}' -f $pair)
    New-Item -ItemType Directory -Path $pairRoot | Out-Null
    foreach ($condition in @('baseline','assisted')) {
        $source = Join-Path $pairRoot "$condition-source"
        Copy-Fixture $fixture $source
        Invoke-Check $source $true (Join-Path $pairRoot "$condition-clean-check.txt")
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Seed-DelegateImpact.ps1') -FixtureRoot $source
        if ($LASTEXITCODE -ne 0) { throw "Could not seed $condition pair $pair." }
        Invoke-Check $source $false (Join-Path $pairRoot "$condition-seeded-check.txt")
    }
    $baseline = Join-Path $pairRoot 'baseline-source'
    $assisted = Join-Path $pairRoot 'assisted-source'
    $baselineDigest = Get-TreeDigest $baseline
    $assistedDigest = Get-TreeDigest $assisted
    if ($baselineDigest -cne $assistedDigest) { throw "Pair $pair source copies differ before agent runs." }
    if ($pair -eq 1) { $sourceFiles = Get-TreeHashes $baseline }
    $impactArgs = @('impact', $assisted, '--changed', 'src/WorkRepository.cs', '--format', 'json', '--limit', '1000')
    Push-Location $repoRoot
    try { $impact = & dotnet run --project $project -c Release --no-launch-profile -- @impactArgs; if ($LASTEXITCODE -ne 0) { throw "BrownPants impact failed for pair $pair." } }
    finally { Pop-Location }
    $evidenceRoot = Join-Path $assisted 'BrownPantsEvidence'
    New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
    $impact | Set-Content -LiteralPath (Join-Path $evidenceRoot 'impact.json') -Encoding utf8
    $parsed = $impact | ConvertFrom-Json
    if (-not ($parsed.Impacts | Where-Object RelationshipKind -eq 'delegate')) { throw "BrownPants did not expose the seeded delegate impact in pair $pair." }
    foreach ($condition in @('baseline','assisted')) {
        $snapshot = Join-Path $output ("input-snapshots/pair-{0:D2}/{1}-source" -f $pair,$condition)
        Copy-Fixture (Join-Path $pairRoot "$condition-source") $snapshot
    }
    $pairInfo += [pscustomobject]@{ pair=$pair; sourceDigest=$baselineDigest; impactPath="pair-$('{0:D2}' -f $pair)/assisted-source/BrownPantsEvidence/impact.json" }
}
$preparation = [ordered]@{
    schemaVersion=1; taskSpecDigest=((Get-Content (Join-Path $PSScriptRoot 'acceptance-criteria.sha256') -Raw).Trim().Split(' ')[0]);
    answerKeyDigest=(Get-Content (Join-Path $PSScriptRoot 'answer-key.sha256') -Raw).Trim().Split(' ')[0];
    promptDigest=(Get-FileHash (Join-Path $PSScriptRoot 'task-prompt.md') -Algorithm SHA256).Hash.ToLowerInvariant();
    schemaDigest=(Get-FileHash (Join-Path $PSScriptRoot 'result.schema.json') -Algorithm SHA256).Hash.ToLowerInvariant();
    sourceFiles=$sourceFiles; pairs=$pairInfo; cleanChecksPassed=$true; seededChecksFailedAsExpected=$true; identicalSeededCode=$true
}
$preparation | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $output 'preparation.json') -Encoding utf8
Write-Output "Prepared three matched delegate-impact pairs at $output"
