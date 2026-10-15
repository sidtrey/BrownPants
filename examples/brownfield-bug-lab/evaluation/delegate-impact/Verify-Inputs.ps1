param([Parameter(Mandatory=$true)][string] $FixtureRoot)
$ErrorActionPreference = 'Stop'
$scenario = $PSScriptRoot
function Assert-Digest([string] $path, [string] $digestPath) {
    $expected = (Get-Content -LiteralPath $digestPath -Raw).Trim().Split(' ')[0]
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expected -cne $actual) { throw "Frozen input digest mismatch: $([IO.Path]::GetFileName($path))" }
}
Assert-Digest (Join-Path $scenario 'answer-key.json') (Join-Path $scenario 'answer-key.sha256')
& pwsh -NoProfile -File (Join-Path $scenario '../../../../scripts/acceptance-criteria-hash.ps1') -SpecPath (Join-Path $scenario 'task-spec.md')
if ($LASTEXITCODE -ne 0) { throw 'Frozen task acceptance criteria digest failed.' }
$root = (Resolve-Path -LiteralPath $FixtureRoot).Path
$project = Join-Path $root 'DelegateImpact.Fixture.csproj'
$output = & dotnet run --project $project -c Release 2>&1
$code = $LASTEXITCODE
if ($code -ne 0 -or ($output -join "`n") -notmatch 'Delegate impact fixture checks passed\.') {
    throw "Fixture check failed: $($output -join ' ')"
}
$repo = [IO.File]::ReadAllText((Join-Path $root 'src/WorkRepository.cs'))
if ($repo -notmatch 'public\s+void\s+Save\(WorkItem\s+item\)\s*(?:=>\s*_saved\.Add\(item\);|\{\s*_saved\.Add\(item\);\s*\})') {
    throw 'The repaired repository does not persist the supplied item.'
}
Write-Output 'Frozen task/key digests and repaired fixture check verified.'
