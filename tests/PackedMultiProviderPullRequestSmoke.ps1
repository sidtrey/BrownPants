[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $ToolPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$smokeRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-provider-pr-smoke-' + [guid]::NewGuid().ToString('N'))
$packedTool = (Resolve-Path -LiteralPath $ToolPath).Path
$savedTokens = @{
    BROWNPANTS_GITLAB_TOKEN = [Environment]::GetEnvironmentVariable('BROWNPANTS_GITLAB_TOKEN', 'Process')
    BROWNPANTS_BITBUCKET_TOKEN = [Environment]::GetEnvironmentVariable('BROWNPANTS_BITBUCKET_TOKEN', 'Process')
}

try {
    [Environment]::SetEnvironmentVariable('BROWNPANTS_GITLAB_TOKEN', $null, 'Process')
    [Environment]::SetEnvironmentVariable('BROWNPANTS_BITBUCKET_TOKEN', $null, 'Process')
    $featurePath = 'specs/039-github-draft-pull-requests'
    $featureDirectory = Join-Path $smokeRoot $featurePath
    $previewDirectory = Join-Path $smokeRoot 'artifacts'
    New-Item -ItemType Directory -Path $featureDirectory, $previewDirectory -Force | Out-Null
    foreach ($fileName in @('spec.md', 'tasks.md', 'acceptance-criteria.sha256', 'acceptance-evidence.json')) {
        Copy-Item -LiteralPath (Join-Path $repositoryRoot (Join-Path $featurePath $fileName)) -Destination $featureDirectory
    }

    $previewPath = 'artifacts/provider-preview.md'
    $previewOutput = & $packedTool pr preview $smokeRoot --feature $featurePath --repository 'example/project' `
        --source 'feature/smoke' --target 'main' --output $previewPath 2>&1
    $previewExitCode = $LASTEXITCODE
    if ($previewExitCode -notin @(0, 1)) { throw "Packed local preview failed with exit code $previewExitCode. $previewOutput" }
    if (-not (Test-Path -LiteralPath (Join-Path $smokeRoot $previewPath) -PathType Leaf)) { throw 'Packed tool did not create a provider-independent preview.' }

    foreach ($provider in @('gitlab', 'bitbucket')) {
        $createOutput = & $packedTool pr create $smokeRoot --provider $provider --preview $previewPath --repository 'example/project' `
            --source 'feature/smoke' --target 'main' --confirm --online 2>&1
        $createExitCode = $LASTEXITCODE
        if ($createExitCode -ne 1) { throw "Packed $provider command did not stop at missing credentials (exit code $createExitCode). $createOutput" }
        if (($createOutput -join "`n") -notmatch "BROWNPANTS_$($provider.ToUpperInvariant())_TOKEN") { throw "Packed $provider command did not explain its missing credential. $createOutput" }
    }

    Write-Output 'Packed GitLab and Bitbucket command routing, offline preview, and credential-gate smoke tests passed.'
}
finally {
    [Environment]::SetEnvironmentVariable('BROWNPANTS_GITLAB_TOKEN', $savedTokens.BROWNPANTS_GITLAB_TOKEN, 'Process')
    [Environment]::SetEnvironmentVariable('BROWNPANTS_BITBUCKET_TOKEN', $savedTokens.BROWNPANTS_BITBUCKET_TOKEN, 'Process')
    if (Test-Path -LiteralPath $smokeRoot) { Remove-Item -LiteralPath $smokeRoot -Recurse -Force }
}
