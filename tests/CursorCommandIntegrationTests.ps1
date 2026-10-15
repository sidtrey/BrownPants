$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sourceCommand = Join-Path $repositoryRoot 'integrations/cursor/commands/brownpants-context.md'
$installer = Join-Path $repositoryRoot 'integrations/cursor/Install.ps1'
$temporaryRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-cursor-tests-' + [guid]::NewGuid().ToString('N'))))
$expectedTempPrefix = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('/', '\')) + [System.IO.Path]::DirectorySeparatorChar

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Throws {
    param([scriptblock]$Action, [string]$MessagePattern, [string]$FailureMessage)
    $caught = $false
    try { & $Action }
    catch { $caught = $_.Exception.Message -match $MessagePattern }
    Assert-True $caught $FailureMessage
}

if (-not (Test-Path -LiteralPath $sourceCommand -PathType Leaf)) {
    throw "Cursor command not found: $sourceCommand"
}
$commandText = [System.IO.File]::ReadAllText($sourceCommand)
foreach ($pattern in @(
    '(?i)/brownpants-context',
    'brownpants inspect \. --format markdown --limit 50',
    '(?i)verify.*(?:source|project|test)',
    '(?i)candidate|heuristic',
    '(?i)do not.*(?:invent|fabricat)',
    '(?i)terminal.*(?:unavailable|disabled|cannot)|(?:unavailable|disabled|cannot).*terminal',
    '(?i)approved.*shell|shell.*approved',
    '(?i)supply|provide.*report'
)) {
    Assert-True ($commandText -match $pattern) "Cursor command is missing required guidance matching: $pattern"
}
Assert-True ($commandText -notmatch '(?i)dotnet tool install|Invoke-WebRequest|curl\s|--semantic|MSBuildWorkspace') 'Cursor command must not install tools, access the network, or invoke semantic/MSBuild mode.'

try {
    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    $destination = Join-Path $temporaryRoot '.cursor/commands/brownpants-context.md'
    $expectedHash = (Get-FileHash -LiteralPath $sourceCommand -Algorithm SHA256).Hash

    $firstOutput = & $installer -ProjectRoot $temporaryRoot 2>&1 | Out-String
    Assert-True (Test-Path -LiteralPath $destination -PathType Leaf) 'Installer did not create the Cursor command.'
    Assert-True ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $expectedHash) 'Installed Cursor command differs from bundled source.'
    Assert-True ($firstOutput -match [regex]::Escape($destination)) 'Installer did not report the destination.'
    $initialWriteTime = (Get-Item -LiteralPath $destination).LastWriteTimeUtc

    $secondOutput = & $installer -ProjectRoot $temporaryRoot 2>&1 | Out-String
    Assert-True ($secondOutput -match '(?i)already current') 'Installer did not report an identical installation as current.'
    Assert-True ((Get-Item -LiteralPath $destination).LastWriteTimeUtc -eq $initialWriteTime) 'Identical reinstall rewrote the command.'

    [System.IO.File]::WriteAllText($destination, 'team-owned command')
    $userFileHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    Assert-Throws { & $installer -ProjectRoot $temporaryRoot } '(?i)refusing to overwrite|different file' 'Installer did not reject a conflicting command.'
    Assert-True ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $userFileHash) 'Installer changed the conflicting command.'

    $invalidRoot = Join-Path $temporaryRoot 'not-a-directory'
    [System.IO.File]::WriteAllText($invalidRoot, 'file')
    Assert-Throws { & $installer -ProjectRoot $invalidRoot } '(?i)directory|project root' 'Installer accepted a file as the project root.'
    $missingRoot = Join-Path $temporaryRoot 'missing-root'
    Assert-Throws { & $installer -ProjectRoot $missingRoot } '(?i)resolve|not found|directory|project root' 'Installer accepted a missing project root.'

    $escapeRoot = Join-Path $temporaryRoot 'escape-project'
    $outsideRoot = Join-Path $temporaryRoot 'outside-target'
    New-Item -ItemType Directory -Path $escapeRoot,$outsideRoot -Force | Out-Null
    $cursorDirectory = Join-Path $escapeRoot '.cursor'
    New-Item -ItemType Directory -Path $cursorDirectory | Out-Null
    $linkedCommands = Join-Path $cursorDirectory 'commands'
    if ($IsWindows) {
        New-Item -ItemType Junction -Path $linkedCommands -Target $outsideRoot | Out-Null
    } else {
        [void][System.IO.Directory]::CreateSymbolicLink($linkedCommands, $outsideRoot)
    }
    Assert-Throws { & $installer -ProjectRoot $escapeRoot } '(?i)outside|escape|reparse|link' 'Installer followed a command directory link outside the project.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $outsideRoot 'brownpants-context.md'))) 'Installer wrote outside the selected project.'

    Write-Output 'Cursor command integration tests passed.'
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
