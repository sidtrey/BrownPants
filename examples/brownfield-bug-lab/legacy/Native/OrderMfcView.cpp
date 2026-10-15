#include "pch.h"
#include "OrderMfcView.h"

IMPLEMENT_DYNCREATE(COrderMfcView, CFormView)
BEGIN_MESSAGE_MAP(COrderMfcView, CFormView)
    ON_COMMAND(ID_ORDER_IMPORT_BATCH, &COrderMfcView::OnImportBatch)
END_MESSAGE_MAP()

void COrderMfcView::OnImportBatch()
{
    COrderManagedBridge::ImportSelectedOrders();
}
