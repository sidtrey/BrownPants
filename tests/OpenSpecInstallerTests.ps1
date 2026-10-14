$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$installer = Join-Path $repositoryRoot 'integrations/openspec/Install.ps1'
$temporaryRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-openspec-tests-' + [guid]::NewGuid().ToString('N'))))
$expectedTempPrefix = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('/', '\')) + [System.IO.Path]::DirectorySeparatorChar

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

try {
    New-Item -ItemType Directory -Path (Join-Path $temporaryRoot 'openspec') -Force | Out-Null
    $destination = Join-Path $temporaryRoot '.agents/skills/openspec-brownpants-context/SKILL.md'
    $managedFilesBefore = @(Get-ChildItem (Join-Path $temporaryRoot 'openspec') -Force -Recurse)

    & $installer -ProjectRoot $temporaryRoot
    Assert-True (Test-Path -LiteralPath $destination -PathType Leaf) 'First install did not create SKILL.md.'
    $sourceHash = (Get-FileHash (Join-Path $repositoryRoot 'integrations/openspec/skills/openspec-brownpants-context/SKILL.md') -Algorithm SHA256).Hash
    $installedHash = (Get-FileHash $destination -Algorithm SHA256).Hash
    Assert-True ($sourceHash -eq $installedHash) 'Installed skill does not match the checked-in source.'
    $skill = Get-Content -LiteralPath $destination -Raw
    Assert-True ($skill -match 'inspect \. --format markdown --limit 10') 'OpenSpec context skill must request bounded initial context.'
    Assert-True ($skill -match '(?i)higher `--limit`') 'OpenSpec context skill must explain how to expand evidence when needed.'
    $initialWriteTime = (Get-Item $destination).LastWriteTimeUtc

    & $installer -ProjectRoot $temporaryRoot
    Assert-True ((Get-Item $destination).LastWriteTimeUtc -eq $initialWriteTime) 'Identical reinstall rewrote the installed skill.'

    Set-Content -LiteralPath $destination -Value 'user-edited skill' -Encoding utf8
    $userEditHash = (Get-FileHash $destination -Algorithm SHA256).Hash
    $conflictRejected = $false
    try { & $installer -ProjectRoot $temporaryRoot }
    catch { $conflictRejected = $_.Exception.Message -match 'Refusing to overwrite a different skill' }
    Assert-True $conflictRejected 'Installer did not reject a conflicting skill.'
    Assert-True ((Get-FileHash $destination -Algorithm SHA256).Hash -eq $userEditHash) 'Installer changed user-edited skill content.'

    $invalidRoot = Join-Path $temporaryRoot 'not-openspec'
    New-Item -ItemType Directory -Path $invalidRoot | Out-Null
    $invalidRootRejected = $false
    try { & $installer -ProjectRoot $invalidRoot }
    catch { $invalidRootRejected = $_.Exception.Message -match 'OpenSpec project root not found' }
    Assert-True $invalidRootRejected 'Installer accepted a directory without openspec/. '
    $managedFilesAfter = @(Get-ChildItem (Join-Path $temporaryRoot 'openspec') -Force -Recurse)
    Assert-True ($managedFilesAfter.Count -eq $managedFilesBefore.Count) 'Installer modified OpenSpec-managed files.'

    Write-Output 'OpenSpec installer tests passed.'
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        $resolvedTemporaryRoot = (Resolve-Path -LiteralPath $temporaryRoot).Path
        if (-not $resolvedTemporaryRoot.StartsWith($expectedTempPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove test path outside the temp directory: $resolvedTemporaryRoot"
        }
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
