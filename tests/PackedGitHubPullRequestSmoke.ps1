[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $ToolPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$smokeRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-github-pr-smoke-' + [guid]::NewGuid().ToString('N'))
$packedTool = (Resolve-Path -LiteralPath $ToolPath).Path

try {
    $featurePath = 'specs/039-github-draft-pull-requests'
    $featureDirectory = Join-Path $smokeRoot $featurePath
    $previewDirectory = Join-Path $smokeRoot 'artifacts'
    New-Item -ItemType Directory -Path $featureDirectory, $previewDirectory -Force | Out-Null
    foreach ($fileName in @('spec.md', 'tasks.md', 'acceptance-criteria.sha256', 'acceptance-evidence.json')) {
        Copy-Item -LiteralPath (Join-Path $repositoryRoot (Join-Path $featurePath $fileName)) -Destination $featureDirectory
    }

    $previewPath = 'artifacts/github-preview.md'
    $previewOutput = & $packedTool pr preview $smokeRoot --feature $featurePath --repository 'example/project' `
        --source 'feature/smoke' --target 'main' --output $previewPath 2>&1
    $previewExitCode = $LASTEXITCODE
    if ($previewExitCode -notin @(0, 1)) { throw "Packed local preview failed with exit code $previewExitCode. $previewOutput" }
    $previewFile = Join-Path $smokeRoot $previewPath
    if (-not (Test-Path -LiteralPath $previewFile -PathType Leaf)) { throw 'Packed tool did not create a provider-independent preview.' }
    $preview = [System.IO.File]::ReadAllText($previewFile)
    if ($preview -notmatch '^<!-- brownpants-pr-preview-v1:') { throw 'Packed preview is missing its integrity metadata.' }

    $createOutput = & $packedTool pr create $smokeRoot --provider github --preview $previewPath --repository 'example/project' `
        --source 'feature/smoke' --target 'main' 2>&1
    $createExitCode = $LASTEXITCODE
    if ($createExitCode -ne 2) { throw "Packed GitHub create command did not reject a missing explicit gate (exit code $createExitCode). $createOutput" }
    if (($createOutput -join "`n") -notmatch 'both --confirm and --online') { throw 'Packed GitHub create gate did not explain its required flags.' }
    if (Test-Path -LiteralPath (Join-Path $smokeRoot '.brownpants/azure-devops.json')) { throw 'The smoke fixture unexpectedly depends on Azure DevOps configuration.' }

    Write-Output 'Packed GitHub PR preview and no-network creation-gate smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $smokeRoot) { Remove-Item -LiteralPath $smokeRoot -Recurse -Force }
}
