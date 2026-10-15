using LegacyOrderDesk.Domain;
using LegacyOrderDesk.Persistence;
using LegacyOrderDesk.Workflows;

internal static class Program
{
    private static void Main()
    {
        var repository = new OrderRepository<Order>();
        var workflow = new OrderImportWorkflow<Order>(repository);
        workflow.ImportOne(new Order(1, false));
        if (repository.Saved.Count != 1)
            throw new InvalidOperationException("Single-item overload did not save exactly one order.");

        var batch = new[] { new Order(2, true), new Order(3, false) };
        workflow.ImportBatch(batch);
        if (repository.Saved.Count != 3)
            throw new InvalidOperationException($"Batch import saved {repository.Saved.Count - 1} of 2 orders.");

        var mappedRepository = new OrderRepository<Order>();
        var mappedWorkflow = new OrderImportWorkflow<Order>(mappedRepository);
        var rows = new[] { new ImportedRow(4, true), new ImportedRow(5, false) };
        mappedWorkflow.ImportMapped(rows, row => new Order(row.SourceId, row.RequiresRetry));
        if (mappedRepository.Saved.Count != 2)
            throw new InvalidOperationException("Generic mapped import did not preserve every source row.");

        Console.WriteLine("LegacyOrderDesk checks passed.");
    }
}
