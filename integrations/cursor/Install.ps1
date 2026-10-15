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

$source = Join-Path $PSScriptRoot 'commands/brownpants-context.md'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Bundled Cursor command not found at '$source'."
}

$destinationDirectory = Join-Path $resolvedRoot '.cursor/commands'
$destination = Join-Path $destinationDirectory 'brownpants-context.md'
$separator = [System.IO.Path]::DirectorySeparatorChar
$rootPrefix = $resolvedRoot.TrimEnd([char[]]@('/', '\')) + $separator
$comparison = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    [StringComparison]::OrdinalIgnoreCase
} else {
    [StringComparison]::Ordinal
}

function Get-ExistingPath {
    param([string] $Path)
    $probe = [System.IO.Path]::GetFullPath($Path)
    while (-not (Test-Path -LiteralPath $probe)) {
        $parent = Split-Path -Parent $probe
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $probe) {
            throw "Could not find an existing parent for '$Path'."
        }
        $probe = $parent
    }
    return (Resolve-Path -LiteralPath $probe -ErrorAction Stop).Path
}

function Assert-ContainedPath {
    param([string] $Path)
    $resolvedProbe = Get-ExistingPath -Path $Path
    if (-not $resolvedProbe.Equals($resolvedRoot, $comparison) -and
        -not $resolvedProbe.StartsWith($rootPrefix, $comparison)) {
        throw "Cursor command destination resolves outside the selected project root: '$resolvedProbe'."
    }
    return $resolvedProbe
}

function Assert-NoReparsePoints {
    param([string] $Path)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $relative = [System.IO.Path]::GetRelativePath($resolvedRoot, $fullPath)
    if ([System.IO.Path]::IsPathRooted($relative) -or $relative -eq '..' -or
        $relative.StartsWith("..$separator", $comparison)) {
        throw "Cursor command destination resolves outside the selected project root: '$fullPath'."
    }

    $current = $resolvedRoot
    foreach ($segment in $relative.Split([char[]]@('/', '\'), [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $current = Join-Path $current $segment
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
                throw "Cursor command destination contains a symbolic link or reparse point: '$current'."
            }
        }
    }
}

[void](Assert-ContainedPath -Path $destinationDirectory)
Assert-NoReparsePoints -Path $destinationDirectory

if (Test-Path -LiteralPath $destination) {
    Assert-NoReparsePoints -Path $destination
    $destinationItem = Get-Item -LiteralPath $destination -Force
    if ($destinationItem.PSIsContainer) {
        throw "Cursor command destination is a directory, not a file: '$destination'."
    }
    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    if ($sourceHash -eq $destinationHash) {
        Write-Output "BrownPants Cursor command is already current: $destination"
        exit 0
    }
    throw "Refusing to overwrite a different Cursor command at '$destination'. Review or move it, then rerun the installer."
}

New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
[void](Assert-ContainedPath -Path $destinationDirectory)
Assert-NoReparsePoints -Path $destinationDirectory
[System.IO.File]::Copy($source, $destination, $false)
Write-Output "Installed BrownPants Cursor command: $destination"
