// Analysis fixture only: this source is not built by the portable managed check.
using namespace LegacyOrderDesk::Domain;
using namespace LegacyOrderDesk::Workflows;

public ref class COrderManagedBridge
{
public:
    static void ImportSelectedOrders()
    {
        OrderImportWorkflow<Order^>^ workflow = gcnew OrderImportWorkflow<Order^>(nullptr);
        workflow->ImportBatch(gcnew System::Collections::Generic::List<Order^>());
    }
};
