param(
    [Parameter(Mandatory=$true)][string] $AnswerKeyPath,
    [Parameter(Mandatory=$true)][string] $BaselineResultPath,
    [Parameter(Mandatory=$true)][string] $AssistedResultPath,
    [Parameter(Mandatory=$true)][string] $OutputPath,
    [switch] $SelfTest
)
$ErrorActionPreference = 'Stop'
$key = Get-Content -Raw -LiteralPath $AnswerKeyPath | ConvertFrom-Json
function Read-Result([string]$path) { Get-Content -Raw -LiteralPath $path | ConvertFrom-Json }
function Normalize-Signature([string]$signature) {
    if (-not $signature) { return '' }
    return (($signature -replace 'global::','' -replace 'global\.', '' -replace 'System\.Collections\.Generic\.', '' -replace 'System\.Func', 'Func') -replace '\s+', '')
}
function Match-Finding($actual, $expected) {
    if ($actual.category -ne $expected.category -or $actual.path -ne $expected.path) { return $false }
    if ($expected.category -eq 'managed-call') {
        # Signature identity survives edits that move the call site's line between seeded and fixed source.
        return $actual.status -eq 'resolved' -and $actual.signal -eq 'resolved call' -and
            (Normalize-Signature $actual.callerSignature) -eq (Normalize-Signature $expected.callerSignature) -and
            (Normalize-Signature $actual.targetSignature) -eq (Normalize-Signature $expected.targetSignature)
    }
    if ($expected.category -eq 'test-identification') {
        if ($actual.status -ne 'candidate') { return $false }
        if ($expected.PSObject.Properties.Name -contains 'line') { return $actual.line -eq $expected.line }
        return $true
    }
    if ($expected.category -in @('architecture-candidate','framework-candidate')) {
        if ($actual.status -ne 'candidate' -or $actual.signal -ne $expected.signal) { return $false }
        if ($expected.PSObject.Properties.Name -contains 'line') { return $actual.line -eq $expected.line }
        if ($expected.PSObject.Properties.Name -contains 'lines') { return $expected.lines -contains $actual.line }
        return $true
    }
    return $false
}
function Match-Decoy($actual, $decoy) {
    if ($actual.category -ne $decoy.category -or $actual.path -ne $decoy.path -or $actual.signal -ne $decoy.signal) { return $false }
    if ($decoy.PSObject.Properties.Name -contains 'line') {
        return $actual.line -eq $decoy.line -and
            (Normalize-Signature $actual.callerSignature) -eq (Normalize-Signature $decoy.callerSignature) -and
            (Normalize-Signature $actual.targetSignature) -eq (Normalize-Signature $decoy.targetSignature)
    }
    return $true
}
function Score-Condition([string]$name, $result, $answerKey) {
    $expected = @($answerKey.findings)
    $actual = @($result.findings)
    $matched = [Collections.Generic.HashSet[int]]::new()
    $truePositive = 0
    $falsePositive = 0
    foreach ($finding in $actual) {
        $isDecoy = @($answerKey.decoys | Where-Object { Match-Decoy $finding $_ }).Count -gt 0
        if ($isDecoy) { $falsePositive++; continue }
        $index = -1
        for ($i=0; $i -lt $expected.Count; $i++) {
            if (-not $matched.Contains($i) -and (Match-Finding $finding $expected[$i])) { $index = $i; break }
        }
        if ($index -ge 0) { [void]$matched.Add($index); $truePositive++ } else { $falsePositive++ }
    }
    $byCategory = foreach ($category in @('managed-call','architecture-candidate','framework-candidate','test-identification')) {
        $categoryExpected = @($expected | Where-Object category -eq $category)
        $categoryActual = @($actual | Where-Object category -eq $category)
        $categoryMatchedIndices = [Collections.Generic.HashSet[int]]::new()
        $categoryMatched = 0
        foreach ($finding in $categoryActual) {
            $isDecoy = @($answerKey.decoys | Where-Object { Match-Decoy $finding $_ }).Count -gt 0
            if ($isDecoy) { continue }
            for ($i=0; $i -lt $categoryExpected.Count; $i++) {
                if (-not $categoryMatchedIndices.Contains($i) -and (Match-Finding $finding $categoryExpected[$i])) {
                    [void]$categoryMatchedIndices.Add($i)
                    $categoryMatched++
                    break
                }
            }
        }
        $categoryFp = $categoryActual.Count - $categoryMatched
        [pscustomobject]@{
            category=$category
            expected=$categoryExpected.Count
            submitted=$categoryActual.Count
            truePositives=$categoryMatched
            falsePositives=$categoryFp
            falseNegatives=($categoryExpected.Count - $categoryMatched)
            precision=if ($categoryActual.Count -eq 0) { 0 } else { [math]::Round($categoryMatched / $categoryActual.Count, 4) }
            recall=if ($categoryExpected.Count -eq 0) { 1 } else { [math]::Round($categoryMatched / $categoryExpected.Count, 4) }
        }
    }
    $precision = if (($truePositive + $falsePositive) -eq 0) { 0 } else { [math]::Round($truePositive / ($truePositive + $falsePositive), 4) }
    $recall = if ($expected.Count -eq 0) { 1 } else { [math]::Round($truePositive / $expected.Count, 4) }
    return [pscustomobject]@{ condition=$name; expected=$expected.Count; submitted=$actual.Count; truePositives=$truePositive; falsePositives=$falsePositive; falseNegatives=($expected.Count - $truePositive); precision=$precision; recall=$recall; byCategory=@($byCategory) }
}
if ($SelfTest) {
    $synthetic = foreach ($item in $key.findings) {
        if ($item.PSObject.Properties.Name -contains 'lines') {
            [pscustomobject]@{ category=$item.category; signal=$item.signal; path=$item.path; line=$item.lines[0]; status='candidate' }
        } elseif ($item.PSObject.Properties.Name -contains 'line') {
            [pscustomobject]@{ category=$item.category; signal=$item.signal; path=$item.path; line=$item.line; callerSignature=($item.callerSignature -replace 'global::','' -replace 'global\.', '' -replace 'System\.Collections\.Generic\.', '' -replace 'System\.Func','Func'); targetSignature=($item.targetSignature -replace 'global::','' -replace 'global\.', '' -replace 'System\.Collections\.Generic\.', '' -replace 'System\.Func','Func'); status=if ($item.category -eq 'managed-call') { 'resolved' } else { 'candidate' } }
        } else {
            [pscustomobject]@{ category=$item.category; signal=$item.signal; path=$item.path; status='candidate' }
        }
    }
    $synthetic += foreach ($decoy in $key.decoys) { [pscustomobject]@{ category=$decoy.category; signal=$decoy.signal; path=$decoy.path; line=$decoy.line; callerSignature=$decoy.callerSignature; targetSignature=$decoy.targetSignature; status='resolved' } }
    $check = Score-Condition 'self-test' ([pscustomobject]@{ findings=@($synthetic) }) $key
    if ($check.truePositives -ne $key.findings.Count -or $check.falsePositives -ne $key.decoys.Count -or $check.falseNegatives -ne 0) {
        throw "Scorer self-test failed: TP=$($check.truePositives), FP=$($check.falsePositives), FN=$($check.falseNegatives)."
    }
    Write-Output "Evaluator self-test passed: $($check.truePositives) expected findings matched, $($check.falsePositives) decoys rejected, signature normalization verified."
    return
}
$baseline = Score-Condition 'spec-kit' (Read-Result $BaselineResultPath) $key
$assisted = Score-Condition 'spec-kit-plus-brownpants' (Read-Result $AssistedResultPath) $key
$managedBaseline = $baseline.byCategory | Where-Object category -eq 'managed-call'
$managedAssisted = $assisted.byCategory | Where-Object category -eq 'managed-call'
$advantage = ($assisted.recall -ge ($baseline.recall + 0.20)) -and ($assisted.precision -ge $baseline.precision)
$report = [pscustomobject]@{
    schemaVersion=1
    criteriaSha256=((Get-Content -Raw -LiteralPath (Join-Path (Split-Path -Parent $AnswerKeyPath) 'acceptance-criteria.sha256')).Trim().Split()[0])
    conditions=@($baseline,$assisted)
    recallGainPercentagePoints=[math]::Round(($assisted.recall - $baseline.recall)*100,2)
    managedCallRecallGainPercentagePoints=[math]::Round(($managedAssisted.recall - $managedBaseline.recall)*100,2)
    advantageThreshold=20
    advantageDemonstrated=$advantage
}
$json = $report | ConvertTo-Json -Depth 10
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), $json + "`n", [Text.UTF8Encoding]::new($false))
Write-Output $json
if (-not $advantage) { Write-Output 'The demo did not meet the predefined overall BrownPants advantage threshold.' }
