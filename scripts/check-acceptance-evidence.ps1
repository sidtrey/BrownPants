[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $RepositoryRoot,
    [Parameter(Mandatory = $true)][string] $SpecPath,
    [Parameter(Mandatory = $true)][string] $TasksPath,
    [Parameter(Mandatory = $true)][string] $EvidencePath
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
$pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }

function Resolve-InputPath {
    param([string] $Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return [System.IO.Path]::GetFullPath($Path) }
    return [System.IO.Path]::GetFullPath((Join-Path $root $Path))
}

function Get-Section {
    param([string] $Text, [string] $Heading)
    $lines = $Text -replace "`r`n?", "`n" -split "`n"
    $index = [Array]::IndexOf($lines, $Heading)
    if ($index -lt 0) { throw "Required spec section '$Heading' was not found." }
    $end = $index + 1
    while ($end -lt $lines.Length -and $lines[$end] -notmatch '^##\s') { $end++ }
    return ($lines[($index + 1)..($end - 1)] -join "`n")
}

foreach ($path in @((Resolve-InputPath $SpecPath), (Resolve-InputPath $TasksPath), (Resolve-InputPath $EvidencePath))) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required evidence input does not exist: $path" }
}
$spec = [System.IO.File]::ReadAllText((Resolve-InputPath $SpecPath))
$criterionIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($section in @(
    (Get-Section $spec '## User Scenarios & Testing'),
    (Get-Section $spec '## Success Criteria')
)) {
    foreach ($match in [regex]::Matches($section, '(?m)^\s*(?:\d+\.\s*)?(?:[-*]\s*)?\*\*(?<id>(?:AC|SC)-\d{3})\*\*')) {
        $criterionId = $match.Groups['id'].Value
        if (-not $criterionIds.Add($criterionId)) { throw "Duplicate criterion ID in acceptance sections: $criterionId" }
    }
}
if ($criterionIds.Count -eq 0) { throw 'No AC-### or SC-### identifiers were found in the acceptance sections.' }

$taskText = [System.IO.File]::ReadAllText((Resolve-InputPath $TasksPath))
$taskStates = @{}
foreach ($match in [regex]::Matches($taskText, '(?m)^\s*-\s+\[(?<state> |x|X)\]\s+(?<id>T\d{3})\b')) {
    $id = $match.Groups['id'].Value
    if ($taskStates.ContainsKey($id)) { throw "Duplicate task ID in task list: $id" }
    $taskStates[$id] = if ($match.Groups['state'].Value -match '[xX]') { 'completed' } else { 'incomplete' }
}
if ($taskStates.Count -eq 0) { throw 'No task IDs were found in the task list.' }

try { $ledger = Get-Content -LiteralPath (Resolve-InputPath $EvidencePath) -Raw | ConvertFrom-Json -AsHashtable }
catch { throw "Evidence ledger is not valid JSON: $($_.Exception.Message)" }
if ($ledger -isnot [System.Collections.IDictionary] -or $ledger.schemaVersion -isnot [long] -or $ledger.schemaVersion -ne 1 -or $ledger.tasks -isnot [array]) {
    throw 'Evidence ledger must be an object with schemaVersion 1 and a tasks array.'
}
$ledgerKeys = @($ledger.Keys | ForEach-Object { [string]$_ })
if (@($ledgerKeys | Where-Object { $_ -notin @('schemaVersion', 'tasks') }).Count -gt 0) { throw 'Evidence ledger contains unsupported top-level properties.' }

$entries = @{}
$coveredCriteria = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($entry in @($ledger.tasks)) {
    if ($entry -isnot [System.Collections.IDictionary]) { throw 'Each evidence task entry must be an object.' }
    $required = @('taskId', 'criterionIds', 'verificationCommand', 'evidencePaths', 'result')
    $keys = @($entry.Keys | ForEach-Object { [string]$_ })
    foreach ($key in $required) { if ($key -notin $keys) { throw "Evidence entry is missing '$key'." } }
    if (@($keys | Where-Object { $_ -notin $required }).Count -gt 0) { throw 'Evidence entry contains unsupported properties.' }

    if ($entry.taskId -isnot [string]) { throw 'Evidence taskId values must be strings.' }
    $id = [string]$entry.taskId
    if ($id -notmatch '^T\d{3}$') { throw "Invalid task ID in evidence ledger: '$id'." }
    if (-not $taskStates.ContainsKey($id)) { throw "Evidence ledger references unknown task ID: $id." }
    if ($entries.ContainsKey($id)) { throw "Duplicate evidence record for task $id." }
    if ($entry.criterionIds -isnot [array]) { throw "Task $id criterionIds must be an array." }
    foreach ($criterionValue in $entry.criterionIds) { if ($criterionValue -isnot [string]) { throw "Task $id criterionIds must contain only strings." } }
    $ids = @($entry.criterionIds | ForEach-Object { [string]$_ })
    if ($ids.Count -eq 0) { throw "Task $id must map to at least one criterion." }
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($criterion in $ids) {
        if ($criterion -notmatch '^(AC|SC)-\d{3}$' -or -not $criterionIds.Contains($criterion)) { throw "Task $id references unknown criterion ID: $criterion." }
        if (-not $seen.Add($criterion)) { throw "Task $id lists criterion $criterion more than once." }
        [void]$coveredCriteria.Add($criterion)
    }
    if ($entry.verificationCommand -isnot [string]) { throw "Task $id verificationCommand must be a string." }
    $command = [string]$entry.verificationCommand
    if ([string]::IsNullOrWhiteSpace($command) -or $command -match '^(TODO|TBD|N/?A|<.*>)$') { throw "Task $id must record a concrete verification command." }
    if ($entry.result -isnot [string] -or ($entry.result -cne 'planned' -and $entry.result -cne 'passed')) { throw "Task $id result must be 'planned' or 'passed'." }
    if ($taskStates[$id] -eq 'completed' -and $entry.result -cne 'passed') { throw "Completed task $id must have result 'passed'." }
    if ($taskStates[$id] -eq 'incomplete' -and $entry.result -cne 'planned') { throw "Incomplete task $id must have result 'planned'." }
    if ($entry.evidencePaths -isnot [array]) { throw "Task $id evidencePaths must be an array." }
    foreach ($evidenceValue in $entry.evidencePaths) { if ($evidenceValue -isnot [string]) { throw "Task $id evidencePaths must contain only strings." } }
    $paths = @($entry.evidencePaths | ForEach-Object { [string]$_ })
    if ($taskStates[$id] -eq 'completed' -and $paths.Count -eq 0) { throw "Completed task $id must identify at least one evidence file." }
    $pathSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($relative in $paths) {
        if ([string]::IsNullOrWhiteSpace($relative) -or [System.IO.Path]::IsPathRooted($relative) -or $relative -match '^(?:[\\/]|[A-Za-z]:[\\/])' -or $relative -match '(^|[\\/])\.\.([\\/]|$)') {
            throw "Task $id has an unsafe evidence path: '$relative'."
        }
        $normalized = $relative.Replace('\', [System.IO.Path]::DirectorySeparatorChar).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        $full = [System.IO.Path]::GetFullPath((Join-Path $root $normalized))
        $prefix = $root + [System.IO.Path]::DirectorySeparatorChar
        if (-not $full.StartsWith($prefix, $pathComparison)) { throw "Task $id evidence path escapes the repository: '$relative'." }
        if (-not $pathSet.Add($relative)) { throw "Task $id repeats evidence path '$relative'." }
        if ($taskStates[$id] -eq 'completed') {
            if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "Task $id evidence file does not exist: '$relative'." }
            $resolvedEvidence = (Resolve-Path -LiteralPath $full).Path
            if (-not $resolvedEvidence.StartsWith($prefix, $pathComparison)) { throw "Task $id evidence file resolves outside the repository: '$relative'." }
        }
    }
    $entries[$id] = $true
}

foreach ($id in ($taskStates.Keys | Sort-Object)) { if (-not $entries.ContainsKey($id)) { throw "Task $id is missing from the evidence ledger." } }
foreach ($id in ($criterionIds | Sort-Object)) { if (-not $coveredCriteria.Contains($id)) { throw "Acceptance criterion $id is not mapped to any task." } }
Write-Output "Acceptance evidence verified: $($taskStates.Count) tasks, $($criterionIds.Count) criteria. Structural evidence only; review semantic test coverage."
