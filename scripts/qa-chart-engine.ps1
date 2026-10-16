[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
Import-Module (Join-Path $PSScriptRoot 'TestRunArtifacts.psm1') -Force
$runDirectory = $null
$managedRun = [string]::IsNullOrWhiteSpace($OutputDirectory)
trap {
    if ($managedRun -and $runDirectory) {
        try { Set-BrownPantsRunState -RunDirectory $runDirectory -State failed } catch { }
        Write-Output "Chart QA failed; artifacts retained at $runDirectory"
    }
    throw
}
if ($managedRun) {
    $runDirectory = New-BrownPantsRun -RepositoryRoot $root -Kind 'flowchart-qa'
    $OutputDirectory = [IO.Path]::GetRelativePath($root, $runDirectory)
}
$rootPrefix = $root.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$outputFullPath = [System.IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
$pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
    [System.StringComparison]::OrdinalIgnoreCase
} else {
    [System.StringComparison]::Ordinal
}
if (-not $outputFullPath.StartsWith($rootPrefix, $pathComparison)) {
    throw 'QA output must remain inside the repository.'
}

$cli = Join-Path $root 'src/BrownPants.Cli/bin/Release/net10.0/BrownPants.Cli.dll'
if (-not (Test-Path -LiteralPath $cli -PathType Leaf)) {
    throw 'Build the Release CLI first: dotnet build BrownPants.sln -c Release'
}

$relativeSource = 'src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs'
$target = 'global::BrownPants.Cli.Analysis.LocalFlowchartDrawioWriter.Write(global::BrownPants.Cli.Analysis.LocalFlowchartDocument, global::BrownPants.Cli.Analysis.LocalFlowchartLayout, global::System.IO.Stream, global::BrownPants.Cli.Analysis.ChartTheme)'
$impactPath = Join-Path $outputFullPath 'brownpants-drawio-writer-impact.json'
$chartPath = Join-Path $outputFullPath 'brownpants-drawio-writer-callers.png'
$chartXmlPath = [System.IO.Path]::ChangeExtension($chartPath, '.drawio')
New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

Write-Output 'Running BrownPants call-impact analysis. MSBuild may evaluate imported project targets.'
& dotnet $cli impact $root --changed $relativeSource --format json --limit 10000 | Set-Content -LiteralPath $impactPath -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw "Call-impact analysis failed with exit code $LASTEXITCODE." }
$impact = Get-Content -LiteralPath $impactPath -Raw | ConvertFrom-Json
if (@($impact.Issues | Where-Object { $_ -match 'sampled|limit' }).Count -gt 0) {
    throw 'Independent call-impact report is capped; choose a smaller source file before comparing charts.'
}
$expectedCalls = @($impact.Impacts | Where-Object {
    $_.ChangedMethod -ceq $target -and $_.Depth -eq 1
})
if ($expectedCalls.Count -eq 0) { throw 'Independent call-impact analysis found no direct callers of the selected chart target.' }

$relativeChartPath = [System.IO.Path]::GetRelativePath($root, $chartPath).Replace('\', '/')
Write-Output 'Generating the same target caller chart through BrownPants.'
$chartOutput = @(& dotnet $cli chart callers $root --source $relativeSource --method $target --semantic `
    --output $relativeChartPath --depth 1 --limit 10000 --direction left-to-right --overwrite 2>&1)
$chartExitCode = $LASTEXITCODE
$chartOutput | ForEach-Object { Write-Output $_ }
if ($chartExitCode -ne 0) { throw "Caller chart failed with exit code $chartExitCode." }
if (-not (Test-Path -LiteralPath $chartPath -PathType Leaf) -or -not (Test-Path -LiteralPath $chartXmlPath -PathType Leaf)) {
    throw 'Caller chart did not create both PNG and editable XML outputs.'
}

[xml]$chart = Get-Content -LiteralPath $chartXmlPath -Raw
$cells = @($chart.SelectNodes('/mxfile/diagram/mxGraphModel/root/mxCell'))
$targetNode = @($cells | Where-Object { $_.tooltip -like "*Compiler symbol: $target*" })
$callSiteNodes = @($cells | Where-Object { $_.value -like 'Call site:*' })
$actualKeys = @($callSiteNodes | ForEach-Object {
    if ($_.tooltip -match 'Source: (.+):([0-9]+)') { "$($Matches[1]):$($Matches[2])" }
} | Sort-Object -Unique)
$expectedKeys = @($expectedCalls | ForEach-Object { "$($_.Path):$($_.Line)" } | Sort-Object -Unique)
$actualCallers = @($callSiteNodes | ForEach-Object {
    if ($_.tooltip -match 'Caller: (.+)') { $Matches[1] }
} | Sort-Object -Unique)
$expectedCallers = @($expectedCalls | ForEach-Object { $_.Caller } | Sort-Object -Unique)
$edgeCount = @($cells | Where-Object { $_.edge -eq '1' }).Count
$expectedEdgeCount = $expectedCalls.Count * 2

$chartMismatch = $targetNode.Count -ne 1 `
    -or ($actualKeys -join '|') -cne ($expectedKeys -join '|') `
    -or ($actualCallers -join '|') -cne ($expectedCallers -join '|') `
    -or $edgeCount -ne $expectedEdgeCount
if ($chartMismatch) {
    throw "Chart/Roslyn mismatch: expected $($expectedCalls.Count) calls, $($expectedCallers.Count) callers, and $expectedEdgeCount edges; chart has $($callSiteNodes.Count) call sites, $($actualCallers.Count) callers, and $edgeCount edges."
}

$coverageText = ($impact.Issues | Out-String)
if ($coverageText -match '(\d+) delegate target\(s\) could not be resolved') {
    $expectedDelegateCount = [int]$Matches[1]
    $expectedGap = "$expectedDelegateCount delegate target(s) could not be resolved"
    if (($chartOutput -join "`n") -notlike "*$expectedGap*") {
        throw 'Caller-chart output did not disclose the unresolved delegate-target count from semantic inspection.'
    }
}
Write-Output "QA passed: target matched; $($expectedCallers.Count) callers, $($expectedCalls.Count) source call sites, and $edgeCount XML edges match the independent Roslyn report."
Write-Output "Coverage notes: $coverageText"
Write-Output "Review the chart at $relativeChartPath and the independent report at $([System.IO.Path]::GetRelativePath($root, $impactPath).Replace('\', '/'))."
if ($managedRun) {
    Set-BrownPantsRunState -RunDirectory $runDirectory -State succeeded
    Invoke-BrownPantsRunCleanup -RepositoryRoot $root -Apply
}
