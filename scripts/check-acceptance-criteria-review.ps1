[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $BaseRef,
    [Parameter(Mandatory = $true)][string] $HeadRef,
    [switch] $Reviewed,
    [string] $RepositoryRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($RepositoryRoot)

function Invoke-GitText {
    param([string[]] $Arguments)
    $output = & git -C $root @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Git command failed ($($Arguments -join ' ')): $($output -join "`n")" }
    return ($output | Out-String).TrimEnd("`r", "`n")
}

function Get-GitFile {
    param([string] $Revision, [string] $Path)
    return Invoke-GitText @('show', "${Revision}:$Path")
}

function Get-ReviewSection {
    param([string] $Text, [string] $Heading)
    $lines = $Text -replace "`r`n?", "`n" -split "`n"
    $index = [Array]::IndexOf($lines, "## $Heading")
    if ($index -lt 0) { throw "Review record is missing the '## $Heading' section." }
    $end = $index + 1
    while ($end -lt $lines.Length -and $lines[$end] -notmatch '^##\s') { $end++ }
    return ($lines[($index + 1)..($end - 1)] -join "`n").Trim()
}

$null = Invoke-GitText @('rev-parse', '--verify', $BaseRef)
$null = Invoke-GitText @('rev-parse', '--verify', $HeadRef)
$diff = Invoke-GitText @('diff', '--name-status', '--find-renames', "$BaseRef...$HeadRef", '--', 'specs')
$changes = @{}
foreach ($line in ($diff -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
    $parts = $line -split "`t"
    $status = $parts[0]
    if ($status -match '^R\d+$') {
        $changes[$parts[1]] = [pscustomobject]@{ Status = 'R'; OldPath = $parts[1]; NewPath = $parts[2] }
        $changes[$parts[2]] = [pscustomobject]@{ Status = 'R'; OldPath = $parts[1]; NewPath = $parts[2] }
    } else {
        $changes[$parts[1]] = [pscustomobject]@{ Status = $status; OldPath = $parts[1]; NewPath = $parts[1] }
    }
}

$baselinePaths = @($changes.Keys | Where-Object { $_ -match '^specs/[^/]+/acceptance-criteria\.sha256$' } | Sort-Object -Unique)
foreach ($path in $baselinePaths) {
    $change = $changes[$path]
    if ($change.Status -eq 'R') { throw "Acceptance baseline may not be renamed: '$($change.OldPath)' -> '$($change.NewPath)'." }
    if ($change.Status -eq 'D') { throw "Acceptance baseline already recorded on the base revision may not be deleted: '$path'." }
    if ($change.Status -eq 'A') {
        $featureDir = $path.Substring(0, $path.LastIndexOf('/'))
        $specPath = "$featureDir/spec.md"
        $specAtBase = $true
        try { $null = Get-GitFile $BaseRef $specPath } catch { $specAtBase = $false }
        if (-not $specAtBase) { continue }
    }
    if ($change.Status -notin @('A', 'M')) { continue }

    if (-not $Reviewed) { throw "Baseline amendment '$path' requires the acceptance-criteria-reviewed label." }
    $featureDir = $path.Substring(0, $path.LastIndexOf('/'))
    $recordPath = "$featureDir/acceptance-criteria-review.md"
    if (-not $changes.ContainsKey($recordPath) -or $changes[$recordPath].Status -eq 'D' -or $changes[$recordPath].Status -eq 'R') {
        throw "Baseline amendment '$path' requires a review record changed in this pull request: '$recordPath'."
    }
    $oldDigest = ''
    if ($change.Status -eq 'M') {
        $oldLine = Get-GitFile $BaseRef $path
        $oldDigest = (($oldLine -split '\s+')[0]).ToLowerInvariant()
        if ($oldDigest -notmatch '^[a-f0-9]{64}$') { throw "Base baseline '$path' does not contain a valid SHA-256 digest." }
    }
    $newLine = Get-GitFile $HeadRef $path
    $newDigest = (($newLine -split '\s+')[0]).ToLowerInvariant()
    if ($newDigest -notmatch '^[a-f0-9]{64}$') { throw "Head baseline '$path' does not contain a valid SHA-256 digest." }
    $record = Get-GitFile $HeadRef $recordPath
    $recordOld = Get-ReviewSection $record 'Previous digest'
    $recordNew = Get-ReviewSection $record 'Proposed digest'
    $reason = Get-ReviewSection $record 'Reason'
    $criteria = Get-ReviewSection $record 'Changed criteria'
    if ($change.Status -eq 'M' -and $recordOld -notmatch "(?im)^\s*$([regex]::Escape($oldDigest))\s*$") { throw "Review record '$recordPath' does not match the previous digest for '$path'." }
    if ($change.Status -eq 'A' -and $recordOld -notmatch '(?im)^\s*(none|new baseline)\s*$') { throw "Review record '$recordPath' must identify the previous digest as 'none' or 'new baseline'." }
    if ($recordNew -notmatch "(?im)^\s*$([regex]::Escape($newDigest))\s*$") { throw "Review record '$recordPath' does not match the proposed digest for '$path'." }
    if ([string]::IsNullOrWhiteSpace($reason)) { throw "Review record '$recordPath' must include a reason." }
    $listedCriteria = @([regex]::Matches($criteria, '\b(?:AC|SC)-\d{3}\b') | ForEach-Object { $_.Value } | Sort-Object -Unique)
    if ($listedCriteria.Count -eq 0) { throw "Review record '$recordPath' must list at least one changed AC-### or SC-### identifier." }
    $knownCriteria = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $specPath = "$featureDir/spec.md"
    foreach ($revision in @($BaseRef, $HeadRef)) {
        try { $specText = Get-GitFile $revision $specPath } catch { continue }
        foreach ($criterionMatch in [regex]::Matches($specText, '(?m)^\s*(?:\d+\.\s*)?(?:[-*]\s*)?\*\*(?<id>(?:AC|SC)-\d{3})\*\*')) {
            [void]$knownCriteria.Add($criterionMatch.Groups['id'].Value)
        }
    }
    foreach ($criterion in $listedCriteria) {
        if (-not $knownCriteria.Contains($criterion)) { throw "Review record '$recordPath' references unknown criterion ID '$criterion'." }
    }
    Write-Output "Reviewed acceptance baseline amendment: $path"
}
Write-Output 'Acceptance baseline review policy verified.'
$global:LASTEXITCODE = 0
