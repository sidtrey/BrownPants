$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$checker = Join-Path $root 'scripts/check-acceptance-evidence.ps1'
if (-not (Test-Path -LiteralPath $checker)) { throw 'Acceptance evidence checker does not exist yet.' }

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-evidence-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $tempRoot 'specs/fixture'), (Join-Path $tempRoot 'tests') -Force | Out-Null
$specPath = Join-Path $tempRoot 'specs/fixture/spec.md'
$tasksPath = Join-Path $tempRoot 'specs/fixture/tasks.md'
$evidencePath = Join-Path $tempRoot 'specs/fixture/acceptance-evidence.json'
$evidenceFile = Join-Path $tempRoot 'tests/ObservedTest.cs'
Set-Content -LiteralPath $specPath -Encoding utf8 -Value @'
# Fixture

## User Scenarios & Testing
1. **AC-001** — Complete one behavior.
2. **AC-002** — Complete another behavior.

## Requirements
- **FR-001**: A requirement.

## Success Criteria
- **SC-001**: Both behaviors are verifiable.
'@
Set-Content -LiteralPath $tasksPath -Encoding utf8 -Value @'
# Tasks
- [ ] T001 [US1] Add a test in tests/ObservedTest.cs.
- [X] T002 [US1] Implement and verify the behavior in tests/ObservedTest.cs.
'@
Set-Content -LiteralPath $evidenceFile -Encoding utf8 -Value 'fixture evidence'

function New-Ledger {
    param([object[]] $Entries)
    @{ schemaVersion = 1; tasks = $Entries } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $evidencePath -Encoding utf8
}

function Invoke-Checker {
    & $checker -RepositoryRoot $tempRoot -SpecPath $specPath -TasksPath $tasksPath -EvidencePath $evidencePath | Out-Null
}

function Assert-Fails {
    param([string] $Name, [object[]] $Entries, [string] $TaskText)
    New-Ledger $Entries
    if ($null -ne $TaskText) { Set-Content -LiteralPath $tasksPath -Value $TaskText -Encoding utf8 }
    $failed = $false
    try { Invoke-Checker } catch { $failed = $true }
    if (-not $failed) { throw "Expected evidence validation to reject '$Name'." }
    Set-Content -LiteralPath $tasksPath -Encoding utf8 -Value @'
# Tasks
- [ ] T001 [US1] Add a test in tests/ObservedTest.cs.
- [X] T002 [US1] Implement and verify the behavior in tests/ObservedTest.cs.
'@
}

try {
    $planned = [pscustomobject]@{
        taskId = 'T001'; criterionIds = @('AC-001'); verificationCommand = 'dotnet test BrownPants.sln'
        evidencePaths = @(); result = 'planned'
    }
    $passed = [pscustomobject]@{
        taskId = 'T002'; criterionIds = @('AC-002', 'SC-001'); verificationCommand = 'dotnet test BrownPants.sln -c Release'
        evidencePaths = @('tests/ObservedTest.cs'); result = 'passed'
    }

    New-Ledger @($planned, $passed)
    $before = (Get-ChildItem -LiteralPath $tempRoot -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash }) -join "`n"
    Invoke-Checker
    $after = (Get-ChildItem -LiteralPath $tempRoot -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash }) -join "`n"
    if ($before -cne $after) { throw 'Evidence validation modified the fixture repository.' }

    Assert-Fails 'unmapped success criterion' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002'); verificationCommand='dotnet test'; evidencePaths=@('tests/ObservedTest.cs'); result='passed' }) $null
    Assert-Fails 'unknown criterion ID' @([pscustomobject]@{ taskId='T001'; criterionIds=@('AC-999'); verificationCommand='dotnet test'; evidencePaths=@(); result='planned' }, $passed) $null
    Assert-Fails 'duplicate task record' @($planned, $passed, $passed) $null
    Assert-Fails 'unknown task ID' @($planned, $passed, [pscustomobject]@{ taskId='T003'; criterionIds=@('SC-001'); verificationCommand='dotnet test'; evidencePaths=@(); result='planned' }) $null
    Assert-Fails 'completed task marked planned' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002','SC-001'); verificationCommand='dotnet test'; evidencePaths=@('tests/ObservedTest.cs'); result='planned' }) $null
    Assert-Fails 'incomplete task marked passed' @([pscustomobject]@{ taskId='T001'; criterionIds=@('AC-001'); verificationCommand='dotnet test'; evidencePaths=@('tests/ObservedTest.cs'); result='passed' }, $passed) $null
    Assert-Fails 'missing completed evidence file' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002','SC-001'); verificationCommand='dotnet test'; evidencePaths=@('tests/Missing.cs'); result='passed' }) $null
    Assert-Fails 'path escaping repository' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002','SC-001'); verificationCommand='dotnet test'; evidencePaths=@('../outside.cs'); result='passed' }) $null
    Assert-Fails 'absolute evidence path' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002','SC-001'); verificationCommand='dotnet test'; evidencePaths=@([System.IO.Path]::GetFullPath($evidenceFile)); result='passed' }) $null
    Assert-Fails 'Windows absolute evidence path' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002','SC-001'); verificationCommand='dotnet test'; evidencePaths=@('C:\outside.cs'); result='passed' }) $null
    Assert-Fails 'UNC evidence path' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002','SC-001'); verificationCommand='dotnet test'; evidencePaths=@('\\server\share\outside.cs'); result='passed' }) $null
    Assert-Fails 'placeholder verification command' @($planned, [pscustomobject]@{ taskId='T002'; criterionIds=@('AC-002','SC-001'); verificationCommand='TODO'; evidencePaths=@('tests/ObservedTest.cs'); result='passed' }) $null
    Assert-Fails 'task absent from ledger' @($planned) $null
    $originalSpec = Get-Content -LiteralPath $specPath -Raw
    Set-Content -LiteralPath $specPath -Encoding utf8 -Value $originalSpec.Replace('AC-002', 'AC-001')
    $duplicateCriterionFailed = $false
    try { Invoke-Checker } catch { $duplicateCriterionFailed = $_.Exception.Message -match 'Duplicate criterion ID' }
    if (-not $duplicateCriterionFailed) { throw 'Expected duplicate criterion IDs in the spec to fail validation.' }
    Set-Content -LiteralPath $specPath -Encoding utf8 -Value $originalSpec
    @{ schemaVersion = '1'; tasks = @($planned, $passed) } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $evidencePath -Encoding utf8
    $schemaVersionFailed = $false
    try { Invoke-Checker } catch { $schemaVersionFailed = $_.Exception.Message -match 'schemaVersion 1' }
    if (-not $schemaVersionFailed) { throw 'Expected a string schemaVersion to fail validation.' }
    Write-Output 'Acceptance evidence tests passed.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
