param([string] $RunRoot, [switch] $SelfTest)
$ErrorActionPreference = 'Stop'
function Normalize-Path([string] $value) { $value.Replace([char]92,[char]47).TrimStart('./') }
function Normalize-Signature([string] $value) {
    $value.Replace('global::','').Replace('global.','').Replace('DelegateImpact.Fixture.','')
}
function Normalize-Relationship([string] $value) {
    if ($value -in @('delegate invocation','delegate-reference')) { return 'delegate' }
    return $value
}
function Match-Finding([object] $finding, [object] $expectedFinding) {
    foreach ($field in @('relationshipKind','path','callerSignature','targetSignature')) {
        $a = [string]$finding[$field]; $e = [string]$expectedFinding[$field]
        if ($field -eq 'path') { $a=Normalize-Path $a; $e=Normalize-Path $e }
        if ($field -like '*Signature') { $a=Normalize-Signature $a; $e=Normalize-Signature $e }
        if ($field -eq 'relationshipKind') { $a=Normalize-Relationship $a; $e=Normalize-Relationship $e }
        if ($a -cne $e) { return $false }
    }
    return $true
}
function Score-Result([object[]] $actual, [object[]] $expectedItems) {
    $matched = [Collections.Generic.HashSet[int]]::new(); $tp=0; $fp=0
    foreach ($item in $actual) {
        $indexFound=-1
        for ($i=0; $i -lt $expectedItems.Count; $i++) {
            if ($null -eq $expectedItems[$i]) { throw "Scorer expected finding $i is null (count $($expectedItems.Count))." }
            if (-not $matched.Contains($i) -and (Match-Finding -finding $item -expectedFinding ($expectedItems[$i]))) { $indexFound=$i; break }
        }
        if ($indexFound -ge 0) { [void]$matched.Add($indexFound); $tp++ } else { $fp++ }
    }
    $fn=$expectedItems.Count-$tp
    [pscustomobject]@{ expected=$expectedItems.Count; submitted=$actual.Count; truePositives=$tp; falsePositives=$fp; falseNegatives=$fn;
        precision=$(if ($actual.Count -eq 0) { 0.0 } else { [math]::Round($tp/$actual.Count,4) });
        recall=$(if ($expectedItems.Count -eq 0) { 1.0 } else { [math]::Round($tp/$expectedItems.Count,4) }) }
}
if ($SelfTest) {
    $key=Get-Content (Join-Path $PSScriptRoot 'answer-key.json') -Raw | ConvertFrom-Json -AsHashtable
    $expected=@($key.expectedFindings)
    $normalizedActual=foreach ($finding in $expected) {
        $copy=[ordered]@{}; foreach($property in $finding.Keys){$copy[$property]=$finding[$property]}
        $copy.path=$copy.path.Replace([char]47,[char]92)
        foreach($field in @('callerSignature','targetSignature')){$copy[$field]=([string]$copy[$field]).Replace('global::','').Replace('DelegateImpact.Fixture.','')}
        if($copy.relationshipKind -eq 'delegate'){$copy.relationshipKind='delegate-reference'}
        $copy
    }
    $perfect=Score-Result -actual @($normalizedActual) -expectedItems @($expected)
    $duplicate=Score-Result -actual (@($expected)+@($expected[0])) -expectedItems @($expected)
    $omission=Score-Result -actual @($expected | Select-Object -First 1) -expectedItems @($expected)
    $decoy=[ordered]@{ relationshipKind='method'; path='src/WorkRepository.cs'; callerSignature='Other.Run()'; targetSignature='Fake.Save()' }
    $withDecoy=Score-Result -actual (@($expected)+@($decoy)) -expectedItems @($expected)
    if ($perfect.recall -ne 1 -or $perfect.precision -ne 1 -or $duplicate.falsePositives -ne 1 -or $omission.falseNegatives -ne 1 -or $withDecoy.falsePositives -ne 1) { throw 'Scorer self-test failed exact/duplicate/omission/decoy checks.' }
    Write-Output 'Scorer self-test passed: exact matches, duplicates, omissions, and decoys are scored correctly.'
    return
}
if ([string]::IsNullOrWhiteSpace($RunRoot)) { throw 'RunRoot is required unless -SelfTest is used.' }
$root=(Resolve-Path -LiteralPath $RunRoot).Path
$null = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Verify-FrozenInputs.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Frozen protocol verification failed.' }
$keyPath=Join-Path $PSScriptRoot 'answer-key.json'
$keyDigest=(Get-Content (Join-Path $PSScriptRoot 'answer-key.sha256') -Raw).Trim().Split(' ')[0]
if ((Get-FileHash $keyPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $keyDigest) { throw 'Answer key digest changed.' }
$key=Get-Content -LiteralPath $keyPath -Raw | ConvertFrom-Json -AsHashtable
& pwsh -NoProfile -File (Join-Path $PSScriptRoot '../../../../scripts/acceptance-criteria-hash.ps1') -SpecPath (Join-Path $PSScriptRoot 'task-spec.md')
if ($LASTEXITCODE -ne 0) { throw 'Frozen task acceptance criteria digest failed.' }
$meta=Get-Content (Join-Path $root 'codex-runs.json') -Raw | ConvertFrom-Json -AsHashtable
if ($meta.conditionOrder -join ',' -cne 'baseline/assisted,assisted/baseline,baseline/assisted') { throw 'Condition order does not match the approved counterbalance.' }
if ($meta.runs.Count -ne 6) { throw 'Exactly six run records are required.' }
$pairDigests=@($meta.preparation.pairs | ForEach-Object sourceDigest | Select-Object -Unique)
if ($pairDigests.Count -ne 1) { throw 'Prepared pair source digests are not identical.' }
$expectedSourceHashes=@{}; foreach ($entry in $meta.preparation.sourceFiles) { $expectedSourceHashes[$entry.path]=$entry.sha256 }
$expectedFindings=[object[]]$key['expectedFindings']
$expected=$expectedFindings
$scored=@()
foreach ($run in $meta.runs) {
    if ($run.codexExitCode -ne 0 -or $run.postcheckExitCode -ne 0 -or -not $run.changedPathsAllowed) { throw "Run gate failed: pair $($run.pair) $($run.condition)." }
    $source=Join-Path $root ("input-snapshots/pair-{0:D2}/{1}-source" -f $run.pair,$run.condition)
    if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw "Frozen input snapshot is missing: $source" }
    $sourceBase=(Resolve-Path -LiteralPath $source).Path.TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)
    $actualSourceHashes=@{}
    Get-ChildItem -LiteralPath $sourceBase -Recurse -File -Force | Where-Object { $_.FullName -notmatch '[\\/](bin|obj|BrownPantsEvidence)[\\/]' } | ForEach-Object {
        $relative=$_.FullName.Substring($sourceBase.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)).Replace([IO.Path]::DirectorySeparatorChar,'/')
        $actualSourceHashes[$relative]=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    if ($actualSourceHashes.Count -ne $expectedSourceHashes.Count) { throw "Frozen source file count differs: pair $($run.pair) $($run.condition)." }
    foreach ($path in $expectedSourceHashes.Keys) { if ($actualSourceHashes[$path] -cne $expectedSourceHashes[$path]) { throw "Frozen source hash differs: pair $($run.pair) $($run.condition) $path." } }
    if ((Test-Path (Join-Path $source 'control')) -or (Test-Path (Join-Path $source 'answer-key.json'))) { throw 'A scorer/control file was exposed in an agent source copy.' }
    if ($run.condition -eq 'baseline' -and (Test-Path (Join-Path $source 'BrownPantsEvidence'))) { throw 'Baseline condition unexpectedly received BrownPants evidence.' }
    if ($run.condition -eq 'assisted' -and -not (Test-Path (Join-Path $source 'BrownPantsEvidence/impact.json'))) { throw 'Assisted condition is missing BrownPants evidence.' }
    $resultPath=Join-Path $root $run.resultPath
    if (-not (Test-Path -LiteralPath $resultPath)) { throw "Structured result missing: $($run.resultPath)" }
    $result=Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json -AsHashtable
    $actualFindings=[object[]]$result['impacts']
    $score=Score-Result -actual $actualFindings -expectedItems $expectedFindings
    $scored += [pscustomobject]@{ pair=$run.pair; condition=$run.condition; score=$score; fixVerified=$true; usage=$run.usage;
        codexVersion=$run.codexVersion; model=$run.model; resultPath=$run.resultPath; eventLogPath=$run.eventLogPath;
        postcheckLogPath=$run.postcheckLogPath; changedPaths=$run.changedPaths }
}
$baseline=@($scored | Where-Object condition -eq 'baseline'); $assisted=@($scored | Where-Object condition -eq 'assisted')
$pooled=@{}
foreach ($condition in @('baseline','assisted')) {
    $group=@($scored | Where-Object condition -eq $condition)
    $tp=($group | Measure-Object { $_.score.truePositives } -Sum).Sum
    $fp=($group | Measure-Object { $_.score.falsePositives } -Sum).Sum
    $fn=($group | Measure-Object { $_.score.falseNegatives } -Sum).Sum
    $pooled[$condition]=[ordered]@{ expected=$expected.Count*$group.Count; submitted=$tp+$fp; truePositives=$tp; falsePositives=$fp; falseNegatives=$fn;
        precision=$(if (($tp+$fp) -eq 0) {0.0} else {[math]::Round($tp/($tp+$fp),4)});
        recall=$(if (($tp+$fn) -eq 0) {1.0} else {[math]::Round($tp/($tp+$fn),4)}) }
}
$pairs=foreach ($pair in 1..3) {
    $base=$baseline | Where-Object pair -eq $pair | Select-Object -First 1
    $assist=$assisted | Where-Object pair -eq $pair | Select-Object -First 1
    [pscustomobject]@{ pair=$pair; baselineRecall=$base.score.recall; assistedRecall=$assist.score.recall; recallGain=$assist.score.recall-$base.score.recall; brownPantsImproved=$assist.score.recall -gt $base.score.recall }
}
$gate=[ordered]@{
    pooledRecallGainPercentagePoints=[math]::Round(($pooled.assisted.recall-$pooled.baseline.recall)*100,2)
    precisionDidNotDecrease=($pooled.assisted.precision -ge $pooled.baseline.precision)
    pairsFavoringBrownPants=@($pairs | Where-Object brownPantsImproved).Count
    allSixRepairsPassed=(@($scored | Where-Object fixVerified).Count -eq 6)
}
$gate.passed=($gate.pooledRecallGainPercentagePoints -ge 20 -and $gate.precisionDidNotDecrease -and $gate.pairsFavoringBrownPants -ge 2 -and $gate.allSixRepairsPassed)
$report=[ordered]@{ schemaVersion=1; taskSpecDigest=$meta.taskSpecDigest; answerKeyDigest=$meta.answerKeyDigest; promptDigest=$meta.promptDigest; schemaDigest=$meta.schemaDigest;
    model=$meta.model; codexVersion=$meta.codexVersion; sandbox=$meta.sandbox; conditionOrder=$meta.conditionOrder; sourceDigests=$meta.preparation.pairs;
    expectedFindings=$expected; scoreNormalization=@('Path separators and leading ./ are normalized.','C# global qualification and the fixture namespace prefix are removed from signatures.','delegate invocation and delegate-reference are accepted as delegate relationship labels.','Duplicate or unmatched submitted findings count as false positives.');
    pooled=$pooled; pairs=$pairs; gate=$gate; runs=$scored; limitations=@('Static call impact is source evidence and does not establish runtime execution.','Only the seeded single-item persistence defect is evaluated.','Results apply to the recorded Codex CLI/model/sandbox and this frozen fixture.') }
$reportPath=Join-Path $root 'comparison.json'; $report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $reportPath -Encoding utf8
$claim=if ($gate.passed) { 'This controlled evaluation met the pre-registered criterion for improved managed-impact recall on this delegate fixture.' } else { 'This controlled evaluation did not demonstrate superiority under the pre-registered criterion.' }
$md=@("# Delegate Impact Comparison",'',"$claim",'',"- Model / CLI: $($meta.model) / $($meta.codexVersion)","- Sandbox: $($meta.sandbox)","- Task acceptance SHA-256: $($meta.taskSpecDigest)","- Answer-key SHA-256: $($meta.answerKeyDigest)","- Matched seeded source SHA-256: $($meta.preparation.pairs[0].sourceDigest)","- Pooled baseline recall / precision: $($pooled.baseline.recall) / $($pooled.baseline.precision)","- Pooled BrownPants recall / precision: $($pooled.assisted.recall) / $($pooled.assisted.precision)","- Recall gain: $($gate.pooledRecallGainPercentagePoints) percentage points", "- Pairs favoring BrownPants: $($gate.pairsFavoringBrownPants) of 3", "- All six repairs passed independent checks: $($gate.allSixRepairsPassed)", "- Gate passed: $($gate.passed)",'', '## Pair Results','', '| Pair | Baseline recall | BrownPants recall | Gain |','|---:|---:|---:|---:|')
foreach ($item in $pairs) { $md += "| $($item.pair) | $($item.baselineRecall) | $($item.assistedRecall) | $([math]::Round($item.recallGain*100,2)) pp |" }
$md += @('', '## Codex Token Usage', '', 'Counts below are the Codex-reported input/output tokens for each task run; they are descriptive and are not part of the success gate.', '', '| Pair | Baseline input / output | BrownPants input / output |', '|---:|---:|---:|')
foreach ($pair in 1..3) {
    $base=$scored | Where-Object { $_.pair -eq $pair -and $_.condition -eq 'baseline' } | Select-Object -First 1
    $assist=$scored | Where-Object { $_.pair -eq $pair -and $_.condition -eq 'assisted' } | Select-Object -First 1
    $baseTokens=if ($base.usage) { "$($base.usage.input_tokens) / $($base.usage.output_tokens)" } else { 'unavailable' }
    $assistTokens=if ($assist.usage) { "$($assist.usage.input_tokens) / $($assist.usage.output_tokens)" } else { 'unavailable' }
    $md += "| $pair | $baseTokens | $assistTokens |"
}
$md += @('', '## Limitations','',"- $($report.limitations -join "`n- ")",'', 'Raw Codex JSONL, structured outputs, usage records, original seeded input snapshots, post-fix source files, and independent check logs are retained in the run archive.')
$md += @('', '## Scoring normalization', '', '- Paths normalize slash direction and a leading `./`.', '- Signatures drop `global::` / `global.` and the fixture namespace prefix.', '- `delegate invocation` and `delegate-reference` normalize to `delegate`.', '- Duplicate and unmatched findings count as false positives.')
$md | Set-Content -LiteralPath (Join-Path $root 'comparison.md') -Encoding utf8
Write-Output "Scored comparison. Gate passed: $($gate.passed). Report: $(Join-Path $root 'comparison.md')"
