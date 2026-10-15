param(
    [Parameter(Mandatory=$true)][string] $DemoRoot,
    [Parameter(Mandatory=$true)][string] $OutputRoot,
    [string] $BrownPantsProject = (Join-Path $PSScriptRoot '../../../src/BrownPants.Cli')
)
$ErrorActionPreference = 'Stop'
function Copy-SourceTree([string]$sourceDirectory, [string]$destinationDirectory) {
    $sourcePrefix = (Resolve-Path -LiteralPath $sourceDirectory).Path.TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $sourceDirectory -Recurse -File -Force |
        Where-Object { $_.FullName.Substring($sourcePrefix.Length) -notmatch '(^|[\\/])(bin|obj)([\\/]|$)' } |
        ForEach-Object {
            $relative = $_.FullName.Substring($sourcePrefix.Length)
            $target = Join-Path $destinationDirectory $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $target
        }
}
$source = (Resolve-Path -LiteralPath $DemoRoot).Path
$brownPantsProjectPath = (Resolve-Path -LiteralPath $BrownPantsProject).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw "Output root already exists; choose a new path: $output" }
New-Item -ItemType Directory -Path $output | Out-Null
New-Item -ItemType Directory -Path (Join-Path $output 'agent-results') | Out-Null
foreach ($condition in @('baseline','assisted')) {
    $destination = Join-Path $output "$condition-source"
    New-Item -ItemType Directory -Path $destination | Out-Null
    Copy-SourceTree (Join-Path $source 'managed') (Join-Path $destination 'managed')
    Copy-SourceTree (Join-Path $source 'legacy') (Join-Path $destination 'legacy')
    $patch = Join-Path $PSScriptRoot 'Seed-BrownfieldBug.ps1'
    & $patch -DemoRoot $destination
}
$control = Join-Path $output 'control'
New-Item -ItemType Directory -Path $control | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'evaluation/bug-report.md') -Destination $control
Copy-Item -LiteralPath (Join-Path $source 'evaluation/task-prompt.md') -Destination $control
Copy-Item -LiteralPath (Join-Path $source 'evaluation/task-spec.md') -Destination $control
Copy-Item -LiteralPath (Join-Path $source 'evaluation/acceptance-criteria.sha256') -Destination $control
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../../../scripts/acceptance-criteria-hash.ps1') -Destination (Join-Path $control 'acceptance-criteria-hash.ps1')
Copy-Item -LiteralPath (Join-Path $source 'evaluation/result.schema.md') -Destination $control
$evidence = Join-Path $output 'brownpants-evidence-assisted'
New-Item -ItemType Directory -Path $evidence | Out-Null
$assisted = Join-Path $output 'assisted-source'
function Invoke-BrownPants([string[]]$commandArguments) {
    $arguments = @('run','--project',$brownPantsProjectPath,'--no-launch-profile','--') + $commandArguments
    Push-Location $repositoryRoot
    try { & dotnet @arguments } finally { Pop-Location }
}
$inspect = Invoke-BrownPants @('inspect',$assisted,'--format','markdown','--limit','1000')
if ($LASTEXITCODE -ne 0) { throw 'BrownPants inspect failed.' }
$inspect | Set-Content -Encoding utf8 (Join-Path $evidence 'inspect.md')
$investigate = Invoke-BrownPants @('investigate',$assisted,'--clue','RequiresRetry','--clue','OrderRepository','--format','markdown','--limit','1000')
if ($LASTEXITCODE -ne 0) { throw 'BrownPants investigate failed.' }
$investigate | Set-Content -Encoding utf8 (Join-Path $evidence 'investigate.md')
$impact = Invoke-BrownPants @('impact',(Join-Path $assisted 'managed'),'--changed','Persistence/OrderRepository.cs','--format','markdown','--limit','1000')
if ($LASTEXITCODE -ne 0) { throw 'BrownPants impact failed.' }
$impact | Set-Content -Encoding utf8 (Join-Path $evidence 'impact.md')
$visibleEvidence = Join-Path $assisted 'BrownPantsEvidence'
New-Item -ItemType Directory -Path $visibleEvidence | Out-Null
Copy-Item -LiteralPath (Join-Path $evidence 'inspect.md') -Destination $visibleEvidence
Copy-Item -LiteralPath (Join-Path $evidence 'investigate.md') -Destination $visibleEvidence
Copy-Item -LiteralPath (Join-Path $evidence 'impact.md') -Destination $visibleEvidence
foreach ($condition in @('baseline','assisted')) {
    $root = Join-Path $output "$condition-source"
    Push-Location $root
    try {
        $testOutput = & dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release 2>&1
        $testCode = $LASTEXITCODE
        $testOutput | Set-Content -Encoding utf8 (Join-Path $output "$condition-seeded-precheck.txt")
        if ($testCode -eq 0) { throw "The seeded regression unexpectedly passed in $condition." }
    } finally { Pop-Location }
}
$seedHashes = foreach ($condition in @('baseline','assisted')) {
    $file = Join-Path $output "$condition-source/managed/Persistence/OrderRepository.cs"
    (Get-FileHash -Algorithm SHA256 -LiteralPath $file).Hash
}
if ($seedHashes[0] -ne $seedHashes[1]) { throw 'Seeded source hashes differ between conditions.' }
Write-Output "Prepared matched copies at $output"
Write-Output "Both prechecks failed as expected; seeded source SHA-256: $($seedHashes[0])"
Write-Output "Agent worktrees exclude the answer key and score script. Share the same task files under control/; give only assisted-source the reports under brownpants-evidence-assisted/."
