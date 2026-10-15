[CmdletBinding()]
param(
    [string] $ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

try {
    $resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot -ErrorAction Stop).Path
} catch {
    throw "Project root does not exist or cannot be resolved: '$ProjectRoot'."
}
if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
    throw "Project root must be an existing directory: '$ProjectRoot'."
}

$source = Join-Path $PSScriptRoot 'prompts/brownpants-context.prompt.md'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Bundled BrownPants Copilot prompt not found at '$source'."
}

$destinationDirectory = Join-Path $resolvedRoot '.github/prompts'
$destination = Join-Path $destinationDirectory 'brownpants-context.prompt.md'
$separator = [System.IO.Path]::DirectorySeparatorChar
$rootPrefix = $resolvedRoot.TrimEnd([char[]]@('/', '\')) + $separator
$comparison = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    [StringComparison]::OrdinalIgnoreCase
} else {
    [StringComparison]::Ordinal
}

function Assert-ContainedPath {
    param([string] $Path)
    $probe = $Path
    while (-not (Test-Path -LiteralPath $probe)) {
        $parent = Split-Path -Parent $probe
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $probe) {
            throw "Could not find an existing parent for '$Path'."
        }
        $probe = $parent
    }
    $resolvedProbe = (Resolve-Path -LiteralPath $probe -ErrorAction Stop).Path
    if (-not $resolvedProbe.Equals($resolvedRoot, $comparison) -and
        -not $resolvedProbe.StartsWith($rootPrefix, $comparison)) {
        throw "Prompt destination resolves outside the selected project root: '$resolvedProbe'."
    }
    return $resolvedProbe
}

$resolvedExistingParent = Assert-ContainedPath -Path $destinationDirectory
if ($resolvedExistingParent -ne $resolvedRoot) {
    $existingAncestors = @()
    $probe = $destinationDirectory
    while ((Test-Path -LiteralPath $probe) -and $probe -ne $resolvedRoot) {
        $existingAncestors += Get-Item -LiteralPath $probe -Force
        $probe = Split-Path -Parent $probe
    }
    if ($existingAncestors | Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint }) {
        throw 'Prompt destination contains a symbolic link or reparse point; refusing installation.'
    }
}

if (Test-Path -LiteralPath $destination) {
    $destinationItem = Get-Item -LiteralPath $destination -Force
    if ($destinationItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
        throw 'Prompt destination is a symbolic link or reparse point; refusing installation.'
    }
    if (-not $destinationItem.PSIsContainer) {
        $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        $destinationHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        if ($sourceHash -eq $destinationHash) {
            Write-Output "BrownPants Copilot prompt is already current: $destination"
            exit 0
        }
        throw "Refusing to overwrite a different prompt at '$destination'. Review or move it, then rerun the installer."
    }
    throw "Prompt destination is a directory, not a file: '$destination'."
}

New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
[void](Assert-ContainedPath -Path $destinationDirectory)
$createdDirectories = @()
$probe = $destinationDirectory
while ($probe -ne $resolvedRoot -and (Test-Path -LiteralPath $probe)) {
    $createdDirectories += Get-Item -LiteralPath $probe -Force
    $probe = Split-Path -Parent $probe
}
if ($createdDirectories | Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint }) {
    throw 'Prompt destination contains a symbolic link or reparse point; refusing installation.'
}
[System.IO.File]::Copy($source, $destination, $false)
Write-Output "Installed BrownPants Copilot prompt: $destination"
