[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [int] $MaxSuccessfulRuns = 5,
    [int] $SuccessfulMaxAgeDays = 14,
    [int] $FailedRetentionDays = 7,
    [switch] $Apply
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'TestRunArtifacts.psm1') -Force
Invoke-BrownPantsRunCleanup -RepositoryRoot $RepositoryRoot -MaxSuccessfulRuns $MaxSuccessfulRuns `
    -SuccessfulMaxAgeDays $SuccessfulMaxAgeDays -FailedRetentionDays $FailedRetentionDays -Apply:$Apply
