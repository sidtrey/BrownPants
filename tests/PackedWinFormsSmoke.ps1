[CmdletBinding()]
param([Parameter(Mandatory = $true)][string] $ToolPath)

$ErrorActionPreference = 'Stop'
$root = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-winforms-smoke-' + [guid]::NewGuid().ToString('N'))
try {
    $source = Join-Path $root 'src'
    New-Item -ItemType Directory -Path $source -Force | Out-Null
    @'
using System.Windows.Forms;
class MainForm : Form { void RefreshView() { Text = "fixture-only"; } }
'@ | Set-Content -LiteralPath (Join-Path $source 'MainForm.cs') -Encoding utf8
    $json = & $ToolPath winforms analyze $root --format json --limit 10 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed WinForms analysis failed with exit code $LASTEXITCODE." }
    if ($json.SchemaVersion -ne 1 -or $json.CandidateCount -ne 1 -or $json.Findings[0].Classification -ne 'Candidate') {
        throw 'Packed WinForms analysis did not return the expected source-only candidate.'
    }
    if (Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object Name -ne 'MainForm.cs') { throw 'Packed WinForms analysis wrote into its source fixture.' }
    Write-Output 'Packed WinForms source-only analysis smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
