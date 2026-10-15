param(
    [string] $RunRoot,
    [switch] $SelfTest
)
$ErrorActionPreference = 'Stop'

function Normalize-Path([string] $path) {
    $value = $path.Replace('\', '/').TrimStart('./')
    if ($value.StartsWith('managed/', [StringComparison]::OrdinalIgnoreCase)) { $value = $value.Substring('managed/'.Length) }
    return $value
}

function Normalize-Signature([string] $signature, [string] $relationshipKind) {
    $value = $signature.Replace('global::', '').Replace('global.', '')
    if ($relationshipKind -eq 'constructor' -and -not $value.Contains('..ctor(', [StringComparison]::Ordinal)) {
        $openParen = $value.LastIndexOf('(')
        $memberDot = $value.LastIndexOf('.', $openParen)
        if ($openParen -ge 0 -and $memberDot -ge 0) {
            $memberName = $value.Substring($memberDot + 1, $openParen - $memberDot - 1)
            $container = $value.Substring(0, $memberDot)
            $firstGeneric = $container.IndexOf('<')
            $typeEnd = if ($firstGeneric -ge 0) { $firstGeneric } else { $container.Length }
            $typeDot = $container.LastIndexOf('.', $typeEnd - 1)
            $typeName = $container.Substring($typeDot + 1, $typeEnd - $typeDot - 1)
            if ($memberName -ceq $typeName) { $value = $value.Substring(0, $memberDot) + '..ctor' + $value.Substring($openParen) }
        }
    }
    return $value
}

function Test-Match([object] $actual, [object] $expected) {
    foreach ($property in @('category', 'relationshipKind', 'path', 'line', 'callerSignature', 'targetSignature', 'signal', 'status')) {
        if ($expected.Contains($property)) {
            if (-not $actual.Contains($property)) { return $false }
            if ($property -eq 'path') {
                if ((Normalize-Path ([string]$actual[$property])) -cne (Normalize-Path ([string]$expected[$property]))) { return $false }
            } elseif ($property -eq 'line' -and $expected.category -eq 'test-identification') {
                continue
            } elseif ($property -eq 'status' -and $expected.category -eq 'test-identification') {
                continue
            } elseif ($property -in @('callerSignature', 'targetSignature')) {
                $kind = [string]$expected.relationshipKind
                if ((Normalize-Signature ([string]$actual[$property]) $kind) -cne (Normalize-Signature ([string]$expected[$property]) $kind)) { return $false }
            } elseif ([string]$actual[$property] -cne [string]$expected[$property]) { return $false }
        }
    }
    return $true
}

function Score-Findings([object[]] $actual, [object[]] $expected, [object[]] $decoys) {
    $matched = [Collections.Generic.HashSet[int]]::new()
    $tp = 0; $fp = 0
    foreach ($finding in $actual) {
        $isDecoy = $false
        foreach ($decoy in $decoys) {
            if ($finding.category -ceq $decoy.category -and (Normalize-Path ([string]$finding.path)) -ceq (Normalize-Path ([string]$decoy.path)) -and $finding.line -eq $decoy.line -and $finding.targetSignature -like "*$($decoy.targetContains)*") { $isDecoy = $true; break }
        }
        if ($isDecoy) { $fp++; continue }
        $matchIndex = -1
        for ($index = 0; $index -lt $expected.Count; $index++) {
            if (-not $matched.Contains($index) -and (Test-Match $finding $expected[$index])) { $matchIndex = $index; break }
        }
        if ($matchIndex -ge 0) { [void]$matched.Add($matchIndex); $tp++ } else { $fp++ }
    }
    $fn = $expected.Count - $tp
    $precision = if (($tp + $fp) -eq 0) { 0.0 } else { [math]::Round($tp / ($tp + $fp), 4) }
    $recall = if ($expected.Count -eq 0) { 1.0 } else { [math]::Round($tp / $expected.Count, 4) }
    $domains = foreach ($category in @('managed-call', 'architecture-candidate', 'test-identification')) {
        $expectedDomain = @($expected | Where-Object category -eq $category)
        $actualDomain = @($actual | Where-Object category -eq $category)
        $domainMatched = 0
        $domainIndices = [Collections.Generic.HashSet[int]]::new()
        foreach ($finding in $actualDomain) {
            $found = $false
            foreach ($decoy in $decoys) {
                if ($finding.category -ceq $decoy.category -and (Normalize-Path ([string]$finding.path)) -ceq (Normalize-Path ([string]$decoy.path)) -and $finding.line -eq $decoy.line -and $finding.targetSignature -like "*$($decoy.targetContains)*") { $found = $true; break }
            }
            if ($found) { continue }
            for ($index = 0; $index -lt $expectedDomain.Count; $index++) {
                if (-not $domainIndices.Contains($index) -and (Test-Match $finding $expectedDomain[$index])) {
                    [void]$domainIndices.Add($index); $domainMatched++; break
                }
            }
        }
        $domainFp = $actualDomain.Count - $domainMatched
        [pscustomobject]@{
            category = $category
            expected = $expectedDomain.Count
            submitted = $actualDomain.Count
            truePositives = $domainMatched
            falsePositives = $domainFp
            falseNegatives = $expectedDomain.Count - $domainMatched
            precision = if ($actualDomain.Count -eq 0) { 0.0 } else { [math]::Round($domainMatched / $actualDomain.Count, 4) }
            recall = if ($expectedDomain.Count -eq 0) { 1.0 } else { [math]::Round($domainMatched / $expectedDomain.Count, 4) }
        }
    }
    [pscustomobject]@{ expected = $expected.Count; submitted = $actual.Count; truePositives = $tp; falsePositives = $fp; falseNegatives = $fn; precision = $precision; recall = $recall; byCategory = @($domains) }
}

if ($SelfTest) {
    $key = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'answer-key.json') -Raw | ConvertFrom-Json -AsHashtable
    $synthetic = foreach ($finding in $key.expectedFindings) {
        $copy = [ordered]@{}; foreach ($property in $finding.Keys) { $copy[$property] = $finding[$property] }
        if (-not $copy.path.StartsWith('managed/')) { $copy.path = "managed/$($copy.path)" }
        foreach ($signatureField in @('callerSignature', 'targetSignature')) {
            if ($copy.Contains($signatureField)) { $copy[$signatureField] = ([string]$copy[$signatureField]).Replace('global::', '').Replace('global.', '') }
        }
        if ($copy.relationshipKind -eq 'constructor' -and $copy.targetSignature -match '\.\.ctor\(') {
            $copy.targetSignature = $copy.targetSignature -replace '\.\.ctor\(', '.OrderImportWorkflow('
        }
        if ($copy.category -eq 'test-identification') { $copy.line = 12 }
        $copy
    }
    $synthetic = @($synthetic) + @([ordered]@{ category='managed-call'; relationshipKind='method'; path='managed/Workflows/OrderImportWorkflow.cs'; line=36; callerSignature=''; targetSignature='AuditJournal.Save'; signal=''; status='resolved'; note='Decoy' })
    $score = Score-Findings $synthetic @($key.expectedFindings) @($key.decoys)
    if ($score.truePositives -ne $key.expectedFindings.Count -or $score.falsePositives -ne 1 -or $score.falseNegatives -ne 0) {
        throw 'Scorer self-test did not match all expected findings and reject its decoy.'
    }
    Write-Output 'Scorer self-test passed: all expected facts matched and the decoy counted as a false positive.'
    return
}

if ([string]::IsNullOrWhiteSpace($RunRoot)) { throw 'RunRoot is required unless -SelfTest is used.' }
$run = (Resolve-Path -LiteralPath $RunRoot).Path
$keyPath = Join-Path $PSScriptRoot 'answer-key.json'
$key = Get-Content -LiteralPath $keyPath -Raw | ConvertFrom-Json -AsHashtable
$controlInputs = @('bug-report.md', 'task-spec.md', 'acceptance-criteria.sha256', 'task-prompt.md', 'result.schema.json')
$metadataPath = Join-Path $run 'codex-runs.json'
$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json -AsHashtable
$preparation = Get-Content -LiteralPath (Join-Path $run 'preparation.json') -Raw | ConvertFrom-Json -AsHashtable
$expectedHashes = @{}; foreach ($file in $preparation.sourceFiles) { $expectedHashes[$file.path] = $file.sha256 }

function Get-CurrentSourceHashes([string] $root) {
    $rootPath = (Resolve-Path -LiteralPath $root).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $result = @{}
    Get-ChildItem -LiteralPath $rootPath -Recurse -File -Force |
        Where-Object { $_.FullName -notmatch '(^|[\\/])(bin|obj|BrownPantsEvidence|control)([\\/]|$)' } |
        ForEach-Object {
            $relative = $_.FullName.Substring($rootPath.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)).Replace([IO.Path]::DirectorySeparatorChar, '/')
            $result[$relative] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    return $result
}

$conditions = @()
foreach ($runInfo in $metadata.runs) {
    $condition = $runInfo.condition
    $sourceRoot = Join-Path $run "$condition-source"
    $controlUnchanged = $true
    foreach ($inputName in $controlInputs) {
        $original = Join-Path $PSScriptRoot $inputName
        $copy = Join-Path $sourceRoot "control/$inputName"
        if ((-not (Test-Path -LiteralPath $copy -PathType Leaf)) -or ((Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash -cne (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash)) {
            $controlUnchanged = $false
        }
    }
    $hashHelperCopy = Join-Path $sourceRoot 'control/acceptance-criteria-hash.ps1'
    if ((Get-FileHash -LiteralPath (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../../../..')) 'scripts/acceptance-criteria-hash.ps1') -Algorithm SHA256).Hash -cne (Get-FileHash -LiteralPath $hashHelperCopy -Algorithm SHA256).Hash) { $controlUnchanged = $false }
    $responsePath = Join-Path $run $runInfo.resultPath
    $response = $null; $responseError = $null
    try {
        if (-not (Test-Path -LiteralPath $responsePath -PathType Leaf)) { throw 'Codex final response is missing.' }
        $response = Get-Content -LiteralPath $responsePath -Raw | ConvertFrom-Json -AsHashtable -ErrorAction Stop
        foreach ($required in @('schemaVersion', 'diagnosis', 'fixSummary', 'changedPaths', 'findings', 'limitations', 'testResult')) {
            if (-not $response.Contains($required)) { throw "Result is missing required field '$required'." }
        }
        if ($response.schemaVersion -ne 1 -or $response.findings -isnot [array]) { throw 'Result schemaVersion/findings shape is invalid.' }
    } catch { $responseError = $_.Exception.Message; $response = [ordered]@{ findings = @() } }

    $sourceHashes = Get-CurrentSourceHashes $sourceRoot
    $changedPaths = [Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    foreach ($path in $expectedHashes.Keys) {
        if (-not $sourceHashes.ContainsKey($path) -or $sourceHashes[$path] -cne $expectedHashes[$path]) { [void]$changedPaths.Add($path) }
    }
    foreach ($path in $sourceHashes.Keys) { if (-not $expectedHashes.ContainsKey($path)) { [void]$changedPaths.Add($path) } }
    $allowed = @($key.expectedCode.allowedChangedPaths)
    $scopeCorrect = @($changedPaths | Where-Object { $_ -notin $allowed }).Count -eq 0
    $implementationPath = Join-Path $sourceRoot $key.expectedCode.path
    $implementation = if (Test-Path -LiteralPath $implementationPath -PathType Leaf) { [IO.File]::ReadAllText($implementationPath) } else { '' }
    $sourceRepairCorrect = ($implementation.Contains($key.expectedCode.requiredText, [StringComparison]::Ordinal)) -and (-not $implementation.Contains($key.expectedCode.forbiddenText, [StringComparison]::Ordinal))
    $postcheckLog = Join-Path $run $runInfo.postcheckLogPath
    $postcheckText = if (Test-Path -LiteralPath $postcheckLog) { Get-Content -LiteralPath $postcheckLog -Raw } else { '' }
    $postcheckPassed = $runInfo.postcheckExitCode -eq 0 -and $postcheckText.Contains('LegacyOrderDesk checks passed.', [StringComparison]::Ordinal)
    $taskHashVerified = $false
    Push-Location $sourceRoot
    try {
        & pwsh -NoProfile -File control/acceptance-criteria-hash.ps1 -SpecPath control/task-spec.md *> $null
        $taskHashVerified = $LASTEXITCODE -eq 0
    } catch { $taskHashVerified = $false } finally { Pop-Location }
    $findings = if ($response.Contains('findings')) { @($response.findings) } else { @() }
    $score = Score-Findings $findings @($key.expectedFindings) @($key.decoys)
    $actionableExpected = @($key.expectedFindings | Where-Object { $_.category -in $key.advantageRule.actionableCategories })
    $actionableActual = @($findings | Where-Object { $_.category -in $key.advantageRule.actionableCategories })
    $actionableScore = Score-Findings $actionableActual $actionableExpected @($key.decoys)
    $conditions += [pscustomobject]@{
        condition = $condition
        codexExitCode = $runInfo.codexExitCode
        resultValid = $null -eq $responseError
        resultError = $responseError
        codeChangedPaths = @($changedPaths)
        changeScopeCorrect = $scopeCorrect
        sourceRepairCorrect = $sourceRepairCorrect
        controlFilesUnchanged = $controlUnchanged
        taskHashVerified = $taskHashVerified
        postcheckPassed = $postcheckPassed
        fixCorrect = ($scopeCorrect -and $sourceRepairCorrect -and $controlUnchanged -and $taskHashVerified -and $postcheckPassed)
        overallFindingScore = $score
        actionableImpactScore = $actionableScore
        usage = $runInfo.usage
        resultPath = $runInfo.resultPath
        eventLogPath = $runInfo.eventLogPath
    }
}

$baseline = @($conditions | Where-Object condition -eq 'baseline')[0]
$assisted = @($conditions | Where-Object condition -eq 'assisted')[0]
$thresholdMet = $null
if ($null -ne $baseline -and $null -ne $assisted -and $baseline.resultValid -and $assisted.resultValid -and $baseline.fixCorrect -and $assisted.fixCorrect) {
    $minimumGain = [double]$key.advantageRule.minimumRecallGainPercentagePoints / 100.0
    $thresholdMet = (($assisted.actionableImpactScore.recall -ge ($baseline.actionableImpactScore.recall + $minimumGain)) -and ($assisted.actionableImpactScore.precision -ge $baseline.actionableImpactScore.precision))
}
$result = [pscustomobject]@{
    schemaVersion = 1
    model = $metadata.model
    codexVersion = $metadata.codexVersion
    sampleSizePairs = 1
    advantageRule = $key.advantageRule
    advantageDemonstrated = $thresholdMet
    conditions = $conditions
    limitations = @('One paired run is a demonstration only.', 'Static constructor and DI findings do not prove runtime activation.', 'Token usage is unavailable if the Codex completion event omits a usage payload.')
}
$outputPath = Join-Path $run 'comparison.json'
$result | ConvertTo-Json -Depth 18 | Set-Content -LiteralPath $outputPath -Encoding utf8
Write-Output "Scored comparison: $outputPath"
Write-Output "Advantage threshold met: $thresholdMet"
foreach ($condition in $conditions) {
    Write-Output "$($condition.condition): fixCorrect=$($condition.fixCorrect), impact recall=$($condition.actionableImpactScore.recall), precision=$($condition.actionableImpactScore.precision), tokens=$($condition.usage | ConvertTo-Json -Compress)"
}
