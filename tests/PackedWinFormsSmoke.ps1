[CmdletBinding()]
param([Parameter(Mandatory = $true)][string] $ToolPath)

$ErrorActionPreference = 'Stop'
$root = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-winforms-smoke-' + [guid]::NewGuid().ToString('N'))
try {
    $source = Join-Path $root 'src'
    New-Item -ItemType Directory -Path $source -Force | Out-Null
    @'
using System.Windows.Forms;
namespace Fixture;
public partial class MainForm : Form { private Label caption = new(); }
'@ | Set-Content -LiteralPath (Join-Path $source 'MainForm.cs') -Encoding utf8
    @'
namespace Fixture;
public partial class MainForm { void RefreshView() { Text = "fixture-only"; caption.Text = "cross-file"; } }
'@ | Set-Content -LiteralPath (Join-Path $source 'MainForm.Update.cs') -Encoding utf8
    @'
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup><TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion></PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $root 'LegacyApp.csproj') -Encoding utf8
    $json = & $ToolPath winforms analyze $root --format json --limit 10 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed WinForms analysis failed with exit code $LASTEXITCODE." }
    if (
        $json.SchemaVersion -ne 1 -or $json.CandidateCount -ne 2 -or $json.IndexedTypeCount -ne 1 -or
        $json.Findings.Count -ne 2 -or $json.Findings[0].Classification -ne 'Candidate'
    ) {
        throw 'Packed WinForms analysis did not return both expected cross-file source-only candidates.'
    }
    if ((Get-ChildItem -LiteralPath $root -Recurse -File).Count -ne 3) { throw 'Packed WinForms analysis wrote into its source fixture.' }
    Write-Output 'Packed WinForms source-only analysis smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
