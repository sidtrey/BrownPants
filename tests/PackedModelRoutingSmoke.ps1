[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $ToolPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$modelRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-packed-models-' + [guid]::NewGuid().ToString('N'))
$packedTool = (Resolve-Path -LiteralPath $ToolPath).Path

try {
    $policyDirectory = Join-Path $modelRoot '.brownpants'
    New-Item -ItemType Directory -Path $policyDirectory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'examples/model-routing-policy.json') -Destination (Join-Path $policyDirectory 'models.json')

    & $packedTool models validate $modelRoot
    if ($LASTEXITCODE -ne 0) { throw "Packed tool model validation failed with exit code $LASTEXITCODE." }

    $report = & $packedTool models report $modelRoot --format json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed tool model report failed with exit code $LASTEXITCODE." }
    if ($report.Profiles.Count -ne 3) { throw "Expected 3 task model profiles, got '$($report.Profiles.Count)' instead." }

    & $packedTool models export $modelRoot --host codex
    if ($LASTEXITCODE -ne 0) { throw "Packed tool model profile export failed with exit code $LASTEXITCODE." }
    $exportedProfile = Join-Path $modelRoot '.codex/agents/brownpants-implementation.toml'
    if (-not (Test-Path -LiteralPath $exportedProfile -PathType Leaf)) { throw 'Packed tool did not create the expected Codex task profile.' }

    & $packedTool models export $modelRoot --host vscode
    if ($LASTEXITCODE -ne 0) { throw "Packed tool VS Code profile export failed with exit code $LASTEXITCODE." }
    $vscodeProfile = Join-Path $modelRoot '.github/agents/brownpants-implementation.agent.md'
    if (-not (Test-Path -LiteralPath $vscodeProfile -PathType Leaf)) { throw 'Packed tool did not create the expected VS Code task profile.' }

    & $packedTool models export $modelRoot --host visual-studio
    if ($LASTEXITCODE -ne 0) { throw "Packed tool Visual Studio profile export failed with exit code $LASTEXITCODE." }
    $visualStudioProfile = Join-Path $modelRoot '.github/agents/brownpants-visual-studio-implementation.agent.md'
    if (-not (Test-Path -LiteralPath $visualStudioProfile -PathType Leaf)) { throw 'Packed tool did not create the expected Visual Studio task profile.' }
    if ((Get-Content -LiteralPath $visualStudioProfile -Raw) -notmatch 'name: "BrownPants Visual Studio implementation"') { throw 'Visual Studio profile does not identify its host.' }

    & $packedTool models export $modelRoot --host claude
    if ($LASTEXITCODE -ne 0) { throw "Packed tool Claude Code profile export failed with exit code $LASTEXITCODE." }
    $claudeProfile = Join-Path $modelRoot '.claude/agents/brownpants-implementation.md'
    if (-not (Test-Path -LiteralPath $claudeProfile -PathType Leaf)) { throw 'Packed tool did not create the expected Claude Code task profile.' }

    Write-Output 'Packed task model CLI smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $modelRoot) { Remove-Item -LiteralPath $modelRoot -Recurse -Force }
}
