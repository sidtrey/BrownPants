[CmdletBinding()]
param([Parameter(Mandatory = $true)][string] $ToolPath)

$ErrorActionPreference = 'Stop'
$root = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-native-impact-smoke-' + [guid]::NewGuid().ToString('N'))
try {
    $native = Join-Path $root 'legacy/Native'
    $bridge = Join-Path $root 'legacy/Bridge'
    New-Item -ItemType Directory -Path $native -Force | Out-Null
    New-Item -ItemType Directory -Path $bridge -Force | Out-Null
    @'
BEGIN_MESSAGE_MAP(COrderMfcView, CFormView)
    ON_COMMAND(ID_ORDER_IMPORT, &COrderMfcView::OnImport)
END_MESSAGE_MAP()

void COrderMfcView::OnImport()
{
    COrderManagedBridge::ImportSelectedOrders();
}
'@ | Set-Content -LiteralPath (Join-Path $native 'OrderMfcView.cpp') -Encoding utf8
    @'
public ref class COrderManagedBridge
{
public:
    static void ImportSelectedOrders() {}
};
'@ | Set-Content -LiteralPath (Join-Path $bridge 'OrderManagedBridge.cpp') -Encoding utf8
    $sourceHashes = @(Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })

    $impact = & $ToolPath impact $root --changed legacy/Bridge/OrderManagedBridge.cpp --format json --limit 20 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed native-impact command failed with exit code $LASTEXITCODE." }
    if (-not $impact.IsComplete) { throw "Expected complete native-only fixture impact; issues: $($impact.Issues -join '; ')" }
    $findings = @($impact.NativeImpacts)
    if ($findings.Count -ne 2) { throw "Expected two native impact links; received $($findings.Count)." }
    $call = $findings | Where-Object { $_.RelationshipKind -eq 'qualified-native-call' }
    $dispatch = $findings | Where-Object { $_.RelationshipKind -eq 'mfc-dispatch' }
    if ($call.Status -ne 'candidate' -or $call.Depth -ne 1 -or $call.TargetSymbol -ne 'COrderManagedBridge::ImportSelectedOrders') {
        throw 'Packed impact did not report the changed bridge call as a depth-one candidate.'
    }
    if ($dispatch.Status -ne 'candidate' -or $dispatch.Depth -ne 2 -or $dispatch.CommandId -ne 'ID_ORDER_IMPORT') {
        throw 'Packed impact did not trace the bridge candidate back to the MFC command map.'
    }

    $evidence = & $ToolPath evidence $root --changed legacy/Bridge/OrderManagedBridge.cpp --format json --limit 20 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed native evidence focus failed with exit code $LASTEXITCODE." }
    if (-not $evidence.focus.impactComplete -or $evidence.focus.semanticImpactAnalyzed) {
        throw 'Packed evidence did not report native-only impact without managed semantic loading.'
    }
    $selected = @($evidence.findings | Where-Object { $_.category -eq 'native-flow-candidate' })
    if ($selected.Count -ne 2) { throw "Expected two focused native candidate findings; received $($selected.Count)." }
    $finalHashes = @(Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
    if (Compare-Object -ReferenceObject $sourceHashes -DifferenceObject $finalHashes) {
        throw 'Packed native impact modified source files in its fixture.'
    }
    if (@(Get-ChildItem -LiteralPath $root -Recurse -File).Count -ne 2) {
        throw 'Packed native impact wrote into its source fixture.'
    }
    Write-Output 'Packed native-impact smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
