[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [ValidateSet('Debug', 'Release')][string] $Configuration = 'Release',
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($RepositoryRoot)
Import-Module (Join-Path $PSScriptRoot 'TestRunArtifacts.psm1') -Force
$run = New-BrownPantsRun -RepositoryRoot $root -Kind 'dotnet-test'
$results = Join-Path $run 'results'
New-Item -ItemType Directory -Path $results -Force | Out-Null
Write-Output "Test output: $run"
try {
    $dotnetArguments = @('test', 'BrownPants.sln', '--configuration', $Configuration)
    if ($NoBuild) { $dotnetArguments += '--no-build' }
    $dotnetArguments += @('--results-directory', $results)
    & dotnet @dotnetArguments
    $code = $LASTEXITCODE
} catch {
    Set-BrownPantsRunState -RunDirectory $run -State failed
    Write-Output "Test invocation failed; artifacts retained at $run"
    throw
}
if ($code -eq 0) {
    Set-BrownPantsRunState -RunDirectory $run -State succeeded
    Invoke-BrownPantsRunCleanup -RepositoryRoot $root -Apply
} else {
    Set-BrownPantsRunState -RunDirectory $run -State failed
    Write-Output "Tests failed; artifacts retained at $run"
}
exit $code
