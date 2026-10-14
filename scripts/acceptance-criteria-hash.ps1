[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $SpecPath,

    [string] $BaselinePath,

    [switch] $Initialize,

    [switch] $Refresh
)

$ErrorActionPreference = 'Stop'
if ($Initialize -and $Refresh) { throw 'Use only one of -Initialize or -Refresh.' }
$spec = [System.IO.Path]::GetFullPath($SpecPath)
if ([string]::IsNullOrWhiteSpace($BaselinePath)) {
    $BaselinePath = Join-Path ([System.IO.Path]::GetDirectoryName($spec)) 'acceptance-criteria.sha256'
}
$baseline = [System.IO.Path]::GetFullPath($BaselinePath)
$content = [System.IO.File]::ReadAllText($spec)

function Get-RequiredSection {
    param([string] $Text, [string] $Heading)
    $lines = $Text -replace "`r`n?", "`n" -split "`n"
    $start = [Array]::IndexOf($lines, $Heading)
    if ($start -lt 0) { throw "Required acceptance heading '$Heading' was not found in '$spec'." }
    $end = $start + 1
    while ($end -lt $lines.Length -and $lines[$end] -notmatch '^##\s') { $end++ }
    return ($lines[$start..($end - 1)] -join "`n").Trim()
}

$acceptance = Get-RequiredSection -Text $content -Heading '## User Scenarios & Testing'
$success = Get-RequiredSection -Text $content -Heading '## Success Criteria'
$canonical = "$acceptance`n`n$success"
$encoding = [System.Text.UTF8Encoding]::new($false)
$bytes = $encoding.GetBytes($canonical)
$sha = [System.Security.Cryptography.SHA256]::HashData($bytes)
$actual = [Convert]::ToHexString($sha).ToLowerInvariant()

if ($Initialize) {
    if (Test-Path -LiteralPath $baseline) { throw "Baseline already exists: $baseline. Use -Refresh only after reviewing intentional criteria changes." }
    [System.IO.File]::WriteAllText($baseline, "$actual  $([System.IO.Path]::GetFileName($spec))`n", $encoding)
    Write-Output "Initialized acceptance criteria baseline: $actual"
    return
}

if ($Refresh) {
    [System.IO.File]::WriteAllText($baseline, "$actual  $([System.IO.Path]::GetFileName($spec))`n", $encoding)
    Write-Output "Refreshed acceptance criteria baseline: $actual"
    return
}

if (-not (Test-Path -LiteralPath $baseline)) { throw "Acceptance criteria baseline is missing: $baseline. Initialize it after reviewing the spec." }
$expectedLine = [System.IO.File]::ReadAllText($baseline).Trim()
$expected = ($expectedLine -split '\s+')[0]
if ($expected -notmatch '^[a-fA-F0-9]{64}$') { throw "Invalid SHA-256 baseline in '$baseline'." }
if (-not [string]::Equals($expected, $actual, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Acceptance criteria changed. Expected $expected; found $actual. Review the spec before explicitly refreshing the baseline."
}
Write-Output "Acceptance criteria verified: $actual"
