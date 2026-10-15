[CmdletBinding()]
param([Parameter(Mandatory = $true)][string] $ToolPath)

$ErrorActionPreference = 'Stop'
$root = Join-Path ([System.IO.Path]::GetTempPath()) ('brownpants-managed-interop-smoke-' + [guid]::NewGuid().ToString('N'))
try {
    $managed = Join-Path $root 'managed'
    $native = Join-Path $root 'legacy/Native'
    $bridge = Join-Path $root 'legacy/Bridge'
    New-Item -ItemType Directory -Path $managed -Force | Out-Null
    New-Item -ItemType Directory -Path $native -Force | Out-Null
    New-Item -ItemType Directory -Path $bridge -Force | Out-Null
    @'
namespace Demo.App;
public class Workflow
{
    public void Run() { }
    public void Save(int value) { }
    public void Save(string value) { }
}
'@ | Set-Content -LiteralPath (Join-Path $managed 'Workflow.cs') -Encoding utf8
    @'
Namespace Demo.App
    Public Class VisualBasicWorkflow
        Public Sub Execute()
        End Sub
    End Class
End Namespace
'@ | Set-Content -LiteralPath (Join-Path $managed 'VisualBasicWorkflow.vb') -Encoding utf8
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
using namespace Demo::App;
public ref class COrderManagedBridge
{
public:
    static void ImportSelectedOrders()
    {
        Workflow^ workflow = gcnew Workflow();
        workflow->Run();
        workflow->Save(1);
        VisualBasicWorkflow^ vbWorkflow = gcnew VisualBasicWorkflow();
        vbWorkflow->Execute();
        NativeOnly^ native = gcnew NativeOnly();
        native->Run();
        // workflow->Run();
        const char* decoy = "workflow->Run()";
    }
};
'@ | Set-Content -LiteralPath (Join-Path $bridge 'OrderManagedBridge.cpp') -Encoding utf8
    $sourceFiles = @(Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName)
    $sourceHashes = @($sourceFiles | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })

    $inventory = & $ToolPath inspect $root --format json --limit 100 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed interop inspection failed with exit code $LASTEXITCODE." }
    $relations = @($inventory.ArchitecturePatterns.NativeFlow.Relations | Where-Object { $_.Kind -eq 'cppcli-managed-call' })
    $run = $relations | Where-Object { $_.TargetSymbol -eq 'Demo.App.Workflow.Run' }
    if ($run.Status -ne 'candidate' -or $run.TargetPath -ne 'managed/Workflow.cs' -or $run.TargetLine -ne 4) {
        throw 'Packed inspect did not locate the unique C# managed target.'
    }
    if (-not ($relations | Where-Object { $_.TargetSymbol -eq 'Demo.App.VisualBasicWorkflow.Execute' -and $_.TargetPath -eq 'managed/VisualBasicWorkflow.vb' })) {
        throw 'Packed inspect did not locate the Visual Basic managed target.'
    }
    if (-not ($relations | Where-Object { $_.TargetSymbol -eq 'Demo.App.Workflow.Save' -and $_.Status -eq 'ambiguous' -and $_.CandidateTargets.Count -eq 2 })) {
        throw 'Packed inspect did not retain overloaded managed candidate locations.'
    }
    if ($relations | Where-Object { $_.TargetSymbol -match 'NativeOnly|Fake' }) {
        throw 'Packed inspect treated wrong-type, comment, or string decoys as managed calls.'
    }

    $impact = & $ToolPath impact $root --changed managed/Workflow.cs --format json --limit 100 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed managed impact failed with exit code $LASTEXITCODE." }
    $nativeImpact = @($impact.NativeImpacts)
    if (-not ($nativeImpact | Where-Object { $_.RelationshipKind -eq 'cppcli-managed-call' -and $_.Status -eq 'candidate' -and $_.Depth -eq 1 })) {
        throw 'Packed managed impact omitted the C++/CLI bridge call.'
    }
    if (-not ($nativeImpact | Where-Object { $_.RelationshipKind -eq 'qualified-native-call' -and $_.Depth -eq 2 })) {
        throw 'Packed managed impact omitted the native bridge caller.'
    }
    if (-not ($nativeImpact | Where-Object { $_.RelationshipKind -eq 'mfc-dispatch' -and $_.CommandId -eq 'ID_ORDER_IMPORT' -and $_.Depth -eq 3 })) {
        throw 'Packed managed impact did not trace to the MFC command map.'
    }

    $evidence = & $ToolPath evidence $root --changed managed/Workflow.cs --format json --limit 100 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Packed managed evidence focus failed with exit code $LASTEXITCODE." }
    $focused = @($evidence.findings | Where-Object { $_.category -eq 'native-flow-candidate' })
    if (-not ($focused | Where-Object { $_.relationKind -eq 'cppcli-managed-call' -and $_.matchedBy -contains 'changed-path:managed/Workflow.cs:native-caller-impact' })) {
        throw 'Packed focused evidence omitted the changed managed target bridge candidate.'
    }
    if (-not ($focused | Where-Object { $_.relationKind -eq 'mfc-dispatch' })) {
        throw 'Packed focused evidence omitted the upstream MFC command map.'
    }

    $finalFiles = @(Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName)
    $finalHashes = @($finalFiles | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
    if (Compare-Object -ReferenceObject $sourceHashes -DifferenceObject $finalHashes) {
        throw 'Packed managed interop analysis modified source files.'
    }
    if ($finalFiles.Count -ne $sourceFiles.Count) { throw 'Packed managed interop analysis wrote into its source fixture.' }
    Write-Output 'Packed managed-interop smoke test passed.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
