$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sourceSkill = Join-Path $repositoryRoot 'integrations/agent-skills/brownpants-context/SKILL.md'
$installer = Join-Path $repositoryRoot 'integrations/agent-skills/Install.ps1'
$temporaryRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-agent-skill-tests-' + [guid]::NewGuid().ToString('N'))))
$expectedTempPrefix = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('/', '\')) + [System.IO.Path]::DirectorySeparatorChar

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Throws {
    param([scriptblock]$Action, [string]$MessagePattern, [string]$FailureMessage)
    $caught = $false
    try { & $Action | Out-Null }
    catch { $caught = $_.Exception.Message -match $MessagePattern }
    Assert-True $caught $FailureMessage
}

if (-not (Test-Path -LiteralPath $sourceSkill -PathType Leaf)) { throw "Bundled BrownPants skill not found: $sourceSkill" }
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) { throw "Agent skill installer not found: $installer" }

$skill = [System.IO.File]::ReadAllText($sourceSkill)
foreach ($pattern in @(
    '(?m)^name:\s*"?brownpants-context',
    'brownpants inspect \. --format markdown --limit 10',
    '(?s)source.*project.*test',
    '(?i)C#.*(?:syntax|Roslyn)|(?:syntax|Roslyn).*C#',
    '(?i)C/C\+\+.*lexical|lexical.*C/C\+\+',
    '(?i)inventory.only|inventory only',
    '(?i)candidate|heuristic',
    '(?i)(?:do not|does not).*?(?:invent|fabricat)',
    '(?i)terminal.*(?:unavailable|disabled|disallowed|cannot)|(?:unavailable|disabled|disallowed|cannot).*terminal',
    '(?i)approved.*environment|environment.*approved',
    '(?i)policy|permission',
    '(?i)network'
)) {
    Assert-True ($skill -match $pattern) "BrownPants skill is missing required guidance matching: $pattern"
}
Assert-True ($skill -notmatch '(?i)dotnet tool install|Invoke-WebRequest|curl\s|(?m)^\s*brownpants inspect .*--semantic|(?:^|\s)(?:codex|claude)\s+-p') 'The shared skill must not install or fetch tools, invoke semantic/MSBuild mode, or call AI-host CLIs.'

$readme = Join-Path $repositoryRoot 'integrations/agent-skills/README.md'
$integrationIndex = Join-Path $repositoryRoot 'integrations/README.md'
$rootReadme = Join-Path $repositoryRoot 'README.md'
$workflow = Join-Path $repositoryRoot '.github/workflows/ci.yml'
if (Test-Path -LiteralPath $readme -PathType Leaf) {
    $readmeText = [System.IO.File]::ReadAllText($readme)
    foreach ($pattern in @('Codex', 'Claude Code', '\$brownpants-context', '/brownpants-context', 'Cowork|cloud', 'Install.ps1')) {
        Assert-True ($readmeText -match $pattern) "Agent skills guide is missing required host guidance matching: $pattern"
    }
} else { throw "Agent skills guide not found: $readme" }
if (Test-Path -LiteralPath $integrationIndex -PathType Leaf) {
    Assert-True ([System.IO.File]::ReadAllText($integrationIndex).Contains('agent-skills/README.md')) 'Integration index does not link the local agent skills guide.'
} else { throw "Integration index not found: $integrationIndex" }
if (Test-Path -LiteralPath $rootReadme -PathType Leaf) {
    Assert-True ([System.IO.File]::ReadAllText($rootReadme).Contains('integrations/agent-skills/README.md')) 'Root README does not link the local agent skills guide.'
} else { throw "Root README not found: $rootReadme" }
if (Test-Path -LiteralPath $workflow -PathType Leaf) {
    Assert-True ([System.IO.File]::ReadAllText($workflow).Contains('./tests/AgentSkillIntegrationTests.ps1')) 'Cross-platform CI does not run the local agent skills check.'
} else { throw "CI workflow not found: $workflow" }

function Invoke-Install {
    param([string]$Root, [string]$Agent)
    & $installer -ProjectRoot $Root -Agent $Agent
}

try {
    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    $skillHash = (Get-FileHash -LiteralPath $sourceSkill -Algorithm SHA256).Hash

    foreach ($agent in @('codex', 'claude')) {
        $project = Join-Path $temporaryRoot "install-$agent"
        New-Item -ItemType Directory -Path $project | Out-Null
        $relativeDirectory = if ($agent -eq 'codex') { '.agents/skills/brownpants-context' } else { '.claude/skills/brownpants-context' }
        $destination = Join-Path $project "$relativeDirectory/SKILL.md"

        $firstOutput = (Invoke-Install -Root $project -Agent $agent 2>&1 | Out-String)
        Assert-True (Test-Path -LiteralPath $destination -PathType Leaf) "$agent installation did not create its project skill."
        Assert-True ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $skillHash) "$agent installation differs from canonical source."
        Assert-True ($firstOutput.Contains($relativeDirectory.Replace('/', [System.IO.Path]::DirectorySeparatorChar))) "$agent installer did not report its relative destination."
        $initialWriteTime = (Get-Item -LiteralPath $destination).LastWriteTimeUtc

        [void](Invoke-Install -Root $project -Agent $agent)
        Assert-True ((Get-Item -LiteralPath $destination).LastWriteTimeUtc -eq $initialWriteTime) "$agent identical reinstall rewrote the skill."

        [System.IO.File]::WriteAllText($destination, 'team-owned skill')
        $conflictHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        Assert-Throws { Invoke-Install -Root $project -Agent $agent } '(?i)conflict|overwrite|different' "$agent installer did not reject a conflicting skill."
        Assert-True ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $conflictHash) "$agent installer changed conflicting content."
    }

    $bothProject = Join-Path $temporaryRoot 'install-both'
    New-Item -ItemType Directory -Path $bothProject | Out-Null
    $bothOutput = (Invoke-Install -Root $bothProject -Agent 'both' 2>&1 | Out-String)
    foreach ($relative in @('.agents/skills/brownpants-context/SKILL.md', '.claude/skills/brownpants-context/SKILL.md')) {
        $destination = Join-Path $bothProject $relative
        Assert-True (Test-Path -LiteralPath $destination -PathType Leaf) "Both-host installation missed $relative."
        Assert-True ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $skillHash) "Both-host installation differs at $relative."
        Assert-True ($bothOutput.Contains($relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar))) "Both-host installer did not report $relative."
    }

    $partialProject = Join-Path $temporaryRoot 'both-conflict-preflight'
    $claudeDestination = Join-Path $partialProject '.claude/skills/brownpants-context/SKILL.md'
    New-Item -ItemType Directory -Path (Split-Path -Parent $claudeDestination) -Force | Out-Null
    [System.IO.File]::WriteAllText($claudeDestination, 'team-owned Claude skill')
    Assert-Throws { Invoke-Install -Root $partialProject -Agent 'both' } '(?i)conflict|overwrite|different' 'Both-host install did not reject a conflict before writing.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $partialProject '.agents'))) 'Both-host install wrote the non-conflicting Codex skill before reporting the Claude conflict.'
    Assert-True ([System.IO.File]::ReadAllText($claudeDestination) -eq 'team-owned Claude skill') 'Both-host install changed the conflicting Claude skill.'

    $invalidRoot = Join-Path $temporaryRoot 'not-a-directory'
    [System.IO.File]::WriteAllText($invalidRoot, 'file')
    Assert-Throws { Invoke-Install -Root $invalidRoot -Agent codex } '(?i)directory|project root' 'Installer accepted a file as project root.'
    $missingRoot = Join-Path $temporaryRoot 'missing-root'
    Assert-Throws { Invoke-Install -Root $missingRoot -Agent claude } '(?i)resolve|not found|directory|project root' 'Installer accepted a missing project root.'
    Assert-Throws { Invoke-Install -Root $temporaryRoot -Agent unsupported } '(?i)validate|agent|codex|claude' 'Installer accepted an unsupported agent selector.'

    $escapeProject = Join-Path $temporaryRoot 'escape-project'
    $outsideDirectory = Join-Path $temporaryRoot 'outside-target'
    New-Item -ItemType Directory -Path $escapeProject, $outsideDirectory -Force | Out-Null
    $linkedDirectory = Join-Path $escapeProject '.agents/skills'
    New-Item -ItemType Directory -Path (Split-Path -Parent $linkedDirectory) -Force | Out-Null
    try {
        if ($IsWindows) { New-Item -ItemType Junction -Path $linkedDirectory -Target $outsideDirectory | Out-Null }
        else { [void][System.IO.Directory]::CreateSymbolicLink($linkedDirectory, $outsideDirectory) }
        Assert-Throws { Invoke-Install -Root $escapeProject -Agent codex } '(?i)outside|escape|reparse|link' 'Installer followed a skill directory link outside the project.'
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $outsideDirectory 'brownpants-context/SKILL.md'))) 'Installer wrote outside the selected project.'
    }
    catch [System.PlatformNotSupportedException] {
        Write-Warning 'Symbolic-link containment scenario skipped because the host does not support links.'
    }
    catch [System.UnauthorizedAccessException] {
        Write-Warning 'Symbolic-link containment scenario skipped because link creation is not permitted.'
    }

    Write-Output 'Agent skill integration tests passed.'
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
