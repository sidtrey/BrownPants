[CmdletBinding()]
param([Parameter(Mandatory = $true)][string] $ToolPath)

$ErrorActionPreference = 'Stop'
$root = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-impact-graph-smoke-' + [guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDefaultCompileItems>true</EnableDefaultCompileItems>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $root 'Fixture.csproj') -Encoding utf8
    @'
namespace Demo;
public class Changed
{
    public void Save() { }
}
'@ | Set-Content -LiteralPath (Join-Path $root 'AChanged.cs') -Encoding utf8
    @'
namespace Demo;
public class Noise { public void Ping() { } }
public class NoiseCaller { public void Run(Noise noise) => noise.Ping(); }
'@ | Set-Content -LiteralPath (Join-Path $root 'BUnrelatedCaller.cs') -Encoding utf8
    @'
namespace Demo;
public class ChangedCaller { public void Run(Changed changed) => changed.Save(); }
'@ | Set-Content -LiteralPath (Join-Path $root 'ZChangedCaller.cs') -Encoding utf8

    $inventory = & $ToolPath inspect $root --format json --semantic --limit 1 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed semantic inspection failed with exit code $LASTEXITCODE." }
    $semantic = $inventory.CSharp.Semantic
    if (@($semantic.ResolvedCalls).Count -ne 1) {
        throw "Expected one public resolved-call sample, got $(@($semantic.ResolvedCalls).Count)."
    }
    if ($semantic.ResolvedInvocationCount -le $semantic.ResolvedCalls.Count) {
        throw 'Fixture did not produce resolved calls beyond the public display sample.'
    }
    if ($semantic.PSObject.Properties.Name -contains 'ImpactGraphCalls' -or
        $semantic.PSObject.Properties.Name -contains 'ImpactGraphOmittedCount') {
        throw 'Internal impact graph details leaked into the public semantic report.'
    }

    $impact = & $ToolPath impact $root --changed AChanged.cs --format json --limit 1 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed impact command failed with exit code $LASTEXITCODE." }
    if (-not ($impact.Impacts | Where-Object { $_.Path -eq 'ZChangedCaller.cs' -and $_.Depth -eq 1 })) {
        throw 'Packed impact omitted the changed method caller beyond the one-edge semantic sample.'
    }
    if ($impact.Issues | Where-Object { $_ -like 'Resolved relationship edges were sampled:*' }) {
        throw 'Packed impact treated public display sampling as impact-graph truncation.'
    }
    Write-Output 'Packed impact-graph coverage smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
