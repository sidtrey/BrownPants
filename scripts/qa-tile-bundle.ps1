[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [ValidateRange(256, 4000)][int] $TileSize = 256,
    [int] $Overlap = 64
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($RepositoryRoot)
if ($Overlap -lt 0 -or $Overlap * 2 -ge $TileSize) { throw 'Overlap must be nonnegative and less than half the tile size.' }
Import-Module (Join-Path $PSScriptRoot 'TestRunArtifacts.psm1') -Force
$runDirectory = New-BrownPantsRun -RepositoryRoot $root -Kind 'tile-bundle-qa'
trap {
    if ($runDirectory) {
        try { Set-BrownPantsRunState -RunDirectory $runDirectory -State failed } catch { }
        Write-Output "Tile-bundle QA failed; artifacts retained at $runDirectory"
    }
    throw
}

$cli = Join-Path $root 'src/BrownPants.Cli/bin/Release/net10.0/BrownPants.Cli.dll'
if (-not (Test-Path -LiteralPath $cli -PathType Leaf)) {
    throw 'Build the Release CLI first: dotnet build BrownPants.sln -c Release'
}

$source = [Text.StringBuilder]::new()
[void]$source.AppendLine('```brownpants-flow')
[void]$source.AppendLine('flowchart tile-stress')
for ($index = 0; $index -lt 2200; $index++) {
    [void]$source.AppendLine(('node n{0:D4} process "Step {0:D4}"' -f $index))
}
for ($index = 0; $index -lt 2199; $index++) {
    [void]$source.AppendLine(('edge n{0:D4} -> n{1:D4}' -f $index, ($index + 1)))
}
[void]$source.AppendLine('```')
$inputPath = Join-Path $runDirectory 'stress.md'
[IO.File]::WriteAllText($inputPath, $source.ToString(), [Text.UTF8Encoding]::new($false))

$relativeInput = [IO.Path]::GetRelativePath($root, $inputPath).Replace('\', '/')
$outputPath = Join-Path $runDirectory 'rendered/stress.png'
$relativeOutput = [IO.Path]::GetRelativePath($root, $outputPath).Replace('\', '/')
New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null

function Invoke-StressRender {
    param([switch] $Overwrite)
    $arguments = @($cli, 'diagram', $root, $relativeInput, '--output', $relativeOutput,
        '--tile-size', $TileSize.ToString([Globalization.CultureInfo]::InvariantCulture),
        '--overlap', $Overlap.ToString([Globalization.CultureInfo]::InvariantCulture))
    if ($Overwrite) { $arguments += '--overwrite' }
    $output = @(& dotnet @arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Output $_ }
    if ($exitCode -ne 0) { throw "Stress render failed with exit code $exitCode." }
}

Invoke-StressRender
$bundle = [IO.Path]::ChangeExtension($outputPath, $null)
$manifestPath = Join-Path $bundle 'stress.tiles.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.tiles.Count -le 1024) { throw "Expected more than 1,024 tiles; found $($manifest.tiles.Count)." }
if ($manifest.canvasWidth -le 0 -or $manifest.canvasHeight -le 0) {
    throw "Stress canvas dimensions must be positive: $($manifest.canvasWidth) x $($manifest.canvasHeight)."
}

function Resolve-ManifestFile {
    param([Parameter(Mandatory)][string] $RelativePath)
    if ([IO.Path]::IsPathRooted($RelativePath) -or $RelativePath -match '(^|/)\.\.(/|$)') {
        throw "Manifest path is not repository-relative and safe: $RelativePath"
    }
    $path = [IO.Path]::GetFullPath((Join-Path $root ($RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))))
    $prefix = $root.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) + [IO.Path]::DirectorySeparatorChar
    $comparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    if (-not $path.StartsWith($prefix, $comparison) -or -not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Manifest path is missing or escapes the repository: $RelativePath"
    }
    return $path
}

$masterPath = Resolve-ManifestFile $manifest.editableSource
[xml]$master = Get-Content -LiteralPath $masterPath -Raw
$masterNodes = @($master.SelectNodes('//mxCell[@vertex="1"]')).Count
$masterEdges = @($master.SelectNodes('//mxCell[@edge="1"]')).Count
if ($masterNodes -ne 2200 -or $masterEdges -ne 2199) {
    throw "Complete source has $masterNodes nodes and $masterEdges edges; expected 2,200 and 2,199."
}

$assembly = [Reflection.Assembly]::LoadFrom($cli)
$parser = $assembly.GetType('BrownPants.Cli.Analysis.LocalFlowchartDrawioParser', $true)
$parseMethod = $parser.GetMethod('Parse', [Reflection.BindingFlags]'Static, NonPublic')
$manifestFiles = [Collections.Generic.List[string]]::new()
$tileXmlBytes = [long]0
foreach ($tile in $manifest.tiles) {
    $imagePath = Resolve-ManifestFile $tile.image
    $xmlPath = Resolve-ManifestFile $tile.editableSource
    $manifestFiles.Add($imagePath)
    $manifestFiles.Add($xmlPath)
    $tileXmlBytes += (Get-Item -LiteralPath $xmlPath).Length

    $bytes = [IO.File]::ReadAllBytes($xmlPath)
    $memory = [ReadOnlyMemory[byte]]::new($bytes)
    try { $null = $parseMethod.Invoke($null, @($memory, $tile.editableSource)) }
    catch { throw "BrownPants could not import tile XML '$($tile.editableSource)': $($_.Exception.GetBaseException().Message)" }

    [xml]$tileDocument = Get-Content -LiteralPath $xmlPath -Raw
    $cells = @($tileDocument.SelectNodes('//mxCell'))
    $cellIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($cell in $cells) { [void]$cellIds.Add($cell.GetAttribute('id')) }
    foreach ($edge in @($tileDocument.SelectNodes('//mxCell[@edge="1"]'))) {
        if (-not $cellIds.Contains($edge.GetAttribute('source')) -or -not $cellIds.Contains($edge.GetAttribute('target'))) {
            throw "Tile '$($tile.editableSource)' has an edge with a missing endpoint cell."
        }
    }
}

$masterBytes = (Get-Item -LiteralPath $masterPath).Length
$totalXmlBytes = $tileXmlBytes + $masterBytes
$maximumXmlBytes = [long]50000000
if ($totalXmlBytes -gt $maximumXmlBytes) {
    throw "Tile XML totals $totalXmlBytes bytes, exceeding the 50,000,000-byte budget."
}
$manifestPath = [IO.Path]::GetFullPath($manifestPath)
$manifestFiles.Add($manifestPath)
$manifestFiles.Add($masterPath)
$before = @($manifestFiles | Sort-Object -Unique | ForEach-Object {
    [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($root, $_).Replace('\', '/'); Hash = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
})

Invoke-StressRender -Overwrite
$after = @($manifestFiles | Sort-Object -Unique | ForEach-Object {
    [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($root, $_).Replace('\', '/'); Hash = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
})
$beforeJson = ConvertTo-Json -InputObject $before -Depth 3 -Compress
$afterJson = ConvertTo-Json -InputObject $after -Depth 3 -Compress
if ($beforeJson -cne $afterJson) { throw 'Repeated render changed one or more PNG, XML, or manifest hashes.' }

Write-Output "Tile-bundle QA passed: $($manifest.tiles.Count) tiles; $masterNodes nodes; $masterEdges edges; $totalXmlBytes XML bytes; $($before.Count) byte-identical artifacts; all tile XML parsed and edge terminals resolved."
Set-BrownPantsRunState -RunDirectory $runDirectory -State succeeded
Invoke-BrownPantsRunCleanup -RepositoryRoot $root -Apply
