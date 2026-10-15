[CmdletBinding()]
param([Parameter(Mandatory = $true)][string] $ToolPath)

$ErrorActionPreference = 'Stop'
$root = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-native-flow-smoke-' + [guid]::NewGuid().ToString('N'))
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

    $json = & $ToolPath evidence $root --format json --limit 20 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed native-flow evidence failed with exit code $LASTEXITCODE." }
    $nativeFlow = @($json.findings | Where-Object { $_.category -eq 'native-flow-candidate' })
    if ($nativeFlow.Count -ne 2) { throw "Expected 2 native-flow findings; received $($nativeFlow.Count)." }
    $dispatch = $nativeFlow | Where-Object { $_.relationKind -eq 'mfc-dispatch' }
    $call = $nativeFlow | Where-Object { $_.relationKind -eq 'qualified-native-call' }
    if ($dispatch.status -ne 'candidate' -or $dispatch.targetSymbol -ne 'COrderMfcView::OnImport') {
        throw 'Packed evidence did not link the MFC command to its handler as a candidate.'
    }
    if ($call.status -ne 'candidate' -or $call.sourceSymbol -ne 'COrderMfcView::OnImport' -or $call.targetSymbol -ne 'COrderManagedBridge::ImportSelectedOrders' -or $call.targetPath -ne 'legacy/Bridge/OrderManagedBridge.cpp' -or $null -eq $call.targetLine) {
        throw 'Packed evidence did not link the MFC handler to its inline C++/CLI method as a candidate.'
    }
    if (@(Get-ChildItem -LiteralPath $root -Recurse -File).Count -ne 2) {
        throw 'Packed native-flow analysis wrote into its source fixture.'
    }
    Write-Output 'Packed native-flow evidence smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
