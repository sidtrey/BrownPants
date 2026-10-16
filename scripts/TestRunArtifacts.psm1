Set-StrictMode -Version Latest

function Get-BrownPantsRunRoot {
    param([Parameter(Mandatory)][string] $RepositoryRoot)
    $repo = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $repoPrefix = $repo.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) + [IO.Path]::DirectorySeparatorChar
    $testResults = [IO.Path]::GetFullPath((Join-Path $repo 'TestResults'))
    if (-not $testResults.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'TestResults root escapes the repository.' }
    $managed = [IO.Path]::GetFullPath((Join-Path $testResults '.brownpants/runs'))
    if (-not $managed.StartsWith($testResults.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Managed run root escapes TestResults.' }
    foreach ($path in @($testResults, (Join-Path $testResults '.brownpants'), $managed)) {
        if (Test-Path -LiteralPath $path) {
            $item = Get-Item -LiteralPath $path -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Refusing reparse-point path: $path" }
        }
    }
    return $managed
}

function New-BrownPantsRun {
    param([Parameter(Mandatory)][string] $RepositoryRoot, [Parameter(Mandatory)][string] $Kind)
    if ($Kind -notmatch '^[a-z0-9-]{1,40}$') { throw 'Run kind must contain lowercase letters, digits, or hyphens (maximum 40 characters).' }
    $root = Get-BrownPantsRunRoot $RepositoryRoot
    [void](New-Item -ItemType Directory -Path $root -Force)
    $runId = '{0}-{1}-{2}' -f [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'), $Kind, [Guid]::NewGuid().ToString('N')
    $directory = Join-Path $root $runId
    [void](New-Item -ItemType Directory -Path $directory)
    $manifest = [ordered]@{ schema = 1; owner = 'BrownPants'; runId = $runId; kind = $Kind; state = 'running'; createdUtc = [DateTime]::UtcNow.ToString('o'); completedUtc = $null }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $directory '.brownpants-run.json') -Encoding utf8
    return $directory
}

function Set-BrownPantsRunState {
    param([Parameter(Mandatory)][string] $RunDirectory, [Parameter(Mandatory)][ValidateSet('succeeded','failed')][string] $State)
    $directory = [IO.Path]::GetFullPath($RunDirectory)
    $manifestPath = Join-Path $directory '.brownpants-run.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Run ownership manifest is missing: $directory" }
    if (((Get-Item -LiteralPath $directory -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or ((Get-Item -LiteralPath $manifestPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Refusing to update a run through a reparse point.' }
    $record = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($record.owner -ne 'BrownPants' -or $record.runId -ne (Split-Path -Leaf $directory)) { throw 'Run ownership manifest does not match its directory.' }
    $record.state = $State
    $record.completedUtc = [DateTime]::UtcNow.ToString('o')
    $record | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8
}

function Test-NoReparsePoints {
    param([Parameter(Mandatory)][string] $Directory)
    $pending = [Collections.Generic.Stack[string]]::new(); $pending.Push($Directory)
    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        foreach ($child in [IO.Directory]::EnumerateFileSystemEntries($current)) {
            $item = Get-Item -LiteralPath $child -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { return $false }
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
        }
    }
    return $true
}

function Invoke-BrownPantsRunCleanup {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string] $RepositoryRoot, [int] $MaxSuccessfulRuns = 5, [int] $SuccessfulMaxAgeDays = 14, [int] $FailedRetentionDays = 7, [switch] $Apply)
    if ($MaxSuccessfulRuns -lt 1 -or $SuccessfulMaxAgeDays -lt 1 -or $FailedRetentionDays -lt 1) { throw 'Retention values must be positive.' }
    $root = Get-BrownPantsRunRoot $RepositoryRoot
    $now = [DateTime]::UtcNow
    $runs = [Collections.Generic.List[object]]::new()
    if (Test-Path -LiteralPath $root -PathType Container) {
        foreach ($dir in Get-ChildItem -LiteralPath $root -Directory -Force | Sort-Object Name) {
            if (($dir.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { Write-Output "SKIP reparse point: $($dir.FullName)"; continue }
            $marker = Join-Path $dir.FullName '.brownpants-run.json'
            if (-not (Test-Path -LiteralPath $marker -PathType Leaf) -or ((Get-Item -LiteralPath $marker -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { Write-Output "SKIP unmarked run: $($dir.FullName)"; continue }
            try { $record = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json; $completed = [DateTime]::Parse($record.completedUtc).ToUniversalTime() }
            catch { Write-Output "SKIP invalid manifest: $($dir.FullName)"; continue }
            if ($record.schema -ne 1 -or $record.owner -ne 'BrownPants' -or $record.runId -ne $dir.Name -or $record.state -notin @('succeeded','failed')) { Write-Output "SKIP untrusted or active run: $($dir.FullName)"; continue }
            $runs.Add([pscustomobject]@{ Directory = $dir.FullName; State = [string]$record.state; Completed = $completed })
        }
    }
    $successful = @($runs | Where-Object State -eq 'succeeded' | Sort-Object -Property @{ Expression = 'Completed'; Descending = $true }, @{ Expression = 'Directory'; Descending = $false })
    $remove = [Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $successful.Count; $i++) {
        if (($now - $successful[$i].Completed).TotalDays -gt $SuccessfulMaxAgeDays -or $i -ge $MaxSuccessfulRuns) { $remove.Add($successful[$i]) }
    }
    foreach ($run in @($runs | Where-Object { $_.State -eq 'failed' -and ($now - $_.Completed).TotalDays -gt $FailedRetentionDays })) { $remove.Add($run) }
    $rootPrefix = $root.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) + [IO.Path]::DirectorySeparatorChar
    $comparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    foreach ($run in $remove | Sort-Object Directory -Unique) {
        $target = [IO.Path]::GetFullPath($run.Directory)
        if (-not $target.StartsWith($rootPrefix, $comparison) -or (Split-Path -Parent $target) -cne $root) { Write-Output "SKIP unsafe path: $target"; continue }
        if (-not (Test-NoReparsePoints $target)) { Write-Output "SKIP run contains reparse point: $target"; continue }
        if ($Apply) { Remove-Item -LiteralPath $target -Recurse -Force; Write-Output "REMOVED $($run.State) run: $target" }
        else { Write-Output "WOULD REMOVE $($run.State) run: $target" }
    }
    $repo = [IO.Path]::GetFullPath($RepositoryRoot)
    $legacyRoot = Join-Path $repo 'TestResults'
    if (Test-Path -LiteralPath $legacyRoot -PathType Container) {
        $legacy = @(Get-ChildItem -LiteralPath $legacyRoot -Force | Where-Object Name -ne '.brownpants' | Sort-Object Name)
        if ($legacy.Count -gt 0) {
            Write-Output "UNMANAGED (preserved): $($legacy.Count) legacy entries under $legacyRoot. Review with scripts/cleanup-test-data.ps1."
            foreach ($entry in $legacy | Select-Object -First 10) { Write-Output "  $($entry.Name)" }
            if ($legacy.Count -gt 10) { Write-Output "  ... $($legacy.Count - 10) more" }
        }
    }
}

Export-ModuleMember -Function New-BrownPantsRun, Set-BrownPantsRunState, Invoke-BrownPantsRunCleanup, Get-BrownPantsRunRoot
