param(
    [Parameter(Mandatory=$true)][string] $DemoRoot,
    [Parameter(Mandatory=$true)][string] $OutputRoot,
    [string] $BrownPantsProject = (Join-Path $PSScriptRoot '../../../../src/BrownPants.Cli')
)
$ErrorActionPreference = 'Stop'

function Copy-Tree([string] $from, [string] $to) {
    $source = (Resolve-Path -LiteralPath $from).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    New-Item -ItemType Directory -Path $to -Force | Out-Null
    Get-ChildItem -LiteralPath $source -Recurse -File -Force |
        Where-Object { $_.FullName.Substring($source.Length) -notmatch '(^|[\\/])(bin|obj)([\\/]|$)' } |
        ForEach-Object {
            $relative = $_.FullName.Substring($source.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar))
            $destination = Join-Path $to $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination
        }
}

function Get-CodeTreeFileHashes([string] $root) {
    $rootPath = (Resolve-Path -LiteralPath $root).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $parts = Get-ChildItem -LiteralPath $rootPath -Recurse -File -Force |
        Where-Object { $_.FullName -notmatch '(^|[\\/])(bin|obj|BrownPantsEvidence|control)([\\/]|$)' } |
        ForEach-Object {
            $relative = $_.FullName.Substring($rootPath.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)).Replace([IO.Path]::DirectorySeparatorChar, '/')
            [pscustomobject]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
        } | Sort-Object -CaseSensitive
    return ,$parts
}

function Get-CodeTreeDigest([string] $root) {
    $parts = Get-CodeTreeFileHashes $root
    $rows = $parts | ForEach-Object { "$($_.path)|$($_.sha256)" }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($rows -join "`n"))
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Run-Check([string] $sourceRoot, [string] $logPath, [bool] $expectSuccess) {
    Push-Location $sourceRoot
    try {
        $output = & dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release 2>&1
        $exitCode = $LASTEXITCODE
        $output | Set-Content -LiteralPath $logPath -Encoding utf8
        $text = $output -join "`n"
    } finally { Pop-Location }
    if ($expectSuccess) {
        if ($exitCode -ne 0 -or $text -notmatch 'LegacyOrderDesk checks passed\.') { throw "Clean check failed in '$sourceRoot'; see '$logPath'." }
    } else {
        if ($exitCode -eq 0 -or $text -notmatch 'Single-item overload did not save exactly one order\.') { throw "Seeded check did not fail with the expected constructor symptom in '$sourceRoot'; see '$logPath'." }
    }
    return $text
}

$demo = (Resolve-Path -LiteralPath $DemoRoot).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw "Output root already exists; choose a new path: $output" }
$brownPantsProjectPath = (Resolve-Path -LiteralPath $BrownPantsProject).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../../..')).Path
$scenario = $PSScriptRoot
$taskSpec = Join-Path $scenario 'task-spec.md'
& pwsh -NoProfile -File (Join-Path $repositoryRoot 'scripts/acceptance-criteria-hash.ps1') -SpecPath $taskSpec
if ($LASTEXITCODE -ne 0) { throw 'Constructor task acceptance hash verification failed.' }

New-Item -ItemType Directory -Path $output | Out-Null
New-Item -ItemType Directory -Path (Join-Path $output 'agent-results') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $output 'brownpants-evidence-assisted') | Out-Null
foreach ($condition in @('baseline', 'assisted')) {
    $sourceRoot = Join-Path $output "$condition-source"
    Copy-Tree (Join-Path $demo 'managed') (Join-Path $sourceRoot 'managed')
    Copy-Tree (Join-Path $demo 'legacy') (Join-Path $sourceRoot 'legacy')
    $control = Join-Path $sourceRoot 'control'
    New-Item -ItemType Directory -Path $control | Out-Null
    foreach ($name in @('bug-report.md', 'task-spec.md', 'acceptance-criteria.sha256', 'task-prompt.md', 'result.schema.json')) {
        Copy-Item -LiteralPath (Join-Path $scenario $name) -Destination $control
    }
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'scripts/acceptance-criteria-hash.ps1') -Destination $control
    Run-Check $sourceRoot (Join-Path $output "$condition-clean-check.txt") $true | Out-Null
    & pwsh -NoProfile -File (Join-Path $scenario 'Seed-ConstructorBug.ps1') -DemoRoot $sourceRoot
    if ($LASTEXITCODE -ne 0) { throw "Could not seed the constructor defect in '$condition'." }
    Run-Check $sourceRoot (Join-Path $output "$condition-seeded-precheck.txt") $false | Out-Null
}

$baseline = Join-Path $output 'baseline-source'
$assisted = Join-Path $output 'assisted-source'
$baselineDigest = Get-CodeTreeDigest $baseline
$assistedDigest = Get-CodeTreeDigest $assisted
if ($baselineDigest -ne $assistedDigest) { throw 'Managed and legacy code copies differ before agent execution.' }
$sourceFiles = Get-CodeTreeFileHashes $baseline

$impactArguments = @('impact', (Join-Path $assisted 'managed'), '--changed', 'Workflows/OrderImportWorkflow.cs', '--format', 'markdown', '--limit', '1000')
Push-Location $repositoryRoot
try {
    $impact = & dotnet run --project $brownPantsProjectPath --no-launch-profile -- @impactArguments
    if ($LASTEXITCODE -ne 0) { throw 'BrownPants constructor-impact analysis failed.' }
} finally { Pop-Location }
$evidenceRoot = Join-Path $output 'brownpants-evidence-assisted'
$impact | Set-Content -LiteralPath (Join-Path $evidenceRoot 'impact.md') -Encoding utf8
$impactText = $impact -join "`n"
$hasConstructorEvidence = $impactText.Contains(' constructs ', [StringComparison]::Ordinal)
$hasTestPathEvidence = $impactText.Contains('test-path candidate', [StringComparison]::Ordinal)
if ((-not $hasConstructorEvidence) -or (-not $hasTestPathEvidence)) {
    throw 'BrownPants impact output did not expose constructor and test-path evidence.'
}
$evidenceArguments = @('evidence', $assisted, '--format', 'json', '--limit', '1000')
Push-Location $repositoryRoot
try {
    $evidence = & dotnet run --project $brownPantsProjectPath --no-launch-profile -- @evidenceArguments
    if ($LASTEXITCODE -ne 0) { throw 'BrownPants evidence export failed.' }
} finally { Pop-Location }
$evidence | Set-Content -LiteralPath (Join-Path $evidenceRoot 'evidence.json') -Encoding utf8
$evidenceReport = $evidence | ConvertFrom-Json
foreach ($requiredSignal in @('DI registration', 'DI resolution')) {
    if (-not ($evidenceReport.Findings | Where-Object { $_.Signal -ceq $requiredSignal })) {
        throw "BrownPants evidence export did not include the expected '$requiredSignal' candidate."
    }
}
$visibleEvidence = Join-Path $assisted 'BrownPantsEvidence'
New-Item -ItemType Directory -Path $visibleEvidence | Out-Null
Copy-Item -LiteralPath (Join-Path $evidenceRoot 'impact.md') -Destination $visibleEvidence
Copy-Item -LiteralPath (Join-Path $evidenceRoot 'evidence.json') -Destination $visibleEvidence

[pscustomobject]@{
    schemaVersion = 1
    cleanChecksPassed = $true
    seededChecksFailedAsExpected = $true
    identicalSeededCode = $true
    seededCodeDigest = $baselineDigest
    sourceFiles = @($sourceFiles)
    assistedEvidenceFiles = @('impact.md', 'evidence.json')
    outputRoot = $output
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'preparation.json') -Encoding utf8

Write-Output "Prepared matched constructor-bug copies at $output"
Write-Output "Clean checks passed; both seeded checks failed with the expected symptom. Seeded code SHA-256: $baselineDigest"
Write-Output 'Only assisted-source contains BrownPantsEvidence/impact.md and evidence.json; the key and scorer remain outside both copies.'
