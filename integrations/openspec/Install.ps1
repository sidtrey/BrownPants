[CmdletBinding()]
param(
    [string] $ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot -ErrorAction Stop).Path
$openSpecRoot = Join-Path $resolvedRoot 'openspec'
if (-not (Test-Path -LiteralPath $openSpecRoot -PathType Container)) {
    throw "OpenSpec project root not found at '$openSpecRoot'. Initialize OpenSpec first."
}

$source = Join-Path $PSScriptRoot 'skills/openspec-brownpants-context/SKILL.md'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Bundled BrownPants skill not found at '$source'."
}

$destinationDirectory = Join-Path $resolvedRoot '.agents/skills/openspec-brownpants-context'
$destination = Join-Path $destinationDirectory 'SKILL.md'
$probe = $destinationDirectory
while (-not (Test-Path -LiteralPath $probe)) {
    $parent = Split-Path -Parent $probe
    if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $probe) {
        throw "Could not find an existing parent for '$destinationDirectory'."
    }
    $probe = $parent
}

$resolvedProbe = (Resolve-Path -LiteralPath $probe -ErrorAction Stop).Path
$separator = [System.IO.Path]::DirectorySeparatorChar
$rootPrefix = $resolvedRoot.TrimEnd([char[]]@('/', '\')) + $separator
$comparison = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    [StringComparison]::OrdinalIgnoreCase
} else {
    [StringComparison]::Ordinal
}
if (-not $resolvedProbe.StartsWith($rootPrefix, $comparison) -and
    -not $resolvedProbe.Equals($resolvedRoot, $comparison)) {
    throw "Skill destination resolves outside the OpenSpec project root: '$resolvedProbe'."
}

if (Test-Path -LiteralPath $destination -PathType Leaf) {
    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    if ($sourceHash -eq $destinationHash) {
        Write-Output "BrownPants OpenSpec skill is already current: $destination"
        exit 0
    }
    throw "Refusing to overwrite a different skill at '$destination'. Review or move it, then rerun the installer."
}
if (Test-Path -LiteralPath $destination -PathType Container) {
    throw "Skill destination is a directory, not a file: '$destination'."
}

New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
[System.IO.File]::Copy($source, $destination, $false)
Write-Output "Installed BrownPants OpenSpec skill: $destination"
