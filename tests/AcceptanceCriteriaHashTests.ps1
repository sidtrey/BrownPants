$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$scriptPath = Join-Path $root 'scripts/acceptance-criteria-hash.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("brownpants-acceptance-hash-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
try {
    $specPath = Join-Path $tempRoot 'spec.md'
    $baselinePath = Join-Path $tempRoot 'acceptance-criteria.sha256'
    @'
# Feature

## User Scenarios & Testing

### Acceptance Scenarios
- Given baseline, when checked, then stable.

## Edge Cases
- This section is not hashed.

## Success Criteria
- Every criterion remains fixed.

## Assumptions
- This section is not hashed.
'@ | Set-Content -LiteralPath $specPath -Encoding utf8

    & $scriptPath -SpecPath $specPath -BaselinePath $baselinePath -Initialize | Out-Null
    & $scriptPath -SpecPath $specPath -BaselinePath $baselinePath | Out-Null
    $originalHash = (Get-Content -LiteralPath $baselinePath -Raw).Trim()
    $spec = Get-Content -LiteralPath $specPath -Raw
    $spec = $spec.Replace('This section is not hashed.', 'This edge case changed.')
    Set-Content -LiteralPath $specPath -Value $spec -Encoding utf8
    & $scriptPath -SpecPath $specPath -BaselinePath $baselinePath | Out-Null
    $spec = (Get-Content -LiteralPath $specPath -Raw).Replace('stable.', 'modified.')
    Set-Content -LiteralPath $specPath -Value $spec -Encoding utf8
    $failedAsExpected = $false
    try { & $scriptPath -SpecPath $specPath -BaselinePath $baselinePath | Out-Null }
    catch { $failedAsExpected = $_.Exception.Message -match 'Acceptance criteria changed' }
    if (-not $failedAsExpected) { throw 'Verifier did not reject changed acceptance criteria.' }
    & $scriptPath -SpecPath $specPath -BaselinePath $baselinePath -Refresh | Out-Null
    & $scriptPath -SpecPath $specPath -BaselinePath $baselinePath | Out-Null
    if ($originalHash -eq (Get-Content -LiteralPath $baselinePath -Raw).Trim()) { throw 'Refresh did not update the baseline.' }
} finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force
}
Write-Output 'Acceptance criteria hash tests passed.'
