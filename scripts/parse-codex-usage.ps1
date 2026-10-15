[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $JsonlPath
)

$ErrorActionPreference = 'Stop'
$resolvedPath = (Resolve-Path -LiteralPath $JsonlPath -ErrorAction Stop).Path
$inputTokens = 0L
$cachedInputTokens = 0L
$outputTokens = 0L
$completedTurns = 0
$usageEvents = 0
$invalidLines = 0
$missingUsage = 0

foreach ($line in [System.IO.File]::ReadLines($resolvedPath)) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    try {
        $event = ConvertFrom-Json -InputObject $line -ErrorAction Stop
    } catch {
        $invalidLines++
        continue
    }

    if ($event.type -ne 'turn.completed') { continue }
    $completedTurns++
    $usage = $event.usage
    if ($null -eq $usage -or
        $usage.input_tokens -isnot [ValueType] -or
        $usage.output_tokens -isnot [ValueType] -or
        $usage.input_tokens -lt 0 -or
        $usage.output_tokens -lt 0) {
        $missingUsage++
        continue
    }

    $inputTokens += [long]$usage.input_tokens
    $outputTokens += [long]$usage.output_tokens
    if ($usage.cached_input_tokens -is [ValueType] -and $usage.cached_input_tokens -ge 0) {
        $cachedInputTokens += [long]$usage.cached_input_tokens
    }
    $usageEvents++
}

$available = $completedTurns -gt 0 -and $usageEvents -eq $completedTurns
$result = [ordered]@{
    usageAvailable = $available
    inputTokens = if ($available) { $inputTokens } else { $null }
    cachedInputTokens = if ($available) { $cachedInputTokens } else { $null }
    outputTokens = if ($available) { $outputTokens } else { $null }
    completedTurns = $completedTurns
    usageEvents = $usageEvents
    invalidLines = $invalidLines
    missingUsage = $missingUsage
}
$result | ConvertTo-Json -Compress
