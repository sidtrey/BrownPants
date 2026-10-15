[CmdletBinding()]
param(
    [string] $ProjectRoot = (Get-Location).Path,
    [ValidateSet('codex', 'claude', 'copilot', 'both', 'all')]
    [string] $Agent = 'both'
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

$source = Join-Path $PSScriptRoot 'brownpants-context/SKILL.md'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Bundled BrownPants context skill not found at '$source'."
}

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
        throw "Skill destination resolves outside the selected project root: '$resolvedProbe'."
    }

    $ancestor = $probe
    while (-not $ancestor.Equals($resolvedRoot, $comparison)) {
        if ((Test-Path -LiteralPath $ancestor) -and
            ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Skill destination contains a symbolic link or reparse point: '$ancestor'."
        }
        $parent = Split-Path -Parent $ancestor
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $ancestor) { break }
        $ancestor = $parent
    }

    return $resolvedProbe
}

$targetDefinitions = @()
if ($Agent -in @('codex', 'both', 'all')) {
    $targetDefinitions += [pscustomobject]@{
        Agent = 'Codex'
        RelativeDirectory = '.agents/skills/brownpants-context'
        Directory = Join-Path $resolvedRoot '.agents/skills/brownpants-context'
    }
}
if ($Agent -in @('claude', 'both', 'all')) {
    $targetDefinitions += [pscustomobject]@{
        Agent = 'Claude Code'
        RelativeDirectory = '.claude/skills/brownpants-context'
        Directory = Join-Path $resolvedRoot '.claude/skills/brownpants-context'
    }
}
if ($Agent -in @('copilot', 'all')) {
    $targetDefinitions += [pscustomobject]@{
        Agent = 'GitHub Copilot'
        RelativeDirectory = '.github/skills/brownpants-context'
        Directory = Join-Path $resolvedRoot '.github/skills/brownpants-context'
    }
}

# Preflight every selected target before creating any directory. A conflict in
# a both-host install therefore cannot leave the other host partially installed.
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$targets = foreach ($definition in $targetDefinitions) {
    $destinationDirectory = $definition.Directory
    [void](Assert-ContainedPath -Path $destinationDirectory)
    $destination = Join-Path $destinationDirectory 'SKILL.md'
    $state = 'missing'

    if (Test-Path -LiteralPath $destination) {
        $destinationItem = Get-Item -LiteralPath $destination -Force
        if ($destinationItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
            throw "Skill destination is a symbolic link or reparse point; refusing installation: '$destination'."
        }
        if ($destinationItem.PSIsContainer) {
            throw "Skill destination is a directory, not a file: '$destination'."
        }
        $destinationHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        if ($destinationHash -eq $sourceHash) { $state = 'current' }
        else { throw "Refusing to overwrite a different skill at '$destination'. Review or move it, then rerun the installer." }
    }

    [pscustomobject]@{
        Agent = $definition.Agent
        RelativePath = Join-Path $definition.RelativeDirectory 'SKILL.md'
        Directory = $destinationDirectory
        Destination = $destination
        State = $state
    }
}

foreach ($target in $targets) {
    if ($target.State -eq 'current') {
        Write-Output "$($target.Agent) BrownPants context skill is already current: $($target.RelativePath)"
        continue
    }

    New-Item -ItemType Directory -Path $target.Directory -Force | Out-Null
    [void](Assert-ContainedPath -Path $target.Directory)
    [System.IO.File]::Copy($source, $target.Destination, $false)
    Write-Output "Installed $($target.Agent) BrownPants context skill: $($target.RelativePath)"
}
