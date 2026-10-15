$ErrorActionPreference = 'Stop'
$manifest = Join-Path $PSScriptRoot 'protocol.sha256'
if (-not (Test-Path -LiteralPath $manifest)) { throw 'Frozen protocol digest manifest is missing.' }
foreach ($line in Get-Content -LiteralPath $manifest) {
    if ($line -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw "Invalid frozen protocol digest line: $line" }
    $expected = $Matches[1].ToLowerInvariant()
    $path = Join-Path $PSScriptRoot $Matches[2]
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Frozen protocol input is missing: $($Matches[2])" }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -cne $expected) { throw "Frozen protocol input changed: $($Matches[2])" }
}
& pwsh -NoProfile -File (Join-Path $PSScriptRoot '../../../../scripts/acceptance-criteria-hash.ps1') -SpecPath (Join-Path $PSScriptRoot 'task-spec.md')
if ($LASTEXITCODE -ne 0) { throw 'Frozen acceptance criteria hash verification failed.' }
Write-Output 'Frozen delegate-impact protocol inputs verified.'
