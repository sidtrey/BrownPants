$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sourcePrompt = Join-Path $repositoryRoot 'integrations/copilot/prompts/brownpants-context.prompt.md'
$installer = Join-Path $repositoryRoot 'integrations/copilot/Install.ps1'
$temporaryRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-copilot-tests-' + [guid]::NewGuid().ToString('N'))))
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

if (-not (Test-Path -LiteralPath $sourcePrompt -PathType Leaf)) {
    throw "Bundled Copilot prompt not found: $sourcePrompt"
}
$prompt = [System.IO.File]::ReadAllText($sourcePrompt)
foreach ($pattern in @(
    '(?m)^name:\s*"?brownpants-context',
    'inspect \. --format markdown --limit 50',
    '(?s)source.*(?:project|test).*files',
    '(?i)candidate|heuristic',
    '(?i)do not.*(?:invent|fabricat)',
    '(?i)terminal.*(?:unavailable|disabled|cannot)|(?:unavailable|disabled|cannot).*terminal',
    '(?i)Visual Studio',
    '(?i)Local harness',
    '(?i)do not use semantic/MSBuild'
)) {
    Assert-True ($prompt -match $pattern) "Copilot prompt is missing required guidance matching: $pattern"
}
Assert-True ($prompt -notmatch '(?i)dotnet tool install|Invoke-WebRequest|curl\s') 'Copilot prompt must not install tools or fetch content.'
Assert-True ($prompt -notmatch '--semantic') 'Copilot prompt must not invoke semantic/MSBuild mode.'

try {
    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    $destination = Join-Path $temporaryRoot '.github/prompts/brownpants-context.prompt.md'
    $expectedHash = (Get-FileHash -LiteralPath $sourcePrompt -Algorithm SHA256).Hash

    $firstOutput = & $installer -ProjectRoot $temporaryRoot 2>&1 | Out-String
    Assert-True (Test-Path -LiteralPath $destination -PathType Leaf) 'Installer did not create the prompt.'
    Assert-True ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $expectedHash) 'Installed prompt differs from bundled source.'
    Assert-True ($firstOutput -match [regex]::Escape($destination)) 'Installer did not report the installed destination.'
    $initialWriteTime = (Get-Item -LiteralPath $destination).LastWriteTimeUtc

    & $installer -ProjectRoot $temporaryRoot | Out-Null
    Assert-True ((Get-Item -LiteralPath $destination).LastWriteTimeUtc -eq $initialWriteTime) 'Identical reinstall rewrote the prompt.'

    [System.IO.File]::WriteAllText($destination, 'team-owned prompt')
    $userFileHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    Assert-Throws { & $installer -ProjectRoot $temporaryRoot } 'Refusing to overwrite' 'Installer did not reject a different prompt.'
    Assert-True ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $userFileHash) 'Installer changed the conflicting prompt.'

    $invalidRoot = Join-Path $temporaryRoot 'not-a-directory'
    [System.IO.File]::WriteAllText($invalidRoot, 'file')
    Assert-Throws { & $installer -ProjectRoot $invalidRoot } '(?i)directory|project root' 'Installer accepted a file as project root.'
    $missingRoot = Join-Path $temporaryRoot 'missing-root'
    Assert-Throws { & $installer -ProjectRoot $missingRoot } '(?i)resolve|not found|directory|project root' 'Installer accepted a missing project root.'

    $escapeRoot = Join-Path $temporaryRoot 'escape-project'
    $outsideRoot = Join-Path $temporaryRoot 'outside-target'
    New-Item -ItemType Directory -Path $escapeRoot,$outsideRoot -Force | Out-Null
    $githubDirectory = Join-Path $escapeRoot '.github'
    New-Item -ItemType Directory -Path $githubDirectory | Out-Null
    $linkedPrompts = Join-Path $githubDirectory 'prompts'
    if ($IsWindows) {
        New-Item -ItemType Junction -Path $linkedPrompts -Target $outsideRoot | Out-Null
    } else {
        [void][System.IO.Directory]::CreateSymbolicLink($linkedPrompts, $outsideRoot)
    }
    Assert-Throws { & $installer -ProjectRoot $escapeRoot } '(?i)outside|escape|reparse|link' 'Installer followed a prompt directory link outside the project.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $outsideRoot 'brownpants-context.prompt.md'))) 'Installer wrote outside the selected project.'

    Write-Output 'Copilot prompt integration tests passed.'
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
