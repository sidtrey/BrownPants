$ErrorActionPreference = 'Stop'
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$root = Join-Path $tempRoot ('brownpants-usage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
try {
    $validPath = Join-Path $root 'valid.jsonl'
    @'
{"type":"turn.completed","usage":{"input_tokens":17,"cached_input_tokens":9,"output_tokens":4}}
{"type":"item.completed","item":{"type":"agent_message","text":"summary"}}
{"type":"turn.completed","usage":{"input_tokens":3,"cached_input_tokens":1,"output_tokens":2}}
'@ | Set-Content -LiteralPath $validPath -Encoding utf8

    $valid = & (Join-Path $PSScriptRoot '..\scripts\parse-codex-usage.ps1') -JsonlPath $validPath | ConvertFrom-Json
    if (-not $valid.usageAvailable -or $valid.inputTokens -ne 20 -or $valid.cachedInputTokens -ne 10 -or $valid.outputTokens -ne 6 -or $valid.completedTurns -ne 2) {
        throw 'Parser did not sum provider usage from completed turn events.'
    }

    $missingPath = Join-Path $root 'missing.jsonl'
    @'
{"type":"turn.completed"}
not-json
'@ | Set-Content -LiteralPath $missingPath -Encoding utf8

    $missing = & (Join-Path $PSScriptRoot '..\scripts\parse-codex-usage.ps1') -JsonlPath $missingPath | ConvertFrom-Json
    if ($missing.usageAvailable -or $null -ne $missing.inputTokens -or $missing.missingUsage -ne 1 -or $missing.invalidLines -ne 1) {
        throw 'Parser must report unavailable usage when provider fields are missing or JSONL is invalid.'
    }

    $mixedPath = Join-Path $root 'mixed.jsonl'
    @'
2026-01-01T00:00:00Z ERROR codex_core::tools::router: command blocked by sandbox
{"type":"turn.completed","usage":{"input_tokens":5,"cached_input_tokens":2,"output_tokens":1}}
'@ | Set-Content -LiteralPath $mixedPath -Encoding utf8

    $mixed = & (Join-Path $PSScriptRoot '..\scripts\parse-codex-usage.ps1') -JsonlPath $mixedPath | ConvertFrom-Json
    if (-not $mixed.usageAvailable -or $mixed.inputTokens -ne 5 -or $mixed.outputTokens -ne 1 -or $mixed.invalidLines -ne 1) {
        throw 'Parser must preserve provider usage while reporting unrelated non-JSON log lines.'
    }

    Write-Output 'Codex usage parser checks passed.'
} finally {
    $resolvedRoot = [System.IO.Path]::GetFullPath($root)
    if (-not $resolvedRoot.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to remove a usage test directory outside the system temp root.'
    }
    Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
}
