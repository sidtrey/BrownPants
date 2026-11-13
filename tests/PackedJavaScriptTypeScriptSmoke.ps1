[CmdletBinding()]
param([Parameter(Mandatory = $true)][string] $ToolPath)

$ErrorActionPreference = 'Stop'
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar))
$root = Join-Path $tempRoot ('brownpants-js-ts-smoke-' + [guid]::NewGuid().ToString('N'))

function Test-NoReparsePoints {
    param([Parameter(Mandatory = $true)][string] $Directory)
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($Directory)
    while ($pending.Count -gt 0) {
        foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($pending.Pop())) {
            $item = Get-Item -LiteralPath $entry -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { return $false }
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
        }
    }
    return $true
}

try {
    $core = Join-Path $root 'packages/core/src'
    $app = Join-Path $root 'packages/app/src'
    New-Item -ItemType Directory -Path $core, $app -Force | Out-Null
    '{"private":true,"workspaces":["packages/*"]}' | Set-Content -LiteralPath (Join-Path $root 'package.json') -Encoding utf8
    '{"name":"@sample/core","main":"src/index.ts"}' | Set-Content -LiteralPath (Join-Path $root 'packages/core/package.json') -Encoding utf8
    @'
export function load<T>(value: T) { return value; }
'@ | Set-Content -LiteralPath (Join-Path $core 'index.ts') -Encoding utf8
    @'
import { load } from '@sample/core';
export const run = () => load('ready');
run();
'@ | Set-Content -LiteralPath (Join-Path $app 'main.ts') -Encoding utf8

    $json = & $ToolPath inspect $root --format json --limit 100 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed JS/TS inspection failed with exit code $LASTEXITCODE." }
    if ($json.SchemaVersion -ne 1 -or $json.JavaScriptTypeScript.ScannedFileCount -ne 2) {
        throw 'Packed JS/TS inspection returned an unexpected schema or source-file count.'
    }
    if (-not ($json.JavaScriptTypeScript.ModuleRelationships | Where-Object {
        $_.Specifier -eq '@sample/core' -and $_.Status -eq 'syntax-resolved' -and $_.TargetPath -eq 'packages/core/src/index.ts'
    })) { throw 'Packed JS/TS inspection did not resolve the declared workspace package.' }
    if (-not ($json.JavaScriptTypeScript.CallRelationships | Where-Object {
        $_.Expression -eq 'load' -and $_.Status -eq 'syntax-resolved' -and $_.TargetPath -eq 'packages/core/src/index.ts'
    })) { throw 'Packed JS/TS inspection did not report the imported generic call target.' }

    $markdown = & $ToolPath inspect $root --format markdown --limit 100
    if ($LASTEXITCODE -ne 0) { throw "Packed JS/TS Markdown inspection failed with exit code $LASTEXITCODE." }
    if ($markdown -notcontains '## JavaScript and TypeScript source evidence' -or
        $markdown -notcontains '- Analysis level: Bounded JavaScript/TypeScript syntax analysis') {
        throw 'Packed JS/TS Markdown omitted the evidence section or its analysis level.'
    }
    Write-Output 'Packed JavaScript/TypeScript workspace and report smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $root -PathType Container) {
        $resolved = [System.IO.Path]::GetFullPath($root)
        $prefix = $tempRoot + [System.IO.Path]::DirectorySeparatorChar + 'brownpants-js-ts-smoke-'
        $leaf = Split-Path -Leaf $resolved
        $parsedGuid = [guid]::Empty
        $validName = $leaf.StartsWith('brownpants-js-ts-smoke-', [StringComparison]::Ordinal) -and
            [guid]::TryParseExact($leaf.Substring('brownpants-js-ts-smoke-'.Length), 'N', [ref]$parsedGuid)
        if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or -not $validName) {
            throw "Refusing to remove unexpected JS/TS smoke path: $resolved"
        }
        if (-not (Test-NoReparsePoints -Directory $resolved)) { throw "Refusing to remove a JS/TS smoke fixture containing a reparse point: $resolved" }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
